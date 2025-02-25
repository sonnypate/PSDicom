using FellowOakDicom;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace PSDicom
{
    internal static class Logging
    {
        public static void ConfigureLogging(string logPath, 
            RollingInterval rollingInterval = RollingInterval.Day,
            int retainedLogLimit = 7)
        { 

            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File(logPath,
                    rollingInterval: rollingInterval,
                    retainedFileCountLimit: retainedLogLimit);

            var logger = loggerConfig.CreateLogger();

            //Stash the logger in the global Log instance for convenience
            Log.Logger = logger;

            new DicomSetupBuilder() // Requires Serilog.Extensions.Logging for the AddSerilog function.
                .RegisterServices(services => services.AddLogging(logging => logging.AddSerilog(logger)))
                .Build();
        }
    }
}
