using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtoPowerDebugger.Models
{
    public class RawAdcData
    {
        public int PrimaryMicroAmp { get; set; }
        public int PrimaryVolt { get; set; }
        public int AuxMicroAmp { get; set; }
        public int AuxVolt { get; set; }
    }
}
