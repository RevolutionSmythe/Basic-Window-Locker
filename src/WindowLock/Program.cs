using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;
using Microsoft.Win32;

namespace WindowLocker
{
    class Program
    {
        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOWMINIMIZED = 2;
        private const int SW_SHOWMAXIMIZED = 3;
        private static List<string> ProcessesToLock = new List<string> ();
        private static Timer Timer = new Timer ();
        private static bool IsLocked = false;
        private static bool IsBlackList = true;
        private static string Password = "";

        [DllImport ("user32.dll")]
        private static extern bool ShowWindowAsync (IntPtr hWnd, int nCmdShow);
        [DllImport ("user32.dll")]
        static extern bool SetForegroundWindow (IntPtr hWnd);

        static void Main (string[] args)
        {
            bool shouldLock = true;
            RegistryKey key = Registry.ClassesRoot.OpenSubKey ("WindowLocker\\Pass.txt");
            if (key == null)
            {
                key = Registry.ClassesRoot.CreateSubKey ("WindowLocker\\Pass.txt");
                key.SetValue ("Pass", ReadPass ());
                Console.WriteLine ();
                shouldLock = false;
            }
            Password = key.GetValue ("Pass").ToString ();
            key.Close ();
            key = Registry.ClassesRoot.OpenSubKey ("WindowLocker\\IsBlackList.txt");
            if (key == null)
            {
                key = Registry.ClassesRoot.CreateSubKey ("WindowLocker\\IsBlackList.txt");
                key.SetValue ("IsBlackList", IsBlackList ? 1 : 0);
            }
            IsBlackList = key.GetValue ("IsBlackList").ToString () == "1";
            key.Close ();
            key = Registry.ClassesRoot.OpenSubKey ("WindowLocker\\Info.txt");
            if (key != null)
            {
                string programs = key.GetValue ("BlockedPrograms").ToString ();
                ProcessesToLock = new List<string> (programs.Split (new string[1] { "," }, StringSplitOptions.RemoveEmptyEntries));
                key.Close ();
            }
            else
            {
                ProcessesToLock.Add ("Skype");
                ProcessesToLock.Add ("IceChat7");
                WriteProcessRegKey ();
            }
            if (args.Contains ("unlock"))
            {
                UnlockPrograms ();
                return;
            }
            Timer.Interval = 100;
            Timer.Elapsed += new ElapsedEventHandler (Timer_Elapsed);
            if (shouldLock)
            {
                Timer.Start ();
                IsLocked = true;
            }
            else
                Console.WriteLine ("Welcome, type help to get started");

            while (true)
            {
                string command = Console.ReadLine ();
                switch (command)
                {
                    case "lock":
                        if (IsLocked)
                            break;
                        Timer.Start ();
                        IsLocked = true;
                        break;
                    case "clear":
                        Console.Clear ();
                        break;
                    case "change pass":
                        if (IsLocked)
                            break;
                        if (ReadPass ("Old Pass: ") != Password)
                        {
                            Console.WriteLine ("Wrong");
                            return;
                        }
                        key = Registry.ClassesRoot.CreateSubKey ("WindowLocker\\Pass.txt");
                        key.SetValue ("Pass", (Password = ReadPass ("New Pass: ")));
                        key.Close ();
                        break;
                    case "change list type":
                        if (IsLocked)
                            break;
                        Console.Write ("Type (blacklist or whitelist): ");
                        switch (Console.ReadLine ())
                        {
                            case "whitelist":
                                IsBlackList = false;
                                ProcessesToLock.Clear ();
                                break;
                            case "blacklist":
                                IsBlackList = true;
                                ProcessesToLock.Clear ();
                                break;
                            default:
                                break;
                        }
                        Console.WriteLine ();
                        key = Registry.ClassesRoot.CreateSubKey ("WindowLocker\\IsBlackList.txt");
                        key.SetValue ("IsBlackList", IsBlackList ? 1 : 0);
                        WriteProcessRegKey ();
                        break;
                    case "help":
                        Console.WriteLine ("lock - locks the programs");
                        Console.WriteLine ("unlock - unlocks the programs");
                        Console.WriteLine ("quit - quits the locking application and unlocks all programs");
                        Console.WriteLine ("add program - adds a new program to the list of programs to block");
                        Console.WriteLine ("remove program - removes a program from the list of programs to block");
                        Console.WriteLine ("show programs - shows all programs in list of programs to block");
                        Console.WriteLine ("show list type - shows whether the list is a whitelist or a blacklist");
                        Console.WriteLine ("change pass - changes the password for the program");
                        Console.WriteLine ("change list type - changes the programs to block to a whitelist or a blacklist");
                        break;
                    case "show programs":
                        if (IsLocked)
                        {
                            if (Password != ReadPass ())
                            {
                                Console.WriteLine ("Wrong");
                                break;
                            }
                        }
                        Console.WriteLine ("Blocked Programs:");
                        foreach (string program in ProcessesToLock)
                        {
                            Console.WriteLine (program);
                        }
                        break;
                    case "show list type":
                        if (IsLocked)
                        {
                            if (Password != ReadPass ())
                            {
                                Console.WriteLine ("Wrong");
                                break;
                            }
                        }
                        Console.WriteLine ("List Type: " + (IsBlackList ? "blacklist" : "whitelist"));
                        break;
                    case "unlock":
                        if (!IsLocked)
                        {
                            UnlockPrograms ();
                            break;
                        }
                        if (Password != ReadPass ())
                        {
                            Console.WriteLine ("Wrong");
                            break;
                        }
                        IsLocked = false;
                        Timer.Stop ();
                        UnlockPrograms ();
                        break;
                    case "quit":
                        if (Password != ReadPass ())
                        {
                            Console.WriteLine ("Wrong");
                            break;
                        }
                        Environment.Exit (4323);
                        break;
                    case "add program":
                        if (IsLocked)
                        {
                            if (Password != ReadPass ())
                            {
                                Console.WriteLine ("Wrong");
                                break;
                            }
                        }
                        Console.Write ("Program title to add : ");
                        string title = Console.ReadLine ();
                        Process[] processes = Process.GetProcesses ();
                        bool found = false;
                        foreach (Process p in processes)
                        {
                            if (p.MainWindowTitle.Contains (title))
                            {
                                ProcessesToLock.Add (p.ProcessName);
                                Console.WriteLine ("found " + p.MainWindowTitle);
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                            Console.WriteLine ("not found");
                        else
                        WriteProcessRegKey ();
                        break;
                    case "remove program":
                        if (IsLocked)
                        {
                            if (Password != ReadPass())
                            {
                                Console.WriteLine ("Wrong");
                                break;
                            }
                        }
                        Console.Write ("Program to remove : ");
                        string rTitle = Console.ReadLine ();
                        foreach (string p in ProcessesToLock)
                        {
                            if (p.Contains (rTitle))
                            {
                                rTitle = p;
                                Console.WriteLine ("found " + rTitle);
                                break;
                            }
                        }
                        ProcessesToLock.Remove (rTitle);
                        WriteProcessRegKey ();
                        break;
                    default:
                        break;
                }
            }
        }

        private static string ReadPass ()
        {
            return ReadPass ("Pass: ");
        }

        private static string ReadPass (string pass)
        {
            Console.Write (pass);
            string rPass = "";
            while (true)
            {
                ConsoleKeyInfo info = Console.ReadKey (true);
                if (info.Key == ConsoleKey.Enter)
                {
                    for (int i = rPass.Length; i < 20; i++)
                        Console.Write ('*');

                    break;
                }
                if (info.Key == ConsoleKey.Backspace)
                {
                    if (rPass.Length != 0)
                    {
                        rPass = rPass.Remove (rPass.Length - 1);
                        Console.SetCursorPosition (Console.CursorLeft - 1, Console.CursorTop);
                        Console.Write (' ');
                        Console.SetCursorPosition (Console.CursorLeft - 1, Console.CursorTop);
                    }
                    continue;
                }
                Console.Write ('*');
                rPass += info.KeyChar;
            }
            Console.WriteLine ();
            return rPass;
        }

        private static void WriteProcessRegKey ()
        {
            RegistryKey key = Registry.ClassesRoot.CreateSubKey ("WindowLocker\\Info.txt");
            string s = "";
            for(int i = 0; i < ProcessesToLock.Count; i++)
                if(i + 1 != ProcessesToLock.Count)
                    s += ProcessesToLock[i] + ",";
                else
                    s += ProcessesToLock[i];

            key.SetValue ("BlockedPrograms", s);
            key.Close ();
        }

        static void Timer_Elapsed (object sender, System.Timers.ElapsedEventArgs e)
        {
            LockPrograms ();
        }

        public static void LockPrograms ()
        {
            if (IsBlackList)
            {
                foreach (string proc in ProcessesToLock)
                {
                    Process[] processes = Process.GetProcessesByName (proc);
                    if (processes.Length > 0)
                    {
                        foreach (Process p in processes)
                        {
                            //SetForegroundWindow (processes[0].MainWindowHandle);
                            ShowWindowAsync (p.MainWindowHandle, SW_SHOWMINIMIZED);
                        }
                    }
                    else
                        Console.WriteLine ("Could not find process " + proc);
                }
            }
            else
            {
                Process[] processes = Process.GetProcesses ();
                Process ourProcess = Process.GetCurrentProcess ();
                foreach (Process p in processes)
                {
                    if (p.Id == ourProcess.Id)
                        continue;
                    if (p.ProcessName == "explorer")
                        continue;
                    bool ok = false;
                    foreach (string proc in ProcessesToLock)
                    {
                        if (p.ProcessName.Contains (proc))
                        {
                            ok = true;
                            break;
                        }
                    }
                    if (!ok)
                    {
                        //SetForegroundWindow (processes[0].MainWindowHandle);
                        ShowWindowAsync (p.MainWindowHandle, SW_SHOWMINIMIZED);
                    }
                }
            }
        }

        public static void UnlockPrograms ()
        {
            if (IsBlackList)
            {
                foreach (string proc in ProcessesToLock)
                {
                    Process[] processes = Process.GetProcessesByName (proc);
                    if (processes.Length > 0)
                    {
                        foreach (Process p in processes)
                        {
                            SetForegroundWindow (p.MainWindowHandle);
                            ShowWindowAsync (p.MainWindowHandle, SW_SHOWMAXIMIZED);
                        }
                    }
                    else
                        Console.WriteLine ("Could not find process " + proc);
                }
            }
        }
    }
}
