using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Drawing;

//ManualResetEvent

namespace TTT
{
    class Connection
    {
        private readonly TcpClient _tcpClient;
        private Thread _bThread;
        private Thread _listenThread;
        private readonly Server _server;
        private Size _screenSize;
        private int _currentLevel = -1;
        private const int CLIENT_PERIOD = 15;
        private const int MINUTE_MILISEC = 60000;
        private int _currentLevelTime = 0;
        private int _prevLevelTime = 0;
        private int _clickedCircleCounter = 0;
        private int _fullCircleCounter = -1;

        private readonly int[,] _levelCircleConfig = new int[3, 2]
        {
            { 60, 1500},
            { 50, 1000},
            { 40, 500}
        };

        private Point _currentClickPoint = new Point();
        private bool _clickHandled = true;

        private int _currentCircleLifetime = 0;
        private readonly ServerCircle _currentCircle = new ServerCircle();
        private bool _currentCircleSent = true;
        private bool _currentCircleKilled = false;

        private readonly Random _random = new Random();

        public Connection(TcpClient client, Server server)
        {
            _server = server;
            _tcpClient = client;
            EndPoint = client.Client.RemoteEndPoint.ToString();
        }
        public string EndPoint { get; private set; }
        public TcpClient TcpClient { get; private set; }

        public void Start()
        {
            _listenThread = new Thread(Listen);
            _bThread = new Thread((x) =>
            {
                try
                {
                    TcpClient tcpClient = x as TcpClient;
                    Console.WriteLine($"Got connection {tcpClient.Client.RemoteEndPoint}\n");
                    while (tcpClient.Connected)
                    {
                        Thread.Sleep(CLIENT_PERIOD);
                        
                        if (_currentLevel >= 0)
                        {
                            HandleGame();
                            if (!_currentCircleSent)
                            {
                                _currentCircleSent = true;
                                SendStr(ServerCommands.TransportPoint + ": " + _currentCircle.ToString());
                            }
                        }
                    }
                }
                catch (SocketException SE)
                {
                    if(SE.Message  == "Удаленный хост принудительно разорвал существующее подключение.")
                    {
                        _server.ConnectionLost(this);
                    }
                }

            });
            _bThread.Start(_tcpClient);
            _listenThread.Start();
        }

        private void HandleGame()
        {
            if(_currentCircleLifetime >= _levelCircleConfig[_currentLevel,1])
            {
                _currentCircleKilled = true;
            }
            if (!_clickHandled)
            {
                if (_currentCircle.InterrectsWithPoint(_currentClickPoint))
                {
                    _currentCircleKilled = true;
                    _clickedCircleCounter++;
                }
                _clickHandled = true;
            }

            if (_currentCircleKilled)
            {
                _currentCircle.X = _random.Next(2 * _levelCircleConfig[_currentLevel, 0], _screenSize.Width - 2 * _levelCircleConfig[_currentLevel, 0]);
                _currentCircle.Y = _random.Next(2 * _levelCircleConfig[_currentLevel, 0], _screenSize.Height - 2 * _levelCircleConfig[_currentLevel, 0]);
                _currentCircle.LifeTime = _levelCircleConfig[_currentLevel, 1];
                _currentCircle.RadiusPix = _levelCircleConfig[_currentLevel, 0];
                _currentCircleSent = false;
                _currentCircleKilled = false;
                _currentCircleLifetime = 0;
                _fullCircleCounter++;
            }
            if (_currentLevelTime >= MINUTE_MILISEC)
            {
                _currentLevel = -1;
                SendStr(ServerCommands.Stop.ToString() + $" You_have_striked_{_clickedCircleCounter}/{_fullCircleCounter}_circles_for_{_prevLevelTime/1000}_sec.");
            }
            _prevLevelTime += CLIENT_PERIOD;
            _currentLevelTime += CLIENT_PERIOD;
            _currentCircleLifetime += CLIENT_PERIOD;
        }


        private void Listen()
        {
            try
            {
                byte[] recArr;
                while (true) 
                { 
                    recArr = new byte[1024];
                    int recLen = _tcpClient.Client.Receive(recArr);
                    if (recLen > 0)
                    {
                        string incStr = Encoding.ASCII.GetString(recArr);
                        Console.WriteLine("Got msg: " +'"'+ incStr +'"'+ $" from {_tcpClient.Client.RemoteEndPoint}");
                        HandleMsg(incStr);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        private void HandleMsg(string msg)
        {
            string[] words = msg.Split();
            string[] buff;
            if (words[0] == ServerCommands.ClickTransport.ToString() + ':')
            {
                buff = words[1].Split(',');
                buff[0] = buff[0][1..];
                buff[1] = buff[^1].Substring(0, buff[1].IndexOf('}'));
                _currentClickPoint.X = int.Parse(buff[0]);
                _currentClickPoint.Y = int.Parse(buff[1]);
                _clickHandled = false;
                SendStr("Recieved click: " + '{' + $"{_currentClickPoint.X},{_currentClickPoint.Y}" + '}');
            }
            else if (words[0] == ServerCommands.InitData.ToString() + ':')
            {
                buff = words[1].Split(',');
                buff[0] = buff[0][1..];
                buff[1] = buff[^1].Substring(0, buff[1].IndexOf('}'));
                _screenSize = new Size(int.Parse(buff[0]), int.Parse(buff[1]));
                SendStr("Size set: " + _screenSize.ToString());
            }
            else if (words[0] == ServerCommands.StartLevel1.ToString())
            {
                _currentLevelTime = 0;
                _clickedCircleCounter = 0;
                _fullCircleCounter = 0;
                _currentLevel = 0;
                SendStr($"{ServerCommands.StartLevel1}");
            }
            else if (words[0] == ServerCommands.StartLevel2.ToString())
            {
                _fullCircleCounter = 0;
                _clickedCircleCounter = 0;
                _currentLevelTime = 0;
                _currentLevel = 1;
                SendStr($"{ServerCommands.StartLevel2}");
            }
            else if (words[0] == ServerCommands.StartLevel3.ToString())
            {
                _fullCircleCounter = 0;
                _clickedCircleCounter = 0;
                _currentLevelTime = 0;
                _currentLevel = 2;
                SendStr($"{ServerCommands.StartLevel3}");
            }
            else if (words[0] == ServerCommands.Stop.ToString())
            {
                _currentLevelTime = MINUTE_MILISEC + 1;
            }
            else if (words[0] == ServerCommands.GameMessage.ToString() + ':')
            {
                SendStr(ServerCommands.GameMessage + ": "+ words[1]);
            }
        }

        private void SendStr(string str)
        {
            byte[] sendByte;
            sendByte = Encoding.ASCII.GetBytes(str);
            _tcpClient.Client.Send(sendByte, sendByte.Length, 0);
            Console.WriteLine("Sent msg: " + '"' + str + '"' + $" to {_tcpClient.Client.RemoteEndPoint}");
        }

    }
}
