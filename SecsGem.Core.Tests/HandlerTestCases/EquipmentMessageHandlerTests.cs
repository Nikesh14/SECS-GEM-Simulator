using EquipmentSimulator.GemEquipment;
using SecsGem.Core.Equipment;
using SecsGem.Core.Interfaces;
using SecsGem.Core.Models;
using SecsGem.Core.SecIIMessage;

namespace SecsGem.Core.Tests.HandlerTestCases
{
    /// <summary>
    /// Tests for <see cref="EquipmentMessageHandler"/> — the equipment-side GEM logic that decides
    /// which SECS reply a primary maps to. This is where message *content* lives now that
    /// <see cref="HsmsSession"/> only handles transport/parity.
    /// </summary>
    [TestClass]
    public sealed class EquipmentMessageHandlerTests
    {
        private static SecsGem.Core.Equipment.Equipment NewEquipment() =>
            new SecsGem.Core.Equipment.Equipment
            {
                Identity = new Identity
                {
                    DeviceId = 1,
                    ModelName = "MODEL-X",
                    SoftwareRevision = "1.0.0",
                    Manufacturer = "ACME",
                    SerialNumber = "SN-1"
                }
            };

        private static IDataMessageHandler NewHandler() => new EquipmentMessageHandler(NewEquipment());

        private static ListItem ValidS1F13Body() =>
            new ListItem(new List<SecsItem> { new AsciiItem("HOST"), new AsciiItem("9.9") });

        private static HsmsMessage Data(byte stream, byte function, bool wbit = true,
            uint systemBytes = 1, SecsItem? payload = null) =>
            new HsmsMessage
            {
                DeviceId = 1,
                SType = SType.Data,
                Stream = stream,
                Function = function,
                Waitbit = wbit,
                SystemBytes = systemBytes,
                Payload = payload
            };

        [TestMethod]
        public void S1F1_NoPayload_ReturnsS1F2_WithIdentityList()
        {
            var reply = NewHandler().Handle(Data(1, 1));

            Assert.AreEqual((byte)1, reply.Stream);
            Assert.AreEqual((byte)2, reply.Function);   // S1F2 On-Line Data

            var list = reply.Payload as ListItem;
            Assert.IsNotNull(list, "S1F2 body must be L,2 {MDLN, SOFTREV}.");
            Assert.AreEqual(2, list!.Value.Count);
            Assert.AreEqual("MODEL-X", ((AsciiItem)list.Value[0]).Value);
            Assert.AreEqual("1.0.0", ((AsciiItem)list.Value[1]).Value);
        }

        [TestMethod]
        public void S1F1_WithUnexpectedPayload_ReturnsS9F7()
        {
            // S1F1 carries no body; a body is illegal data.
            var reply = NewHandler().Handle(Data(1, 1, payload: new AsciiItem("unexpected")));

            Assert.AreEqual((byte)9, reply.Stream);
            Assert.AreEqual((byte)7, reply.Function);   // S9F7 Illegal Data
            Assert.IsInstanceOfType(reply.Payload, typeof(BinaryItem));
        }

        [TestMethod]
        public void S1F13_Valid_ReturnsS1F14_WithCommackAndEquipmentIdentity()
        {
            var body = new ListItem(new List<SecsItem>
            {
                new AsciiItem("HOST"),
                new AsciiItem("9.9")
            });

            var reply = NewHandler().Handle(Data(1, 13, payload: body));

            Assert.AreEqual((byte)1, reply.Stream);
            Assert.AreEqual((byte)14, reply.Function);  // S1F14

            var outer = reply.Payload as ListItem;
            Assert.IsNotNull(outer);
            Assert.AreEqual(2, outer!.Value.Count);

            var commack = outer.Value[0] as BinaryItem;
            Assert.IsNotNull(commack);
            CollectionAssert.AreEqual(new byte[] { 0 }, commack!.Value.ToArray()); // Accepted

            // Inner list must be the EQUIPMENT's identity, not the host's.
            var inner = outer.Value[1] as ListItem;
            Assert.IsNotNull(inner);
            Assert.AreEqual(2, inner!.Value.Count);
            Assert.AreEqual("MODEL-X", ((AsciiItem)inner.Value[0]).Value);
            Assert.AreEqual("1.0.0", ((AsciiItem)inner.Value[1]).Value);
        }

        [TestMethod]
        public void S1F13_Malformed_ReturnsS9F7_WithMhead()
        {
            // L,1 instead of L,2 => illegal data.
            var body = new ListItem(new List<SecsItem> { new AsciiItem("only-one") });

            var reply = NewHandler().Handle(Data(1, 13, payload: body));

            Assert.AreEqual((byte)9, reply.Stream);
            Assert.AreEqual((byte)7, reply.Function);   // S9F7
            var mhead = reply.Payload as BinaryItem;
            Assert.IsNotNull(mhead);
            Assert.AreEqual(10, mhead!.Value.Count, "S9 MHEAD is the offending 10-byte header.");
        }

