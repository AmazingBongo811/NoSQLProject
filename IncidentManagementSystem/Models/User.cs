using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace IncidentManagementSystem.Models
{
    /// <summary>
    /// Represents a user in the incident management system.
    /// Supports both Regular employees and Service Desk personnel.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Unique identifier for the user in MongoDB
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        /// <summary>
        /// Unique username for login purposes
        /// </summary>
        [BsonElement("username")]
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// User's email address - must be unique
        /// </summary>
        [BsonElement("email")]
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Hashed password for authentication
        /// </summary>
        [BsonElement("passwordHash")]
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// User's first name
        /// </summary>
        [BsonElement("firstName")]
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// User's last name
        /// </summary>
        [BsonElement("lastName")]
        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// User role - either "Regular" or "ServiceDesk"
        /// </summary>
        [BsonElement("role")]
        [Required]
        public UserRole Role { get; set; } = UserRole.Regular;

        /// <summary>
        /// Department the user belongs to (optional)
        /// </summary>
        [BsonElement("department")]
        public string? Department { get; set; }

        /// <summary>
        /// Timestamp when the user account was created
        /// </summary>
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the user account was last updated
        /// </summary>
        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Indicates if the user account is active
        /// </summary>
        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Timestamp of the user's last login (optional)
        /// </summary>
        [BsonElement("lastLogin")]
        public DateTime? LastLogin { get; set; }

        /// <summary>
        /// Full name property for display purposes
        /// </summary>
        [BsonIgnore]
        public string FullName => $"{FirstName} {LastName}";

        /// <summary>
        /// Check if user is a Service Desk employee
        /// </summary>
        [BsonIgnore]
        public bool IsServiceDesk => Role == UserRole.ServiceDesk;
    }

    /// <summary>
    /// Enumeration for user roles in the system
    /// </summary>
    public enum UserRole
    {
        Regular,
        ServiceDesk
    }
}