using SecsGem.Core.HSMS;
using SecsGem.Core.Interfaces;
using SecsGem.Core.Models;
using SecsGem.Core.SecIIMessage;

namespace SecsGem.Core.Tests.HsmsTestCases
{
    /// <summary>
    /// Tests for <see cref="HsmsSession"/> — the HSMS control state machine and the data-message
    /// parity split. Data-message *content* now lives behind <see cref="IDataMessageHandler"/>, so
    /// these tests verify the session's own responsibilities: the select / linktest / deselect
    /// handshake, the reject path, and that primaries are delegated to the handler while secondaries
    /// are passed straight through. A fake handler stands in for the real GEM logic.
    /// _conn is only touched on Separate_req, so a null connection is fine for these cases.
    /// </summary>
    [TestClass]
    public sealed class HsmsSessionTests
    {
        // Test double: records what it was asked to handle and returns a canned reply.
        private sealed class FakeHandler : IDataMessageHandler
        {
            public HsmsMessage? LastMessage;
            public int CallCount;
            public SecsMessage Reply = new StubMessage();
            public SecsMessage Handle(HsmsMessage message)
            {
                LastMessage = message;
                CallCount++;
                return Reply;
            }
        }

        // A minimal SecsMessage so delegation tests don't depend on any real GEM message shape.
        private sealed class StubMessage : SecsMessage
        {
            public override byte Stream => 3;
            public override byte Function => 4;
            public override bool Waitbit => false;
            public override SecsItem? Payload => new AsciiItem("HANDLED");
        }

        private static HsmsSession NewSession(IDataMessageHandler? handler = null)
            => new HsmsSession(null!, handler ?? new FakeHandler(), deviceId: 1);

        // Strip the 4-byte length prefix and decode the HSMS header of a reply.
        private static HsmsMessage DecodeReply(byte[] framed)
            => new HsmsDecoder(framed.Skip(4).ToArray()).Decode();

        // --- Control handshake ---

        [TestMethod]
        public void InitialState_IsNotConnected()
        {
            Assert.AreEqual(SessionState.NotConnected, NewSession().CurrentSessionState);
        }

        [TestMethod]
        public void SelectReq_ReturnsSelectRsp_AndEntersSelected()
        {
            var session = NewSession();
            var request = new HsmsMessage { DeviceId = 255, SType = SType.Select_req, SystemBytes = 4000 };

            var bytes = session.ProcessMessage(request, false, out var response);

            Assert.AreEqual(SType.Select_rsp, response.SType);
            Assert.AreEqual(SessionState.Selected, session.CurrentSessionState);

            // 4-byte length prefix + 10-byte header, no payload.
            Assert.AreEqual(14, bytes.Length);
            var decoded = DecodeReply(bytes);
            Assert.AreEqual(SType.Select_rsp, decoded.SType);
            Assert.AreEqual((ushort)255, decoded.DeviceId);
            Assert.AreEqual((uint)4000, decoded.SystemBytes);
        }

        [TestMethod]
        public void LinktestReq_ReturnsLinktestRsp_AndKeepsState()
        {
            var session = NewSession();
            session.ProcessMessage(new HsmsMessage { SType = SType.Select_req }, false, out _); // reach Selected

            var bytes = session.ProcessMessage(
                new HsmsMessage { SType = SType.Linktest_req, SystemBytes = 7 }, false, out var response);

            Assert.AreEqual(SType.Linktest_rsp, response.SType);
            Assert.AreEqual(SessionState.Selected, session.CurrentSessionState);
            Assert.AreEqual(SType.Linktest_rsp, DecodeReply(bytes).SType);
        }

        [TestMethod]
        public void DeselectReq_ReturnsDeselectRsp_AndEntersSeparated()
        {
            var session = NewSession();

            var bytes = session.ProcessMessage(
                new HsmsMessage { SType = SType.Deselect_req }, false, out var response);

            Assert.AreEqual(SType.Deselect_rsp, response.SType);
            Assert.AreEqual(SessionState.Seperated, session.CurrentSessionState);
            Assert.AreEqual(SType.Deselect_rsp, DecodeReply(bytes).SType);
        }

        [TestMethod]
        public void SelectRsp_EntersSelected_WithNoReplyBytes()
        {
            var session = NewSession();

            var bytes = session.ProcessMessage(
                new HsmsMessage { SType = SType.Select_rsp }, false, out _);

            Assert.AreEqual(0, bytes.Length);
            Assert.AreEqual(SessionState.Selected, session.CurrentSessionState);
        }

        [TestMethod]
        public void LinktestRsp_ProducesNoReply_AndKeepsState()
        {
            var session = NewSession();
            session.ProcessMessage(new HsmsMessage { SType = SType.Select_req }, false, out _); // reach Selected

            var bytes = session.ProcessMessage(
                new HsmsMessage { SType = SType.Linktest_rsp }, false, out _);

            Assert.AreEqual(0, bytes.Length);
            Assert.AreEqual(SessionState.Selected, session.CurrentSessionState);
        }

