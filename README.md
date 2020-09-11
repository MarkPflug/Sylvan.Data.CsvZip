# Sylvan.Data.CsvZip
A .NET implementation of the [csvz](https://github.com/secretGeek/csvz) specification.

_Sylvan.Data.CsvZip_ is a library for programatically creating and reading `.csvz` files.

_Sylvan.Tools.CsvZip_ is a .NET global tool for creating `.csvz` files from the commandline.

## Sylvan.Data.CsvZip Library

### Installation
`Install-Package Sylvan.Data.CsvZip`

Primary API is via `CsvZipPackage`.

### Usage

Currently supports csvz-0, csvz-meta-tables, csvz-meta-columns.

#### Creating
```C#
using Sylvan.Data.Csv;

var csvz = CsvZipPackage.Create("data.csvz");
var entry = csvz.CreateEntry("states");

DbConnection conn = GetMyFavoriteDataSource();
DbCommand cmd = conn.CreateCommand();
cmd.CommandText = "select * from States";

DbDataReader data = cmd.ExecuteReader();
entry.WriteData(data);

```

#### Reading
```C#
using Sylvan.Data.Csv;

var csvz = CsvZipPackage.Open("data.csvz");
var entry = csvz.GetEntry("states");
// reader will expose a compatible schema (`GetColumnSchema`) as was written.
// So common bulk copy tools can be used to quickly load it into a different database provider
// with an equivalent schema.
DbDataReader reader = entry.GetDataReader();
```

## Sylvan.Tools.CsvZip tool

A command line utility that can create `.csvz` files from the `.csv` files in a directory.
Currently only supports creating `.csvz` files. 
Performs schema analysis when creating `.csvz` files to identify the data types of columns to populate column metadata.

### Installation
`dotnet tool install -g Sylvan.Data.CsvZip`

### Usage

`csvz`

Creates a csvz file with the name of the current directory, containing the csv files in the current directory.

`csvz d:\myData\`

Creates a csvz file named myData.csvz in the current directory, containing the csv files from d:\myData.

`csvz d:\myData\ data.csvz` 

Creates a csvz file named data.csvz in the current directory, containing the csv files from d:\myData.



