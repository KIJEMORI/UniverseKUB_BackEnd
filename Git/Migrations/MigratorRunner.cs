using FluentMigrator.Runner.VersionTableInfo;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Migrations
{
    public class MigratorRunner(string connectionString)
    {
        public void Migrate()
        {

            WaitForDatabase();

            var serviceProvider = CreateServices();

            using var scope = serviceProvider.CreateScope();
            UpdateDatabase(scope.ServiceProvider);
        }

        private IServiceProvider CreateServices()
        {
            Console.WriteLine(typeof(MigratorRunner).Assembly.FullName);

            // Зависимости
            // Хотим fluentMigrator с постгресом
            // и чтобы искал миграции в текущем проекте.
            // Также добавляем консольное логирование и
            // собственную реализацию интерфейса IVersionTableMetaData 
            // (которая хранит накаченные миграции) 
            return new ServiceCollection()
                .AddFluentMigratorCore()
                .ConfigureRunner(rb => rb
                    .AddPostgres()
                    .WithGlobalConnectionString(connectionString)
                    .ScanIn(typeof(MigratorRunner).Assembly).For.Migrations())
                .AddLogging(lb => lb.AddFluentMigratorConsole())
                .AddScoped<IVersionTableMetaData, VersionTable>()
                .BuildServiceProvider(false);
        }

        private void UpdateDatabase(IServiceProvider serviceProvider)
        {
            // Мигрируем базу
            var runner = serviceProvider.GetRequiredService<IMigrationRunner>();
            runner.MigrateUp();
            
            // создаем и открываем коннект к бд
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            // перегружаем композитные типы
            connection.ReloadTypes();
        }

        private void WaitForDatabase()
        {
            var maxAttempts = 30;
            var delay = 2000; // 2 seconds

            for (int i = 0; i < maxAttempts; i++)
            {
                try
                {
                    using var connection = new NpgsqlConnection(connectionString);
                    connection.Open();
                    Console.WriteLine("Database connection successful");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Attempt {i + 1}/{maxAttempts}: Database not ready - {ex.Message}");
                    if (i == maxAttempts - 1) throw;
                    Thread.Sleep(delay);
                }
            }
        }
    }
}