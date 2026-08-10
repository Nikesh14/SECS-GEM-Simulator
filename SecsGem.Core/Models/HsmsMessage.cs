using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecsGem.Core.Models
{
    public class HsmsMessage
    {
        public ushort DeviceId { get; set; }
        public byte? Stream { get; set; }
        public byte? Function { get; set; }
        public bool Waitbit { get; set; } = false;
        public SType SType { get; set; }
        public byte PType { get; set; } = 0;
        public uint SystemBytes { get; set; }
        public SecsItem? Payload  { get; set; }
    }

    public enum SType
    {
        Data = 0,
        Select_req = 1,
        Select_rsp = 2,
        Deselect_req = 3,
        Deselect_rsp = 4,
        Linktest_req = 5,
        Linktest_rsp = 6,
        Reject_req = 7,
        Separate_req = 9
    }

    public enum Commack
    {
        Accepted = 0,
        Denied = 1
    }
    public enum Oflack
    {
        Accepted = 0,
        Denied = 1
    }
    public enum Onlack
    {
        Accepted = 0,
        Denied = 1
    }
}
