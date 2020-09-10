using System;

namespace Sylvan.Data.Csv
{
    static class Extensions
    {
        public static void WriteField(this CsvWriter csv, bool? value)
        {
            if (value.HasValue)
                csv.WriteField(value.Value);
            else
                csv.WriteField();
        }

        public static void WriteField(this CsvWriter csv, int? value)
        {
            if (value.HasValue)
                csv.WriteField(value.Value);
            else
                csv.WriteField();
        }

        public static void WriteField(this CsvWriter csv, long? value)
        {
            if (value.HasValue)
                csv.WriteField(value.Value);
            else
                csv.WriteField();
        }

        public static void WriteField(this CsvWriter csv, DateTime? value)
        {
            if (value.HasValue)
                csv.WriteField(value.Value);
            else
                csv.WriteField();
        }

        public static string? ReadString(this CsvDataReader csv, int idx)
        {
            return
                idx < 0 || csv.IsDBNull(idx)
                ? null
                : csv.GetString(idx);
        }

        public static long? ReadInt64(this CsvDataReader csv, int idx)
        {
            return
                idx < 0 || csv.IsDBNull(idx)
                ? (long?)null
                : csv.GetInt64(idx);
        }

        public static int? ReadInt32(this CsvDataReader csv, int idx)
        {
            return
                idx < 0 || csv.IsDBNull(idx)
                ? (int?)null
                : csv.GetInt32(idx);
        }

        public static bool? ReadBoolean(this CsvDataReader csv, int idx)
        {
            return
                idx < 0 || csv.IsDBNull(idx)
                ? (bool?)null
                : csv.GetBoolean(idx);
        }

        public static DateTime? ReadDateTime(this CsvDataReader csv, int idx)
        {
            return
                idx < 0 || csv.IsDBNull(idx)
                ? (DateTime?)null
                : csv.GetDateTime(idx);
        }
    }
}
