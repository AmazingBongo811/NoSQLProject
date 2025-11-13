using IncidentManagementSystem.Models;

namespace IncidentManagementSystem.Repositories
{
    /// <summary>
    /// Repository interface specific to Ticket entity operations.
    /// Extends generic repository with ticket-specific queries and aggregations.
    /// 
    /// Aggregation Pipeline Strategy:
    /// - Dashboard statistics use MongoDB aggregation framework
    /// - Complex filtering combines multiple criteria efficiently
    /// - Grouping operations for reporting and analytics
    /// </summary>
    public interface ITicketRepository : IRepository<Ticket>
    {
        /// <summary>
        /// Get tickets reported by specific user
        /// </summary>
        /// <param name="userId">Reporter user ID</param>
        /// <param name="skip">Number of documents to skip</param>
        /// <param name="limit">Maximum number of documents to return</param>
        /// <returns>Collection of tickets reported by user</returns>
        Task<IEnumerable<Ticket>> GetTicketsByReporterAsync(string userId, int skip = 0, int limit = 100);

        /// <summary>
        /// Get tickets assigned to specific service desk employee
        /// </summary>
        /// <param name="assigneeId">Assignee user ID</param>
        /// <param name="skip">Number of documents to skip</param>
        /// <param name="limit">Maximum number of documents to return</param>
        /// <returns>Collection of assigned tickets</returns>
        Task<IEnumerable<Ticket>> GetTicketsByAssigneeAsync(string assigneeId, int skip = 0, int limit = 100);

        /// <summary>
        /// Get tickets filtered by status
        /// </summary>
        /// <param name="status">Ticket status to filter by</param>
        /// <param name="skip">Number of documents to skip</param>
        /// <param name="limit">Maximum number of documents to return</param>
        /// <returns>Collection of tickets with specified status</returns>
        Task<IEnumerable<Ticket>> GetTicketsByStatusAsync(TicketStatus status, int skip = 0, int limit = 100);

        /// <summary>
        /// Get tickets filtered by priority level
        /// </summary>
        /// <param name="priority">Priority level to filter by</param>
        /// <param name="skip">Number of documents to skip</param>
        /// <param name="limit">Maximum number of documents to return</param>
        /// <returns>Collection of tickets with specified priority</returns>
        Task<IEnumerable<Ticket>> GetTicketsByPriorityAsync(TicketPriority priority, int skip = 0, int limit = 100);

        /// <summary>
        /// Get tickets filtered by category
        /// </summary>
        /// <param name="category">Category to filter by</param>
        /// <param name="skip">Number of documents to skip</param>
        /// <param name="limit">Maximum number of documents to return</param>
        /// <returns>Collection of tickets in specified category</returns>
        Task<IEnumerable<Ticket>> GetTicketsByCategoryAsync(TicketCategory category, int skip = 0, int limit = 100);

        /// <summary>
        /// Get tickets for specific reporter with status filtering (for user dashboard)
        /// </summary>
        /// <param name="reporterId">Reporter user ID</param>
        /// <param name="status">Optional status filter</param>
        /// <param name="skip">Number of documents to skip</param>
        /// <param name="limit">Maximum number of documents to return</param>
        /// <returns>Collection of filtered tickets</returns>
        Task<IEnumerable<Ticket>> GetUserTicketsAsync(string reporterId, TicketStatus? status = null, int skip = 0, int limit = 100);

        /// <summary>
        /// Search tickets by text in title or description
        /// </summary>
        /// <param name="searchTerm">Text to search for</param>
        /// <param name="skip">Number of documents to skip</param>
        /// <param name="limit">Maximum number of documents to return</param>
        /// <returns>Collection of matching tickets</returns>
        Task<IEnumerable<Ticket>> SearchTicketsAsync(string searchTerm, int skip = 0, int limit = 100);

        /// <summary>
        /// Add comment to existing ticket
        /// </summary>
        /// <param name="ticketId">Ticket identifier</param>
        /// <param name="comment">Comment to add</param>
        /// <returns>True if comment added successfully</returns>
        Task<bool> AddCommentAsync(string ticketId, TicketComment comment);

