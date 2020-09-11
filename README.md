# Sylvan.Data.CsvZip
Implementation of the [csvz](https://github.com/secretGeek/csvz) specification as a .NET library.

## Installation
`Install-Package Sylvan.Data.CsvZip`

Primary API is via `CsvZipPackage`.

Currently supports csvz-0, csvz-meta-tables, csvz-meta-columns.

### Creating
```C#

var csvz = CsvZipPackage.Create("data.csvz");
var entry = csvz.CreateEntry("states");

DbConnection conn = GetMyFavoriteDataSource();
DbCommand cmd = conn.CreateCommand();
cmd.CommandText = "select * from States";

DbDataReader data = cmd.ExecuteReader();
entry.WriteData(data);

```


### Reading
```C#

var csvz = CsvZipPackage.Open("data.csvz");
var entry = csvz.Get("states");
// reader will expose a compatible schema (`GetColumnSchema`) as was written.
// So common bulk copy tools can be used to quickly load it into a different database provider
// with an equivalent schema.
DbDataReader reader = entry.GetDataReader();
```
