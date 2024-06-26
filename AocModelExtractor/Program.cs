﻿using AocModelExtractor;
using AocModelExtractor.Extensions;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

// Extension method to handle recursive directory copying
public static class DirectoryExt
{
    public static void Copy(string sourceDir, string destDir, bool copySubDirs)
    {
        // Get the subdirectories for the specified directory.
        DirectoryInfo dir = new DirectoryInfo(sourceDir);
        DirectoryInfo[] dirs = dir.GetDirectories();

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException(
                "Source directory does not exist or could not be found: "
                + sourceDir);
        }

        // If the destination directory doesn't exist, create it.
        if (!Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        // Get the files in the directory and copy them to the new location.
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            string temppath = Path.Combine(destDir, file.Name);
            file.CopyTo(temppath, true);
        }

        // If copying subdirectories, copy them and their contents to new location.
        if (copySubDirs)
        {
            foreach (DirectoryInfo subdir in dirs)
            {
                string temppath = Path.Combine(destDir, subdir.Name);
                Copy(subdir.FullName, temppath, copySubDirs);
            }
        }
    }
}

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

        // Install Cethleann
        await urls["cethleann"]!.DownloadAndExtractAsync(".\\Cethleann", "Cethleann");

        if (!string.IsNullOrEmpty(urls["cethleann-patch"]))
        {
            // Install Cethleann patch
            await urls["cethleann-patch"]!.DownloadAndExtractAsync(".\\Cethleann", "Cethleann Patch");
        }

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
        Console.WriteLine("Extracting RDB Archives. . .");
        Directory.CreateDirectory(".\\extracted-rdb");
        Process.Start($".\\Cethleann\\Cethleann.DataExporter.exe", $"--nyotengu \"{Path.GetFullPath(".\\extracted-rdb")}\" \"{Path.GetFullPath($"{aoc}\\asset")}\"").WaitForExit();

        if (!Directory.Exists(".\\extracted-rdb\\CharacterEditor") || !Directory.Exists(".\\extracted-rdb\\FieldEditor4"))
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("Failed to extract the Character and/or Field Editor. Please review your game dump.");
            Console.ResetColor();
            Console.ReadLine();
            return;
        }

        // Load hash list
        Console.WriteLine("Loading Hash List. . .");
        string[] hashList = Resource.Load("hash-list").ToString().Split("\n");

        // Patch g1t textures
        Console.WriteLine("Patching g1t textures. . .");
        Parallel.ForEach(hashList, hashMap =>
        {
            try
            {
                string[] hashes = hashMap.Split(" ");
                string model = hashes[0];
                string ktid = hashes[1];
                string kidsobjdb = hashes[2];
                string folder = kidsobjdb.StartsWith("CharacterEditor") ? "CharacterEditor" : "FieldEditor4";

                Directory.CreateDirectory($".\\extracted-rdb\\{folder}\\merged\\{model}");
                File.Copy($".\\extracted-rdb\\{folder}\\g1m\\{model}.g1m", $".\\extracted-rdb\\{folder}\\merged\\{model}\\{model}.g1m", true);
                Process.Start($".\\Cethleann\\Nyotengu.KTID.exe", $".\\extracted-rdb\\KIDSSystemResource\\kidsobjdb\\{kidsobjdb} .\\extracted-rdb\\MaterialEditor\\g1t .\\extracted-rdb\\{folder}\\ktid\\{ktid}.ktid").WaitForExit();
                File.Move($".\\extracted-rdb\\{folder}\\ktid\\{ktid}.g1t", $".\\extracted-rdb\\{folder}\\merged\\{model}\\{ktid}.g1t", true);
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

        // Optional: download/install Noesis
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("Do you wish to install Noesis? (Y/n)");
        Console.ResetColor();
        var resp = Console.ReadLine();

        if (resp?.ToLower() != "n")
        {
            // Download Noesis
            await urls["noesis"]!.DownloadAndExtractAsync(".\\Noesis", "Noesis");

            // Download Project G1M
            await urls["project-g1m"]!.DownloadAndExtractAsync(".\\Noesis", "Project G1M");
        }
    }
}
