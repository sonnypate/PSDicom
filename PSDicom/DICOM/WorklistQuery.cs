using FellowOakDicom;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using Serilog;

namespace PSDicom.DICOM
{
    internal class WorklistQuery
    {
        private ILogger _logger;

        public List<WorklistResponse> WorklistResponses { get; set; } = new List<WorklistResponse>();

        public WorklistQuery()
        {
            _logger = Log.ForContext<WorklistQuery>();
        }

        internal async Task<List<DicomDataset>> PerformWorklistQuery(IDicomClient client, DicomCFindRequest request, CancellationToken cancellationToken)
        {
            var worklistItems = new List<DicomDataset>();

            request.OnResponseReceived = (DicomCFindRequest rq, DicomCFindResponse rp) =>
            {
                if (rp.HasDataset)
                {
                    _logger.Information("Study UID: {SUID}", rp.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID));
                    worklistItems.Add(rp.Dataset);
                }
                else
                {
                    _logger.Warning(rp.Status.ToString());
                }
            };

            await client.AddRequestAsync(request);
            await client.SendAsync(cancellationToken);

            return worklistItems;
        }

        internal void GetWorklistValuesFromDataset(List<DicomDataset> datasets)
        {
            WorklistResponses.Clear();

            foreach (var dataset in datasets)
            {
                var worklist = new WorklistResponse();

                worklist.PatientName = dataset.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty);
                worklist.PatientId = dataset.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty);
                worklist.Accession = dataset.GetSingleValueOrDefault(DicomTag.AccessionNumber, string.Empty);
                worklist.ExamDescription = dataset.GetSingleValueOrDefault(DicomTag.RequestedProcedureDescription, string.Empty);
                worklist.StudyInstanceUID = dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);

                var scheduledProcedureStep = dataset.GetSequence(DicomTag.ScheduledProcedureStepSequence);
                if (scheduledProcedureStep.Items.Count > 0)
                {
                    foreach (var item in scheduledProcedureStep)
                    {
                        worklist.Modality = item.GetSingleValueOrDefault(DicomTag.Modality, string.Empty);
                        worklist.ScheduledStationAET = item.GetSingleValueOrDefault(DicomTag.ScheduledStationAETitle, string.Empty);
                        worklist.ScheduledStationName = item.GetSingleValueOrDefault(DicomTag.ScheduledStationName, string.Empty);
                        worklist.ScheduledStudyDate = item.GetSingleValueOrDefault(DicomTag.ScheduledProcedureStepStartDate, string.Empty);
                        worklist.ScheduledStudyTime = item.GetSingleValueOrDefault(DicomTag.ScheduledProcedureStepStartTime, string.Empty);
                    }
                }

                WorklistResponses.Add(worklist);
            }
        }
    }
}