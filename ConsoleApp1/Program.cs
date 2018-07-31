using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            int ppid = 0;
            if (args != null && args.Length > 0)
            {
                int.TryParse(args[0], out ppid);
            }
            ProtectProcess.AntiDebug.DebugSelf(ppid);
            Console.WriteLine("Self-Debugging...");
            Console.ReadLine();
        }
    }
}
