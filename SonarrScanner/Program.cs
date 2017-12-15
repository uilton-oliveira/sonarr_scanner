using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace Sonarr_Scanner
{
    class Program
    {
        private static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private static List<Monitor> monitors = new List<Monitor>();

        static void Main(string[] args)
        {

            Console.WriteLine("Sonarr Scanner starting...");
            if (!Utiliy.IsRunningOnMono())
            {
                Console.WriteLine("Running on .NET");
                ContextMenu menu;
                MenuItem mnuScan;
                MenuItem mnuExit;
                NotifyIcon notificationIcon;
                Thread notifyThread = new Thread(
                    delegate ()
                    {
                        menu = new ContextMenu();

                        mnuScan = new MenuItem("Scan");
                        menu.MenuItems.Add(0, mnuScan);

                        mnuExit = new MenuItem("Exit");
                        menu.MenuItems.Add(1, mnuExit);

                        notificationIcon = new NotifyIcon()
                        {
                            ContextMenu = menu,
                            Text = "Sonarr Scanner"
                        };
                        notificationIcon.Icon = Properties.Resources.icon;
                        mnuExit.Click += new EventHandler(MenuExit_Click);
                        mnuScan.Click += new EventHandler(MenuScan_Click);

                        notificationIcon.Visible = true;
                        Application.Run();
                    }
                );

                notifyThread.Start();
            }
            else
            {

                Console.WriteLine("Running on MONO");
            }



            Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e) {
                Exit();
                e.Cancel = true;
            };

            Console.WriteLine("Loading settings...");
            Settings.Init();

            monitors.Add(new Monitor(Settings.Sonarr, cancellationTokenSource.Token));
            monitors.Add(new Monitor(Settings.Radarr, cancellationTokenSource.Token));

            bool anyTrue = false;
            foreach (Monitor monitor in monitors)
            {
                Console.WriteLine($"Starting {monitor.Settings.Provider()} Monitor");
                if (monitor.Init())
                    anyTrue = true;
            }
            if (!anyTrue)
            {
                var err = "All monitors startup failed, make sure that you configured the config file (settings_radarr.json and/or settings_sonarr.json) correctly, aborting...";
                Console.WriteLine(err);
                if (!Utiliy.IsRunningOnMono())
                {
                    MessageBox.Show(err);
                }
                Exit();
            }

            Task.Delay(TimeSpan.FromMinutes(1), cancellationTokenSource.Token);


        }

        private static void MenuExit_Click(object sender, EventArgs e)
        {
            Exit();
        }

        private static void MenuScan_Click(object sender, EventArgs e)
        {
            foreach (Monitor monitor in monitors)
            {
                Console.WriteLine($"Running manual scan on {monitor.Settings.Provider()}");
                monitor.ScanNow();
            }
        }

        private static void Exit()
        {
            Console.WriteLine("Exiting...");
            cancellationTokenSource.Cancel();
            Application.Exit();
        }
    }
}