        [TestMethod]
        public void UnknownFunction_InKnownStream_ReturnsS9F5_WithMhead()
        {
            // Stream 1 is known, function 5 is not => Unrecognized Function.
            var reply = NewHandler().Handle(Data(1, 5, systemBytes: 0x01020304));

            Assert.AreEqual((byte)9, reply.Stream);
            Assert.AreEqual((byte)5, reply.Function);   // S9F5

            var mhead = reply.Payload as BinaryItem;
            Assert.IsNotNull(mhead, "S9F5 must carry the offending MHEAD as binary.");
            var expectedHeader = new byte[]
            {
                0x00, 0x01,             // DeviceId = 1
                0x81,                   // Stream 1 | W-bit (bit 7)  — SEMI E5/E37 layout
                0x05,                   // Function 5
                0x00,                   // PType
                0x00,                   // SType = Data
                0x01, 0x02, 0x03, 0x04  // SystemBytes
            };
            CollectionAssert.AreEqual(expectedHeader, mhead!.Value.ToArray());
        }

        [TestMethod]
        public void UnknownStream_ReturnsS9F3_WithMhead()
        {
            // Stream 7 is not supported at all => Unrecognized Stream.
            var reply = NewHandler().Handle(Data(7, 1));

            Assert.AreEqual((byte)9, reply.Stream);
            Assert.AreEqual((byte)3, reply.Function);   // S9F3
            Assert.IsInstanceOfType(reply.Payload, typeof(BinaryItem));
        }

        // --- Communication-state edge cases ---

        [TestMethod]
        public void S1F13_WhenAlreadyCommunicating_StillAccepts_AndStaysCommunicating()
        {
            var equipment = NewEquipment();
            IDataMessageHandler handler = new EquipmentMessageHandler(equipment);

            // First handshake drives NotCommunicating -> Communicating.
            handler.Handle(Data(1, 13, payload: ValidS1F13Body()));
            Assert.AreEqual(CommunicationStatus.Communicating, equipment.Communicationstate.CurrentStatus);

            // A second S1F13 while already Communicating is still accepted, and the state is unchanged.
            var reply = handler.Handle(Data(1, 13, payload: ValidS1F13Body()));

            Assert.AreEqual((byte)1, reply.Stream);
            Assert.AreEqual((byte)14, reply.Function);
            var commack = (reply.Payload as ListItem)!.Value[0] as BinaryItem;
            Assert.IsNotNull(commack);
            CollectionAssert.AreEqual(new byte[] { (byte)Commack.Accepted }, commack!.Value.ToArray());
            Assert.AreEqual(CommunicationStatus.Communicating, equipment.Communicationstate.CurrentStatus,
                "A repeat S1F13 must not change the communication state.");
        }

        [TestMethod]
        public void S1F13_WhenDisabled_IsDenied_AndStaysDisabled()
        {
            var equipment = NewEquipment();
            equipment.Communicationstate.CurrentStatus = CommunicationStatus.Disabled;
            IDataMessageHandler handler = new EquipmentMessageHandler(equipment);

            var reply = handler.Handle(Data(1, 13, payload: ValidS1F13Body()));

            // Still an S1F14, but COMMACK must not be Accepted, and the state must stay Disabled.
            Assert.AreEqual((byte)1, reply.Stream);
            Assert.AreEqual((byte)14, reply.Function);
            var commack = (reply.Payload as ListItem)!.Value[0] as BinaryItem;
            Assert.IsNotNull(commack);
            Assert.AreNotEqual((byte)Commack.Accepted, commack!.Value[0], "A disabled equipment must not accept.");
            Assert.AreEqual(CommunicationStatus.Disabled, equipment.Communicationstate.CurrentStatus,
                "A denied S1F13 must leave the equipment Disabled.");
        }

        // --- S1F15 Request OFFLINE ---

        [TestMethod]
        public void S1F15_WhenCommunicating_ReturnsS1F16Accepted_AndGoesOffline()
        {
            var equipment = NewEquipment();
            equipment.Communicationstate.CurrentStatus = CommunicationStatus.Communicating;
            equipment.ControlState.CurrentControlState = ControlStatus.OnlineRemote;
            IDataMessageHandler handler = new EquipmentMessageHandler(equipment);

            var reply = handler.Handle(Data(1, 15));   // S1F15, no payload

            Assert.AreEqual((byte)1, reply.Stream);
            Assert.AreEqual((byte)16, reply.Function, "Reply to S1F15 must be S1F16 (not S1F1).");
            Assert.IsFalse(reply.Waitbit, "A reply must not set the W-bit.");

            var ack = reply.Payload as BinaryItem;
            Assert.IsNotNull(ack);
            Assert.AreEqual((byte)Oflack.Accepted, ack!.Value[0]);
            Assert.AreEqual(ControlStatus.Offline, equipment.ControlState.CurrentControlState,
                "A granted S1F15 must move the equipment Offline.");
        }

