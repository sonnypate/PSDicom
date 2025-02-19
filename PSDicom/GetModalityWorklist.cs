using FellowOakDicom;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using PSDicom.DICOM;
using Serilog;
using System.Management.Automation;

namespace PSDicom
{
    [Cmdlet(VerbsCommon.Get, "ModalityWorklist")]
    public class GetModalityWorklist : Cmdlet
    {
        private IEnumerable<WorklistResponse> WorklistResponses = new List<WorklistResponse>();
        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

            Log.Information("Hello, world!");
        }
    }
}
