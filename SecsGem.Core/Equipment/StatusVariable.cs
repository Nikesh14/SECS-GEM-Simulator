using SecsGem.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecsGem.Core.Equipment
{
    public class StatusVariable
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Func<SecsItem> ValueProvider { get; set; }
    }
}
