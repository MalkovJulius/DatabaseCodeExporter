using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseCodeExporter
{
    //Реализация класса не делалась для разных БД или таблиц, конкретно для однного частного случая
    internal class Exporter
    {
        private readonly string rootPath;
        private readonly string folderPath;
        //private readonly SemaphoreSlim semaphore = new SemaphoreSlim(3);

        public Exporter(string _rootPath)
        {
            rootPath = _rootPath;
            folderPath = Path.Combine(rootPath, "Scripts");
        }

        /// <summary>
        /// A method of extracting from a database from a specific table using
        /// </summary>
        internal void ExportScriptsFromDB()
        {
            var connectionString = new ConfigurationBuilder()
                .SetBasePath(rootPath)
                .AddJsonFile("setting.json", optional: false, reloadOnChange: true)
                .Build()
                .GetConnectionString("DefaultConnection");

            var sqlQuery = new ConfigurationBuilder()
                .SetBasePath(rootPath)
                .AddJsonFile("sqlQueries.json", optional: false, reloadOnChange: true)
                .Build()["GetScripts"];

            //Create the connection to DB
            using (var con = new SqlConnection(connectionString))
            {
                con.Open();

                //Create the SQL command
                using (var cmd = new SqlCommand(sqlQuery, con))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read() && !reader.IsDBNull(0))
                        {
                            var scriptDto = new ScriptDTO
                            {
                                Name = reader.GetString(0),
                                Description = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                Code = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)
                            };
                            CreateFiles(scriptDto);
                        }
                    }
                }
            }
        }

        private void CreateFiles(ScriptDTO scriptDTO)
        {
            if (scriptDTO is null) return;

            var fileName = Path.Combine(folderPath, scriptDTO.Name + ".vb");
            File.WriteAllText(fileName, $"//{scriptDTO.Description}\n{scriptDTO.Code}");            

            Console.WriteLine($"File {scriptDTO.Name} was created/updated");
        }
    }
}
