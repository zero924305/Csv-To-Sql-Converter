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
    public static class CsvDataReader
    {
        public static async Task<string> RemoveSpecialCharacters(string str)
        {
            StringBuilder sb = new();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    sb.Append(c);
                }
            }
            return await Task.FromResult("#"+sb.ToString());
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
            StringBuilder queryHeadBuilder = new();
                queryHeadBuilder.Append("DROP TABLE IF EXISTS " + queryname + "\n");
                queryHeadBuilder.Append("CREATE TABLE " + queryname + "\n");
                queryHeadBuilder.Append("(\n [tempuniqueID] [int] IDENTITY(1,1) NOT NULL,\n");
            int count = 0;

            foreach (string x in header)
            {
                count++;
                queryHeadBuilder.Append(" [" + count + "_" + (x.Length > 128 ? x.Substring(0, 100) : x) + "] Varchar(MAX),\n");
            }

            var queryHeader = queryHeadBuilder.ToString().Remove(queryHeadBuilder.Length - 2) + "\n)";

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

            sqlStatement = await SplitDataRow(queryData, insertIntoTable, datachunk);

            return await Task.FromResult(sqlStatement);
        }

        private static async Task<string> SplitDataRow(List<string> queryData, string insertIntoTable, List<string> datachunk)
        {
            StringBuilder sqlstatementBuilder = new();

            for (int i = 0; i < queryData.Count; i++)
            {
                //split for each 800 rows
                if (i % 800 == 0 && i > 0)
                {
                    sqlstatementBuilder.Append(insertIntoTable + string.Join(",\n", datachunk) + "\n\n");
                    datachunk.Clear();
                }
                datachunk.Add(queryData.ElementAt(i));
            }

            if (datachunk.Count > 0)
                sqlstatementBuilder.Append(insertIntoTable + string.Join(",\n", datachunk));

            return await Task.FromResult(sqlstatementBuilder.ToString());
        }
    }
}