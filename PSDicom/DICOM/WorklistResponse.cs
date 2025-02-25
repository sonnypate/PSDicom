namespace PSDicom.DICOM
{
    internal class WorklistResponse
    {
        public string PatientName { get; set; } = string.Empty;
        public string PatientId { get; set; } = string.Empty;
        public string Accession { get; set; } = string.Empty;
        public string Modality { get; set; } = string.Empty;
        public string ExamDescription { get; set; } = string.Empty;
        public string ScheduledStationAET { get; set; } = string.Empty;
        public string ScheduledStationName { get; set; } = string.Empty;
        public string ScheduledStudyDate { get; set; } = string.Empty;
        public string ScheduledStudyTime { get; set; } = string.Empty;
        public string StudyInstanceUID { get; set; } = string.Empty;
    }
}
