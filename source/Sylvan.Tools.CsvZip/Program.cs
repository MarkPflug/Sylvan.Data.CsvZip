using Sylvan.Data;
using Sylvan.Data.Csv;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

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

        static void Package(string dir, string file)
        {
            dir ??= ".";

            if(file == null)
            {
                var d = Path.GetDirectoryName(Path.GetFullPath(dir));
                var f = Path.GetFileName(d);
                file = f + ".csvz";
            }

            using var csvz = CsvZipPackage.Create(file);

            foreach (var csv in Directory.EnumerateFiles(dir, "*.csv", SearchOption.TopDirectoryOnly)) {
                var data = CsvDataReader.Create(csv);
                var analyzer = new SchemaAnalyzer();
                var result = analyzer.Analyze(data);

                var schema = result.GetSchema();
                var csvSchema = new CsvSchema(schema);

                var csvOpts = new CsvDataReaderOptions { Schema = csvSchema };

                data = CsvDataReader.Create(csv, csvOpts);
                var entry = csvz.CreateEntry(Path.GetFileName(csv));
                entry.WriteData(data);
            }
        }
    }
}
