using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;

namespace ProtectProcess
{
    //inspired from here: http://csharptest.net/1051/managed-anti-debugging-how-to-prevent-users-from-attaching-a-debugger/index.html
    public static class AntiDebug
    {
        // Debugging thread main loop
        static void DebuggerThread(object arg)
        {
            DEBUG_EVENT evt = new DEBUG_EVENT();
            evt.bytes = new byte[1024];
            // Attach to the process we provided the thread as an argument
            if (!DebugActiveProcess((int)arg))
                throw new Win32Exception();

            while (true)
            {
                // wait for a debug event
                if (!WaitForDebugEvent(out evt, -1))
                    throw new Win32Exception();
                // return DBG_CONTINUE for all events but the exception type
                int continueFlag = DBG_CONTINUE;
                if (evt.dwDebugEventCode == DebugEventType.EXCEPTION_DEBUG_EVENT)
                    continueFlag = DBG_EXCEPTION_NOT_HANDLED;
                // continue running the debugee
                ContinueDebugEvent(evt.dwProcessId, evt.dwThreadId, continueFlag);
            }
        }

        public static void DebugSelf(int ppid)
        {
            Console.WriteLine("Debugging {0}", ppid);
            Process self = Process.GetCurrentProcess();
            // Child process?
            if (ppid != 0)
            {
                Console.WriteLine("Child Process");
                Process pdbg = Process.GetProcessById(ppid);
                new Thread(KillOnExit) { IsBackground = true, Name = "KillOnExit" }.Start(pdbg);
                //Wait for our parent to debug us
                WaitForDebugger();
                //Start debugging our parent process
                DebuggerThread(ppid);
                //Now is a good time to die.
                Environment.Exit(1);
            }
            else // else we are the Parent process...
            {
                Console.WriteLine("Parent Process");
                ProcessStartInfo psi =
                new ProcessStartInfo(Environment.GetCommandLineArgs()[0], self.Id.ToString())
                {
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    ErrorDialog = false,
                    //WindowStyle = ProcessWindowStyle.Hidden
                };
                // Start the child process
                Process pdbg = Process.Start(psi);
                if (pdbg == null)
                    throw new ApplicationException("Unable to debug");
                // Monitor the child process
                new Thread(KillOnExit) { IsBackground = true, Name = "KillOnExit" }.Start(pdbg);
                // Debug the child process
                new Thread(DebuggerThread) { IsBackground = true, Name = "DebuggerThread" }.Start(pdbg.Id);
                // Wait for the child to debug us
                WaitForDebugger();
            }
        }
        static void WaitForDebugger()
        {
            DateTime start = DateTime.Now;
            while (!IsDebuggerPresent())
            {
                if ((DateTime.Now - start).TotalMinutes > 1)
                    throw new TimeoutException("Debug operation timeout.");
                Thread.Sleep(1);
            }
        }
        static void KillOnExit(object process)
        {
            ((Process)process).WaitForExit();
            Environment.Exit(1);
        }

        const int DBG_CONTINUE = 0x00010002;
        const int DBG_EXCEPTION_NOT_HANDLED = unchecked((int)0x80010001);

        enum DebugEventType : int
        {
            CREATE_PROCESS_DEBUG_EVENT = 3, //Reports a create-process debugging event. The value of u.CreateProcessInfo specifies a CREATE_PROCESS_DEBUG_INFO structure.
            CREATE_THREAD_DEBUG_EVENT = 2, //Reports a create-thread debugging event. The value of u.CreateThread specifies a CREATE_THREAD_DEBUG_INFO structure.
            EXCEPTION_DEBUG_EVENT = 1, //Reports an exception debugging event. The value of u.Exception specifies an EXCEPTION_DEBUG_INFO structure.
            EXIT_PROCESS_DEBUG_EVENT = 5, //Reports an exit-process debugging event. The value of u.ExitProcess specifies an EXIT_PROCESS_DEBUG_INFO structure.
            EXIT_THREAD_DEBUG_EVENT = 4, //Reports an exit-thread debugging event. The value of u.ExitThread specifies an EXIT_THREAD_DEBUG_INFO structure.
            LOAD_DLL_DEBUG_EVENT = 6, //Reports a load-dynamic-link-library (DLL) debugging event. The value of u.LoadDll specifies a LOAD_DLL_DEBUG_INFO structure.
            OUTPUT_DEBUG_STRING_EVENT = 8, //Reports an output-debugging-string debugging event. The value of u.DebugString specifies an OUTPUT_DEBUG_STRING_INFO structure.
            RIP_EVENT = 9, //Reports a RIP-debugging event (system debugging error). The value of u.RipInfo specifies a RIP_INFO structure.
            UNLOAD_DLL_DEBUG_EVENT = 7, //Reports an unload-DLL debugging event. The value of u.UnloadDll specifies an UNLOAD_DLL_DEBUG_INFO structure.
        }

        [StructLayout(LayoutKind.Sequential)]
        struct DEBUG_EVENT
        {
            [MarshalAs(UnmanagedType.I4)]
            public DebugEventType dwDebugEventCode;
            public int dwProcessId;
            public int dwThreadId;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
            public byte[] bytes;
        }

        [DllImport("Kernel32.dll", SetLastError = true)]
        static extern bool DebugActiveProcess(int dwProcessId);
        [DllImport("Kernel32.dll", SetLastError = true)]
        static extern bool WaitForDebugEvent([Out] out DEBUG_EVENT lpDebugEvent, int dwMilliseconds);
        [DllImport("Kernel32.dll", SetLastError = true)]
        static extern bool ContinueDebugEvent(int dwProcessId, int dwThreadId, int dwContinueStatus);
        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern bool IsDebuggerPresent();
    }
}
