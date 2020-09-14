using System;
using System.IO;
using Xunit;

namespace Sylvan.Tools.CsvZip
{
    public class MainTests
    {
        const string NotConcurrent = "These tests cannot be run concurrently";

        [Fact(Skip = NotConcurrent)]
        public void Test1()
        {
            var sw = new StringWriter();
            Console.SetOut(sw);
            Console.SetError(sw);
            Program.Main("--dir", ".", "--file", "test.zip");
            Program.Main("remove", "--file", "test.zip", "--name", "states.csv");
            Program.Main("add", "--file", "test.zip", "--name", "states.csv");
            Program.Main("add", "--file", "test.zip", "--name", "states.csv", "--overwrite");
            Program.Main("tables", "--file", "test.zip");
            Program.Main("columns", "--file", "test.zip", "--name", "states.csv");
        }

        [Fact(Skip = NotConcurrent)]
        public void Test2()
        {
            var sw = new StringWriter();
            Console.SetOut(sw);
            Console.SetError(sw);
            Program.Main(".", "test.zip");
            Program.Main("remove", "test.zip", "states.csv");
            Program.Main("add", "test.zip", "states.csv");
            Program.Main("add", "test.zip", "states.csv", "--overwrite");
            Program.Main("tables", "test.zip");
            Program.Main("columns", "test.zip", "states.csv");
        }
    }
}
