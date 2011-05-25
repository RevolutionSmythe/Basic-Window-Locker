using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace ConsoleApplication1
{
    class Program
    {
        private const int SW_HIDE = 0;

        [DllImport ("user32.dll")]
        public static extern IntPtr FindWindow (string lpClassName, string lpWindowName);

        [DllImport ("user32.dll")]
        static extern bool ShowWindow (IntPtr hWnd, int nCmdShow);
        static void Main (string[] args)
        {
            IntPtr hWnd = FindWindow (null, Console.Title); //Hide our window
            if (hWnd != IntPtr.Zero)
                ShowWindow (hWnd, SW_HIDE);

            Thread t = new Thread (RunThread);
            t.Start ();
        }

        public static void RunThread ()
        {
            ProcessStartInfo startInfo;
            Process p;
            while (true)
            {
                //Kill any existing
                Process[] allProcesses = Process.GetProcessesByName ("WindowLocker");
                foreach (Process pr in allProcesses)
                {
                    pr.Kill ();
                }
                startInfo = new ProcessStartInfo (Path.Combine (Environment.CurrentDirectory, "WindowLocker.exe"), "");
                startInfo.UseShellExecute = true;
                startInfo.CreateNoWindow = false;
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                p = Process.Start (startInfo);
                p.WaitForExit ();
                try
                {
                    p.Kill ();
                }
                catch (InvalidOperationException)
                {
                }
                Console.WriteLine ("Got exit code " + p.ExitCode);
                if (p.ExitCode == 4323)
                {
                    break;
                }
            }
            //Unlock everything forcefully
            startInfo = new ProcessStartInfo (Path.Combine (Environment.CurrentDirectory, "WindowLocker.exe"), "unlock");
            startInfo.UseShellExecute = true;
            startInfo.CreateNoWindow = false;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p = Process.Start (startInfo);
            p.WaitForExit ();
        }
    }
}