        /// <summary>
        /// Add activity log entry to ticket
        /// </summary>
        /// <param name="ticketId">Ticket identifier</param>
        /// <param name="activity">Activity to log</param>
        /// <returns>True if activity logged successfully</returns>
        Task<bool> AddActivityAsync(string ticketId, TicketActivity activity);

        /// <summary>
        /// Update ticket status with activity logging
        /// </summary>
        /// <param name="ticketId">Ticket identifier</param>
        /// <param name="newStatus">New status to set</param>
        /// <param name="updatedBy">User performing the update</param>
        /// <returns>True if status updated successfully</returns>
        Task<bool> UpdateStatusAsync(string ticketId, TicketStatus newStatus, UserInfo updatedBy);

        /// <summary>
        /// Assign ticket to service desk employee
        /// </summary>
        /// <param name="ticketId">Ticket identifier</param>
        /// <param name="assignee">Service desk employee to assign</param>
        /// <param name="assignedBy">User performing the assignment</param>
        /// <returns>True if assignment successful</returns>
        Task<bool> AssignTicketAsync(string ticketId, UserInfo assignee, UserInfo assignedBy);

        /// <summary>
        /// Get ticket statistics for user dashboard (aggregation pipeline)
        /// </summary>
        /// <param name="userId">User ID to get statistics for</param>
        /// <returns>Dashboard statistics object</returns>
        Task<UserDashboardStats> GetUserDashboardStatsAsync(string userId);

        /// <summary>
        /// Get ticket statistics for service desk dashboard (aggregation pipeline)
        /// </summary>
        /// <returns>Service desk dashboard statistics</returns>
        Task<ServiceDeskDashboardStats> GetServiceDeskDashboardStatsAsync();

        /// <summary>
        /// Get tickets created within date range
        /// </summary>
        /// <param name="startDate">Start date (inclusive)</param>
        /// <param name="endDate">End date (inclusive)</param>
        /// <param name="skip">Number of documents to skip</param>
        /// <param name="limit">Maximum number of documents to return</param>
        /// <returns>Collection of tickets in date range</returns>
        Task<IEnumerable<Ticket>> GetTicketsByDateRangeAsync(DateTime startDate, DateTime endDate, int skip = 0, int limit = 100);

        /// <summary>
        /// Get overdue tickets (open tickets past expected resolution time)
        /// </summary>
        /// <param name="cutoffDate">Date threshold for overdue calculation</param>
        /// <param name="skip">Number of documents to skip</param>
        /// <param name="limit">Maximum number of documents to return</param>
        /// <returns>Collection of overdue tickets</returns>
        Task<IEnumerable<Ticket>> GetOverdueTicketsAsync(DateTime cutoffDate, int skip = 0, int limit = 100);
    }

    /// <summary>
    /// Dashboard statistics for regular users
    /// </summary>
    public class UserDashboardStats
    {
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int ResolvedTickets { get; set; }
        public int ClosedTickets { get; set; }
        public double OpenPercentage => TotalTickets > 0 ? (double)OpenTickets / TotalTickets * 100 : 0;
        public double ResolvedPercentage => TotalTickets > 0 ? (double)ResolvedTickets / TotalTickets * 100 : 0;
        public double ClosedPercentage => TotalTickets > 0 ? (double)ClosedTickets / TotalTickets * 100 : 0;
    }

    /// <summary>
    /// Dashboard statistics for service desk employees
    /// </summary>
    public class ServiceDeskDashboardStats
    {
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int ResolvedTickets { get; set; }
        public int ClosedTickets { get; set; }
        public int UnassignedTickets { get; set; }
        public double AverageResolutionTimeHours { get; set; }
        public Dictionary<TicketPriority, int> TicketsByPriority { get; set; } = new();
        public Dictionary<TicketCategory, int> TicketsByCategory { get; set; } = new();
        public Dictionary<string, int> TicketsByAssignee { get; set; } = new();
        public double OpenPercentage => TotalTickets > 0 ? (double)OpenTickets / TotalTickets * 100 : 0;
        public double ResolvedPercentage => TotalTickets > 0 ? (double)ResolvedTickets / TotalTickets * 100 : 0;
        public double ClosedPercentage => TotalTickets > 0 ? (double)ClosedTickets / TotalTickets * 100 : 0;
    }
}