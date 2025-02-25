using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace MWL_Tester.DICOM
{
    public class Connection
    {
        [Parameter(
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            Position = 1,
            HelpMessage = "IP address or hostname of DICOM server.")]
        public string CalledHost { get; set; } = "127.0.0.1";
        public string CalledAET { get; set; } = "CALLEDAET";

        [ValidateRange(1, 65535)]
        public int Port { get; set; } = 104;
        public bool UseTLS { get; set; } = false;
        public string CallingAET { get; set; } = "CALLINGAET";
    }
}