using MongoDB.Driver;
using MongoDB.Bson;
using IncidentManagementSystem.Data;
using IncidentManagementSystem.Models;

namespace IncidentManagementSystem.Repositories
{
    /// <summary>
    /// MongoDB implementation of the Ticket repository.
    /// Provides comprehensive CRUD operations, complex queries, and aggregation pipelines.
    /// 
    /// Aggregation Pipeline Design:
    /// - Uses MongoDB's powerful aggregation framework for dashboard statistics
    /// - Optimizes complex grouping and calculation operations
    /// - Implements efficient filtering and sorting for large datasets
    /// 
    /// Query Optimization Strategy:
    /// - Leverages compound indexes for multi-criteria filtering
    /// - Uses projection to minimize data transfer when possible
    /// - Implements efficient pagination patterns
    /// </summary>
    public class TicketRepository : ITicketRepository
    {
        private readonly IMongoCollection<Ticket> _tickets;

        /// <summary>
        /// Initialize repository with MongoDB context
        /// </summary>
        /// <param name="context">MongoDB database context</param>
        public TicketRepository(MongoDbContext context)
        {
            _tickets = context.Tickets ?? throw new ArgumentNullException(nameof(context.Tickets));
        }

        #region Generic Repository Implementation

        /// <summary>
        /// Get all tickets with pagination, sorted by creation date (newest first)
        /// </summary>
        public async Task<IEnumerable<Ticket>> GetAllAsync(int skip = 0, int limit = 100)
        {
            try
            {
                return await _tickets
                    .Find(FilterDefinition<Ticket>.Empty)
                    .Skip(skip)
                    .Limit(limit)
                    .SortByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve tickets", ex);
            }
        }

        /// <summary>
        /// Get ticket by MongoDB ObjectId
        /// </summary>
        public async Task<Ticket?> GetByIdAsync(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return null;

                return await _tickets
                    .Find(t => t.Id == id)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve ticket with ID {id}", ex);
            }
        }

        /// <summary>
        /// Create new ticket with initial activity log
        /// </summary>
        public async Task<Ticket> CreateAsync(Ticket ticket)
        {
            try
            {
                if (ticket == null)
                    throw new ArgumentNullException(nameof(ticket));

                // Set timestamps
                ticket.CreatedAt = DateTime.UtcNow;
                ticket.UpdatedAt = DateTime.UtcNow;

                // Add creation activity
                var createActivity = new TicketActivity
                {
                    Action = "Created",
                    PerformedBy = ticket.Reporter,
                    Timestamp = ticket.CreatedAt,
                    Description = "Ticket created"
                };
                ticket.Activities.Add(createActivity);

                await _tickets.InsertOneAsync(ticket);
                return ticket;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create ticket", ex);
            }
        }

