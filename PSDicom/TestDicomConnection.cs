using FellowOakDicom.Network;
using PSDicom.DICOM;
using Serilog;
using System.Management.Automation;

namespace PSDicom
{
    [Cmdlet(VerbsDiagnostic.Test, "DicomConnection")]
    public class TestDicomConnection : Cmdlet
    {
        private ILogger _logger = Log.ForContext<TestDicomConnection>();

        [Parameter(
            Mandatory = true,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            Position = 1,
            HelpMessage = "Connection details: IP address, port, calling AET, and called AET.")]
        public required Connection Connection { get; set; }

        [Parameter(
            Position = 2,
            HelpMessage = "Log file path.")]
        public string? LogPath { get; set; }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            if (!string.IsNullOrEmpty(LogPath))
            {
                Logging.ConfigureLogging(LogPath);
            }

            _logger.Information("Logging configured");
        }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            CancellationToken cancellationToken = new CancellationToken();

            try
            {
                PerformConnectionTest(cancellationToken).Wait();
                WriteVerbose($"Connection to '{Connection.CalledAET}' was successful.");
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "Error", ErrorCategory.NotSpecified, Connection));
            }

            // If timed out, cancel the request.
            if (cancellationToken.IsCancellationRequested)
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

        private async Task PerformConnectionTest(CancellationToken cancellationToken)
        {
            _logger.Information("Starting C-Echo");

            var client = new Client(Connection).GetDicomClient();

            if (client == null)
            {
                _logger.Error("Client is null");
                return;
            }

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

