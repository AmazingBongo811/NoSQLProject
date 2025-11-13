using MongoDB.Driver;
using IncidentManagementSystem.Models;

namespace IncidentManagementSystem.Data
{
    /// <summary>
    /// MongoDB database context that provides access to collections.
    /// Implements the repository pattern for data access abstraction.
    /// 
    /// Design Decision: Using a context class similar to Entity Framework
    /// to provide a consistent interface for data access operations.
    /// 
    /// Alternative Considered: Direct IMongoDatabase injection
    /// Rationale: Context provides better abstraction and easier testing
    /// </summary>
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;
        private readonly MongoDbSettings _settings;

        /// <summary>
        /// Gets the MongoDB database instance
        /// </summary>
        public IMongoDatabase Database => _database;

        /// <summary>
        /// Initializes a new instance of the MongoDbContext
        /// </summary>
        /// <param name="settings">MongoDB configuration settings</param>
        public MongoDbContext(MongoDbSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            
            // Create MongoDB client with connection string
            var client = new MongoClient(_settings.ConnectionString);
            
            // Get reference to the database
            _database = client.GetDatabase(_settings.DatabaseName);
            
            // Initialize collections and indexes
            InitializeCollections();
        }

        /// <summary>
        /// Users collection accessor
        /// Provides CRUD operations for User documents
        /// </summary>
        public IMongoCollection<User> Users => _database.GetCollection<User>("users");

        /// <summary>
        /// Tickets collection accessor
        /// Provides CRUD operations for Ticket documents
        /// </summary>
        public IMongoCollection<Ticket> Tickets => _database.GetCollection<Ticket>("tickets");

        /// <summary>
        /// Departments collection accessor
        /// Provides CRUD operations for Department documents
        /// </summary>
        public IMongoCollection<Department> Departments => _database.GetCollection<Department>("departments");

        /// <summary>
        /// Initialize collections and create indexes for optimal query performance.
        /// 
        /// Index Strategy Rationale:
        /// - Single field indexes for unique constraints and frequent filters
        /// - Compound indexes for common query patterns (user + status, status + date)
        /// - Text indexes for search functionality
        /// 
        /// Performance Consideration: Indexes are created asynchronously to avoid
        /// blocking application startup. In production, indexes should be created
        /// during deployment or database migration process.
        /// </summary>
        private void InitializeCollections()
        {
            try
            {
                // Create indexes for Users collection
                CreateUserIndexes();
                
                // Create indexes for Tickets collection
                CreateTicketIndexes();
                
                // Create indexes for Departments collection
                CreateDepartmentIndexes();
            }
            catch (Exception ex)
            {
                // Log index creation errors but don't fail application startup
                // In production, use proper logging framework
                Console.WriteLine($"Warning: Failed to create indexes: {ex.Message}");
            }
        }

        /// <summary>
        /// Create indexes for the Users collection
        /// </summary>
        private void CreateUserIndexes()
        {
            var usersCollection = Users;

            // Unique index on username
            var usernameIndex = Builders<User>.IndexKeys.Ascending(u => u.Username);
            usersCollection.Indexes.CreateOneAsync(new CreateIndexModel<User>(usernameIndex, 
                new CreateIndexOptions { Unique = true, Name = "idx_username_unique" }));

            // Unique index on email
            var emailIndex = Builders<User>.IndexKeys.Ascending(u => u.Email);
            usersCollection.Indexes.CreateOneAsync(new CreateIndexModel<User>(emailIndex, 
                new CreateIndexOptions { Unique = true, Name = "idx_email_unique" }));

            // Index on role for role-based queries
            var roleIndex = Builders<User>.IndexKeys.Ascending(u => u.Role);
            usersCollection.Indexes.CreateOneAsync(new CreateIndexModel<User>(roleIndex, 
                new CreateIndexOptions { Name = "idx_role" }));

            // Index on isActive for filtering active users
            var activeIndex = Builders<User>.IndexKeys.Ascending(u => u.IsActive);
            usersCollection.Indexes.CreateOneAsync(new CreateIndexModel<User>(activeIndex, 
                new CreateIndexOptions { Name = "idx_isActive" }));

            // Index on department for departmental queries
            var departmentIndex = Builders<User>.IndexKeys.Ascending(u => u.Department);
            usersCollection.Indexes.CreateOneAsync(new CreateIndexModel<User>(departmentIndex, 
                new CreateIndexOptions { Name = "idx_department" }));
        }

