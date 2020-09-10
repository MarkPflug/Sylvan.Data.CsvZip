using System.IO;
using System.Linq;
using Xunit;

namespace Sylvan.Data.Csv
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            using var csvz = CsvZipPackage.Create("db.zip");

            {
                var entry = csvz.CreateEntry("states");
                var data = CsvDataReader.Create(new StringReader("Code,Name\r\nOR,Oregon\r\nWA,Washington\r\nCA,California\r\nAK,Alaska\r\nHI,Hawaii"));
                entry.WriteData(data);

                entry = csvz.CreateEntry("zip");
                data = CsvDataReader.Create(new StringReader("State,Zip\r\nOR,97123\r\nCA,90210\r\nOR97701"));
                entry.WriteData(data);
            }

            var c = csvz.Entries.Count();
            var e = csvz.GetEntry("states");
            Assert.Equal("states", e.Name);
            Assert.Equal(5, e.RowCount);
            Assert.Equal(2, e.ColumnCount);
            Assert.Equal(77, e.Length);
        }
    }
}
