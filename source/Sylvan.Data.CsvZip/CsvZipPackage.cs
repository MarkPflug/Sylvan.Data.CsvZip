using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Sylvan.Data.Csv
{
    [Flags]
    public enum CsvZipConformance
    {
        /// <summary>
        /// The csvz-0 conformance.
        /// </summary>
        Basic = 1,
        MetaTables = 2,
        MetaColumns = 4,
        MetaRelations = 8,
    }

    public sealed class CsvZipPackage : IDisposable
    {
        const string TablesMetaEntryName = "_meta/tables.csv";
        const string ColumnsMetaEntryName = "_meta/columns.csv";

        readonly ZipArchive zip;
        Dictionary<string, TableInfo> tables;
        List<ColumnInfo> columns;

        public static CsvZipPackage Create(string filename)
        {
            var stream = File.Create(filename);
            return new CsvZipPackage(stream);
        }

        public static CsvZipPackage Open(string filename)
        {
            var stream = File.Open(filename, FileMode.OpenOrCreate);
            return new CsvZipPackage(stream);
        }

        static readonly CsvDataReaderOptions Options =
            new CsvDataReaderOptions
            {
                HeaderComparer = StringComparer.OrdinalIgnoreCase
            };

        public CsvZipPackage(Stream stream)
        {
            this.zip = new ZipArchive(stream, ZipArchiveMode.Update);
            tables = new Dictionary<string, TableInfo>(StringComparer.OrdinalIgnoreCase);
            columns = new List<ColumnInfo>();
            InitializeMetadata();
        }

        void InitializeMetadata()
        {
            {
                var te = zip.GetEntry(TablesMetaEntryName);
                if (te == null)
                {
                    // TODO set conformance no meta tables
                }
                else
                {
                    try
                    {
                        var sw = new StreamReader(te.Open());
                        var tablesCsv = CsvDataReader.Create(sw, Options);
                        // todo: handle extended metadata? punt for now.
                        var filenameIdx = tablesCsv.GetOrdinal("filename");
                        var bytesIdx = tablesCsv.GetOrdinal("bytes");
                        var rowsIdx = tablesCsv.GetOrdinal("rows");
                        var colsIdx = tablesCsv.GetOrdinal("columns");
                        var descriptionIdx = tablesCsv.GetOrdinal("description");
                        var publishedIdx = tablesCsv.GetOrdinal("published");
                        var sourceIdx = tablesCsv.GetOrdinal("source");

                        while (tablesCsv.Read())
                        {
                            string filename = tablesCsv.GetString(filenameIdx);

                            long? bytes = tablesCsv.ReadInt64(bytesIdx);
                            long? rows = tablesCsv.ReadInt64(rowsIdx);
                            int? cols = tablesCsv.ReadInt32(colsIdx);
                            string? description = tablesCsv.ReadString(descriptionIdx);
                            DateTime? published = tablesCsv.ReadDateTime(publishedIdx);
                            string? source = tablesCsv.ReadString(sourceIdx);

                            var t =
                                new TableInfo
                                {
                                    filename = filename,
                                    bytes = bytes,
                                    rowCount = rows,
                                    colCount = cols,
                                    description = description,
                                    published = published,
                                    source = source,
                                };
                            this.tables.Add(filename, t);
                        }
                    }
                    catch (Exception)
                    {
                        // TODO ? ignore and treat as csvz-0 ?
                    }
                }
            }
            {
                var te = zip.GetEntry(ColumnsMetaEntryName);
                if (te == null)
                {
                    // TODO set conformance no meta columns
                }
                else
                {
                    try
                    {
                        var sw = new StreamReader(te.Open());
                        var csv = CsvDataReader.Create(sw, Options);
                        // todo: handle extended metadata? punt for now.
                        var filenameIdx = csv.GetOrdinal("filename");
                        var columnIdx = csv.GetOrdinal("column");
                        var ordinalIdx = csv.GetOrdinal("ordinal");
                        var typeIdx = csv.GetOrdinal("type");
                        var nullableIdx = csv.GetOrdinal("nullable");
                        var maxLengthIdx = csv.GetOrdinal("max-length");
                        var uniqueIdx = csv.GetOrdinal("unique");
                        var primaryKeyIdx = csv.GetOrdinal("primary-key");
                        var descriptionIdx = csv.GetOrdinal("description");
                        var unitsIdx = csv.GetOrdinal("units");
                        var publishedIdx = csv.GetOrdinal("published");
                        var sourceIdx = csv.GetOrdinal("source");

                        while (csv.Read())
                        {
                            string filename = csv.GetString(filenameIdx);
                            var c =
                                new ColumnInfo
                                {
                                    filename = filename,
                                    column = csv.ReadString(columnIdx),
                                    ordinal = csv.ReadInt32(ordinalIdx),
                                    type = csv.ReadString(typeIdx),
                                    nullable = csv.ReadBoolean(nullableIdx),
                                    maxLength = csv.ReadInt32(maxLengthIdx),
                                    unique = csv.ReadBoolean(uniqueIdx),
                                    primaryKey = csv.ReadBoolean(primaryKeyIdx),
                                    description = csv.ReadString(descriptionIdx),
                                    units = csv.ReadString(unitsIdx),
                                    published = csv.ReadDateTime(publishedIdx),
                                    source = csv.ReadString(sourceIdx),
                                };
                            c.Init();
                            this.columns.Add(c);
                        }
                    }
                    catch (Exception)
                    {
                        // TODO ? ignore and treat as csvz-0 ?
                    }
                }
            }
        }

        static string NormalizeName(string name)
        {
            return
                StringComparer.OrdinalIgnoreCase.Equals(Path.GetExtension(name), ".csv")
                ? name
                : name + ".csv";
        }
        public Entry CreateEntry(string name)
        {
            name = NormalizeName(name);
            return new Entry(this, name, true);
        }

        public Entry GetEntry(string name)
        {
            name = NormalizeName(name);
            if (zip.GetEntry(name) == null) throw new ArgumentOutOfRangeException(nameof(name));

            return new Entry(this, name, false);
        }

        public CsvZipConformance Conformance
        {
            get => CsvZipConformance.Basic;
        }

        void UpdateMetadata()
        {
            var tablesEntry = zip.GetEntry(TablesMetaEntryName);

            if (tablesEntry == null)
            {
                tablesEntry = zip.CreateEntry(TablesMetaEntryName);
            }
            {
                using var stream = tablesEntry.Open();
                stream.SetLength(0);
                using var tw = new StreamWriter(stream, Encoding.UTF8);
                using var csv = new CsvWriter(tw);
                csv.WriteField("filename");
                csv.WriteField("bytes");
                csv.WriteField("rows");
                csv.WriteField("columns");
                csv.WriteField("description");
                csv.WriteField("published");
                csv.WriteField("source");
                csv.EndRecord();
                foreach (var t in tables.Values.OrderBy(t => t.filename))
                {
                    csv.WriteField(t.filename);
                    csv.WriteField(t.bytes);
                    csv.WriteField(t.rowCount);
                    csv.WriteField(t.colCount);
                    csv.WriteField(t.description);
                    csv.WriteField(t.published);
                    csv.WriteField(t.source);
                    csv.EndRecord();
                }
            }
            var columnsEntry = zip.GetEntry(ColumnsMetaEntryName);

            if (columnsEntry == null)
            {
                columnsEntry = zip.CreateEntry(ColumnsMetaEntryName);
            }
            {
                using var stream = columnsEntry.Open();
                stream.SetLength(0);
                using var tw = new StreamWriter(stream, Encoding.UTF8);
                using var csv = new CsvWriter(tw);
                csv.WriteField("filename");
                csv.WriteField("column");
                csv.WriteField("ordinal");
                csv.WriteField("type");
                csv.WriteField("nullable");
                csv.WriteField("max-length");
                csv.WriteField("unique");
                csv.WriteField("primary-key");
                csv.WriteField("description");
                csv.WriteField("units");
                csv.WriteField("published");
                csv.WriteField("source");
                csv.EndRecord();
                foreach (var t in columns.OrderBy(t => t.filename).ThenBy(t => t.ordinal).ThenBy(t => t.column))
                {
                    csv.WriteField(t.filename);
                    csv.WriteField(t.column);
                    csv.WriteField(t.ordinal);
                    csv.WriteField(t.type);
                    csv.WriteField(t.nullable);
                    csv.WriteField(t.maxLength);
                    csv.WriteField(t.unique);
                    csv.WriteField(t.primaryKey);
                    csv.WriteField(t.description);
                    csv.WriteField(t.units);
                    csv.WriteField(t.published);
                    csv.WriteField(t.source);
                    csv.EndRecord();
                }
            }
        }

        class TableInfo
        {
            public string? filename;
            public long? bytes;
            public long? rowCount;
            public int? colCount;
            public DateTime? published;
            public string? description;
            public string? source;
        }

        class ColumnInfo : DbColumn
        {
            internal string? filename;
            internal string? column;
            internal int? ordinal;
            internal string? type;
            internal bool? nullable;
            internal int? maxLength;
            internal bool? unique;
            internal bool? primaryKey;
            internal string? description;
            internal string? units;
            internal DateTime? published;
            internal string? source;

            internal void Init()
            {
                this.BaseTableName = filename;
                this.ColumnName = column;
                this.ColumnOrdinal = ordinal;
                this.DataTypeName = type;
                this.AllowDBNull = nullable;
                this.ColumnSize = maxLength;
                this.IsUnique = unique;
                this.IsKey = primaryKey;
            }
        }

        public class Entry : IDbColumnSchemaGenerator
        {
            readonly CsvZipPackage pkg;
            readonly string name;
            bool isNew;

            internal Entry(CsvZipPackage pkg, string name, bool isNew)
            {
                this.pkg = pkg;
                this.name = name;
                this.isNew = isNew;
            }

            // todo: check this annotation when nullability lands in BCL. Might have to throw instead.
            public ReadOnlyCollection<DbColumn>? GetColumnSchema()
            {
                var entry = pkg.zip.GetEntry(name);
                if (entry == null)
                    return null;

                var cols =
                    pkg.columns
                    .Where(c => StringComparer.OrdinalIgnoreCase.Equals(this.name, c.filename))
                    .OrderBy(c => c.ColumnOrdinal)
                    .Cast<DbColumn>()
                    .ToList();

                if (cols.Any())
                {
                    return new ReadOnlyCollection<DbColumn>(cols);
                }
                return null;
            }

            public string Name => Path.GetFileNameWithoutExtension(name);

            public long? Length
            {
                get
                {
                    if (pkg.tables.TryGetValue(this.name, out var t))
                    {
                        return t.bytes;
                    }
                    return null;
                }
            }

            public long? ColumnCount
            {
                get
                {
                    if (pkg.tables.TryGetValue(this.name, out var t))
                    {
                        return t.colCount;
                    }
                    return null;
                }
            }

            public long? RowCount
            {
                get
                {
                    if (pkg.tables.TryGetValue(this.name, out var t))
                    {
                        return t.rowCount;
                    }
                    return null;
                }
            }

            public void Delete()
            {
                var e = pkg.zip.GetEntry(this.name);
                if (e != null)
                {
                    e?.Delete();
                    pkg.tables.Remove(this.name);
                    foreach (var col in pkg.columns.Where(c => StringComparer.OrdinalIgnoreCase.Equals(c.filename, this.name)).ToArray())
                    {
                        pkg.columns.Remove(col);
                    }
                }
                else
                {
                    isNew = true;
                }
            }

            public void WriteData(DbDataReader data)
            {
                if (data == null) throw new ArgumentNullException(nameof(data));

                var entry = pkg.zip.GetEntry(name);
                if (entry != null)
                    throw new InvalidOperationException();

                entry = pkg.zip.CreateEntry(name);
                long length;
                long count;
                using (var stream = entry.Open())
                using (var tw = new StreamWriter(stream, Encoding.UTF8))
                using (var csv = new CsvDataWriter(tw))
                {
                    count = csv.Write(data);
                    tw.Flush();
                    length = stream.Length;
                }
                var table = new TableInfo
                {
                    filename = name,
                    bytes = length,
                    colCount = data.FieldCount,
                    rowCount = count,
                };
                pkg.tables.Add(name, table);

                var colSchema = data.GetColumnSchema();
                foreach (var col in colSchema)
                {
                    var ci = new ColumnInfo
                    {
                        filename = name,
                        ordinal = col.ColumnOrdinal,
                        column = col.ColumnName,
                        maxLength = col.ColumnSize,
                        type = col.DataTypeName,
                        unique = col.IsUnique,
                        nullable = col.AllowDBNull,
                        primaryKey = col.IsKey,
                        description = null,
                        published = null,
                        source = null,
                        units = null,
                    };
                    ci.Init();
                    pkg.columns.Add(ci);
                }

                pkg.UpdateMetadata();
            }

            public DbDataReader GetDataReader()
            {
                var entry = pkg.zip.GetEntry(this.name);
                if (entry == null)
                    throw new InvalidOperationException();

                var schema = this.GetColumnSchema();

                var opts = new CsvDataReaderOptions { Schema = schema == null ? null : new CsvSchema(schema) };

                var reader = new StreamReader(entry.Open());
                return CsvDataReader.Create(reader, opts);
            }
        }

        static bool IsRootCsv(ZipArchiveEntry entry)
        {
            var name = entry.FullName;

            var ext = Path.GetExtension(name);
            if (StringComparer.OrdinalIgnoreCase.Equals(ext, ".csv") == false)
                return false;
            var dir = Path.GetDirectoryName(name);
            return dir == string.Empty;
        }

        static Func<ZipArchiveEntry, bool> IsRootCsvSelector = IsRootCsv;

        public IEnumerable<Entry> Entries
        {
            get
            {
                var entries = zip.Entries.Where(IsRootCsvSelector).ToArray();
                foreach (var entry in entries)
                {
                    yield return new Entry(this, entry.FullName, false);
                }
            }
        }

        void IDisposable.Dispose()
        {
            this.zip.Dispose();
        }
    }
}
