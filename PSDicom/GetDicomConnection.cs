using PSDicom.DICOM;
using System.Management.Automation;

namespace PSDicom
{
    [Cmdlet(VerbsCommon.Get, "DicomConnection")]
    [OutputType(typeof(Connection))]
    public class GetDicomConnection : Cmdlet
    {
        [Parameter(
            HelpMessage = "The IP or hostname of the DICOM server.")]
        public string CalledHost { get; set; } = "127.0.0.1";

        [Parameter(
            HelpMessage = "The AET of the DICOM server.")]
        public string CalledAET { get; set; } = "CALLEDAET";

        [Parameter(
            HelpMessage = "The port of the DICOM server.")]
        [ValidateRange(1, 65535)]
        public int Port { get; set; } = 104;

        [Parameter(
            HelpMessage = "Enable TLS for the connection.")]
        public bool UseTLS { get; set; } = false;

        [Parameter(
            HelpMessage = "The AET of the calling application.")]
        public string CallingAET { get; set; } = "CALLINGAET";

        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            Connection connection = new Connection() { 
                CalledAET = CalledAET,
                CalledHost = CalledHost,
                Port = Port,
                UseTLS = UseTLS,
                CallingAET = CallingAET
            };

            WriteObject(connection);
        }
    }
}
