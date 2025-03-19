using FellowOakDicom.Network.Client;

namespace PSDicom.DICOM
{
    internal class Client
    {
        private Connection _connection;

        internal Client(Connection connection)
        {
            _connection = connection;
        }

        internal IDicomClient GetDicomClient()
        {
            var client = DicomClientFactory.Create(
                _connection.CalledHost,
                _connection.Port,
                _connection.UseTLS,
                _connection.CallingAET,
                _connection.CalledAET);

            return client;
        }
    }
}
