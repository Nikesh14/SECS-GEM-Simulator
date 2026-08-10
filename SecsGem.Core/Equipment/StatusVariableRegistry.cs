using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecsGem.Core.Equipment
{
    public class StatusVariableRegistry
    {
        private readonly Dictionary<uint, StatusVariable> _statusVariables;

        public StatusVariableRegistry(Dictionary<uint, StatusVariable> statusVariables)
        {
            _statusVariables = statusVariables;
        }

        public void Register(StatusVariable variable)
        {

        }
        //public StatusVariable Lookup(uint id)
        //{

        //}
        //public bool ValidateDuplicates(StatusVariable variable)
        //{
        //    if(_sta)
        //}
    }
}
