using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Collections.Concurrent;
using SecsGem.Core.HSMS;
using SecsGem.Core.Events;
using SecsGem.Core.Models;
using SecsGem.Core.Equipment;
using SecsGem.Core.Interfaces;


namespace SecsGem.Core.Transport
{
    public class TcpServer
    {
        private readonly TcpListener _tcpListener;
        private readonly Equipment.Equipment _equipment;
        private readonly CancellationToken _token;
        private readonly IDataMessageHandler _messageHandler;
        private readonly List<Connection> ActiveConnections = new List<Connection>();
        private readonly object _lockObject = new object();

        public TcpServer(IPAddress ipAddress, int port, Equipment.Equipment equipment, IDataMessageHandler mesageHandler, CancellationToken token)
        {
            _tcpListener = new TcpListener(ipAddress, port);
            _tcpListener.Server.LingerState = new LingerOption(true, 30);
            _tcpListener.Server.ReceiveTimeout = 300;
            _tcpListener.Server.SendTimeout = 300;
            _equipment = equipment;
            _token = token;
            _messageHandler = mesageHandler;
        }

        public void Start()
        {
            try
            {
                _tcpListener.Start();
                Console.WriteLine("Server Started!");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server failed to start. Exception : {ex.Message}");
                throw;
            }
        }

        public void Stop()
        {
            try
            {
                _tcpListener.Stop();
                _tcpListener.Dispose();
                Console.WriteLine("Server Stopped!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server failed to stop. Exception : {ex.Message}");
                throw;
            }
        }

        public async Task AcceptLoopAsync()
        {
            while (!_token.IsCancellationRequested) 
            {
                try
                {
                    var client = await _tcpListener.AcceptTcpClientAsync(_token);

                    var conn = new Connection(client, _token);
                    var session = new HsmsSession(conn, _messageHandler, _equipment.Identity.DeviceId);
                    lock (_lockObject)
                        ActiveConnections.Add(conn);
                    
                    conn.DataReceived += Subscribe.OnDataReceived!;
                    conn.Disconnected += OnDisconnection!;
                    conn.PacketAssembled += Subscribe.OnPacketAssemble!;
                    conn.RequestRecieved += Subscribe.OnHsmsRequestRecieved!;
                    conn.EquipmentConnectionStateReceived += Subscribe.OnEquipmentStateChange!;

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await conn.ReadDataFromBuffer(session);
                        }
                        catch (Exception ex)
                        {
                            conn.Dispose();
                            Console.WriteLine($"Error in reading data from buffer. Exception : {ex.Message}");
                        }
                    });
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Issue detected : {ex.Message}");
                }
            } 
        }
        public void OnDisconnection(object eventSender, EventArgs e)
        {
            if (e != null)
            {
                lock (_lockObject)
                {
                    var connToDispose = (Connection)eventSender;
                    if (connToDispose != null)
                    {
                        ActiveConnections.Remove(connToDispose);
                        connToDispose.Dispose();
                        Console.WriteLine($"Connection to {connToDispose.TcpClient.Client.RemoteEndPoint} closed and disposed!");
                    }
                }
            }
        }
    }
}