        [TestMethod]
        public void S1F15_WhenNotCommunicating_ReturnsS1F16Denied()
        {
            var equipment = NewEquipment();   // defaults to NotCommunicating
            IDataMessageHandler handler = new EquipmentMessageHandler(equipment);

            var reply = handler.Handle(Data(1, 15));

            Assert.AreEqual((byte)1, reply.Stream);
            Assert.AreEqual((byte)16, reply.Function);
            var ack = reply.Payload as BinaryItem;
            Assert.IsNotNull(ack);
            Assert.AreEqual((byte)Oflack.Denied, ack!.Value[0],
                "An offline request before communication is established must be denied.");
        }

        [TestMethod]
        public void S1F15_WithUnexpectedPayload_ReturnsS9F7()
        {
            var equipment = NewEquipment();
            equipment.Communicationstate.CurrentStatus = CommunicationStatus.Communicating;
            IDataMessageHandler handler = new EquipmentMessageHandler(equipment);

            var reply = handler.Handle(Data(1, 15, payload: new AsciiItem("unexpected")));

            Assert.AreEqual((byte)9, reply.Stream);
            Assert.AreEqual((byte)7, reply.Function);   // S9F7 Illegal Data
        }

        // --- S1F17 Request ONLINE ---

        [TestMethod]
        public void S1F17_WhenCommunicating_ReturnsS1F18Accepted_AndGoesOnlineRemote()
        {
            var equipment = NewEquipment();
            equipment.Identity.DefaultOnlineState = "OnlineRemote";
            equipment.Communicationstate.CurrentStatus = CommunicationStatus.Communicating;
            // ControlState defaults to Offline.
            IDataMessageHandler handler = new EquipmentMessageHandler(equipment);

            var reply = handler.Handle(Data(1, 17));   // S1F17, no payload

            Assert.AreEqual((byte)1, reply.Stream);
            Assert.AreEqual((byte)18, reply.Function, "Reply to S1F17 must be S1F18 (not S1F1).");
            Assert.IsFalse(reply.Waitbit, "A reply must not set the W-bit.");

            var ack = reply.Payload as BinaryItem;
            Assert.IsNotNull(ack);
            Assert.AreEqual((byte)Onlack.Accepted, ack!.Value[0]);
            Assert.AreEqual(ControlStatus.OnlineRemote, equipment.ControlState.CurrentControlState,
                "The equipment must adopt the configured online substate.");
        }

        [TestMethod]
        public void S1F17_HonoursDefaultOnlineState_Local()
        {
            var equipment = NewEquipment();
            equipment.Identity.DefaultOnlineState = "OnlineLocal";
            equipment.Communicationstate.CurrentStatus = CommunicationStatus.Communicating;
            IDataMessageHandler handler = new EquipmentMessageHandler(equipment);

            var reply = handler.Handle(Data(1, 17));

            Assert.AreEqual((byte)18, reply.Function);
            Assert.AreEqual(ControlStatus.OnlineLocal, equipment.ControlState.CurrentControlState);
        }

        [TestMethod]
        public void S1F17_WhenNotCommunicating_ReturnsS1F18Denied()
        {
            var equipment = NewEquipment();   // defaults to NotCommunicating
            IDataMessageHandler handler = new EquipmentMessageHandler(equipment);

            var reply = handler.Handle(Data(1, 17));

            Assert.AreEqual((byte)18, reply.Function);
            var ack = reply.Payload as BinaryItem;
            Assert.IsNotNull(ack);
            Assert.AreEqual((byte)Onlack.Denied, ack!.Value[0]);
            Assert.AreEqual(ControlStatus.Offline, equipment.ControlState.CurrentControlState,
                "A denied online request must leave the equipment Offline.");
        }

        [TestMethod]
        public void S1F17_WithUnexpectedPayload_ReturnsS9F7()
        {
            var equipment = NewEquipment();
            equipment.Communicationstate.CurrentStatus = CommunicationStatus.Communicating;
            IDataMessageHandler handler = new EquipmentMessageHandler(equipment);

            var reply = handler.Handle(Data(1, 17, payload: new AsciiItem("unexpected")));

            Assert.AreEqual((byte)9, reply.Stream);
            Assert.AreEqual((byte)7, reply.Function);   // S9F7 Illegal Data
        }
    }
}
