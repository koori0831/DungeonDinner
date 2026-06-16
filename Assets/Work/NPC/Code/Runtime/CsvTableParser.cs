using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Work.NPC.Code.Runtime
{
    public static class CsvTableParser
    {
        public static List<Dictionary<string, string>> Parse(TextAsset textAsset)
        {
            if (textAsset == null)
                throw new ArgumentNullException(nameof(textAsset));

            List<List<string>> table = ParseRows(textAsset.text);
            List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();
            List<string> headers = null;

            foreach (List<string> rowValues in table)
            {
                if (IsEmptyRow(rowValues))
                    continue;

                if (rowValues.Count > 0 && rowValues[0].TrimStart().StartsWith("#"))
                    continue;

                if (headers == null)
                {
                    headers = rowValues;
                    if (headers.Count > 0)
                        headers[0] = headers[0].TrimStart('\uFEFF');
                    continue;
                }

                Dictionary<string, string> row = new Dictionary<string, string>();
                for (int i = 0; i < headers.Count; i++)
                {
                    string value = i < rowValues.Count ? rowValues[i] : string.Empty;
                    row[headers[i]] = value;
                }

                rows.Add(row);
            }

            return rows;
        }

        private static List<List<string>> ParseRows(string text)
        {
            List<List<string>> rows = new List<List<string>>();
            List<string> currentRow = new List<string>();
            StringBuilder currentField = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            currentField.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        currentField.Append(c);
                    }

                    continue;
                }

                switch (c)
                {
                    case '"':
                        inQuotes = true;
                        break;
                    case ',':
                        AddField(currentRow, currentField);
                        break;
                    case '\r':
                        if (i + 1 < text.Length && text[i + 1] == '\n')
                            i++;
                        AddRow(rows, currentRow, currentField);
                        break;
                    case '\n':
                        AddRow(rows, currentRow, currentField);
                        break;
                    default:
                        currentField.Append(c);
                        break;
                }
            }

            if (currentField.Length > 0 || currentRow.Count > 0)
                AddRow(rows, currentRow, currentField);

            return rows;
        }

        private static void AddField(List<string> row, StringBuilder field)
        {
            row.Add(field.ToString().Trim());
            field.Clear();
        }

        private static void AddRow(List<List<string>> rows, List<string> row, StringBuilder field)
        {
            AddField(row, field);
            rows.Add(new List<string>(row));
            row.Clear();
        }

        private static bool IsEmptyRow(List<string> row)
        {
            if (row.Count == 0)
                return true;

            for (int i = 0; i < row.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(row[i]) == false)
                    return false;
            }

            return true;
        }
    }
}