        /// <summary>
        /// Create indexes for the Tickets collection
        /// </summary>
        private void CreateTicketIndexes()
        {
            var ticketsCollection = Tickets;

            // Index on reporter.userId for user-specific ticket queries
            var reporterIndex = Builders<Ticket>.IndexKeys.Ascending(t => t.Reporter.UserId);
            ticketsCollection.Indexes.CreateOneAsync(new CreateIndexModel<Ticket>(reporterIndex, 
                new CreateIndexOptions { Name = "idx_reporter_userId" }));

            // Index on assignee.userId for assigned ticket queries
            var assigneeIndex = Builders<Ticket>.IndexKeys.Ascending(t => t.Assignee.UserId);
            ticketsCollection.Indexes.CreateOneAsync(new CreateIndexModel<Ticket>(assigneeIndex, 
                new CreateIndexOptions { Name = "idx_assignee_userId" }));

            // Index on status for status-based filtering
            var statusIndex = Builders<Ticket>.IndexKeys.Ascending(t => t.Status);
            ticketsCollection.Indexes.CreateOneAsync(new CreateIndexModel<Ticket>(statusIndex, 
                new CreateIndexOptions { Name = "idx_status" }));

            // Index on priority for priority-based queries
            var priorityIndex = Builders<Ticket>.IndexKeys.Ascending(t => t.Priority);
            ticketsCollection.Indexes.CreateOneAsync(new CreateIndexModel<Ticket>(priorityIndex, 
                new CreateIndexOptions { Name = "idx_priority" }));

            // Index on category for category-based filtering
            var categoryIndex = Builders<Ticket>.IndexKeys.Ascending(t => t.Category);
            ticketsCollection.Indexes.CreateOneAsync(new CreateIndexModel<Ticket>(categoryIndex, 
                new CreateIndexOptions { Name = "idx_category" }));

            // Index on createdAt for date-based sorting (descending for recent first)
            var createdAtIndex = Builders<Ticket>.IndexKeys.Descending(t => t.CreatedAt);
            ticketsCollection.Indexes.CreateOneAsync(new CreateIndexModel<Ticket>(createdAtIndex, 
                new CreateIndexOptions { Name = "idx_createdAt_desc" }));

            // Index on updatedAt for recently updated tickets
            var updatedAtIndex = Builders<Ticket>.IndexKeys.Descending(t => t.UpdatedAt);
            ticketsCollection.Indexes.CreateOneAsync(new CreateIndexModel<Ticket>(updatedAtIndex, 
                new CreateIndexOptions { Name = "idx_updatedAt_desc" }));

            // Compound index for user dashboard queries (reporter + status)
            var userDashboardIndex = Builders<Ticket>.IndexKeys
                .Ascending(t => t.Reporter.UserId)
                .Ascending(t => t.Status);
            ticketsCollection.Indexes.CreateOneAsync(new CreateIndexModel<Ticket>(userDashboardIndex, 
                new CreateIndexOptions { Name = "idx_reporter_status_compound" }));

            // Compound index for service desk dashboard (status + created date)
            var serviceDeskIndex = Builders<Ticket>.IndexKeys
                .Ascending(t => t.Status)
                .Descending(t => t.CreatedAt);
            ticketsCollection.Indexes.CreateOneAsync(new CreateIndexModel<Ticket>(serviceDeskIndex, 
                new CreateIndexOptions { Name = "idx_status_createdAt_compound" }));

            // Text index for search functionality on title and description
            var textIndex = Builders<Ticket>.IndexKeys
                .Text(t => t.Title)
                .Text(t => t.Description);
            ticketsCollection.Indexes.CreateOneAsync(new CreateIndexModel<Ticket>(textIndex, 
                new CreateIndexOptions { Name = "idx_text_search" }));
        }

        /// <summary>
        /// Create indexes for the Departments collection
        /// </summary>
        private void CreateDepartmentIndexes()
        {
            var departmentsCollection = Departments;

            // Unique index on department name
            var nameIndex = Builders<Department>.IndexKeys.Ascending(d => d.Name);
            departmentsCollection.Indexes.CreateOneAsync(new CreateIndexModel<Department>(nameIndex, 
                new CreateIndexOptions { Unique = true, Name = "idx_name_unique" }));

            // Index on isActive for filtering active departments
            var activeIndex = Builders<Department>.IndexKeys.Ascending(d => d.IsActive);
            departmentsCollection.Indexes.CreateOneAsync(new CreateIndexModel<Department>(activeIndex, 
                new CreateIndexOptions { Name = "idx_isActive" }));
        }

        /// <summary>
        /// Test the database connection
        /// </summary>
        /// <returns>True if connection is successful, false otherwise</returns>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                // Try to ping the database
                await _database.RunCommandAsync((Command<MongoDB.Bson.BsonDocument>)"{ping:1}");
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get database statistics for monitoring
        /// </summary>
        /// <returns>Database statistics object</returns>
        public async Task<MongoDB.Bson.BsonDocument> GetDatabaseStatsAsync()
        {
            try
            {
                var command = new MongoDB.Bson.BsonDocument("dbStats", 1);
                return await _database.RunCommandAsync<MongoDB.Bson.BsonDocument>(command);
            }
            catch
            {
                return new MongoDB.Bson.BsonDocument();
            }
        }
    }
}