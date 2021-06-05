using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TTT
{
    class Server
    {
        private const string IP_ADRESS_STR = "127.0.0.1";
        private const int PORT = 11000;
        private readonly TcpListener listener;
        private readonly Thread thread;
        private readonly List<Connection> _connections;

        public Server()
        {
            _connections = new List<Connection>();
            listener = new TcpListener(IPAddress.Parse(IP_ADRESS_STR),PORT);
            thread = new Thread(Listen);
        }

        public void Start()
        {
            thread.Start();
        }

        private void Listen()
        {
            listener.Start();
            Connection bCon;
            while (true)
            {
                Thread.Sleep(15);
                if (listener.Pending())
                {
                    bCon = new Connection(listener.AcceptTcpClient(),this);
                    _connections.Add(bCon);
                    bCon.Start();
                }
            }
        }

        private void SendStr(TcpClient tcpClient, string str)
        {
            byte[] sendByte;
            sendByte = Encoding.ASCII.GetBytes(str);
            Console.WriteLine("Sent msg - " + str + $" to {tcpClient.Client.RemoteEndPoint}");
            _ = tcpClient.Client.Send(sendByte, sendByte.Length, 0);
        }

        public void ConnectionLost(Connection connection)
        {
            if (_connections.Contains(connection))
            {
                Console.WriteLine($"Connection removed: {connection.EndPoint}");
                _connections.Remove(connection);
                PrintConnections();
            }
        }
        private void PrintConnections()
        {
            if(_connections.Count == 0)
            {
                Console.WriteLine("No connections");
            }
            else
            {
                Console.WriteLine("Current connections:");
                foreach (var el in _connections)
                {
                    Console.WriteLine(el.EndPoint);
                }
                Console.WriteLine("--------------------------------");
            }
        }
    }
}
