using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using Xunit;

namespace Sylvan.Data.Csv
{
    public class CsvZipPackageTests
    {
        [Fact]
        public void Test1()
        {
            using var csvz = CsvZipPackage.Create("db.zip");

            {
                var entry = csvz.CreateEntry("states");
                var data = CsvDataReader.Create(new StringReader("Code,Name\r\nOR,Oregon\r\nWA,Washington\r\nCA,California\r\nAK,Alaska\r\nHI,Hawaii"));
                entry.WriteData(data);

                entry = csvz.CreateEntry("postal");
                data = CsvDataReader.Create(new StringReader("State,PostalCode\r\nOR,97123\r\nCA,90210\r\nOR97701"));
                entry.WriteData(data);
            }

            var c = csvz.Entries.Count();
            var e = csvz.GetEntry("states");
            Assert.Equal("states", e.Name);
            Assert.Equal(5, e.RowCount);
            Assert.Equal(2, e.ColumnCount);
            Assert.Equal(77, e.Length);

            var postalEntry = csvz.GetEntry("postal");
            var ps = postalEntry.GetColumnSchema();

            Assert.NotNull(ps);

            var dr = postalEntry.GetDataReader();
            while (dr.Read())
            {
                var state = dr.GetString(0);
                var zip = dr.GetString(1);
            }
        }

        [Fact(Skip = "uses local database")]
        public void DbTest()
        {
            var csb = new SqlConnectionStringBuilder { DataSource = ".", InitialCatalog = "Sylvan", IntegratedSecurity = true };
            var conn = (DbConnection)new SqlConnection(csb.ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "select * from State";
            var reader = cmd.ExecuteReader();

            using var csvz = CsvZipPackage.Create("data.csvz");
            var entry = csvz.CreateEntry("states");
            entry.WriteData(reader);
        }
    }
}
