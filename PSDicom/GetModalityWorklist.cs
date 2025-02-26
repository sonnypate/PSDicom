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
        private ILogger _logger = Log.ForContext<GetModalityWorklist>();
        private readonly WorklistQuery _worklistQuery = new WorklistQuery();
        private IEnumerable<WorklistResponse> WorklistResponses = new List<WorklistResponse>();

        // Inputs:
        [Parameter(
            Mandatory = true,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            Position = 1,
            HelpMessage = "Connection details: IP address, port, calling AET, and called AET.")]
        public required Connection Connection { get; set; }
        public string? PatientId { get; set; }
        public string? PatientName { get; set; }
        public string? StationAet { get; set; }
        public string? StationName { get; set; }
        public string? Modality { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        // LogPath will use Serilog to log to a file.
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

        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            WorklistQuery worklistQuery = new WorklistQuery();

            var cancellationToken = new System.Threading.CancellationToken();

            WriteObject(_worklistQuery.WorklistResponses);
        }

        protected override void EndProcessing()
        {
            base.EndProcessing();

            // Clean up logging before shutting down.
            Log.CloseAndFlush();
        }

        private async Task PerformWorklistQuery(CancellationToken cancellationToken)
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

            try
            {
                var client = new Client(Connection).GetDicomClient();

                var dataset = await _worklistQuery.PerformWorklistQuery(client, request, cancellationToken);
                _worklistQuery.GetWorklistValuesFromDataset(dataset);
            }
            catch (AggregateException ex)
            {
                Log.Error("Error: {exception}", ex);
                return;
            }
        }
    }
}
