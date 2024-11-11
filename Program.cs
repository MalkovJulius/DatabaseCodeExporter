using DatabaseCodeExporter;
using Serilog;
using System.Xml.Serialization;

// Настройка логирования
Log.Logger = new LoggerConfiguration()
    .WriteTo.File($"logs/{DateTime.Now}log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
try
{
    string choice;
    do
    {
        Console.WriteLine("Menu\n1 - Extract Scripts from Database\n2 - Update Database with Scripts\n0 - Exit");

        choice = Console.ReadLine();
        switch (choice)
        {
            case "0":
                Console.WriteLine("The program is closing");
                break;
            case "1":
                var exp = new Exporter(AppContext.BaseDirectory);
                exp.ExportScriptsFromDB();
                break;
            case "2":
                Console.WriteLine("Not implemented\n");
                break;
            default:
                Console.WriteLine("You can only enter 1, 2 or 0\n");
                break;
        }
    } while (choice != "0");
}
catch (Exception e)
{
    Log.Error(e.Message);
}
finally
{
    Log.CloseAndFlush();
}
