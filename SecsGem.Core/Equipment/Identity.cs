using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecsGem.Core.Equipment
{
    public class Identity
    {
        public string ModelName { get; set; } = string.Empty;
        public string SoftwareRevision { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public ushort DeviceId { get; set; }
        public string DefaultOnlineState { get; set; } = string.Empty;
    }
}
