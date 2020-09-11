using Sylvan.Data;
using Sylvan.Data.Csv;
using System;
using System.Collections;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Sylvan.Tools.CsvZip
{
    class Program
    {
        static void Main(string[] args)
        {
            var rootCommand = new RootCommand
            {
                new Option<string>(
                    "--dir",
                    getDefaultValue: () => ".",
                    description: "The directory to package into a CSVZ"
                ),
                new Option<string>(
                    "--file",
                    description: "The name of the target CSVZ file"                 
                )
            };

            rootCommand.Description = "Packages a CSVZ file.";
            rootCommand.Handler = CommandHandler.Create<string, string>(Package);
            rootCommand.Invoke(args);
        }

        static void Error(string msg) {
            var c = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(msg);
            Console.ForegroundColor = c;
        }

        static IEnumerable<string> GetCsvs(string dir)
        {
            return Directory.EnumerateFiles(dir, "*.csv", SearchOption.TopDirectoryOnly);
        }

        static int Package(string dir, string file)
        {
            dir ??= ".";

            if (!Directory.Exists(dir))
            {
                Error($"Directory '{dir}' doesn't exist.");
                return 1;
            }

            if (GetCsvs(dir).Any() == false)
            {
                Error($"Directory '{dir}' doesn't contain any csvs.");
                return 2;
            }

            if (file == null)
            {
                var d = Path.GetFullPath(dir);
                var f = Path.GetFileName(d);
                file = f + ".csvz";
            }

            Console.WriteLine("Creating: " + file);
            using var csvz = CsvZipPackage.Create(file);
            Stopwatch sw;

            foreach (var csv in Directory.EnumerateFiles(dir, "*.csv", SearchOption.TopDirectoryOnly)) {
                Console.WriteLine($"Processing {Path.GetFileName(csv)}: ");
                var data = CsvDataReader.Create(csv);
                var analyzer = new SchemaAnalyzer();
                Console.Write($"  Analyzing. ");
                sw = Stopwatch.StartNew();
                var result = analyzer.Analyze(data);
                Console.WriteLine(sw.Elapsed.ToString());

                var schema = result.GetSchema();
                var csvSchema = new CsvSchema(schema);

                var csvOpts = new CsvDataReaderOptions { Schema = csvSchema };
                Console.Write($"  Writing.   ");
                data = CsvDataReader.Create(csv, csvOpts);
                sw = Stopwatch.StartNew();
                var entry = csvz.CreateEntry(Path.GetFileName(csv));
                entry.WriteData(data);
                Console.WriteLine(sw.Elapsed.ToString());
            }
            Console.WriteLine("Done.")
            return 0;
        }
    }
}
