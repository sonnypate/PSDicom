using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using PSDicom.DICOM;
using Serilog;
using System.Collections.ObjectModel;
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
                var response = PerformConnectionTest(_cts.Token);
                response.Wait();
                WriteVerbose($"Connection to '{Connection.CalledAET}' was successful.");

                WriteObject(response.Result);
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

        private async Task<DicomConnectionResponse> PerformConnectionTest(CancellationToken cancellationToken)
        {
            _logger.Information("Starting C-Echo");

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

            client.AssociationAccepted += (sender, args) =>
            {
                _logger.Verbose($"Association to '{Connection.CalledAET}' accepted.");
            };

            client.AssociationRejected += (sender, args) =>
            {
                _logger.Verbose($"Association to '{Connection.CalledAET}' rejected.");
            };

            client.AssociationReleased += (sender, args) =>
            {
                _logger.Verbose($"Association to '{Connection.CalledAET}' released.");
            };

            try
            {
                client.NegotiateAsyncOps();

                for (int i = 0; i < 10; i++)
                {
                    await client.AddRequestAsync(dicomCEchoRequest);
                }

                await client.SendAsync(cancellationToken);
            }
            catch (AggregateException ex)
            {
                _logger.Error("Error: {exception}", ex);
                throw;
            }

            return dicomConnectionResponse;
        }
    }
}

