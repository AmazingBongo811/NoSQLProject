using Microsoft.AspNetCore.Mvc;
using IncidentManagementSystem.Repositories;
using IncidentManagementSystem.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace IncidentManagementSystem.Controllers
{
    public class AuthController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IUserRepository userRepository, ILogger<AuthController> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        // GET: /Auth/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            
            // If user is already authenticated, redirect to appropriate dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("ServiceDesk"))
                {
                    return RedirectToAction("Index", "ServiceDesk");
                }
                return RedirectToAction("Index", "Dashboard");
            }
            
            return View();
        }

        // POST: /Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            try
            {
                // Find user by email
                var users = await _userRepository.GetAllAsync();
                var user = users.FirstOrDefault(u => u.Email.ToLower() == email.ToLower());

                if (user == null)
                {
                    ModelState.AddModelError("", "Invalid email or password.");
                    ViewBag.ReturnUrl = returnUrl;
                    return View();
                }

                // Hash the input password to compare with stored hash
                // This matches the hashing method used in DatabaseSeedService
                var hashedPassword = HashPassword(password);
                
                if (user.PasswordHash != hashedPassword)
                {
                    ModelState.AddModelError("", "Invalid email or password.");
                    ViewBag.ReturnUrl = returnUrl;
                    return View();
                }

                // Create claims for the user
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id!),
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true, // Remember me
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) // 8 hours session
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation($"User {user.Email} logged in successfully");

                // Redirect to appropriate dashboard based on role
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                // Redirect based on user role
                if (user.Role == UserRole.ServiceDesk)
                {
                    return RedirectToAction("Index", "ServiceDesk");
                }

                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login attempt for email: {Email}", email);
                ModelState.AddModelError("", "An error occurred during login. Please try again.");
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }
        }

        // POST: /Auth/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userEmail = User.Identity?.Name ?? "Unknown";
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                _logger.LogInformation($"User {userEmail} logged out successfully");
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: /Auth/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }

        /// <summary>
        /// Simple password hashing to match DatabaseSeedService
        /// In production, use BCrypt or Argon2
        /// </summary>
        private string HashPassword(string password)
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password + "salt"));
        }
    }
}