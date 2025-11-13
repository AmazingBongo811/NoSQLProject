namespace IncidentManagementSystem.Data
{
    /// <summary>
    /// Configuration settings for MongoDB connection and database.
    /// Used for dependency injection and configuration management.
    /// </summary>
    public class MongoDbSettings
    {
        /// <summary>
        /// MongoDB connection string
        /// Should include authentication if required
        /// Example: "mongodb://localhost:27017" or "mongodb+srv://user:pass@cluster.mongodb.net"
        /// </summary>
        public string ConnectionString { get; set; } = "mongodb://localhost:27017";

        /// <summary>
        /// Name of the database to use for the incident management system
        /// </summary>
        public string DatabaseName { get; set; } = "IncidentManagementDB";

        /// <summary>
        /// Configuration section name in appsettings.json
        /// </summary>
        public const string SectionName = "MongoDB";
    }
}