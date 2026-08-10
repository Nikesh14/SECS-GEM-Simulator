using SecsGem.Core.HSMS;
using SecsGem.Core.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using SecsGem.Core.Events;
using SecsGem.Core.Interfaces;

namespace HostSimulator
{
    public class HostTcpConnection
    {
        private readonly TcpClient _client;
        private readonly CancellationToken _token;
        private readonly ushort _deviceId;
        private readonly Connection _conn;
        private readonly HsmsSession _session;
        private readonly List<Connection> ActiveConnections = new List<Connection>();



        public HostTcpConnection(TcpClient client, ushort deviceId, IDataMessageHandler messageHandler,CancellationToken token)
        {
            _client = client;
            _token = token;
            _deviceId = deviceId;
            _conn = new Connection(_client, _token);
            _session = new HsmsSession(_conn, messageHandler, _deviceId);
        }
        public HsmsSession Session => _session;
        public async Task<Connection> ConnectAsync()
        {
            _conn.DataReceived += Subscribe.OnDataReceived!;
            _conn.PacketAssembled += Subscribe.OnPacketAssemble!;
            
            _conn.HostConnectionStateReceived += Subscribe.OnHostStateChange!;

            _ = Task.Run(async () =>
            {
                try
                {
                    await _conn.ReadDataFromBuffer(Session!);
                    
                }
                catch (Exception ex)
                {
                    _conn.Dispose();
                    Console.WriteLine($"Error in reading data from buffer. Exception : {ex.Message}");
                }
            });
            return _conn;
        }
       
    }
}
