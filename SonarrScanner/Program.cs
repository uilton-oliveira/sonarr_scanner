using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace Sonarr_Scanner
{
    class Program
    {
        private static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        
        static void Main(string[] args)
        {

            Console.WriteLine("Sonarr Scanner starting...");
            if (!Utiliy.IsRunningOnMono())
            {
                Console.WriteLine("Running on .NET");
                ContextMenu menu;
                MenuItem mnuExit;
                NotifyIcon notificationIcon;
                Thread notifyThread = new Thread(
                    delegate ()
                    {
                        menu = new ContextMenu();
                        mnuExit = new MenuItem("Exit");
                        menu.MenuItems.Add(0, mnuExit);

                        notificationIcon = new NotifyIcon()
                        {
                            ContextMenu = menu,
                            Text = "Main"
                        };
                        notificationIcon.Icon = Properties.Resources.icon;
                        mnuExit.Click += new EventHandler(MenuExit_Click);

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
            Console.WriteLine("Starting Sonarr Monitor");
            bool sonarr = new Monitor(Settings.Sonarr, cancellationTokenSource.Token).Init();
            Console.WriteLine("Starting Radarr Monitor");
            bool radarr = new Monitor(Settings.Radarr, cancellationTokenSource.Token).Init();

            if (!sonarr && !radarr)
            {
                Console.WriteLine("Both sonarr and radarr startup failed, aborting...");
                Exit();
            }

            Task.Delay(TimeSpan.FromMinutes(1), cancellationTokenSource.Token);


        }

        private static void MenuExit_Click(object sender, EventArgs e)
        {
            Exit();
        }

        private static void Exit()
        {
            Console.WriteLine("Exiting...");
            cancellationTokenSource.Cancel();
            Application.Exit();
        }
    }
}
