using FluentMigrator.Runner;
using Microsoft.Extensions.Configuration;

namespace Migrations
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Contains("--dryrun"))
            {
                return;
            }

            // Получаем строку подключения из конфига `appsettings.{Environment}.json`
            var connectionString = Environment.GetEnvironmentVariable("DbSettings__MigrationConnectionString");

            if (string.IsNullOrEmpty(connectionString))
            {
                var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
                                  throw new InvalidOperationException("ASPNETCORE_ENVIRONMENT in not set");

                var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"appsettings.{environmentName}.json")
                .AddEnvironmentVariables()
                .Build();
                
                connectionString = config["DbSettings:MigrationConnectionString"];
            }

            var migrationRunner = new MigratorRunner(connectionString);

            // Мигрируемся
            migrationRunner.Migrate();
        }
    }
}