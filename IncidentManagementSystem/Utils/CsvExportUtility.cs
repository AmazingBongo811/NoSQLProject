using IncidentManagementSystem.Data;
using IncidentManagementSystem.Repositories;
using IncidentManagementSystem.Services;
using Microsoft.Extensions.Configuration;

namespace IncidentManagementSystem.Utils
{
    /// <summary>
    /// Console utility for exporting collections to CSV files for deliverable 2.
    /// This utility can be run independently to generate the required CSV exports.
    /// </summary>
    public class CsvExportUtility
    {
        public static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("=== Incident Management System - CSV Export Utility ===");
                Console.WriteLine("Preparing to export collections to CSV files for deliverable 2...\n");

                // Load configuration
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false)
                    .AddJsonFile("appsettings.Development.json", optional: true)
                    .Build();

                // Get MongoDB settings
                var mongoSettings = new MongoDbSettings();
                configuration.GetSection(MongoDbSettings.SectionName).Bind(mongoSettings);

                // Initialize MongoDB context
                var mongoContext = new MongoDbContext(mongoSettings);

                // Initialize repositories
                var userRepository = new UserRepository(mongoContext);
                var ticketRepository = new TicketRepository(mongoContext);

                // Initialize services
                var seedService = new DatabaseSeedService(mongoContext, userRepository, ticketRepository);
                var csvExportService = new CsvExportService(userRepository, ticketRepository);

                // Check current database state
                Console.WriteLine("📊 Checking current database state...");
                var userCount = await userRepository.GetCountAsync();
                var ticketCount = await ticketRepository.GetCountAsync();
                
                Console.WriteLine($"   Users in database: {userCount}");
                Console.WriteLine($"   Tickets in database: {ticketCount}");

                // Seed database if needed
                if (userCount < 100 || ticketCount < 100)
                {
                    Console.WriteLine("\n🌱 Database needs seeding (minimum 100 documents per collection)...");
                    Console.WriteLine("   Seeding database with sample data...");
                    
                    var seedSuccess = await seedService.SeedDatabaseAsync();
                    if (seedSuccess)
                    {
                        Console.WriteLine("   ✅ Database seeding completed successfully!");
                        
                        // Get updated counts
                        userCount = await userRepository.GetCountAsync();
                        ticketCount = await ticketRepository.GetCountAsync();
                        Console.WriteLine($"   Updated counts - Users: {userCount}, Tickets: {ticketCount}");
                    }
                    else
                    {
                        Console.WriteLine("   ❌ Database seeding failed!");
                        return;
                    }
                }
                else
                {
                    Console.WriteLine("   ✅ Database already contains sufficient data for export.");
                }

                // Export to CSV files
                Console.WriteLine("\n📁 Exporting collections to CSV files...");
                var exportResult = await csvExportService.ExportAllToFilesAsync();
                
                Console.WriteLine(exportResult);

                // Display summary
                Console.WriteLine("\n📋 Export Summary:");
                Console.WriteLine("   The following files have been created for your deliverable 2 submission:");
                Console.WriteLine("   • Users CSV - Contains all user records");
                Console.WriteLine("   • Tickets CSV - Contains all ticket records");
                Console.WriteLine("   • TicketActivities CSV - Contains activity audit logs");
                Console.WriteLine("   • TicketComments CSV - Contains all ticket comments");
                Console.WriteLine("   • Statistics CSV - Contains collection counts and export metadata");

                Console.WriteLine("\n✨ CSV export completed successfully!");
                Console.WriteLine("   You can now submit these CSV files for your deliverable 2.");
                
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Error during CSV export: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
        }
    }
}