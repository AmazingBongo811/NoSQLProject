using IncidentManagementSystem.Models;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace IncidentManagementSystem.Services
{
    public interface ITicketSearchService
    {
        Task<List<Ticket>> SearchTicketsAsync(TicketSearchCriteria criteria, string? userId = null);
        Task<List<Ticket>> QuickSearchAsync(string searchTerm, string? userId = null);
    }

    public class TicketSearchService : ITicketSearchService
    {
        private readonly IMongoCollection<Ticket> _tickets;

        public TicketSearchService(IMongoDatabase database)
        {
            _tickets = database.GetCollection<Ticket>("tickets");
        }

        /// <summary>
        /// Performs advanced search with AND/OR operations and proper ordering
        /// This is the INDIVIDUAL FUNCTIONALITY implementation as per rubrics
        /// </summary>
        public async Task<List<Ticket>> SearchTicketsAsync(TicketSearchCriteria criteria, string? userId = null)
        {
            var filterBuilder = Builders<Ticket>.Filter;
            var filters = new List<FilterDefinition<Ticket>>();

            // Base filter for user access (if userId is provided, show only user's tickets)
            if (!string.IsNullOrEmpty(userId))
            {
                // Search for tickets where user is either reporter OR assignee
                var userFilters = new List<FilterDefinition<Ticket>>
                {
                    filterBuilder.Eq(t => t.Reporter.UserId, userId),
                    filterBuilder.Eq("assignee.userId", userId)
                };
                filters.Add(filterBuilder.Or(userFilters));
            }

            // Status filter
            if (criteria.Status.HasValue)
            {
                filters.Add(filterBuilder.Eq(t => t.Status, criteria.Status.Value));
            }

            // Priority filter
            if (criteria.Priority.HasValue)
            {
                filters.Add(filterBuilder.Eq(t => t.Priority, criteria.Priority.Value));
            }

            // Category filter
            if (criteria.Category.HasValue)
            {
                filters.Add(filterBuilder.Eq(t => t.Category, criteria.Category.Value));
            }

            // Assignee filter (for service desk users)
            if (!string.IsNullOrEmpty(criteria.AssigneeId))
            {
                filters.Add(filterBuilder.Eq("assignee.userId", criteria.AssigneeId));
            }

            // Date range filters
            if (criteria.DateFrom.HasValue)
            {
                // Start of day for DateFrom
                var dateFrom = criteria.DateFrom.Value.Date;
                filters.Add(filterBuilder.Gte(t => t.CreatedAt, dateFrom));
            }

            if (criteria.DateTo.HasValue)
            {
                // End of day for DateTo (inclusive)
                var dateTo = criteria.DateTo.Value.Date.AddDays(1).AddTicks(-1);
                filters.Add(filterBuilder.Lte(t => t.CreatedAt, dateTo));
            }

            // Advanced text search with AND/OR operations
            if (!string.IsNullOrWhiteSpace(criteria.SearchText))
            {
                var textFilter = BuildAdvancedTextFilter(criteria.SearchText, filterBuilder);
                if (textFilter != null)
                {
                    filters.Add(textFilter);
                }
            }

            // Combine all filters with AND operation
            var finalFilter = filters.Count > 0 
                ? filterBuilder.And(filters) 
                : filterBuilder.Empty;

            // Build sort definition - most recent first (as per individual requirements)
            var sort = Builders<Ticket>.Sort.Descending(t => t.CreatedAt);

            // Execute query
            var query = _tickets.Find(finalFilter).Sort(sort);

            // Apply pagination if specified
            if (criteria.Skip.HasValue && criteria.Skip > 0)
            {
                query = query.Skip(criteria.Skip.Value);
            }

            if (criteria.Limit.HasValue && criteria.Limit > 0)
            {
                query = query.Limit(criteria.Limit.Value);
            }
            else
            {
                query = query.Limit(100); // Default limit to prevent performance issues
            }

            return await query.ToListAsync();
        }

        /// <summary>
        /// Quick search functionality for simple text searches
        /// </summary>
        public async Task<List<Ticket>> QuickSearchAsync(string searchTerm, string? userId = null)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return new List<Ticket>();
            }

            var criteria = new TicketSearchCriteria
            {
                SearchText = searchTerm,
                Limit = 50
            };

            return await SearchTicketsAsync(criteria, userId);
        }

        /// <summary>
        /// Builds advanced text filter with AND/OR operations
        /// Supports syntax like: "network AND server OR router"
        /// </summary>
        private FilterDefinition<Ticket>? BuildAdvancedTextFilter(string searchText, FilterDefinitionBuilder<Ticket> filterBuilder)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return null;

            try
            {
                // Split by OR operations first (lowest precedence)
                var orParts = Regex.Split(searchText, @"\s+OR\s+", RegexOptions.IgnoreCase)
                    .Where(part => !string.IsNullOrWhiteSpace(part))
                    .ToList();

                if (orParts.Count == 1)
                {
                    // No OR operations, handle AND operations only
                    return BuildAndFilter(orParts[0], filterBuilder);
                }

                // Multiple OR parts, build each part and combine with OR
                var orFilters = new List<FilterDefinition<Ticket>>();

                foreach (var orPart in orParts)
                {
                    var andFilter = BuildAndFilter(orPart.Trim(), filterBuilder);
                    if (andFilter != null)
                    {
                        orFilters.Add(andFilter);
                    }
                }

                return orFilters.Count > 0 ? filterBuilder.Or(orFilters) : null;
            }
            catch
            {
                // If parsing fails, treat as simple text search
                return BuildSimpleTextFilter(searchText, filterBuilder);
            }
        }

        /// <summary>
        /// Builds AND filter for terms connected with AND operation
        /// </summary>
        private FilterDefinition<Ticket>? BuildAndFilter(string andExpression, FilterDefinitionBuilder<Ticket> filterBuilder)
        {
            // Split by AND operations
            var andParts = Regex.Split(andExpression, @"\s+AND\s+", RegexOptions.IgnoreCase)
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => part.Trim())
                .ToList();

            var andFilters = new List<FilterDefinition<Ticket>>();

            foreach (var term in andParts)
            {
                var termFilter = BuildSimpleTextFilter(term, filterBuilder);
                if (termFilter != null)
                {
                    andFilters.Add(termFilter);
                }
            }

            return andFilters.Count > 0 ? filterBuilder.And(andFilters) : null;
        }

        /// <summary>
        /// Builds simple text filter for a single term across multiple fields
        /// </summary>
        private FilterDefinition<Ticket> BuildSimpleTextFilter(string term, FilterDefinitionBuilder<Ticket> filterBuilder)
        {
            var cleanTerm = term.Trim().Trim('"', '\''); // Remove quotes if present
            
            if (string.IsNullOrWhiteSpace(cleanTerm))
                return filterBuilder.Empty;

            // Escape regex special characters to avoid regex errors
            var escapedTerm = System.Text.RegularExpressions.Regex.Escape(cleanTerm);
            
            // Search across multiple text fields with case-insensitive partial matching
            var textFilters = new List<FilterDefinition<Ticket>>
            {
                filterBuilder.Regex(t => t.Title, new MongoDB.Bson.BsonRegularExpression(escapedTerm, "i")),
                filterBuilder.Regex(t => t.Description, new MongoDB.Bson.BsonRegularExpression(escapedTerm, "i"))
            };

            // If it looks like a ticket ID (starts with #), search by ID too
            if (cleanTerm.StartsWith("#") && cleanTerm.Length > 1)
            {
                var idPart = System.Text.RegularExpressions.Regex.Escape(cleanTerm.Substring(1));
                textFilters.Add(filterBuilder.Regex(t => t.Id, new MongoDB.Bson.BsonRegularExpression(idPart, "i")));
            }

            return filterBuilder.Or(textFilters);
        }
    }

    /// <summary>
    /// Search criteria class for advanced ticket searching
    /// </summary>
    public class TicketSearchCriteria
    {
        public string? SearchText { get; set; }
        public TicketStatus? Status { get; set; }
        public TicketPriority? Priority { get; set; }
        public TicketCategory? Category { get; set; }
        public string? AssigneeId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int? Skip { get; set; }
        public int? Limit { get; set; }

        public bool HasCriteria => 
            !string.IsNullOrWhiteSpace(SearchText) ||
            Status.HasValue ||
            Priority.HasValue ||
            Category.HasValue ||
            !string.IsNullOrWhiteSpace(AssigneeId) ||
            DateFrom.HasValue ||
            DateTo.HasValue;
    }
}