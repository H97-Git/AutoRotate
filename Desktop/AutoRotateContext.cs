using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AutoRotate.Properties;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace AutoRotateWinform
{

    /*
     * Notify Icon : https://www.codeproject.com/tips/627796/doing-a-notifyicon-program-the-right-way
     * Asynch Task : https://blogs.msdn.microsoft.com/benwilli/2016/06/30/asynchronous-infinite-loops-instead-of-timers/
     *
     */
    class AutoRotateContext : ApplicationContext
    {
        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayIconContextMenu;
        private ToolStripMenuItem _closeMenuItem;
        private ToolStripMenuItem _aboutMenuItem;
        private ToolStripMenuItem _startUpMenuItem;

        private Settings _settings = new Settings();
        private static readonly string RunningPath = AppDomain.CurrentDomain.BaseDirectory;
        private readonly string _fileName =
            $"{Path.GetFullPath(Path.Combine(RunningPath, @"..\..\"))}Resources\\settings.json";
        private readonly WebClient _client = new WebClient();
        private string _orientation = "Landscape";

        private const string _raspberryPiIp = "http://0.0.0.0/";

        private AboutForm _aboutForm = new AboutForm();

        public AutoRotateContext()
        {
            Application.ApplicationExit += new EventHandler(this.OnApplicationExit);
            LoadSettings();
            InitializeComponent();
            _trayIcon.Visible = true;
            _trayIcon.BalloonTipText = "AutoRotate is running !";
            _trayIcon.ShowBalloonTip(5000);
        }

        public void ARTask() 
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    CheckOrientation(GetOrientation());
                    await Task.Delay(200);
                }
            });
        }

        public string  GetOrientation()
        {
            string orientation = _client.DownloadString(_raspberryPiIp);
            if (string.IsNullOrEmpty(orientation))
            {
                GetOrientation();
            }
            return orientation;
        }

        public void CheckOrientation(string orientation)
        {
            if (!string.IsNullOrEmpty(orientation))
            {
                if (orientation != _orientation)
                {
                    _orientation = orientation;
                    SendOrientationKeys(_orientation);
                }
            }
        }

        public void SendOrientationKeys(string orientation)
        {
            if (orientation == "Landscape")
            {
                try
                {
                    SendKeys.SendWait("^%{UP}");
                    _trayIcon.BalloonTipText = "The shortcut CTRL+ALT+UP has been sent";
                    _trayIcon.ShowBalloonTip(5000);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
            else if (orientation == "Portrait")
            {
                try
                {
                    SendKeys.SendWait("^%{LEFT}");
                    _trayIcon.BalloonTipText = "The shortcut CTRL+ALT+LEFT has been sent";
                    _trayIcon.ShowBalloonTip(5000);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }
        private void InitializeComponent()
        {
            _trayIcon = new NotifyIcon
            {
                BalloonTipIcon = ToolTipIcon.Info,
                BalloonTipTitle = " AutoRotate ",
                Text = "AutoRotate Rotate the screen with Raspberry Pi",
                Icon = Resources.TrayIcon
            };
            //The icon is added to the project resources.
            //Here I assume that the name of the file is 'TrayIcon.ico'

            //Optional - handle doubleclicks on the icon:
            _trayIcon.DoubleClick += TrayIcon_DoubleClick;

            //Optional - Add a context menu to the TrayIcon:
            _trayIconContextMenu = new ContextMenuStrip();
            _closeMenuItem = new ToolStripMenuItem();
            _aboutMenuItem = new ToolStripMenuItem();
            _startUpMenuItem = new ToolStripMenuItem();
            _trayIconContextMenu.SuspendLayout();

            // 
            // TrayIconContextMenu
            // 
            _trayIconContextMenu.Items.AddRange(new ToolStripItem[] {
                _startUpMenuItem, _aboutMenuItem, _closeMenuItem});
            _trayIconContextMenu.Name = "_trayIconContextMenu";
            _trayIconContextMenu.Size = new Size(153, 70);
            // 
            // CloseMenuItem
            // 
            _closeMenuItem.Name = "_closeMenuItem";
            _closeMenuItem.Size = new Size(152, 22);
            _closeMenuItem.Text = "Close the program";
            _closeMenuItem.Click += new EventHandler(this.CloseMenuItem_Click);

            // 
            // AboutMenuItem
            // 
            _aboutMenuItem.Name = "_aboutMenuItem";
            _aboutMenuItem.Size = new Size(152, 22);
            _aboutMenuItem.Text = "About";
            _aboutMenuItem.Click += new EventHandler(this.AboutMenuItem_Click);
            // 
            // _startUpMenuItem
            // 
            _startUpMenuItem.Name = "_startUpMenuItem";
            _startUpMenuItem.Size = new Size(152, 22);
            _startUpMenuItem.Text = "Start with windows";
            _startUpMenuItem.Click += new EventHandler(this.StartUpMenuItem_Click);
            _startUpMenuItem.Checked = _settings.Checked;

            _trayIconContextMenu.ResumeLayout(false);
            _trayIcon.ContextMenuStrip = _trayIconContextMenu;
            ARTask();

        }

        public void ToggleAboutForm()
        {
            if (_aboutForm.Visible)
            {
                _aboutForm.Visible = false;
            }
            else
            {
                _aboutForm.Visible = true;
            }
        }
        public void LoadSettings()
        {
            StreamReader streamreader = new StreamReader(_fileName);
            var json = streamreader.ReadToEnd();
            _settings = JsonConvert.DeserializeObject<Settings>(json);
            streamreader.Close();
        }
        public void SaveSettings()
        {
            var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
            File.WriteAllText(_fileName, json);
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            //Cleanup so that the icon will be removed when the application is closed
            _trayIcon.Visible = false;
        }
        private void StartUpMenuItem_Click(object sender, EventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (!_startUpMenuItem.Checked)
            {
                rk.SetValue(AppDomain.CurrentDomain.FriendlyName, Application.ExecutablePath);
                _startUpMenuItem.Checked = true;
                _settings.Checked = true;
            }
            else
            {
                rk.DeleteValue(AppDomain.CurrentDomain.FriendlyName, false);
                _startUpMenuItem.Checked = false;
                _settings.Checked = false;
            }
        }
        private void TrayIcon_DoubleClick(object sender, EventArgs e)
        {
            ToggleAboutForm();
        }
        private void AboutMenuItem_Click(object sender, EventArgs e)
        {
            ToggleAboutForm();
        }
        private void CloseMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Do you really want to close ?",
                    "Close", MessageBoxButtons.YesNo, MessageBoxIcon.Stop,
                    MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                SaveSettings();
                Application.Exit();
            }
        }
    }
}
