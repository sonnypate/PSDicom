using Serilog;
using System.Management.Automation;

namespace PSDicom
{
    [Cmdlet(VerbsDiagnostic.Test, "Log")]
    public class TestLog : Cmdlet
    {
        private ILogger _logger = Log.ForContext<TestLog>();
        private string _logPath = string.Empty;

        public TestLog()
        {
            // Set the default log path to the desktop.
            _logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TestLog.log");

            Logging.ConfigureLogging(_logPath);
            _logger.Information("Logging configured");
        }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
        }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            _logger.Information("Information log");
            _logger.Warning("Warning log");
            _logger.Error("Error log"); 
            _logger.Fatal("Fatal log");
            _logger.Debug("Debug log");
            _logger.Verbose("Verbose log");
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

            // Clean up logging before shutting down.
            Log.CloseAndFlush();
        }
    }
}

