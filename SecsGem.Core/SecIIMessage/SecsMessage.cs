using Microsoft.Extensions.Configuration;
using SecsGem.Core.Equipment;
using SecsGem.Core.Host;
using SecsGem.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace SecsGem.Core.SecIIMessage
{
    public abstract class SecsMessage
    {
        public abstract byte Stream { get; }
        public abstract byte Function { get; }
        public abstract bool Waitbit { get; } 
        public abstract SecsItem? Payload { get; }
    }

    public sealed class S1F1 : SecsMessage
    {
        public override byte Stream => 1;
        public override byte Function =>  1;
        public override bool Waitbit => true;
        public override SecsItem? Payload => null;
    }
    public sealed class S1F2 (Equipment.Identity equipmentIdentity) : SecsMessage
    {
        private readonly SecsItem? _cachedPayload = GeneratePayload(equipmentIdentity);
        public override byte Stream => 1;
        public override byte Function => 2;
        public override bool Waitbit => false;
        public override SecsItem? Payload => _cachedPayload;

        private static SecsItem GeneratePayload(Equipment.Identity equipmentIdentity)
        {
            var lstSecItem = new List<SecsItem>();
            if (equipmentIdentity == null)
                return new ListItem(new List<SecsItem>());
            else
            {
                lstSecItem.Add(new AsciiItem($"{equipmentIdentity.ModelName}"));
                lstSecItem.Add(new AsciiItem($"{equipmentIdentity.SoftwareRevision}"));
            }
            return new ListItem(lstSecItem);
        }
    }
    public sealed class S9F5(SecsItem? payload = null) : SecsMessage
    {
        public override byte Stream => 9;
        public override byte Function => 5;
        public override bool Waitbit => false;
        public override SecsItem? Payload => payload;
    }
    public sealed class S9F3(SecsItem? payload = null) : SecsMessage
    {
        public override byte Stream => 9;
        public override byte Function => 3;
        public override bool Waitbit => false;
        public override SecsItem? Payload => payload;
    }
    public sealed class S9F7(SecsItem? payload = null) : SecsMessage
    {
        public override byte Stream => 9;
        public override byte Function => 7;
        public override bool Waitbit => false;
        public override SecsItem? Payload => payload;
    }
    public sealed class S1F13(Host.Identity hostIdentity) : SecsMessage 
    {
        // Calculated exactly once during instantiation
        private readonly SecsItem? _cachedPayload = GeneratePayload(hostIdentity);
        public override byte Stream => 1;
        public override byte Function => 13;
        public override bool Waitbit => true;
        public override SecsItem? Payload => _cachedPayload;

        private static SecsItem GeneratePayload(Host.Identity hostIdentity)
        {
            var lstSecItem = new List<SecsItem>();
            if (hostIdentity == null)
                return new ListItem(new List<SecsItem>());
            else
            {
                lstSecItem.Add(new AsciiItem($"{hostIdentity.ModelName}"));
                lstSecItem.Add(new AsciiItem($"{hostIdentity.SoftwareRevision}"));
            }
            return new ListItem(lstSecItem);
        }
    }
    public sealed class S1F14(Equipment.Identity equipmentIdentity, Commack ack) : SecsMessage
    {
        private readonly SecsItem? _cachedPayload = GeneratePayload(equipmentIdentity);
        public override byte Stream => 1;
        public override byte Function => 14;
        public override bool Waitbit => false;
        public override SecsItem? Payload => new ListItem(new List<SecsItem>(){
            new BinaryItem(new List<byte>() { (byte)ack }),
            _cachedPayload!
        });
        private static SecsItem GeneratePayload(Equipment.Identity equipmentIdentity)
        {
            var lstSecItem = new List<SecsItem>();
            if (equipmentIdentity == null)
                return new ListItem(new List<SecsItem>());
            else
            {
                lstSecItem.Add(new AsciiItem($"{equipmentIdentity.ModelName}"));
                lstSecItem.Add(new AsciiItem($"{equipmentIdentity.SoftwareRevision}"));
            }
            return new ListItem(lstSecItem);
        }
    }
    public sealed class S1F15 : SecsMessage
    {
        public override byte Stream => 1;
        public override byte Function => 15;
        public override bool Waitbit => true;
        public override SecsItem? Payload => null;
    }
    public sealed class S1F16(Oflack ofAck) : SecsMessage
    {
        public override byte Stream => 1;
        public override byte Function => 16;
        public override bool Waitbit => false;   // reply: no W-bit
        public override SecsItem? Payload => new BinaryItem(new List<byte>() { (byte)ofAck });
    }
    public sealed class S1F17 : SecsMessage
    {
        public override byte Stream => 1;
        public override byte Function => 17;
        public override bool Waitbit => true;
        public override SecsItem? Payload => null;
    }
    public sealed class S1F18(Onlack onAck) : SecsMessage
    {
        public override byte Stream => 1;
        public override byte Function => 18;
        public override bool Waitbit => false;   // reply: no W-bit
        public override SecsItem? Payload => new BinaryItem(new List<byte>() { (byte)onAck });
    }


}
