using FellowOakDicom;
using FellowOakDicom.Network;
using PSDicom.DICOM;
using Serilog;
using System.Management.Automation;

namespace PSDicom
{
    [Cmdlet(VerbsCommon.Get, "ModalityWorklist")]
    [OutputType(typeof(WorklistResponse))]
    public class GetModalityWorklist : Cmdlet
    {
        CancellationTokenSource _cts = new CancellationTokenSource();
        int _timeout = 30; // Default to 30 seconds.

        // Inputs:
        [Parameter(
            Mandatory = true,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            Position = 0,
            HelpMessage = "Connection details: IP address, port, calling AET, and called AET.")]
        public required Connection Connection { get; set; }
        
        [Parameter(
            Position = 1,
            HelpMessage = "Patient ID (MRN).")]
        public string? PatientId { get; set; }
        
        [Parameter(
            Position = 2,
            HelpMessage = "Patient name.")]
        public string? PatientName { get; set; }
        
        [Parameter(
            Position = 3,
            HelpMessage = "Station AE title.")]
        public string? StationAet { get; set; }
        
        [Parameter(
            Position = 4,
            HelpMessage = "Station name.")]
        public string? StationName { get; set; }
        
        [Parameter(
            Position = 5,
            HelpMessage = "Modality.")]
        public string? Modality { get; set; }
        
        [Parameter(
            Position = 6,
            HelpMessage = "Start date.")]
        public DateTime StartDate { get; set; }
        
        [Parameter(
            Position = 7,
            HelpMessage = "End date.")]
        public DateTime EndDate { get; set; }
        
        // LogPath will use Serilog to log to a file.
        [Parameter(
            Position = 8,
            ParameterSetName = "FileLog",
            HelpMessage = "Log file path. Include the filename and .log extension.")]
        public string? LogPath { get; set; }
        
        // LogDimseDataset and LogDataPDUs will log the DICOM dataset and data PDUs.
        [Parameter(
            Position = 9,
            ParameterSetName = "FileLog",
            HelpMessage = "Log DICOM dataset.")]
        public SwitchParameter LogDimseDataset { get; set; } = false;
        
        [Parameter(
            Position = 10,
            ParameterSetName = "FileLog",
            HelpMessage = "Log data PDUs.")]
        public SwitchParameter LogDataPDUs { get; set; } = false;

        [Parameter(
            Position = 11, 
            HelpMessage = "Timeout in seconds. Default is 30.")]
        [ValidateRange(1, 60)]
        public int Timeout
        {
            get { return _timeout; }
            set { _timeout = value * 1000; }
        }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            _cts = new CancellationTokenSource();
            _cts.CancelAfter(Timeout);

            if (!string.IsNullOrEmpty(LogPath))
            {
                Logging.ConfigureLogging(LogPath);
                Log.Information("Logging configured.");
                WriteVerbose($"Logging to {LogPath}");
            }
        }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            WorklistQuery worklistQuery = new WorklistQuery();

            try
            {
                var client = new Client(Connection).GetDicomClient();
                client.ServiceOptions.LogDimseDatasets = LogDimseDataset;
                client.ServiceOptions.LogDataPDUs = LogDataPDUs;

                var request = CreateCFindRequest();
                Task<List<DicomDataset>> task = worklistQuery.PerformWorklistQuery(client, request, _cts.Token);
                task.Wait();
                List<DicomDataset> dataset = task.Result;
                worklistQuery.GetWorklistValuesFromDataset(dataset);
            }
            catch (AggregateException ex)
            {
                Log.Error("Error: {exception}", ex);
                return;
            }

            // If timed out, cancel the request.
            if (_cts.Token.IsCancellationRequested)
            {
                WriteVerbose("Worklist query timed out.");
                return;
            }
            else
            {
                WriteVerbose($"Connection to '{Connection.CalledAET}' was successful.");
            }

            WriteObject(worklistQuery.WorklistResponses);
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

        private DicomCFindRequest CreateCFindRequest()
        {

            WriteVerbose("Starting worklist query");

            var request = DicomCFindRequest.CreateWorklistQuery(
                patientId: PatientId,
                patientName: PatientName,
                stationAE: StationAet,
                stationName: StationName,
                modality: Modality
            );

            // Get the ScheduledProcedureStepSequence from the dataset created by the CreateWorklistQuery function.
            // Using this instead of the built-in scheduledDateTime parameter from the CreateWorklistQuery function because it requires
            // a range. This allows me to optionally select either a start date, both, or none:
            foreach (var item in request.Dataset.GetSequence(DicomTag.ScheduledProcedureStepSequence))
            {
                // If a start date is selected, use this.
                if (StartDate != default(DateTime))
                {
                    item?.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDate, StartDate);
                }

                // if the end date is selected, but there's no start date, then copy the end date to the start date.
                if (StartDate == default(DateTime) && EndDate != default(DateTime))
                {
                    StartDate = EndDate;
                }

                // If both start date and end date are selected then create a range with date and time.
                if (StartDate != default(DateTime) && EndDate != default(DateTime))
                {
                    var dr = new DicomDateRange(StartDate, EndDate.AddDays(1).AddTicks(-1));
                    item?.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDate, dr);
                    item?.AddOrUpdate(DicomTag.ScheduledProcedureStepStartTime, dr);
                }
            }

            return request;
        }
    }
}
