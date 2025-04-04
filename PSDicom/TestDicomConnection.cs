﻿using FellowOakDicom.Network;
using PSDicom.DICOM;
using Serilog;
using System.Diagnostics;
using System.Management.Automation;

namespace PSDicom
{
    [Cmdlet(VerbsDiagnostic.Test, "DicomConnection")]
    [OutputType(typeof(DicomConnectionResponse))]
    public class TestDicomConnection : Cmdlet
    {
        CancellationTokenSource _cts = new CancellationTokenSource();
        private int _timeout = 30000; // Default is 30 seconds.

        [Parameter(
            Mandatory = true,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            Position = 0,
            HelpMessage = "Connection object that contains the IP address or hostname, port, calling AET, and called AET. Use Get-DicomConnection to create the connection object.")]
        public required Connection Connection { get; set; }

        [Parameter(
            HelpMessage = "The full path to the log file. Include the filename and extension.")]
        public string? LogPath { get; set; }

        // LogDimseDataset and LogDataPDUs will log the DICOM dataset and data PDUs.
        [Parameter(
            HelpMessage = "Enable the DICOM Message Service Element (DIMSE) dataset logging.")]
        public SwitchParameter LogDimseDataset { get; set; } = false;
        
        [Parameter(
            HelpMessage = "Enable the DICOM Protocol Data Unit (PDU) logging.")]
        public SwitchParameter LogDataPDUs { get; set; } = false;

        [Parameter(
            HelpMessage = "Timeout in seconds. Default is 30.")]
        [ValidateRange(1, 60)]
        public int Timeout
        {
            get { return _timeout; }
            set { _timeout = value * 1000; }
        }

        [Parameter(
            HelpMessage = "Number of c-echo attempts. Default is 1.")]
        [ValidateRange(1, 10)]
        public int Attempts { get; set; } = 1;

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            _cts = new CancellationTokenSource();
            _cts.CancelAfter(Timeout);

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
                    dicomConnectionResponse.Attempt = i + 1;
                    dicomConnectionResponse.Status = DicomStatus.ProcessingFailure.ToString();
                    
                    client.AddRequestAsync(dicomCEchoRequest).Wait();
                    
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    client.SendAsync(_cts.Token).Wait();
                    stopwatch.Stop();
                    dicomConnectionResponse.Time = stopwatch.ElapsedMilliseconds;
                    
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

