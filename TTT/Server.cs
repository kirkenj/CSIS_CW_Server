using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace CSIS_CW_Server
{
    class Server
    {
        private const string IP_ADRESS_STR = "127.0.0.1";
        private const int PORT = 11000;
        private readonly TcpListener _listener;
        private readonly Thread _thread;
        private readonly List<Connection> _connections;
        public Server()
        {
            _connections = new List<Connection>();
            _listener = new TcpListener(IPAddress.Parse(IP_ADRESS_STR), PORT);
            _thread = new Thread(WaitForConnections);
        }
        public void Start()
        {
            _thread.Start();
        }
        private void WaitForConnections()
        {
            _listener.Start();
            Connection bCon;
            Console.WriteLine("Жду соединения...");
            while (true)
            {
                Thread.Sleep(15);
                if (_listener.Pending())
                {
                    bCon = new Connection(_listener.AcceptTcpClient(), this);
                    _connections.Add(bCon);
                    bCon.Start();

                }
            }
        }
        public void ConnectionLost(Connection connection)
        {
            if (_connections.Contains(connection))
            {
                Console.WriteLine($"Соединение разорвано: {connection.EndPoint}");
                _connections.Remove(connection);
                PrintConnections();
            }
        }
        private void PrintConnections()
        {
            if (_connections.Count == 0)
            {
                Console.WriteLine("Нет соединений");
            }
            else
            {
                Console.WriteLine("Текущие соединения:");
                foreach (var el in _connections)
                {
                    Console.WriteLine(el.EndPoint);
                }
                Console.WriteLine("--------------------------------");
            }
        }
    }
}
