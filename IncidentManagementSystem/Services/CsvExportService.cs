using System.Text;
using System.Globalization;
using IncidentManagementSystem.Models;
using IncidentManagementSystem.Repositories;

namespace IncidentManagementSystem.Services
{
    /// <summary>
    /// Service for exporting collections to CSV format for deliverable 2 requirements.
    /// Exports all collections in a structured format suitable for academic submission.
    /// </summary>
    public class CsvExportService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITicketRepository _ticketRepository;

        public CsvExportService(IUserRepository userRepository, ITicketRepository ticketRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
        }

        /// <summary>
        /// Export all users to CSV format
        /// </summary>
        public async Task<string> ExportUsersAsync()
        {
            var users = await _userRepository.GetAllAsync(0, int.MaxValue);
            var csv = new StringBuilder();

            // CSV Header
            csv.AppendLine("Id,Username,Email,FirstName,LastName,Role,Department,CreatedAt,UpdatedAt,IsActive,LastLogin");

            // Data rows
            foreach (var user in users)
            {
                csv.AppendLine($"{EscapeCsvField(user.Id)}," +
                              $"{EscapeCsvField(user.Username)}," +
                              $"{EscapeCsvField(user.Email)}," +
                              $"{EscapeCsvField(user.FirstName)}," +
                              $"{EscapeCsvField(user.LastName)}," +
                              $"{EscapeCsvField(user.Role.ToString())}," +
                              $"{EscapeCsvField(user.Department)}," +
                              $"{EscapeCsvField(user.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))}," +
                              $"{EscapeCsvField(user.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))}," +
                              $"{user.IsActive}," +
                              $"{EscapeCsvField(user.LastLogin?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))}");
            }

            return csv.ToString();
        }

        /// <summary>
        /// Export all tickets to CSV format (flattened structure for CSV compatibility)
        /// </summary>
        public async Task<string> ExportTicketsAsync()
        {
            var tickets = await _ticketRepository.GetAllAsync(0, int.MaxValue);
            var csv = new StringBuilder();

            // CSV Header
            csv.AppendLine("Id,Title,Description,Status,Priority,Category," +
                          "ReporterUserId,ReporterUsername,ReporterEmail,ReporterFullName,ReporterDepartment," +
                          "AssigneeUserId,AssigneeUsername,AssigneeEmail,AssigneeFullName,AssigneeDepartment," +
                          "CreatedAt,UpdatedAt,ResolvedAt,ClosedAt,CommentsCount,ActivitiesCount");

            // Data rows
            foreach (var ticket in tickets)
            {
                csv.AppendLine($"{EscapeCsvField(ticket.Id)}," +
                              $"{EscapeCsvField(ticket.Title)}," +
                              $"{EscapeCsvField(ticket.Description)}," +
                              $"{EscapeCsvField(ticket.Status.ToString())}," +
                              $"{EscapeCsvField(ticket.Priority.ToString())}," +
                              $"{EscapeCsvField(ticket.Category.ToString())}," +
                              $"{EscapeCsvField(ticket.Reporter.UserId)}," +
                              $"{EscapeCsvField(ticket.Reporter.Username)}," +
                              $"{EscapeCsvField(ticket.Reporter.Email)}," +
                              $"{EscapeCsvField(ticket.Reporter.FullName)}," +
                              $"{EscapeCsvField(ticket.Reporter.Department)}," +
                              $"{EscapeCsvField(ticket.Assignee?.UserId)}," +
                              $"{EscapeCsvField(ticket.Assignee?.Username)}," +
                              $"{EscapeCsvField(ticket.Assignee?.Email)}," +
                              $"{EscapeCsvField(ticket.Assignee?.FullName)}," +
                              $"{EscapeCsvField(ticket.Assignee?.Department)}," +
                              $"{EscapeCsvField(ticket.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))}," +
                              $"{EscapeCsvField(ticket.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))}," +
                              $"{EscapeCsvField(ticket.ResolvedAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))}," +
                              $"{EscapeCsvField(ticket.ClosedAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))}," +
                              $"{ticket.Comments.Count}," +
                              $"{ticket.Activities.Count}");
            }

            return csv.ToString();
        }

        /// <summary>
        /// Export ticket activities to separate CSV (since they are embedded arrays)
        /// </summary>
        public async Task<string> ExportTicketActivitiesAsync()
        {
            var tickets = await _ticketRepository.GetAllAsync(0, int.MaxValue);
            var csv = new StringBuilder();

            // CSV Header
            csv.AppendLine("TicketId,Action,PerformedByUserId,PerformedByUsername,PerformedByEmail," +
                          "PerformedByFullName,Timestamp,Description,OldValue,NewValue");

            // Data rows - flatten activities from all tickets
            foreach (var ticket in tickets)
            {
                foreach (var activity in ticket.Activities)
                {
                    csv.AppendLine($"{EscapeCsvField(ticket.Id)}," +
                                  $"{EscapeCsvField(activity.Action)}," +
                                  $"{EscapeCsvField(activity.PerformedBy.UserId)}," +
                                  $"{EscapeCsvField(activity.PerformedBy.Username)}," +
                                  $"{EscapeCsvField(activity.PerformedBy.Email)}," +
                                  $"{EscapeCsvField(activity.PerformedBy.FullName)}," +
                                  $"{EscapeCsvField(activity.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))}," +
                                  $"{EscapeCsvField(activity.Description)}," +
                                  $"{EscapeCsvField(activity.OldValue)}," +
                                  $"{EscapeCsvField(activity.NewValue)}");
                }
            }

            return csv.ToString();
        }