        [TestMethod]
        public void DeselectRsp_EntersSeparated_WithNoReplyBytes()
        {
            var session = NewSession();

            var bytes = session.ProcessMessage(
                new HsmsMessage { SType = SType.Deselect_rsp }, false, out _);

            Assert.AreEqual(0, bytes.Length);
            Assert.AreEqual(SessionState.Seperated, session.CurrentSessionState);
        }

        // --- Reject path ---

        [TestMethod]
        public void ErrorFlag_OnControlMessage_ReturnsReject_AndLeavesStateUnchanged()
        {
            var session = NewSession();
            var request = new HsmsMessage { DeviceId = 1, SType = SType.Select_req, SystemBytes = 9 };

            var bytes = session.ProcessMessage(request, encounteredError: true, out var response);

            Assert.AreEqual(SType.Reject_req, response.SType);
            Assert.AreEqual(SessionState.NotConnected, session.CurrentSessionState); // error path skips the switch
            Assert.AreEqual(SType.Reject_req, DecodeReply(bytes).SType);
        }

        [TestMethod]
        public void ErrorFlag_OnDataMessage_ReturnsReject()
        {
            // Regression guard for the reject-crash fix: a Data message rejected via the error flag
            // must produce a valid Reject_req reply. The response no longer inherits Stream/Function,
            // so HsmsEncoder can encode it (previously this threw InvalidOperationException).
            var handler = new FakeHandler();
            var session = NewSession(handler);
            var dataMessage = new HsmsMessage
            {
                DeviceId = 1,
                SType = SType.Data,
                Stream = 1,
                Function = 1,
                SystemBytes = 42
            };

            var bytes = session.ProcessMessage(dataMessage, encounteredError: true, out var response);

            Assert.AreEqual(0, handler.CallCount, "The error path must not reach the handler.");
            Assert.AreEqual(SType.Reject_req, response.SType);
            Assert.IsNull(response.Stream, "A Reject_req reply must not carry a Stream.");
            Assert.IsNull(response.Function, "A Reject_req reply must not carry a Function.");
            Assert.AreEqual(SessionState.NotConnected, session.CurrentSessionState); // error path skips the switch

            var decoded = DecodeReply(bytes);
            Assert.AreEqual(SType.Reject_req, decoded.SType);
            Assert.AreEqual((uint)42, decoded.SystemBytes);
        }

        // --- Data parity split: primary -> handler, secondary -> pass through ---

        [TestMethod]
        public void DataPrimary_IsDelegatedToHandler_AndItsReplyIsFramed()
        {
            // An incoming primary (odd function) must be handed to the message handler, and the
            // handler's returned SecsMessage framed into the reply (echoing the request's SystemBytes).
            var handler = new FakeHandler();               // returns StubMessage: S3F4, payload "HANDLED"
            var session = NewSession(handler);
            var primary = new HsmsMessage
            {
                DeviceId = 1,
                SType = SType.Data,
                Stream = 1,
                Function = 1,       // odd => primary
                Waitbit = true,
                SystemBytes = 4242
            };

            var bytes = session.ProcessMessage(primary, false, out var response);

            Assert.AreEqual(1, handler.CallCount, "A primary must be delegated to the handler exactly once.");
            Assert.AreSame(primary, handler.LastMessage);

            Assert.AreEqual(SType.Data, response.SType);
            Assert.AreEqual((byte)3, response.Stream);      // from StubMessage
            Assert.AreEqual((byte)4, response.Function);
            Assert.IsInstanceOfType(response.Payload, typeof(AsciiItem));

            Assert.IsTrue(bytes.Length > 0, "A primary must produce a reply on the wire.");
            var decoded = DecodeReply(bytes);
            Assert.AreEqual((byte)3, decoded.Stream);
            Assert.AreEqual((byte)4, decoded.Function);
            Assert.AreEqual((uint)4242, decoded.SystemBytes, "The reply must echo the request's SystemBytes.");
        }

        [TestMethod]
        public void DataSecondary_IsPassedThroughIntact_HandlerNotCalled()
        {
            // An incoming secondary (even function) is a reply to something WE sent. It must be
            // handed back intact via the out-response, NOT sent to the handler, and NOTHING replied.
            var handler = new FakeHandler();
            var session = NewSession(handler);
            var payload = new AsciiItem("REPLY-BODY");
            var secondary = new HsmsMessage
            {
                DeviceId = 1,
                SType = SType.Data,
                Stream = 1,
                Function = 2,       // even => secondary/reply
                Waitbit = false,
                SystemBytes = 55,
                Payload = payload
            };

            var bytes = session.ProcessMessage(secondary, false, out var response);

            Assert.AreEqual(0, handler.CallCount, "A reply (even function) must not go to the handler.");
            Assert.AreEqual(0, bytes.Length, "A reply must not be answered on the wire.");
            Assert.AreEqual(SType.Data, response.SType);
            Assert.AreEqual((byte)1, response.Stream);
            Assert.AreEqual((byte)2, response.Function);
            Assert.AreSame(payload, response.Payload, "The incoming reply's payload must be preserved for the caller.");
            Assert.AreEqual((uint)55, response.SystemBytes);
        }
    }
}
