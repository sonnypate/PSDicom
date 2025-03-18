using FellowOakDicom.Network;
using PSDicom.DICOM;
using Serilog;
using System.Management.Automation;

namespace PSDicom
{
    [Cmdlet(VerbsDiagnostic.Test, "DicomConnection")]
    public class TestDicomConnection : Cmdlet
    {
        CancellationTokenSource _cts = new CancellationTokenSource();
        private int _timeout = 30;

        [Parameter(
            Mandatory = true,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            Position = 0,
            HelpMessage = "Connection details: IP address, port, calling AET, and called AET.")]
        public required Connection Connection { get; set; }

        [Parameter(
            Position = 1,
            HelpMessage = "Log file path.")]
        public string? LogPath { get; set; }

        // LogDimseDataset and LogDataPDUs will log the DICOM dataset and data PDUs.
        [Parameter(
            Position = 2,
            HelpMessage = "Log DICOM dataset.")]
        public SwitchParameter LogDimseDataset { get; set; } = false;
        [Parameter(
            Position = 3,
            HelpMessage = "Log data PDUs.")]
        public SwitchParameter LogDataPDUs { get; set; } = false;

        [Parameter(Position = 4)]
        [ValidateRange(1, 60)]
        public int Timeout
        {
            get { return _timeout; }
            set { _timeout = value * 100; }
        }

        [Parameter(Position = 5)]
        [ValidateRange(1, 10)]
        public int Attempts { get; set; } = 1;

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            _cts = new CancellationTokenSource();
            _cts.CancelAfter(30000);

            if (!string.IsNullOrEmpty(LogPath))
            {
                Logging.ConfigureLogging(LogPath);
                Log.Information("Logging configured");
                WriteVerbose($"Logging to {LogPath}");
            }
        }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            Log.Information("Starting C-Echo");

            var client = new Client(Connection).GetDicomClient();
            var dicomCEchoRequest = new DicomCEchoRequest();
            client.ServiceOptions.LogDimseDatasets = LogDimseDataset;
            client.ServiceOptions.LogDataPDUs = LogDataPDUs;
            var dicomConnectionResponse = new DicomConnectionResponse()
            {
                CallingAET = Connection.CallingAET,
                CalledAET = Connection.CalledAET,
                CalledHost = Connection.CalledHost,
                Port = Connection.Port,
                Status = DicomStatus.ProcessingFailure.ToString()
            };

            dicomCEchoRequest.OnResponseReceived += (request, response) =>
            {
                dicomConnectionResponse.Status = response.Status.ToString();
            };

            try
            {
                client.NegotiateAsyncOps();

                for (int i = 0; i < Attempts; i++)
                {
                    WriteProgress(new ProgressRecord(1, "Testing DICOM connection", $"Attempt {i + 1}"));
                    
                    dicomConnectionResponse.Attempt = i + 1;
                    dicomConnectionResponse.Status = DicomStatus.ProcessingFailure.ToString();
                    client.AddRequestAsync(dicomCEchoRequest).Wait();
                    client.SendAsync(_cts.Token).Wait();
                    WriteObject(dicomConnectionResponse);
                }
                WriteVerbose($"Connection to '{Connection.CalledAET}' was successful.");
            }
            catch (AggregateException ex)
            {
                Log.Error("Error: {exception}", ex);
                WriteError(new ErrorRecord(ex, "Error", ErrorCategory.NotSpecified, Connection));
            }

            // If timed out, cancel the request.
            if (_cts.Token.IsCancellationRequested)
            {
                WriteVerbose("Test cancelled");
                return;
            }
            else
            {
                WriteVerbose($"Connection to '{Connection.CalledAET}' was successful.");
            }
        }

        protected override void EndProcessing()
        {
            base.EndProcessing();

            // Clean up logging before shutting down.
            Log.CloseAndFlush();
        }

        protected override void StopProcessing()
        {
            base.StopProcessing();
            _cts.Cancel();

            // Clean up logging before shutting down.
            Log.CloseAndFlush();
        }
    }
}

