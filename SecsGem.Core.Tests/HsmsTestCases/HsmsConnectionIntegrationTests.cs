using EquipmentSimulator.GemEquipment;
using HostSimulator;
using SecsGem.Core.HSMS;
using SecsGem.Core.Interfaces;
using SecsGem.Core.Models;
using SecsGem.Core.SecIIMessage;
using SecsGem.Core.Transport;
using System.Net;
using System.Net.Sockets;

namespace SecsGem.Core.Tests.HsmsTestCases
{
    /// <summary>
    /// End-to-end tests that wire the real <see cref="TcpServer"/> (the equipment) to the real
    /// <see cref="HostTcpConnection"/> (the host) over a loopback socket, and drive the HSMS
    /// handshake and a full SECS transaction through them.
    /// </summary>
    [TestClass]
    public sealed class HsmsConnectionIntegrationTests
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

        // The host only receives replies (secondaries) in these tests, so its handler is never
        // invoked; a benign stand-in keeps the constructor happy.
        private sealed class NoopHandler : IDataMessageHandler
        {
            public SecsMessage Handle(HsmsMessage message) => new S9F5();
        }

        private static SecsGem.Core.Equipment.Equipment NewEquipment() =>
            new SecsGem.Core.Equipment.Equipment
            {
                Identity = new SecsGem.Core.Equipment.Identity
                {
                    DeviceId = 1,
                    ModelName = "EQP-1",
                    SoftwareRevision = "2.0"
                }
            };

        private static int GetFreePort()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            return port;
        }

        private static byte[] Frame(SType sType, uint systemBytes)
            => new HsmsEncoder(new HsmsMessage { DeviceId = 1, SType = sType, SystemBytes = systemBytes }).Encode();

        [TestMethod]
        [TestCategory("Integration")]
        public async Task SelectReq_ViaSimulators_HostBecomesSelected()
        {
            var port = GetFreePort();
            using var cts = new CancellationTokenSource();

            // Equipment side — the real server.
            var equipment = NewEquipment();
            var server = new TcpServer(IPAddress.Loopback, port, equipment, new EquipmentMessageHandler(equipment), cts.Token);
            server.Start();
            var acceptLoop = Task.Run(() => server.AcceptLoopAsync());

            // Host side — the real host connection.
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port);
            var hostConn = await new HostTcpConnection(client, deviceId: 1, new NoopHandler(), cts.Token).ConnectAsync();

            try
            {
                // The host raises a host-state-change event when it processes the equipment's Select.rsp.
                var selected = new TaskCompletionSource();
                hostConn.HostConnectionStateReceived += (_, e) =>
                {
                    if (e.SessionState == SessionState.Selected) selected.TrySetResult();
                };

                await hostConn.WriteDataToBufferAsync(Frame(SType.Select_req, 7));

                // Completes if the round trip (Select.req -> equipment -> Select.rsp -> host) works.
                await selected.Task.WaitAsync(Timeout);
            }
            finally
            {
                cts.Cancel();
                server.Stop();
                await SwallowAsync(acceptLoop);
            }
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task SeparateReq_ViaSimulators_TearsDownConnection()
        {
            var port = GetFreePort();
            using var cts = new CancellationTokenSource();

            var equipment = NewEquipment();
            var server = new TcpServer(IPAddress.Loopback, port, equipment, new EquipmentMessageHandler(equipment), cts.Token);
            server.Start();
            var acceptLoop = Task.Run(() => server.AcceptLoopAsync());

            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port);
            var hostConn = await new HostTcpConnection(client, deviceId: 1, new NoopHandler(), cts.Token).ConnectAsync();

            try
            {
                var selected = new TaskCompletionSource();
                var disconnected = new TaskCompletionSource();
                hostConn.HostConnectionStateReceived += (_, e) =>
                {
                    if (e.SessionState == SessionState.Selected) selected.TrySetResult();
                };
                hostConn.Disconnected += (_, _) => disconnected.TrySetResult();

                // 1. Establish the connection.
                await hostConn.WriteDataToBufferAsync(Frame(SType.Select_req, 100));
                await selected.Task.WaitAsync(Timeout);

                // 2. Separate — the equipment disposes its side, which closes the socket. The host's
                //    read loop then hits a 0-byte read and raises its Disconnected event.
                await hostConn.WriteDataToBufferAsync(Frame(SType.Separate_req, 101));
                await disconnected.Task.WaitAsync(Timeout);
            }
            finally
            {
                cts.Cancel();
                server.Stop();
                await SwallowAsync(acceptLoop);
            }
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task S1F13_ViaSimulators_HostReceivesS1F14()
        {
            var port = GetFreePort();
            using var cts = new CancellationTokenSource();

            var equipment = NewEquipment();
            var server = new TcpServer(IPAddress.Loopback, port, equipment, new EquipmentMessageHandler(equipment), cts.Token);
            server.Start();
            var acceptLoop = Task.Run(() => server.AcceptLoopAsync());

            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port);
            var hostTcp = new HostTcpConnection(client, deviceId: 1, new NoopHandler(), cts.Token);
            await hostTcp.ConnectAsync();
            var session = hostTcp.Session;

            try
            {
                // SendAsync selects the session, sends S1F13, and returns the equipment's decoded reply.
                var reply = await session.SendAsync(
                    new S1F13(new SecsGem.Core.Host.Identity { ModelName = "HOST", SoftwareRevision = "9.9" }))
                    .WaitAsync(Timeout);

                Assert.AreEqual(SType.Data, reply.SType);
                Assert.AreEqual((byte)1, reply.Stream);
                Assert.AreEqual((byte)14, reply.Function);   // S1F14

                // Body decoded off the wire: L,2 { <B COMMACK=0>, L,2 { MDLN, SOFTREV } }
                var outer = reply.Payload as ListItem;
                Assert.IsNotNull(outer, "S1F14 payload must be a list.");
                Assert.AreEqual(2, outer!.Value.Count);

                var commack = outer.Value[0] as BinaryItem;
                Assert.IsNotNull(commack, "First element must be COMMACK (binary).");
                CollectionAssert.AreEqual(new byte[] { 0 }, commack!.Value.ToArray()); // 0 = Accepted

                // Inner list is the EQUIPMENT's identity, not the host's.
                var inner = outer.Value[1] as ListItem;
                Assert.IsNotNull(inner, "Second element must be the L,2 {MDLN, SOFTREV} list.");
                Assert.AreEqual(2, inner!.Value.Count);
                Assert.AreEqual("EQP-1", ((AsciiItem)inner.Value[0]).Value);
                Assert.AreEqual("2.0", ((AsciiItem)inner.Value[1]).Value);
            }
            finally
            {
                cts.Cancel();
                server.Stop();
                await SwallowAsync(acceptLoop);
            }
        }

        // Await a background task during teardown without letting its expected disposal/cancellation
        // exceptions fail the test.
        private static async Task SwallowAsync(Task task)
        {
            try { await task.WaitAsync(Timeout); }
            catch { /* accept-loop cancellation / socket disposal on shutdown */ }
        }
    }
}
