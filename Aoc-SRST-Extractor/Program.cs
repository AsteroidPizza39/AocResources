using AocModelExtractor;
using AocModelExtractor.Extensions;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;


class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("Please specify the path to your Age of Calamity game files.");
            Console.ResetColor();
            Console.ReadLine();
            return;
        }

        long timeStamp = DateTime.Now.ToBinary();
        string log = "";
        string aoc = args[0];
        var urls = Resource.Load("urls.json").ParseJson<Dictionary<string, string?>>()!;

        // Install QuickBMS
        await urls["quickbms"]!.DownloadAndExtractAsync(".\\QuickBMS", "QuickBMS");


        // Merge directories [Recursive]
        string[] dirs = Directory.GetDirectories(aoc);
        if (dirs.Select(x => Path.GetFileName(x)).Contains("01002B00111A2000"))
        {
            Console.WriteLine("Merging AoC directories. . .");
            foreach (var dir in dirs.Where(x => Path.GetFileName(x).StartsWith("01002B00111A")))
            {
                DirectoryExt.Copy(Path.Combine(dir, "romfs"), ".\\romfs", true);
            }
            aoc = ".\\romfs";
        }

        // Run Cethleann extractor
        Console.WriteLine("Extracting Audio. . .");
        Directory.CreateDirectory(".\\extracted-KNS");
        Process.Start($".\\QuickBMS\\quickbms.exe", $".\\srst.bms \"{Path.GetFullPath(".\\extracted-KNS")}\"").WaitForExit();


            }
            catch (Exception ex)
            {
                string exs = $"[{DateTime.Now}] {ex}\n";
                log += exs;
                Console.WriteLine(exs);
            }
        });

        // Print logs
        File.WriteAllText($".\\log-{timeStamp}.txt", log);


    }
}
