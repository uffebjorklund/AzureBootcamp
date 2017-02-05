using System;
using Serilog;

namespace AzureBootCamp.DataPump
{
    class Program
    {
        static void Main(string[] args)
        {
            //var logSwicth = new Serilog.Core.LoggingLevelSwitch(Serilog.Events.LogEventLevel.Debug);
            //Log.Logger = new LoggerConfiguration().MinimumLevel.ControlledBy(logSwicth)
            //    .WriteTo.LiterateConsole()                
            //    .CreateLogger();

            var pump = new DataPump.ValmetDataPump();
            pump.Run();
            Console.ReadLine();
        }
    }
}
