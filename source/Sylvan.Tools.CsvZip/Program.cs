﻿using ConsoleTables;
using Sylvan.Data;
using Sylvan.Data.Csv;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Sylvan.Tools.CsvZip
{
    public class Program
    {
        // params, so it can be easily called from unit tests.
        public static void Main(params string[] args)
        {
            var rootCommand = new RootCommand
            {
                new Argument<string>(
                    "--dir",
                    getDefaultValue: () => ".",
                    description: "The directory to package into a CSVZ"
                ),
                new Argument<string>(
                    "--file",
                    getDefaultValue: () => null,
                    description: "The name of the target CSVZ file"
                )
            };

            rootCommand.Description = "Packages a CSVZ file.";
            rootCommand.Handler = CommandHandler.Create<string, string>(Package);

            var addCmd = new Command("add")
            {
                 new Argument<string>(
                    "--file",
                    getDefaultValue: () => null,
                    description: "The csv file from which to remove a table."
                ),
                new Argument<string>(
                    "--name",
                    getDefaultValue: () => null,
                    description: "The name of the table to remove."
                ),
                new Option<bool>(
                    "--overwrite",
                    getDefaultValue: () => false,
                    description: "Wether to overwrite an existing table."
                )
            };
            addCmd.Handler = CommandHandler.Create<string, string, bool>(Add);
            rootCommand.Add(addCmd);

            var remCmd = new Command("remove")
            {
                new Argument<string>(
                    "--file",
                    getDefaultValue: () => null,
                    description: "The csv file from which to remove a table."
                ),
                new Argument<string>(
                    "--name",
                    getDefaultValue: () => null,
                    description: "The name of the table to remove."
                )
            };
            remCmd.Handler = CommandHandler.Create<string, string>(Remove);
            rootCommand.Add(remCmd);

            var tablesCmd = new Command("tables")
            {
                new Argument<string>(
                    "--file",
                    getDefaultValue: () => null,
                    description: "The csv file to list table."
                )               
            };
            tablesCmd.Handler = CommandHandler.Create<string>(Tables);
            rootCommand.Add(tablesCmd);

            var columnsCmd = new Command("columns")
            {
                new Argument<string>(
                    "--file",
                    getDefaultValue: () => null,
                    description: "The csv file from which to list columns."
                ),
                 new Argument<string>(
                    "--name",
                    getDefaultValue: () => null,
                    description: "The table from which to list columns."
                )
            };
            columnsCmd.Handler = CommandHandler.Create<string, string>(Columns);
            rootCommand.Add(columnsCmd);

            rootCommand.Invoke(args);
        }

        static int Tables(string file)
        {
            if (!File.Exists(file))
            {
                Error("File not found: " + file);
                return 1;
            }

            using var csvz = CsvZipPackage.Open(file);
            
            var table = new ConsoleTable("Table", "Size", "Rows", "Columns");
            table.Configure(o => o.NumberAlignment = Alignment.Right);
            foreach(var entry in csvz.Entries)
            {
                table.AddRow(entry.Name, entry.Length, entry.RowCount, entry.ColumnCount);
            }

            table.Write(Format.Minimal);

            return 0;
        }

        static int Columns(string file, string name)
        {
            if (!File.Exists(file))
            {
                Error("File not found: " + file);
                return 1;
            }

            using var csvz = CsvZipPackage.Open(file);

            var entry = csvz.FindEntry(name);

            var cols = entry.GetColumnSchema();

            var table = new ConsoleTable("Column", "Ordinal", "Type", "Nullable");
            table.Configure(o => o.NumberAlignment = Alignment.Right);
            foreach (var col in cols.OrderBy(c => c.ColumnOrdinal))
            {
                table.AddRow(col.ColumnName, col.ColumnOrdinal, col.DataTypeName, col.AllowDBNull);
            }

            table.Write(Format.Minimal);

            return 0;
        }

        static int Add(string file, string name, bool overwrite)
        {
            if (!File.Exists(file))
            {
                Error("File not found: " + file);
                return 1;
            }

            if (!File.Exists(name))
            {
                Error("File not found: " + file);
                return 2;
            }

            using var csvz = CsvZipPackage.Open(file);
            var entry = csvz.FindEntry(name);
            if (entry != null)
            {
                if (overwrite)
                {
                    Console.WriteLine("Entry already exists, overwriting.");
                    entry.Delete();
                }
                else
                {
                    Error("File " + file + " alrady contain a table named " + name + ", specify overwrite to replace.");
                    return 3;
                }
            }
            WriteCsv(csvz, name);
            Console.WriteLine($"Added {name} to {file}.");
            return 0;
        }

        static int Remove(string file, string name)
        {
            if (!File.Exists(file))
            {
                Error("File not found: " + file);
                return 1;
            }

            using var csvz = CsvZipPackage.Open(file);
            var entry = csvz.FindEntry(name);
            if (entry == null)
            {
                Error("File " + file + " doesn't contain a table named " + name);
                return 2;
            }
            entry.Delete();
            Console.WriteLine($"Removed {name} from {file}.");
            return 0;
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
            foreach (var csv in Directory.EnumerateFiles(dir, "*.csv", SearchOption.TopDirectoryOnly))
            {
                WriteCsv(csvz, csv);
            }
            Console.WriteLine("Done.");
            return 0;
        }

        static void WriteCsv(CsvZipPackage csvz, string csv)
        {
            Console.WriteLine($"Processing {Path.GetFileName(csv)}: ");
            var data = CsvDataReader.Create(csv);
            var analyzer = new SchemaAnalyzer();
            Console.Write($"  Analyzing. ");
            var sw = Stopwatch.StartNew();
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

        static void Error(string msg)
        {
            var c = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(msg);
            Console.ForegroundColor = c;
        }

        static IEnumerable<string> GetCsvs(string dir)
        {
            return Directory.EnumerateFiles(dir, "*.csv", SearchOption.TopDirectoryOnly);
        }

    }
}
