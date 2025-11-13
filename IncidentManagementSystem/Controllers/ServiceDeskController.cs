using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IncidentManagementSystem.Repositories;
using IncidentManagementSystem.Models;
using IncidentManagementSystem.ViewModels;
using System.Security.Claims;

namespace IncidentManagementSystem.Controllers
{
    [Authorize(Roles = "ServiceDesk")]
    public class ServiceDeskController : Controller
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<ServiceDeskController> _logger;

        public ServiceDeskController(
            ITicketRepository ticketRepository,
            IUserRepository userRepository,
            ILogger<ServiceDeskController> logger)
        {
            _ticketRepository = ticketRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Get all tickets for service desk overview
                var allTickets = await _ticketRepository.GetAllAsync();
                var ticketList = allTickets.ToList();

                // Calculate statistics (only 3 statuses: Open, Resolved, Closed)
                var openTickets = ticketList.Count(t => t.Status == TicketStatus.Open);
                var resolvedTickets = ticketList.Count(t => t.Status == TicketStatus.Resolved);
                var closedTickets = ticketList.Count(t => t.Status == TicketStatus.Closed);
                var urgentTickets = ticketList.Count(t => t.Priority == TicketPriority.High && t.Status == TicketStatus.Open);
                
                // Get recent tickets (last 10)
                var recentTickets = ticketList
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(10)
                    .ToList();

                // Get unassigned tickets
                var unassignedTickets = ticketList.Count(t => t.Assignee == null);

                var viewModel = new ServiceDeskDashboardViewModel
                {
                    TotalTickets = ticketList.Count,
                    OpenTickets = openTickets,
                    ResolvedTickets = resolvedTickets,
                    ClosedTickets = closedTickets,
                    UrgentTickets = urgentTickets,
                    UnassignedTickets = unassignedTickets,
                    RecentTickets = recentTickets
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading service desk dashboard");
                return View("Error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> AllTickets(string status = "", string priority = "", string assignedTo = "")
        {
            try
            {
                var tickets = await _ticketRepository.GetAllAsync();
                var filteredTickets = tickets.AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<TicketStatus>(status, out var ticketStatus))
                {
                    filteredTickets = filteredTickets.Where(t => t.Status == ticketStatus);
                }

                if (!string.IsNullOrEmpty(priority) && Enum.TryParse<TicketPriority>(priority, out var ticketPriority))
                {
                    filteredTickets = filteredTickets.Where(t => t.Priority == ticketPriority);
                }

                if (!string.IsNullOrEmpty(assignedTo))
                {
                    if (assignedTo == "unassigned")
                    {
                        filteredTickets = filteredTickets.Where(t => t.Assignee == null);
                    }
                    else
                    {
                        filteredTickets = filteredTickets.Where(t => t.Assignee != null && t.Assignee.UserId == assignedTo);
                    }
                }

                var result = filteredTickets
                    .OrderByDescending(t => t.CreatedAt)
                    .ToList();

                // Get service desk users for assignment dropdown
                var serviceDeskUsers = await _userRepository.GetAllAsync();
                var serviceDeskUsersList = serviceDeskUsers.Where(u => u.Role == UserRole.ServiceDesk).ToList();
                
                ViewBag.ServiceDeskUsers = serviceDeskUsersList;
                ViewBag.CurrentStatus = status;
                ViewBag.CurrentPriority = priority;
                ViewBag.CurrentAssignedTo = assignedTo;

                return View(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading tickets for service desk");
                return View("Error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ViewTicket(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return NotFound();
                }

                var ticket = await _ticketRepository.GetByIdAsync(id);
                if (ticket == null)
                {
                    return NotFound();
                }

                // Get service desk users for assignment
                var serviceDeskUsers = await _userRepository.GetAllAsync();
                var serviceDeskUsersList = serviceDeskUsers.Where(u => u.Role == UserRole.ServiceDesk).ToList();
                
                ViewBag.ServiceDeskUsers = serviceDeskUsersList;

                return View(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ticket {TicketId} for service desk", id);
                return View("Error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTicketStatus(string ticketId, string status, string assignedTo = "")
        {
            try
            {
                var ticket = await _ticketRepository.GetByIdAsync(ticketId);
                if (ticket == null)
                {
                    return NotFound();
                }

                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = User.Identity?.Name;
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                
                var currentUser = new UserInfo
                {
                    UserId = currentUserId ?? "",
                    Username = userName ?? "",
                    Email = userEmail ?? "",
                    FullName = userName ?? ""
                };

                var changes = new List<string>();

                // Update status if changed
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<TicketStatus>(status, out var newStatus) && ticket.Status != newStatus)
                {
                    var oldStatus = ticket.Status;
                    ticket.Status = newStatus;
                    changes.Add($"Status changed from {oldStatus} to {newStatus}");
                }

                // Update assignment if provided
                if (!string.IsNullOrEmpty(assignedTo))
                {
                    if (assignedTo == "unassign")
                    {
                        var oldAssignee = ticket.Assignee?.FullName ?? "Unassigned";
                        ticket.Assignee = null;
                        changes.Add($"Unassigned from {oldAssignee}");
                    }
                    else
                    {
                        var assigneeUser = await _userRepository.GetByIdAsync(assignedTo);
                        if (assigneeUser != null)
                        {
                            var oldAssignee = ticket.Assignee?.FullName ?? "Unassigned";
                            ticket.Assignee = new UserInfo
                            {
                                UserId = assigneeUser.Id!,
                                Username = assigneeUser.Username,
                                Email = assigneeUser.Email,
                                FullName = assigneeUser.FullName
                            };
                            changes.Add($"Assigned from {oldAssignee} to {ticket.Assignee.FullName}");
                        }
                    }
                }

                if (changes.Any())
                {
                    ticket.UpdatedAt = DateTime.UtcNow;

                    // Add activity log
                    var activity = new TicketActivity
                    {
                        Action = "Updated",
                        PerformedBy = currentUser,
                        Timestamp = DateTime.UtcNow,
                        Description = string.Join(", ", changes)
                    };

                    ticket.Activities.Add(activity);

                    await _ticketRepository.UpdateAsync(ticketId, ticket);

                    TempData["SuccessMessage"] = "Ticket updated successfully!";
                }

                return RedirectToAction("ViewTicket", new { id = ticketId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ticket {TicketId}", ticketId);
                TempData["ErrorMessage"] = "Failed to update ticket. Please try again.";
                return RedirectToAction("ViewTicket", new { id = ticketId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(string ticketId, string commentContent, bool isInternal = false)
        {
            try
            {
                if (string.IsNullOrEmpty(ticketId) || string.IsNullOrWhiteSpace(commentContent))
                {
                    return BadRequest();
                }

                var ticket = await _ticketRepository.GetByIdAsync(ticketId);
                if (ticket == null)
                {
                    return NotFound();
                }

                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = User.Identity?.Name;
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

                // Add new comment
                var newComment = new TicketComment
                {
                    Content = commentContent.Trim(),
                    Author = new UserInfo
                    {
                        UserId = currentUserId ?? "",
                        Username = userName ?? "",
                        Email = userEmail ?? "",
                        FullName = userName ?? ""
                    },
                    Timestamp = DateTime.UtcNow,
                    IsInternal = isInternal
                };

                ticket.Comments.Add(newComment);
                ticket.UpdatedAt = DateTime.UtcNow;

                await _ticketRepository.UpdateAsync(ticketId, ticket);

                TempData["SuccessMessage"] = isInternal ? "Internal note added successfully!" : "Comment added successfully!";
                return RedirectToAction("ViewTicket", new { id = ticketId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment to ticket {TicketId}", ticketId);
                TempData["ErrorMessage"] = "Failed to add comment. Please try again.";
                return RedirectToAction("ViewTicket", new { id = ticketId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Analytics()
        {
            try
            {
                var allTickets = await _ticketRepository.GetAllAsync();
                var tickets = allTickets.ToList();

                // Calculate various analytics
                var totalTickets = tickets.Count;
                var openTickets = tickets.Count(t => t.Status == TicketStatus.Open);
                var resolvedTickets = tickets.Count(t => t.Status == TicketStatus.Resolved);
                var closedTickets = tickets.Count(t => t.Status == TicketStatus.Closed);

                // Priority breakdown
                var highPriorityTickets = tickets.Count(t => t.Priority == TicketPriority.High);
                var mediumPriorityTickets = tickets.Count(t => t.Priority == TicketPriority.Medium);
                var lowPriorityTickets = tickets.Count(t => t.Priority == TicketPriority.Low);
                var criticalPriorityTickets = tickets.Count(t => t.Priority == TicketPriority.Critical);

                // Calculate average resolution time for resolved tickets
                var resolvedTicketsWithTimes = tickets
                    .Where(t => t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed)
                    .ToList();

                var avgResolutionTime = resolvedTicketsWithTimes.Any()
                    ? resolvedTicketsWithTimes.Average(t => (t.UpdatedAt - t.CreatedAt).TotalHours)
                    : 0;

                var viewModel = new AnalyticsViewModel
                {
                    TotalTickets = totalTickets,
                    OpenTickets = openTickets,
                    ResolvedTickets = resolvedTickets,
                    ClosedTickets = closedTickets,
                    HighPriorityTickets = highPriorityTickets,
                    MediumPriorityTickets = mediumPriorityTickets,
                    LowPriorityTickets = lowPriorityTickets,
                    CriticalPriorityTickets = criticalPriorityTickets,
                    AverageResolutionTimeHours = Math.Round(avgResolutionTime, 2)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading analytics");
                return View("Error");
            }
        }
    }
}