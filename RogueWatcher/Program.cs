using RogueBot;
using System.Diagnostics;
using System.IO;

namespace RogueWatcher
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: RogueWatcher.exe <option>");
                Console.WriteLine("options:");
                Console.WriteLine("-r report on instances");
                Console.WriteLine("-p <pid> peek at an instance");
                return;
            }

            var option = args[0];
            if (option.Contains("-r"))
            {
                Report(); 
            }
            else if (option.Contains("-p"))
            {
                if (args.Length < 2) 
                {
                    Console.WriteLine("Missing pid for peek");
                    return;
                }

                var pid = int.Parse(args[1]);
                Peek(pid);
            }
            else
            {
                Console.WriteLine($"Invalid option {option}");
            }
        }

        private static void Peek(int pid)
        {
            var process = Process.GetProcessById(pid);
            var console = new ConsoleController(process);

            var maps = console.ReadMap();

            Native.AttachConsole(Native.ATTACH_PARENT_PROCESS);

            Console.WriteLine("=====MAP=====");
            for (int y = 0; y < maps.Length; y++)
            {
                var line = new string(maps[y]);
                Console.WriteLine(line);
            }
        }

        private static void Report()
        {
            var processes = Process.GetProcessesByName("rogue54");

            Console.WriteLine($"PID | Level | Runtime");
            var result = new List<string>();

            foreach (var process in processes)
            {
                try
                {
                    var console = new ConsoleController(process);
                    var map = new Map(console.ReadMap());
                    var player = new Player(map, console);
                    var id = process.Id;
                    var level = player == null ? 0 : player.Level;
                    var runTime = DateTime.Now - process.StartTime;
                    result.Add($"{id} | {level} | {runTime}");
                }
                catch (Exception e) 
                { 
                    Console.WriteLine(e); 
                }
            }


            // --- CRITICAL STEP: Re-attach to YOUR console to print ---
            Native.FreeConsole();
            Native.AttachConsole(Native.ATTACH_PARENT_PROCESS);

            // Re-sync the .NET Console handles (otherwise WriteLine still fails)
            var writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
            Console.SetOut(writer);

            foreach(var line in result)
            {
                Console.WriteLine(line);
            }
        }
    }
}