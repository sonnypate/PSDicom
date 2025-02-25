using FellowOakDicom.Network;
using MWL_Tester.DICOM;
using PSDicom.DICOM;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PSDicom
{
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

        public string? LogPath { get; set; }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            if (!string.IsNullOrEmpty(LogPath))
            {
                Logging.ConfigureLogging(LogPath);
            }

            _logger.Information("Hello, world!");

        }

        private async Task PerformConnectionTest(CancellationToken cancellationToken)
        {
            _logger.Information("Starting C-Echo");

            var client = new Client(Connection).GetDicomClient();

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
                WriteError(new ErrorRecord(ex, "Error", ErrorCategory.NotSpecified, Connection));
                return;
            }

            // If the cancel button was clicked, then update status, otherwise if it made it this far then it was successful.
            if (cancellationToken.IsCancellationRequested)
            {
                WriteDebug("Test cancelled"); 
                return;
            }
            else
            {
                WriteDebug($"Connection to '{Connection.CalledAET}' was successful.");
            }
        }
    }
}

