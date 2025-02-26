namespace PSDicom.DICOM
{
    public class Connection
    {
        public string CalledHost { get; set; } = "127.0.0.1";
        public string CalledAET { get; set; } = "CALLEDAET";
        public int Port { get; set; } = 104;
        public bool UseTLS { get; set; } = false;
        public string CallingAET { get; set; } = "CALLINGAET";
    }
}