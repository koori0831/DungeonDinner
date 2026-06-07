using System.Collections.Generic;
using System.Linq;

namespace Work.NPC.Code.Data
{
    public static class CsvRowReader
    {
        public static string Get(IReadOnlyDictionary<string, string> row, string key, string fallback = "")
        {
            if (row.TryGetValue(key, out string value))
                return value;

            return fallback;
        }

        public static int GetInt(IReadOnlyDictionary<string, string> row, string key, int fallback = 0)
        {
            string value = Get(row, key);
            return int.TryParse(value, out int result) ? result : fallback;
        }

        public static bool GetBool(IReadOnlyDictionary<string, string> row, string key, bool fallback = false)
        {
            string value = Get(row, key);
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            switch (value.Trim().ToLowerInvariant())
            {
                case "1":
                case "true":
                case "yes":
                case "y":
                case "on":
                    return true;
                case "0":
                case "false":
                case "no":
                case "n":
                case "off":
                    return false;
                default:
                    return fallback;
            }
        }

        public static List<string> GetList(IReadOnlyDictionary<string, string> row, string key, char separator = '|')
        {
            string value = Get(row, key);
            if (string.IsNullOrWhiteSpace(value))
                return new List<string>();

            return value
                .Split(separator)
                .Select(item => item.Trim())
                .Where(item => string.IsNullOrEmpty(item) == false)
                .ToList();
        }
    }
}
