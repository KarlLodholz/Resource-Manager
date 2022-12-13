using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using System.Management;

namespace Resource_Manager
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayApp());
        }
    }

    public class TrayApp : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private bool managed;
        private ManagementEventWatcher startWatch;
        private ManagementEventWatcher stopWatch;
        private string cmd_str = @"""C:\Program Files (x86)\Steam\steamapps\common\wallpaper_engine\wallpaper32.exe""";
        private List<string> intensive_programs = new List<string>() { "League of Lege", "VALORANT"};
        public TrayApp()
        {
            // Initialize Tray Icon
            trayIcon = new NotifyIcon()
            {
                Icon = new System.Drawing.Icon("C:\\Users\\Lodho\\Documents\\Code\\C#\\Resource Manager\\Resource Manager\\Resources\\AppIcon.ico"),
                ContextMenu = new ContextMenu(new MenuItem[] {
                    new MenuItem("Exit", Exit)
                }),
                Visible = true
            };
            managed = false;
            WaitForProcess();
        }

        void ToggleManage()
        {
            string cmd_args;
            if (managed) //resources no longer need to be managed
            {
                //unpause wallpaperengine
                cmd_args = "-control play";

                //start the selected processes
                Process.Start(@"""C:\Program Files\PowerToys\PowerToys.exe""");

            }
            else        //when resources need to be managed
            {
                //pause wallpaperengine
                cmd_args = "-control pause";

                //kill the selected processes
                Process[] arr_p = Process.GetProcessesByName("PowerToys");
                for (int i = 0; i < arr_p.Length; i++)
                {
                    //Console.WriteLine(arr_p[i].ToString());
                    arr_p[i].Kill();
                }


            }

            // toggle wallpaperengine
            ProcessStartInfo psi = new ProcessStartInfo(cmd_str);
            psi.Arguments = cmd_args;
            Process.Start(psi);

            managed = !managed;
        }

        void WaitForProcess()
        {
            this.startWatch = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
            this.startWatch.EventArrived += new EventArrivedEventHandler(startWatch_EventArrived);
            this.startWatch.Start();

            this.stopWatch = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace"));
            this.stopWatch.EventArrived += new EventArrivedEventHandler(stopWatch_EventArrived);
            // this does not need to be started because when the program boots up, there will not be intensive progragms
            // this.stopWatch.Start();
        }

        void stopWatch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            //this.stopWatch.Stop();
            string p = e.NewEvent.Properties["ProcessName"].Value.ToString();
            Console.WriteLine("Process stopped: {0}", p);
            if(isIntensive(p))
            {
                // check all running processes and make sure none are intensive
                bool intsv = false;
                Process[] running = Process.GetProcesses();
                foreach (Process r in running)
                {
                    if (isIntensive(r.ToString()))
                    {
                        intsv = true;
                        break;
                    }
                }
                if (!intsv)
                {
                    // stop looking for new things that could be intensive because its already running
                    this.stopWatch.Stop();
                    // start looking for when the intensive tasks are done
                    this.startWatch.Start();
                    ToggleManage();
                }
                
            }
        }

        void startWatch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            //this.startWatch.Stop();
            string p = e.NewEvent.Properties["ProcessName"].Value.ToString();
            Console.WriteLine("Process stopped: {0}", p);
            if (isIntensive(p))
            {
                ToggleManage();
                // stop looking for starting processes
                this.startWatch.Stop();
                // start looking for ending processes
                this.stopWatch.Start();
            }
        }

        bool isIntensive(string p)
        {
            foreach (string ip in intensive_programs)
            {
                if (p.Contains(ip)) //return true if the program passed was the same as one of the intensive programs
                {
                    return true;
                }
            }
            return false;
        }

        void Exit(object sender, EventArgs e)
        {
            // Hide tray icon, otherwise it will remain shown until user mouses over it
            trayIcon.Visible = false;
            // Quit the program
            Application.Exit();
        }
    }
}
