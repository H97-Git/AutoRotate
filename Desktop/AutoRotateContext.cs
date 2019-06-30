using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AutoRotateWinform
{

    /*
     * Notify Icon : https://www.codeproject.com/tips/627796/doing-a-notifyicon-program-the-right-way
     * Asynch Task : https://blogs.msdn.microsoft.com/benwilli/2016/06/30/asynchronous-infinite-loops-instead-of-timers/
     * <div>Icons made by <a href="https://www.freepik.com/" title="Freepik">Freepik</a> from <a href="https://www.flaticon.com/"                 title="Flaticon">www.flaticon.com</a> is licensed by <a href="http://creativecommons.org/licenses/by/3.0/"                 title="Creative Commons BY 3.0" target="_blank">CC 3.0 BY</a></div>
     *
     */
    class AutoRotateContext : ApplicationContext
    {
        private NotifyIcon TrayIcon;
        private ContextMenuStrip TrayIconContextMenu;
        private ToolStripMenuItem CloseMenuItem;
        private ToolStripMenuItem AboutMenuItem;
        private ToolStripMenuItem StartUpMenuItem;

        readonly WebClient _client = new WebClient();
        private string _orientation = "Landscape";

        public AutoRotateContext()
        {
            Application.ApplicationExit += new EventHandler(this.OnApplicationExit);
            InitializeComponent();
            TrayIcon.Visible = true;
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
            string orientation = _client.DownloadString("http://192.168.0.47/");
            if(orientation == "")
            {
               GetOrientation();
            }
            else
            {
                return orientation;
            }
            return "Landscape";
        }
        public void SendOrientationKeys(string orientation)
        {
            if (orientation == "Landscape")
            {
                try
                {
                    SendKeys.SendWait("^%{UP}");
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
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        public void CheckOrientation(string orientation)
        {
            if (orientation != _orientation)
            {
                _orientation = orientation;
                SendOrientationKeys(_orientation);
            }
        }
        private void InitializeComponent()
        {
            TrayIcon = new NotifyIcon
            {
                BalloonTipIcon = ToolTipIcon.Info,
                BalloonTipText = "I noticed that you double-clicked me! What can I do for you?",
                BalloonTipTitle = "You called Master?",
                Text = "AutoRotate Rotate the screen with Raspberry Pi",
                Icon = Properties.Resources.TrayIcon
            };

            //The icon is added to the project resources.
            //Here I assume that the name of the file is 'TrayIcon.ico'

            //Optional - handle doubleclicks on the icon:
            TrayIcon.DoubleClick += TrayIcon_DoubleClick;

            //Optional - Add a context menu to the TrayIcon:
            TrayIconContextMenu = new ContextMenuStrip();
            CloseMenuItem = new ToolStripMenuItem();
            AboutMenuItem = new ToolStripMenuItem();
            StartUpMenuItem = new ToolStripMenuItem();
            TrayIconContextMenu.SuspendLayout();

            // 
            // TrayIconContextMenu
            // 
            TrayIconContextMenu.Items.AddRange(new ToolStripItem[] {
                StartUpMenuItem, AboutMenuItem, CloseMenuItem});
            TrayIconContextMenu.Name = "TrayIconContextMenu";
            TrayIconContextMenu.Size = new Size(153, 70);
            // 
            // CloseMenuItem
            // 
            CloseMenuItem.Name = "CloseMenuItem";
            CloseMenuItem.Size = new Size(152, 22);
            CloseMenuItem.Text = "Close the program";
            CloseMenuItem.Click += new EventHandler(this.CloseMenuItem_Click);

            // 
            // AboutMenuItem
            // 
            AboutMenuItem.Name = "AboutMenuItem";
            AboutMenuItem.Size = new Size(152, 22);
            AboutMenuItem.Text = "About";
            AboutMenuItem.Click += new EventHandler(this.AboutMenuItem_Click);
            // 
            // StartUpMenuItem
            // 
            StartUpMenuItem.Name = "StartUpMenuItem";
            StartUpMenuItem.Size = new Size(152, 22);
            StartUpMenuItem.Text = "Start with windows";
            StartUpMenuItem.Click += new EventHandler(this.StartUpMenuItem_Click);
            //StartUpMenuItem.Checked = Properties.Settings.Default.SWWChecked;

            TrayIconContextMenu.ResumeLayout(false);
            TrayIcon.ContextMenuStrip = TrayIconContextMenu;
            ARTask();

        }
        private void OnApplicationExit(object sender, EventArgs e)
        {
            //Cleanup so that the icon will be removed when the application is closed
            TrayIcon.Visible = false;
        }
        private void TrayIcon_DoubleClick(object sender, EventArgs e)
        {
            TrayIcon.ShowBalloonTip(10000);
        }
        private void StartUpMenuItem_Click(object sender, EventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (!StartUpMenuItem.Checked)
            {
                rk.SetValue(AppDomain.CurrentDomain.FriendlyName, Application.ExecutablePath);
                StartUpMenuItem.Checked = true;
                //Properties.Settings.Default.SWWChecked = true;
            }
            else
            {
                rk.DeleteValue(AppDomain.CurrentDomain.FriendlyName, false);
                StartUpMenuItem.Checked = false;
                //Properties.Settings.Default.SWWChecked = false;
            }
        }
        private void AboutMenuItem_Click(object sender, EventArgs e)
        {
            Form1 form1 = new Form1();
            form1.Show();
        }
        private void CloseMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Do you really want to close ?",
                    "Close", MessageBoxButtons.YesNo, MessageBoxIcon.Stop,
                    MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                Application.Exit();
            }
        }
    }
}
