using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace sonarr_scanner
{
    class Program
    {
        private static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private static List<Monitor> monitors = new List<Monitor>();

        private static void Main(string[] args)
        {

            Console.WriteLine("Sonarr Scanner starting...");

            Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e) {
                Exit();
                e.Cancel = true;
            };

            Console.WriteLine("Loading settings...");
            Settings.Init();

            monitors.Add(new Monitor(Settings.Sonarr, cancellationTokenSource.Token));
            monitors.Add(new Monitor(Settings.Radarr, cancellationTokenSource.Token));

            var anyTrue = false;
            foreach (var monitor in monitors)
            {
                Console.WriteLine($"Starting {monitor.Settings.Provider()} Monitor");
                if (monitor.Init())
                    anyTrue = true;
            }
            if (!anyTrue)
            {
                var err = "All monitors startup failed, make sure that you configured the config file (settings_radarr.json and/or settings_sonarr.json) correctly, aborting...";
                Console.WriteLine(err);
                Exit();
            }

        }

        private static void Exit()
        {
            Console.WriteLine("Exiting...");
            cancellationTokenSource.Cancel();
            Environment.Exit(0);
        }
    }
}
