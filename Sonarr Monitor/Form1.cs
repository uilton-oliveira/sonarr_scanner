using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sonarr_Monitor
{
    public partial class Form1 : Form
    {
        bool alertDisplayed = false;
        CancellationTokenSource cancellationTokenSource = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void SaveSettings_Click(object sender, EventArgs e)
        {
            var settings = Properties.Settings.Default;
            if (interval.Value < 10)
            {
                interval.Value = 10;
            }

            settings.interval = interval.Value;
            settings.url = url.Text;
            settings.apiKey = apiKey.Text;
            settings.Save();
            
            StartSonarrThread();
        }

        private void toggleVisible()
        {
            var settings = Properties.Settings.Default;
            if (!this.Visible)
            {
                this.ShowInTaskbar = true;
                this.Opacity = 100;
                this.Show();
            }
            else
            {
                this.ShowInTaskbar = false;
                this.Opacity = 0;
                this.Hide();
            }
            settings.visible = this.Visible;
            settings.Save();
        }

        private void openSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toggleVisible();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == this.WindowState)
            {
                if (!alertDisplayed)
                {
                    alertDisplayed = true;
                    notifyIcon1.ShowBalloonTip(2);
                    toggleVisible();
                }
            }
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            toggleVisible();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            var settings = Properties.Settings.Default;
            settings.MainWindowPlacement = WindowPlacement.GetPlacement(this.Handle);
            settings.visible = this.Visible;
            settings.Save();

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var settings = Properties.Settings.Default;
            interval.Value = settings.interval;
            url.Text = settings.url;
            apiKey.Text = settings.apiKey;
            BeginInvoke(new MethodInvoker(delegate
            {
                this.Visible = Properties.Settings.Default.visible;
                StartSonarrThread();
            }));
            
        }

        private void StartSonarrThread()
        {
            var oldValue = Sonarr.currentInterval;
            var newValue = Properties.Settings.Default.interval;
            var changed = Properties.Settings.Default.interval != Sonarr.currentInterval;
            Debug.WriteLine($"oldValue: {oldValue} / newValue: {newValue} / changed: {changed}");
            if (changed)
            {
                if (cancellationTokenSource != null)
                    cancellationTokenSource.Cancel();
                cancellationTokenSource = new CancellationTokenSource();
                
            } else
            {
                return;
            }

            BeginInvoke(new MethodInvoker(async delegate
            {
                try
                {
                    Debug.WriteLine("Starting Sonarr Monitor Thread");
                    await Sonarr.StartMonitor(TimeSpan.FromMinutes((double)Properties.Settings.Default.interval), cancellationTokenSource.Token);
                }
                catch (TaskCanceledException ex)
                {
                    Debug.WriteLine("Sonarr Monitor Thread cancelled");
                }
            }));
        }

        protected override void OnLoad(EventArgs e)
        {
            var settings = Properties.Settings.Default;
            WindowPlacement.SetPlacement(this.Handle, settings.MainWindowPlacement);
            if (!settings.visible)
            {
                this.Visible = false; // Hide form window.
                this.ShowInTaskbar = false; // Remove from taskbar.
                this.Opacity = 0;
            }
            base.OnLoad(e);
        }
    }
    
}
