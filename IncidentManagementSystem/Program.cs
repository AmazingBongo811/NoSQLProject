using IncidentManagementSystem.Data;
using IncidentManagementSystem.Repositories;
using IncidentManagementSystem.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Configure MongoDB
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection(MongoDbSettings.SectionName));

// Register MongoDB context
builder.Services.AddSingleton<MongoDbContext>(serviceProvider =>
{
    var settings = builder.Configuration.GetSection(MongoDbSettings.SectionName).Get<MongoDbSettings>();
    return new MongoDbContext(settings ?? new MongoDbSettings());
});

// Register IMongoDatabase for TicketSearchService
builder.Services.AddScoped<IMongoDatabase>(serviceProvider =>
{
    var context = serviceProvider.GetRequiredService<MongoDbContext>();
    return context.Database;
});

// Register repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();

// Register services
builder.Services.AddScoped<DatabaseSeedService>();
builder.Services.AddScoped<CsvExportService>();
builder.Services.AddScoped<ITicketSearchService, TicketSearchService>();

// Add authentication services
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

// Add authorization services
builder.Services.AddAuthorization();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
