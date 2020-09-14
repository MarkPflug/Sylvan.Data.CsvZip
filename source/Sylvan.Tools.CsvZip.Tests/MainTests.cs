using System;
using System.IO;
using Xunit;

namespace Sylvan.Tools.CsvZip
{
    public class MainTests
    {
        class SWDebug
        {
            StringWriter sw;
            public SWDebug(StringWriter sw)
            {
                this.sw = sw;
            }

            public string Output => sw.ToString();

            public override string ToString() => sw.ToString();
        }

        [Fact]
        public void Test1()
        {
            var sw = new StringWriter();
            var swd = new SWDebug(sw);
            Console.SetOut(sw);
            Console.SetError(sw);
            Program.Main("--dir", ".", "--file", "test.zip");
            Program.Main("remove", "--file", "test.zip", "--name", "states.csv");
            Program.Main("add", "--file", "test.zip", "--name", "states.csv");
            Program.Main("add", "--file", "test.zip", "--name", "states.csv", "--overwrite");
            Program.Main("tables", "--file", "test.zip");
            Program.Main("columns", "--file", "test.zip", "--name", "states.csv");
        }

        [Fact]
        public void Test2()
        {
            var sw = new StringWriter();
            var swd = new SWDebug(sw);
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
