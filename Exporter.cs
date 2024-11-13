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
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(3);

        public Exporter(string _rootPath)
        {
            rootPath = _rootPath;
            folderPath = Path.Combine(rootPath, "Scripts");
        }

        /// <summary>
        /// A method of extracting from a database from a specific table using
        /// </summary>
        internal async Task ExportScriptsFromDBAsync()
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
                await con.OpenAsync();

                //Create the SQL command
                using (var cmd = new SqlCommand(sqlQuery, con))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var tasks = new List<Task>();
                        while (await reader.ReadAsync() && !reader.IsDBNull(0))
                        {
                            var scriptDto = new ScriptDTO
                            {
                                Name = reader.GetString(0),
                                Description = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                Code = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)
                            };
                            tasks.Add(CreateFilesAsync(scriptDto));
                        }
                        await Task.WhenAll(tasks);
                    }
                }
            }
        }

        private async Task CreateFilesAsync(ScriptDTO scriptDTO)
        {
            if (scriptDTO is null) return;

            var fileName = Path.Combine(folderPath, scriptDTO.Name + ".vb");

            await semaphore.WaitAsync();
            try
            {
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                await File.WriteAllTextAsync(fileName, $"//{scriptDTO.Description}\n{scriptDTO.Code}");
                Console.WriteLine($"File {scriptDTO.Name} was created/updated");
            }
            finally
            {
                semaphore.Release();
            }            
        }
    }
}
