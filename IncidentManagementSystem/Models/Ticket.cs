using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace IncidentManagementSystem.Models
{
    /// <summary>
    /// Represents a ticket (incident) in the management system.
    /// Contains embedded user information for performance optimization.
    /// </summary>
    public class Ticket
    {
        /// <summary>
        /// Unique identifier for the ticket in MongoDB
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        /// <summary>
        /// Brief title describing the incident
        /// </summary>
        [BsonElement("title")]
        [Required]
        [StringLength(200, MinimumLength = 5)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of the incident
        /// </summary>
        [BsonElement("description")]
        [Required]
        [StringLength(2000, MinimumLength = 10)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Current status of the ticket
        /// </summary>
        [BsonElement("status")]
        public TicketStatus Status { get; set; } = TicketStatus.Open;

        /// <summary>
        /// Priority level of the ticket
        /// </summary>
        [BsonElement("priority")]
        public TicketPriority Priority { get; set; } = TicketPriority.Medium;

        /// <summary>
        /// Category classification of the incident
        /// </summary>
        [BsonElement("category")]
        public TicketCategory Category { get; set; } = TicketCategory.Other;

        /// <summary>
        /// Information about the user who reported the ticket
        /// Embedded for performance optimization
        /// </summary>
        [BsonElement("reporter")]
        [Required]
        public UserInfo Reporter { get; set; } = new UserInfo();

        /// <summary>
        /// Information about the assigned Service Desk employee
        /// Optional - tickets may not be assigned initially
        /// </summary>
        [BsonElement("assignee")]
        public UserInfo? Assignee { get; set; }

        /// <summary>
        /// Timestamp when the ticket was created
        /// </summary>
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the ticket was last updated
        /// </summary>
        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the ticket was resolved (optional)
        /// </summary>
        [BsonElement("resolvedAt")]
        public DateTime? ResolvedAt { get; set; }

        /// <summary>
        /// Timestamp when the ticket was closed (optional)
        /// </summary>
        [BsonElement("closedAt")]
        public DateTime? ClosedAt { get; set; }

        /// <summary>
        /// Activity log for audit trail
        /// </summary>
        [BsonElement("activities")]
        public List<TicketActivity> Activities { get; set; } = new List<TicketActivity>();

        /// <summary>
        /// Comments and communication history
        /// </summary>
        [BsonElement("comments")]
        public List<TicketComment> Comments { get; set; } = new List<TicketComment>();

        /// <summary>
        /// Calculated property for ticket age in days
        /// </summary>
        [BsonIgnore]
        public int AgeDays => (DateTime.UtcNow - CreatedAt).Days;

        /// <summary>
        /// Calculated property for resolution time in hours (if resolved)
        /// </summary>
        [BsonIgnore]
        public double? ResolutionTimeHours => ResolvedAt?.Subtract(CreatedAt).TotalHours;

        /// <summary>
        /// Check if ticket is currently open
        /// </summary>
        [BsonIgnore]
        public bool IsOpen => Status == TicketStatus.Open;

        /// <summary>
        /// Check if ticket is resolved
        /// </summary>
        [BsonIgnore]
        public bool IsResolved => Status == TicketStatus.Resolved;

        /// <summary>
        /// Check if ticket is closed
        /// </summary>
        [BsonIgnore]
        public bool IsClosed => Status == TicketStatus.Closed;
    }

    /// <summary>
    /// Embedded user information for performance optimization
    /// Reduces the need for joins in common queries
    /// </summary>
    public class UserInfo
    {
        /// <summary>
        /// Reference to the User document
        /// </summary>
        [BsonElement("userId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Username for display and identification
        /// </summary>
        [BsonElement("username")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Email for contact purposes
        /// </summary>
        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Full name for display
        /// </summary>
        [BsonElement("fullName")]
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Department information (optional)
        /// </summary>
        [BsonElement("department")]
        public string? Department { get; set; }
    }

    /// <summary>
    /// Represents an activity/action performed on a ticket
    /// Used for audit trail and history tracking
    /// </summary>
    public class TicketActivity
    {
        /// <summary>
        /// Type of action performed
        /// </summary>
        [BsonElement("action")]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// User who performed the action
        /// </summary>
        [BsonElement("performedBy")]
        public UserInfo PerformedBy { get; set; } = new UserInfo();

        /// <summary>
        /// When the action was performed
        /// </summary>
        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional description of the action
        /// </summary>
        [BsonElement("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Previous value (for update operations)
        /// </summary>
        [BsonElement("oldValue")]
        public string? OldValue { get; set; }

        /// <summary>
        /// New value (for update operations)
        /// </summary>
        [BsonElement("newValue")]
        public string? NewValue { get; set; }
    }

    /// <summary>
    /// Represents a comment on a ticket
    /// Used for communication between users and service desk
    /// </summary>
    public class TicketComment
    {
        /// <summary>
        /// Unique identifier for the comment
        /// </summary>
        [BsonElement("commentId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string CommentId { get; set; } = ObjectId.GenerateNewId().ToString();

        /// <summary>
        /// Author of the comment
        /// </summary>
        [BsonElement("author")]
        public UserInfo Author { get; set; } = new UserInfo();

        /// <summary>
        /// Content of the comment
        /// </summary>
        [BsonElement("content")]
        [Required]
        [StringLength(1000, MinimumLength = 1)]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// When the comment was created
        /// </summary>
        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Whether the comment is internal (Service Desk only)
        /// </summary>
        [BsonElement("isInternal")]
        public bool IsInternal { get; set; } = false;
    }

    /// <summary>
    /// Enumeration for ticket status values
    /// </summary>
    public enum TicketStatus
    {
        Open,
        Resolved,
        Closed
    }

    /// <summary>
    /// Enumeration for ticket priority levels
    /// </summary>
    public enum TicketPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Enumeration for ticket categories
    /// </summary>
    public enum TicketCategory
    {
        Hardware,
        Software,
        Network,
        Access,
        Other
    }
}