        /// <summary>
        /// Update existing ticket with automatic timestamp update
        /// </summary>
        public async Task<Ticket?> UpdateAsync(string id, Ticket ticket)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id) || ticket == null)
                    return null;

                ticket.Id = id;
                ticket.UpdatedAt = DateTime.UtcNow;

                var result = await _tickets.ReplaceOneAsync(
                    t => t.Id == id,
                    ticket);

                return result.MatchedCount > 0 ? ticket : null;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to update ticket with ID {id}", ex);
            }
        }

        /// <summary>
        /// Delete ticket permanently from database
        /// Note: In production, consider soft delete for audit purposes
        /// </summary>
        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return false;

                var result = await _tickets.DeleteOneAsync(t => t.Id == id);
                return result.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to delete ticket with ID {id}", ex);
            }
        }

        /// <summary>
        /// Check if ticket exists by ID
        /// </summary>
        public async Task<bool> ExistsAsync(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return false;

                var count = await _tickets.CountDocumentsAsync(t => t.Id == id);
                return count > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to check existence of ticket with ID {id}", ex);
            }
        }

        /// <summary>
        /// Get total count of tickets
        /// </summary>
        public async Task<long> GetCountAsync()
        {
            try
            {
                return await _tickets.CountDocumentsAsync(FilterDefinition<Ticket>.Empty);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to get ticket count", ex);
            }
        }

        #endregion

        #region Ticket-Specific Query Methods

        /// <summary>
        /// Get tickets reported by specific user with pagination
        /// Uses compound index on reporter.userId and status for optimal performance
        /// </summary>
        public async Task<IEnumerable<Ticket>> GetTicketsByReporterAsync(string userId, int skip = 0, int limit = 100)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return Enumerable.Empty<Ticket>();

                return await _tickets
                    .Find(t => t.Reporter.UserId == userId)
                    .Skip(skip)
                    .Limit(limit)
                    .SortByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve tickets for reporter {userId}", ex);
            }
        }

        /// <summary>
        /// Get tickets assigned to specific service desk employee
        /// </summary>
        public async Task<IEnumerable<Ticket>> GetTicketsByAssigneeAsync(string assigneeId, int skip = 0, int limit = 100)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(assigneeId))
                    return Enumerable.Empty<Ticket>();

                return await _tickets
                    .Find(t => t.Assignee != null && t.Assignee.UserId == assigneeId)
                    .Skip(skip)
                    .Limit(limit)
                    .SortByDescending(t => t.Priority)
                    .ThenBy(t => t.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve tickets for assignee {assigneeId}", ex);
            }
        }

        /// <summary>
        /// Get tickets filtered by status with pagination
        /// Uses indexed status field for efficient filtering
        /// </summary>
        public async Task<IEnumerable<Ticket>> GetTicketsByStatusAsync(TicketStatus status, int skip = 0, int limit = 100)
        {
            try
            {
                return await _tickets
                    .Find(t => t.Status == status)
                    .Skip(skip)
                    .Limit(limit)
                    .SortByDescending(t => t.UpdatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve tickets with status {status}", ex);
            }
        }

        /// <summary>
        /// Get tickets filtered by priority level
        /// </summary>
        public async Task<IEnumerable<Ticket>> GetTicketsByPriorityAsync(TicketPriority priority, int skip = 0, int limit = 100)
        {
            try
            {
                return await _tickets
                    .Find(t => t.Priority == priority)
                    .Skip(skip)
                    .Limit(limit)
                    .SortBy(t => t.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve tickets with priority {priority}", ex);
            }
        }

        /// <summary>
        /// Get tickets filtered by category
        /// </summary>
        public async Task<IEnumerable<Ticket>> GetTicketsByCategoryAsync(TicketCategory category, int skip = 0, int limit = 100)
        {
            try
            {
                return await _tickets
                    .Find(t => t.Category == category)
                    .Skip(skip)
                    .Limit(limit)
                    .SortByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve tickets with category {category}", ex);
            }
        }

        /// <summary>
        /// Get tickets for specific user with optional status filtering
        /// Optimized for user dashboard queries
        /// </summary>
        public async Task<IEnumerable<Ticket>> GetUserTicketsAsync(string reporterId, TicketStatus? status = null, int skip = 0, int limit = 100)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reporterId))
                    return Enumerable.Empty<Ticket>();

                var filterBuilder = Builders<Ticket>.Filter;
                var filter = filterBuilder.Eq(t => t.Reporter.UserId, reporterId);

                if (status.HasValue)
                {
                    filter = filterBuilder.And(filter, filterBuilder.Eq(t => t.Status, status.Value));
                }

                return await _tickets
                    .Find(filter)
                    .Skip(skip)
                    .Limit(limit)
                    .SortByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve user tickets for {reporterId}", ex);
            }
        }

        /// <summary>
        /// Search tickets using MongoDB text search on title and description
        /// Requires text index on title and description fields
        /// </summary>
        public async Task<IEnumerable<Ticket>> SearchTicketsAsync(string searchTerm, int skip = 0, int limit = 100)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return Enumerable.Empty<Ticket>();

                // Use text search if available, otherwise use regex
                var filter = Builders<Ticket>.Filter.Text(searchTerm);
                
                return await _tickets
                    .Find(filter)
                    .Skip(skip)
                    .Limit(limit)
                    .Sort(Builders<Ticket>.Sort.MetaTextScore("score"))
                    .ToListAsync();
            }
            catch (Exception)
            {
                // Fallback to regex search if text index not available
                var regexFilter = Builders<Ticket>.Filter.Or(
                    Builders<Ticket>.Filter.Regex(t => t.Title, new BsonRegularExpression(searchTerm, "i")),
                    Builders<Ticket>.Filter.Regex(t => t.Description, new BsonRegularExpression(searchTerm, "i"))
                );

                return await _tickets
                    .Find(regexFilter)
                    .Skip(skip)
                    .Limit(limit)
                    .SortByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Get tickets created within specific date range
        /// </summary>
        public async Task<IEnumerable<Ticket>> GetTicketsByDateRangeAsync(DateTime startDate, DateTime endDate, int skip = 0, int limit = 100)
        {
            try
            {
                var filter = Builders<Ticket>.Filter.And(
                    Builders<Ticket>.Filter.Gte(t => t.CreatedAt, startDate),
                    Builders<Ticket>.Filter.Lte(t => t.CreatedAt, endDate)
                );

                return await _tickets
                    .Find(filter)
                    .Skip(skip)
                    .Limit(limit)
                    .SortByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve tickets in date range {startDate} to {endDate}", ex);
            }
        }

        /// <summary>
        /// Get overdue open tickets past the cutoff date
        /// </summary>
        public async Task<IEnumerable<Ticket>> GetOverdueTicketsAsync(DateTime cutoffDate, int skip = 0, int limit = 100)
        {
            try
            {
                var filter = Builders<Ticket>.Filter.And(
                    Builders<Ticket>.Filter.Eq(t => t.Status, TicketStatus.Open),
                    Builders<Ticket>.Filter.Lt(t => t.CreatedAt, cutoffDate)
                );

                return await _tickets
                    .Find(filter)
                    .Skip(skip)
                    .Limit(limit)
                    .SortBy(t => t.CreatedAt) // Oldest first
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve overdue tickets before {cutoffDate}", ex);
            }
        }

        #endregion

        #region Ticket Modification Methods

        /// <summary>
        /// Add comment to existing ticket using array update operation
        /// </summary>
        public async Task<bool> AddCommentAsync(string ticketId, TicketComment comment)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ticketId) || comment == null)
                    return false;

                var update = Builders<Ticket>.Update
                    .Push(t => t.Comments, comment)
                    .Set(t => t.UpdatedAt, DateTime.UtcNow);

                var result = await _tickets.UpdateOneAsync(
                    t => t.Id == ticketId,
                    update);

                return result.MatchedCount > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to add comment to ticket {ticketId}", ex);
            }
        }

        /// <summary>
        /// Add activity log entry to ticket using array update operation
        /// </summary>
        public async Task<bool> AddActivityAsync(string ticketId, TicketActivity activity)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ticketId) || activity == null)
                    return false;

                var update = Builders<Ticket>.Update
                    .Push(t => t.Activities, activity)
                    .Set(t => t.UpdatedAt, DateTime.UtcNow);

                var result = await _tickets.UpdateOneAsync(
                    t => t.Id == ticketId,
                    update);

                return result.MatchedCount > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to add activity to ticket {ticketId}", ex);
            }
        }

        /// <summary>
        /// Update ticket status with automatic timestamp and activity logging
        /// </summary>
        public async Task<bool> UpdateStatusAsync(string ticketId, TicketStatus newStatus, UserInfo updatedBy)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ticketId) || updatedBy == null)
                    return false;

                var now = DateTime.UtcNow;
                var activity = new TicketActivity
                {
                    Action = "Status Changed",
                    PerformedBy = updatedBy,
                    Timestamp = now,
                    NewValue = newStatus.ToString(),
                    Description = $"Status changed to {newStatus}"
                };

                var updateBuilder = Builders<Ticket>.Update
                    .Set(t => t.Status, newStatus)
                    .Set(t => t.UpdatedAt, now)
                    .Push(t => t.Activities, activity);

                // Set resolution/closure timestamps based on status
                if (newStatus == TicketStatus.Resolved)
                {
                    updateBuilder = updateBuilder.Set(t => t.ResolvedAt, now);
                }
                else if (newStatus == TicketStatus.Closed)
                {
                    updateBuilder = updateBuilder.Set(t => t.ClosedAt, now);
                }

                var result = await _tickets.UpdateOneAsync(
                    t => t.Id == ticketId,
                    updateBuilder);

                return result.MatchedCount > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to update status for ticket {ticketId}", ex);
            }
        }

        /// <summary>
        /// Assign ticket to service desk employee with activity logging
        /// </summary>
        public async Task<bool> AssignTicketAsync(string ticketId, UserInfo assignee, UserInfo assignedBy)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ticketId) || assignee == null || assignedBy == null)
                    return false;

                var activity = new TicketActivity
                {
                    Action = "Assigned",
                    PerformedBy = assignedBy,
                    Timestamp = DateTime.UtcNow,
                    Description = $"Assigned to {assignee.FullName}",
                    NewValue = assignee.Username
                };

                var update = Builders<Ticket>.Update
                    .Set(t => t.Assignee, assignee)
                    .Set(t => t.UpdatedAt, DateTime.UtcNow)
                    .Push(t => t.Activities, activity);

                var result = await _tickets.UpdateOneAsync(
                    t => t.Id == ticketId,
                    update);

                return result.MatchedCount > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to assign ticket {ticketId}", ex);
            }
        }

        #endregion

        #region Dashboard Statistics (Aggregation Pipelines)

        /// <summary>
        /// Get user dashboard statistics using MongoDB aggregation pipeline
        /// Calculates ticket counts by status for a specific user
        /// </summary>
        public async Task<UserDashboardStats> GetUserDashboardStatsAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return new UserDashboardStats();

                var pipeline = new BsonDocument[]
                {
                    new("$match", new BsonDocument("reporter.userId", userId)),
                    new("$group", new BsonDocument
                    {
                        { "_id", "$status" },
                        { "count", new BsonDocument("$sum", 1) }
                    }),
                    new("$group", new BsonDocument
                    {
                        { "_id", BsonNull.Value },
                        { "total", new BsonDocument("$sum", "$count") },
                        { "statuses", new BsonDocument("$push", new BsonDocument
                            {
                                { "status", "$_id" },
                                { "count", "$count" }
                            })
                        }
                    })
                };

                var result = await _tickets.AggregateAsync<BsonDocument>(pipeline);
                var document = await result.FirstOrDefaultAsync();

                if (document == null)
                    return new UserDashboardStats();

                var stats = new UserDashboardStats
                {
                    TotalTickets = document.GetValue("total", 0).AsInt32
                };

                var statusArray = document.GetValue("statuses", new BsonArray()).AsBsonArray;
                foreach (var statusDoc in statusArray)
                {
                    var statusName = statusDoc["status"].AsString;
                    var count = statusDoc["count"].AsInt32;

                    switch (statusName)
                    {
                        case "Open":
                            stats.OpenTickets = count;
                            break;
                        case "Resolved":
                            stats.ResolvedTickets = count;
                            break;
                        case "Closed":
                            stats.ClosedTickets = count;
                            break;
                    }
                }

                return stats;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get user dashboard stats for {userId}", ex);
            }
        }

        /// <summary>
        /// Get service desk dashboard statistics using comprehensive aggregation pipeline
        /// Provides detailed statistics across all tickets for service desk overview
        /// </summary>
        public async Task<ServiceDeskDashboardStats> GetServiceDeskDashboardStatsAsync()
        {
            try
            {
                // Main statistics pipeline
                var mainPipeline = new BsonDocument[]
                {
                    new("$facet", new BsonDocument
                    {
                        // Status distribution
                        { "statusStats", new BsonArray
                            {
                                new BsonDocument("$group", new BsonDocument
                                {
                                    { "_id", "$status" },
                                    { "count", new BsonDocument("$sum", 1) }
                                })
                            }
                        },
                        // Priority distribution
                        { "priorityStats", new BsonArray
                            {
                                new BsonDocument("$group", new BsonDocument
                                {
                                    { "_id", "$priority" },
                                    { "count", new BsonDocument("$sum", 1) }
                                })
                            }
                        },
                        // Category distribution
                        { "categoryStats", new BsonArray
                            {
                                new BsonDocument("$group", new BsonDocument
                                {
                                    { "_id", "$category" },
                                    { "count", new BsonDocument("$sum", 1) }
                                })
                            }
                        },
                        // Assignee distribution
                        { "assigneeStats", new BsonArray
                            {
                                new BsonDocument("$match", new BsonDocument("assignee", new BsonDocument("$ne", BsonNull.Value))),
                                new BsonDocument("$group", new BsonDocument
                                {
                                    { "_id", "$assignee.fullName" },
                                    { "count", new BsonDocument("$sum", 1) }
                                })
                            }
                        },
                        // Resolution time calculation
                        { "resolutionStats", new BsonArray
                            {
                                new BsonDocument("$match", new BsonDocument("resolvedAt", new BsonDocument("$ne", BsonNull.Value))),
                                new BsonDocument("$addFields", new BsonDocument("resolutionTimeHours",
                                    new BsonDocument("$divide", new BsonArray
                                    {
                                        new BsonDocument("$subtract", new BsonArray { "$resolvedAt", "$createdAt" }),
                                        3600000 // Convert milliseconds to hours
                                    })
                                )),
                                new BsonDocument("$group", new BsonDocument
                                {
                                    { "_id", BsonNull.Value },
                                    { "avgResolutionTime", new BsonDocument("$avg", "$resolutionTimeHours") }
                                })
                            }
                        },
                        // Total and unassigned counts
                        { "generalStats", new BsonArray
                            {
                                new BsonDocument("$group", new BsonDocument
                                {
                                    { "_id", BsonNull.Value },
                                    { "total", new BsonDocument("$sum", 1) },
                                    { "unassigned", new BsonDocument("$sum",
                                        new BsonDocument("$cond", new BsonArray
                                        {
                                            new BsonDocument("$eq", new BsonArray { "$assignee", BsonNull.Value }),
                                            1,
                                            0
                                        })
                                    )}
                                })
                            }
                        }
                    })
                };

                var result = await _tickets.AggregateAsync<BsonDocument>(mainPipeline);
                var document = await result.FirstOrDefaultAsync();

                if (document == null)
                    return new ServiceDeskDashboardStats();

                var stats = new ServiceDeskDashboardStats();

                // Parse general statistics
                var generalStats = document.GetValue("generalStats", new BsonArray()).AsBsonArray;
                if (generalStats.Count > 0)
                {
                    var generalDoc = generalStats[0].AsBsonDocument;
                    stats.TotalTickets = generalDoc.GetValue("total", 0).AsInt32;
                    stats.UnassignedTickets = generalDoc.GetValue("unassigned", 0).AsInt32;
                }

                // Parse status statistics
                var statusStats = document.GetValue("statusStats", new BsonArray()).AsBsonArray;
                foreach (var statusDoc in statusStats)
                {
                    var statusName = statusDoc["_id"].AsString;
                    var count = statusDoc["count"].AsInt32;

                    switch (statusName)
                    {
                        case "Open":
                            stats.OpenTickets = count;
                            break;
                        case "Resolved":
                            stats.ResolvedTickets = count;
                            break;
                        case "Closed":
                            stats.ClosedTickets = count;
                            break;
                    }
                }

                // Parse priority statistics
                var priorityStats = document.GetValue("priorityStats", new BsonArray()).AsBsonArray;
                foreach (var priorityDoc in priorityStats)
                {
                    var priorityName = priorityDoc["_id"].AsString;
                    var count = priorityDoc["count"].AsInt32;

                    if (Enum.TryParse<TicketPriority>(priorityName, out var priority))
                    {
                        stats.TicketsByPriority[priority] = count;
                    }
                }

                // Parse category statistics
                var categoryStats = document.GetValue("categoryStats", new BsonArray()).AsBsonArray;
                foreach (var categoryDoc in categoryStats)
                {
                    var categoryName = categoryDoc["_id"].AsString;
                    var count = categoryDoc["count"].AsInt32;

                    if (Enum.TryParse<TicketCategory>(categoryName, out var category))
                    {
                        stats.TicketsByCategory[category] = count;
                    }
                }

                // Parse assignee statistics
                var assigneeStats = document.GetValue("assigneeStats", new BsonArray()).AsBsonArray;
                foreach (var assigneeDoc in assigneeStats)
                {
                    var assigneeName = assigneeDoc["_id"].AsString;
                    var count = assigneeDoc["count"].AsInt32;
                    stats.TicketsByAssignee[assigneeName] = count;
                }

                // Parse resolution time statistics
                var resolutionStats = document.GetValue("resolutionStats", new BsonArray()).AsBsonArray;
                if (resolutionStats.Count > 0)
                {
                    var resolutionDoc = resolutionStats[0].AsBsonDocument;
                    stats.AverageResolutionTimeHours = resolutionDoc.GetValue("avgResolutionTime", 0.0).AsDouble;
                }

                return stats;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to get service desk dashboard stats", ex);
            }
        }

        #endregion
    }
}