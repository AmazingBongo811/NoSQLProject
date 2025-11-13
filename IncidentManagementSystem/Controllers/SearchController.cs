using IncidentManagementSystem.Models;
using IncidentManagementSystem.Repositories;
using IncidentManagementSystem.Services;
using IncidentManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IncidentManagementSystem.Controllers
{
    [Authorize]
    public class SearchController : Controller
    {
        private readonly ITicketSearchService _searchService;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<SearchController> _logger;

        public SearchController(ITicketSearchService searchService, IUserRepository userRepository, ILogger<SearchController> logger)
        {
            _searchService = searchService;
            _userRepository = userRepository;
            _logger = logger;
        }

        /// <summary>
        /// Display the advanced search page
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var model = new AdvancedSearchViewModel();
            
            // Populate assignee list for service desk users
            var isServiceDesk = await IsCurrentUserServiceDesk();
            if (isServiceDesk)
            {
                var serviceUsers = await _userRepository.GetByRoleAsync(UserRole.ServiceDesk);
                model.AvailableAssignees = serviceUsers.Select(u => (dynamic)new { u.Id, DisplayName = $"{u.FirstName} {u.LastName}" }).ToList();
            }
            
            return View(model);
        }

        /// <summary>
        /// Process search request and display results
        /// This is the main INDIVIDUAL FUNCTIONALITY endpoint
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Search(AdvancedSearchViewModel model)
        {
            try
            {
                // Validate date range
                if (model.DateFrom.HasValue && model.DateTo.HasValue && model.DateFrom > model.DateTo)
                {
                    ModelState.AddModelError("DateTo", "End date must be after start date");
                }

                if (!ModelState.IsValid)
                {
                    var isServiceDesk = await IsCurrentUserServiceDesk();
                    if (isServiceDesk)
                    {
                        var serviceUsers = await _userRepository.GetByRoleAsync(UserRole.ServiceDesk);
                        model.AvailableAssignees = serviceUsers.Select(u => (dynamic)new { u.Id, DisplayName = $"{u.FirstName} {u.LastName}" }).ToList();
                    }
                    return View("Index", model);
                }

                var currentUserId = GetCurrentUserId();
                var isServiceDeskUser = await IsCurrentUserServiceDesk();

                _logger.LogInformation("Search request - UserId: {UserId}, IsServiceDesk: {IsServiceDesk}, SearchText: '{SearchText}'", 
                    currentUserId, isServiceDeskUser, model.SearchText);

                // Build search criteria
                var criteria = new TicketSearchCriteria
                {
                    SearchText = model.SearchText?.Trim(),
                    Status = model.Status,
                    Priority = model.Priority,
                    Category = model.Category,
                    AssigneeId = model.AssigneeId,
                    DateFrom = model.DateFrom,
                    DateTo = model.DateTo,
                    Skip = (model.Page - 1) * model.PageSize,
                    Limit = model.PageSize
                };

                // For regular users, restrict to their own tickets unless they're service desk
                var searchUserId = isServiceDeskUser ? null : currentUserId;
                var results = await _searchService.SearchTicketsAsync(criteria, searchUserId);

                _logger.LogInformation("Search completed - Found {ResultCount} results", results.Count);

                // Populate results
                model.Results = results;
                model.HasSearched = true;
                model.TotalResults = results.Count;

                // If service desk user, populate assignee dropdown for future searches
                if (isServiceDeskUser)
                {
                    var serviceUsers = await _userRepository.GetByRoleAsync(UserRole.ServiceDesk);
                    model.AvailableAssignees = serviceUsers.Select(u => (dynamic)new { u.Id, DisplayName = $"{u.FirstName} {u.LastName}" }).ToList();
                }

                return View("Index", model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Search failed: {ex.Message}");
                
                // Repopulate assignees on error for service desk users
                var isServiceDesk = await IsCurrentUserServiceDesk();
                if (isServiceDesk)
                {
                    var serviceUsers = await _userRepository.GetByRoleAsync(UserRole.ServiceDesk);
                    model.AvailableAssignees = serviceUsers.Select(u => (dynamic)new { u.Id, DisplayName = $"{u.FirstName} {u.LastName}" }).ToList();
                }
                
                return View("Index", model);
            }
        }

        /// <summary>
        /// Quick search API endpoint for AJAX requests
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> QuickSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new List<object>());
            }

            try
            {
                var currentUserId = GetCurrentUserId();
                var isServiceDesk = await IsCurrentUserServiceDesk();
                
                // For regular users, restrict to their own tickets
                var searchUserId = isServiceDesk ? null : currentUserId;
                var results = await _searchService.QuickSearchAsync(query, searchUserId);

                // Return simplified data for quick search
                var quickResults = results.Take(10).Select(t => new
                {
                    id = t.Id,
                    subject = t.Title,
                    status = t.Status.ToString(),
                    priority = t.Priority.ToString(),
                    timestamp = t.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                });

                return Json(quickResults);
            }
            catch
            {
                return Json(new List<object>());
            }
        }

        /// <summary>
        /// Search suggestions API for autocomplete
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchSuggestions(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return Json(new List<string>());
            }

            try
            {
                var currentUserId = GetCurrentUserId();
                var isServiceDesk = await IsCurrentUserServiceDesk();
                
                var searchUserId = isServiceDesk ? null : currentUserId;
                var results = await _searchService.QuickSearchAsync(term, searchUserId);

                // Extract unique terms from subjects for suggestions
                var suggestions = results
                    .SelectMany(t => t.Title.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    .Where(word => word.Length > 2 && word.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .Distinct()
                    .Take(5)
                    .ToList();

                return Json(suggestions);
            }
            catch
            {
                return Json(new List<string>());
            }
        }

        private string GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        }

        private async Task<bool> IsCurrentUserServiceDesk()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return false;

            var user = await _userRepository.GetByIdAsync(userId);
            return user?.Role == UserRole.ServiceDesk;
        }
    }
}