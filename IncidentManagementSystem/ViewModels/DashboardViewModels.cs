using IncidentManagementSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace IncidentManagementSystem.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int ResolvedTickets { get; set; }
        public int UrgentTickets { get; set; }
        public List<Ticket> RecentTickets { get; set; } = new();
        public string UserName { get; set; } = "";
    }

    public class ServiceDeskDashboardViewModel
    {
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int ResolvedTickets { get; set; }
        public int ClosedTickets { get; set; }
        public int UrgentTickets { get; set; }
        public int UnassignedTickets { get; set; }
        public List<Ticket> RecentTickets { get; set; } = new();
    }

    public class AnalyticsViewModel
    {
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int ResolvedTickets { get; set; }
        public int ClosedTickets { get; set; }
        public int HighPriorityTickets { get; set; }
        public int MediumPriorityTickets { get; set; }
        public int LowPriorityTickets { get; set; }
        public int CriticalPriorityTickets { get; set; }
        public double AverageResolutionTimeHours { get; set; }
    }

    /// <summary>
    /// ViewModel for Advanced Search functionality (Individual Requirement)
    /// Supports AND/OR search operations with result ordering
    /// </summary>
    public class AdvancedSearchViewModel
    {
        [Display(Name = "Search Terms")]
        [StringLength(500, ErrorMessage = "Search text cannot exceed 500 characters")]
        public string SearchText { get; set; } = "";

        [Display(Name = "Status")]
        public TicketStatus? Status { get; set; }

        [Display(Name = "Priority")]
        public TicketPriority? Priority { get; set; }

        [Display(Name = "Category")]
        public TicketCategory? Category { get; set; }

        [Display(Name = "Assigned To")]
        public string? AssigneeId { get; set; }

        [Display(Name = "Date From")]
        [DataType(DataType.Date)]
        public DateTime? DateFrom { get; set; }

        [Display(Name = "Date To")]
        [DataType(DataType.Date)]
        public DateTime? DateTo { get; set; }

        // Pagination properties
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalResults { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalResults / PageSize);

        // Results
        public List<Ticket> Results { get; set; } = new();
        public bool HasSearched { get; set; }

        // For service desk users - available assignees
        public List<dynamic> AvailableAssignees { get; set; } = new();

        // Search examples for help text
        public static List<string> SearchExamples => new()
        {
            "network AND server",
            "email OR printer",
            "password reset AND urgent",
            "network AND (server OR router)",
            "#12345 (search by ticket ID)"
        };

        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }
}