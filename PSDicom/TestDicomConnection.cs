using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using PSDicom.DICOM;
using Serilog;
using System.Diagnostics;
using System.Management.Automation;

namespace PSDicom
{
    [Cmdlet(VerbsDiagnostic.Test, "DicomConnection")]
    public class TestDicomConnection : Cmdlet
    {
        private ILogger _logger = Log.ForContext<TestDicomConnection>();
        CancellationTokenSource _cts = new CancellationTokenSource();


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

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            _cts = new CancellationTokenSource();

            if (!string.IsNullOrEmpty(LogPath))
            {
                Logging.ConfigureLogging(LogPath);
            }

            _logger.Information("Logging configured");
        }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            try
            {
                PerformConnectionTest(_cts.Token).Wait();
                WriteVerbose($"Connection to '{Connection.CalledAET}' was successful.");
            }
            catch (Exception ex)
            {
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

        private async Task PerformConnectionTest(CancellationToken cancellationToken)
        {
            _logger.Information("Starting C-Echo");

            var client = new Client(Connection).GetDicomClient();
            client.ServiceOptions.LogDimseDatasets = LogDimseDataset;
            client.ServiceOptions.LogDataPDUs = LogDataPDUs;

            client.AssociationAccepted += (sender, args) =>
            {
                _logger.Information("Association accepted");

                var response = new DicomConnectionResponse() { 
                    CallingAET = Connection.CallingAET,
                    CalledAET = Connection.CalledAET,
                    CalledHost = Connection.CalledHost,
                    Port = Connection.Port,
                    Status = "Association accepted"
                };


            };

            client.AssociationRejected += (sender, args) =>
            {
                _logger.Error("Association rejected: {reason}", args.Reason);

                var response = new DicomConnectionResponse()
                {
                    CallingAET = Connection.CallingAET,
                    CalledAET = Connection.CalledAET,
                    CalledHost = Connection.CalledHost,
                    Port = Connection.Port,
                    Status = "Association accepted"
                };


            };

                client.AssociationReleased += (sender, args) =>
                {
                    _logger.Information("Association released");

                    var response = new DicomConnectionResponse()
                    {
                        CallingAET = Connection.CallingAET,
                        CalledAET = Connection.CalledAET,
                        CalledHost = Connection.CalledHost,
                        Port = Connection.Port,
                        Status = "Association accepted"
                    };


                };

            try
            {
                client.NegotiateAsyncOps();

                for (int i = 0; i < 10; i++)
                {
                    ProgressRecord progressRecord = new(1, "Testing connection", $"Sending C-Echo request {i + 1} of 10");
                    WriteProgress(progressRecord);
                    await client.AddRequestAsync(new DicomCEchoRequest());
                }

                await client.SendAsync(cancellationToken);
            }
            catch (AggregateException ex)
            {
                _logger.Error("Error: {exception}", ex);
                throw;
            }
        }
    }
}

