using System;

namespace Sylvan.Data.Csv
{
	public class CsvZipPackage : IDisposable
	{
		ZipArchive zip;

		public CsvZipPackage(string filename)
		{
			var stream = File.Create(filename);
			this.zip = new ZipArchive(stream, ZipArchiveMode.Create);
		}

		void IDisposable.Dispose()
		{
			zip.Dispose();
		}

		public void WriteEntry(string name, DbDataReader data)
		{
			var entry = zip.CreateEntry(name + ".csv");
			using var oStream = entry.Open();
			using var writer = new StreamWriter(oStream, Encoding.UTF8);
			using var csvWriter = new CsvDataWriter(writer);
			csvWriter.Write(data);
		}

		void UpdateTables()
		{

		}

		public class Entry
		{
			ZipArchiveEntry entry;

			internal Entry(ZipArchiveEntry entry)
			{
				this.entry = entry;
			}

			public string Name
			{
				get
				{
					return Path.GetFileNameWithoutExtension(entry.Name);
				}
			}

			public DbDataReader GetDataReader()
			{
				var reader = new StreamReader(entry.Open());
				return CsvDataReader.Create(reader);
			}
		}

		static bool IsRootCsv(ZipArchiveEntry entry)
		{
			var name = entry.FullName;

			var ext = Path.GetExtension(name);
			return StringComparer.OrdinalIgnoreCase.Equals(ext, ".csv");
		}

		static Func<ZipArchiveEntry, bool> IsRootCsvSelector = IsRootCsv;

		public IEnumerable<Entry> Entries
		{
			get
			{
				foreach (var entry in zip.Entries.Where(IsRootCsvSelector))
				{
					yield return new Entry(entry);
				}
			}
		}

		void IDisposable.Dispose()
		{
			this.zip.Dispose();
		}
	}
}