        /// <summary>
        /// Export ticket comments to separate CSV (since they are embedded arrays)
        /// </summary>
        public async Task<string> ExportTicketCommentsAsync()
        {
            var tickets = await _ticketRepository.GetAllAsync(0, int.MaxValue);
            var csv = new StringBuilder();

            // CSV Header
            csv.AppendLine("TicketId,CommentId,AuthorUserId,AuthorUsername,AuthorEmail," +
                          "AuthorFullName,Content,Timestamp,IsInternal");

            // Data rows - flatten comments from all tickets
            foreach (var ticket in tickets)
            {
                foreach (var comment in ticket.Comments)
                {
                    csv.AppendLine($"{EscapeCsvField(ticket.Id)}," +
                                  $"{EscapeCsvField(comment.CommentId)}," +
                                  $"{EscapeCsvField(comment.Author.UserId)}," +
                                  $"{EscapeCsvField(comment.Author.Username)}," +
                                  $"{EscapeCsvField(comment.Author.Email)}," +
                                  $"{EscapeCsvField(comment.Author.FullName)}," +
                                  $"{EscapeCsvField(comment.Content)}," +
                                  $"{EscapeCsvField(comment.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))}," +
                                  $"{comment.IsInternal}");
                }
            }

            return csv.ToString();
        }

        /// <summary>
        /// Export collection statistics for verification
        /// </summary>
        public async Task<string> ExportStatisticsAsync()
        {
            var userCount = await _userRepository.GetCountAsync();
            var ticketCount = await _ticketRepository.GetCountAsync();
            
            var csv = new StringBuilder();
            csv.AppendLine("Collection,DocumentCount,ExportTimestamp");
            csv.AppendLine($"Users,{userCount},{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            csv.AppendLine($"Tickets,{ticketCount},{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");

            return csv.ToString();
        }

        /// <summary>
        /// Save all CSV exports to files
        /// </summary>
        public async Task<string> ExportAllToFilesAsync(string outputDirectory = "CSVExports")
        {
            try
            {
                // Create output directory if it doesn't exist
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var results = new StringBuilder();

                // Export Users
                var usersContent = await ExportUsersAsync();
                var usersFile = Path.Combine(outputDirectory, $"Users_{timestamp}.csv");
                await File.WriteAllTextAsync(usersFile, usersContent, Encoding.UTF8);
                results.AppendLine($"✓ Users exported to: {usersFile}");

                // Export Tickets
                var ticketsContent = await ExportTicketsAsync();
                var ticketsFile = Path.Combine(outputDirectory, $"Tickets_{timestamp}.csv");
                await File.WriteAllTextAsync(ticketsFile, ticketsContent, Encoding.UTF8);
                results.AppendLine($"✓ Tickets exported to: {ticketsFile}");

                // Export Activities
                var activitiesContent = await ExportTicketActivitiesAsync();
                var activitiesFile = Path.Combine(outputDirectory, $"TicketActivities_{timestamp}.csv");
                await File.WriteAllTextAsync(activitiesFile, activitiesContent, Encoding.UTF8);
                results.AppendLine($"✓ Ticket Activities exported to: {activitiesFile}");

                // Export Comments
                var commentsContent = await ExportTicketCommentsAsync();
                var commentsFile = Path.Combine(outputDirectory, $"TicketComments_{timestamp}.csv");
                await File.WriteAllTextAsync(commentsFile, commentsContent, Encoding.UTF8);
                results.AppendLine($"✓ Ticket Comments exported to: {commentsFile}");

                // Export Statistics
                var statsContent = await ExportStatisticsAsync();
                var statsFile = Path.Combine(outputDirectory, $"Statistics_{timestamp}.csv");
                await File.WriteAllTextAsync(statsFile, statsContent, Encoding.UTF8);
                results.AppendLine($"✓ Statistics exported to: {statsFile}");

                results.AppendLine($"\n📁 All files exported to directory: {Path.GetFullPath(outputDirectory)}");
                results.AppendLine($"📅 Export completed at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

                return results.ToString();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to export CSV files: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Escape CSV fields to handle commas, quotes, and newlines
        /// </summary>
        private static string EscapeCsvField(string? field)
        {
            if (string.IsNullOrEmpty(field))
                return string.Empty;

            // If field contains comma, quote, or newline, wrap in quotes and escape quotes
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }

            return field;
        }
    }
}