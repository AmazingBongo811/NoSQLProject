using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace IncidentManagementSystem.Models
{
    /// <summary>
    /// Represents a department in the organization.
    /// Used for organizational structure and reporting.
    /// </summary>
    public class Department
    {
        /// <summary>
        /// Unique identifier for the department in MongoDB
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        /// <summary>
        /// Name of the department - must be unique
        /// </summary>
        [BsonElement("name")]
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the department's function (optional)
        /// </summary>
        [BsonElement("description")]
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Manager information (optional)
        /// </summary>
        [BsonElement("manager")]
        public UserInfo? Manager { get; set; }

        /// <summary>
        /// Timestamp when the department was created
        /// </summary>
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Indicates if the department is active
        /// </summary>
        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;
    }
}