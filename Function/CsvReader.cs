using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvToSqlConverter.Function
{
    public class CSVReader
    {
        public static async Task<string> RemoveSpecialCharacters(string str)
        {
            StringBuilder sb = new();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_')
                {
                    sb.Append(c);
                }
            }
            return await Task.FromResult(sb.ToString());
        }

        public static async Task<string[]> GetHeader(string reader)
        {
            string[] header;
            using (StreamReader streamReader = new(reader, Encoding.UTF8))
            {
                using var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture);
                csvReader.Read();
                csvReader.ReadHeader();
                header = csvReader.HeaderRecord;
            }
            return await Task.FromResult(header);
        }

        public static async Task<string> GetSqlHeader(string[] header, string queryname)
        {
            string queryHeader = "";
            string subHeader;
            queryHeader += "DROP TABLE IF EXISTS " + queryname + "\n";
            queryHeader += "CREATE TABLE " + queryname + "\n";
            queryHeader += "(\n [tempuniqueID] [int] IDENTITY(1,1) NOT NULL,\n";

            foreach (string x in header)
            {
                if (x.Length > 128)
                    subHeader = x.Substring(0, 127);
                else
                    subHeader = x;

                queryHeader += " [" + subHeader + "] Varchar(MAX),\n";
            }
            queryHeader = queryHeader.Remove(queryHeader.Length - 2) + "\n)";

            return await Task.FromResult(queryHeader);
        }

        public static async Task<dynamic> GetsqlRecord(string reader)
        {
            using StreamReader streamReader = new(reader, Encoding.UTF8);
            using var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture);
            var rows = csvReader.GetRecords<dynamic>().ToList();
            return await Task.FromResult(rows);
        }

        public static async Task<string> GetDataRecord(List<object> lccs, string queryname)
        {
            List<string> queryData = new();
            List<string> datachunk = new();
            string insertIntoTable = "insert into " + queryname + " values \n";
            string sqlStatement = "";
            foreach (System.Dynamic.ExpandoObject ex in lccs)
            {
                List<string> list = ex.Select(x => x.Value.ToString().Trim()).ToList();

                list = list.Select(x =>
                       x.Replace("\\'", "'")
                        .Replace("’", "'")
                        .Replace("\'", "'")
                        .Replace("''", "'")
                        .Replace("`", "'")
                        .Replace("'", "''")
                        .Replace("\"\"", "''")).ToList();

                var concatenatedValues = String.Join(',', list.Select(x => "'" + x + "'"));
                queryData.Add("(" + concatenatedValues + ")");
            }

            sqlStatement = await SplitDataRow(queryData, insertIntoTable, sqlStatement, datachunk);

            return await Task.FromResult(sqlStatement);
        }

        private static async Task<string> SplitDataRow(List<string> queryData, string insertIntoTable, string sqlStatement, List<string> datachunk)
        {
            for (int i = 0; i < queryData.Count; i++)
            {
                //split for each 800 rows
                if (i % 800 == 0 && i > 0)
                {
                    sqlStatement += insertIntoTable + string.Join(",\n", datachunk) + "\n\n";
                    datachunk.Clear();
                }
                datachunk.Add(queryData.ElementAt(i));
            }

            if (datachunk.Count > 0)
                sqlStatement += insertIntoTable + string.Join(",\n", datachunk);

            return await Task.FromResult(sqlStatement);
        }
    }
}