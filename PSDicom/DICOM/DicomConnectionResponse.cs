using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSDicom.DICOM
{
    internal class DicomConnectionResponse
    {
        public int Attempt { get; set; } = 0;
        public string CallingAET { get; set; } = string.Empty;
        public string CalledHost { get; set; } = string.Empty;
        public string CalledAET { get; set; } = string.Empty;
        public int Port { get; set; } = 0;
        public string Status { get; set; } = string.Empty;
        public long Time { get; set; } = 0;
    }
}
