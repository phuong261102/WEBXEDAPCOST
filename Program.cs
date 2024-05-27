using App.Data;
using App.ExtendMethods;
using App.Models;
using App.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<RouteOptions>(options =>
{
    options.AppendTrailingSlash = false;
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = false;
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Add configuration.
var appConfiguration = builder.Configuration;

// Add DbContext.
builder.Services.AddDbContext<AppDbContext>(options =>
{
    string? connectString = appConfiguration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectString);
});

builder.Services.AddMemoryCache();
builder.Services.AddScoped<CacheService>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(cfg =>
{
    cfg.Cookie.Name = "appxedap";
    cfg.IdleTimeout = new TimeSpan(0, 30, 0);
});

// Add mail service
builder.Services.AddOptions();
var mailsetting = appConfiguration.GetSection("MailSettings");
builder.Services.Configure<MailSettings>(mailsetting);
builder.Services.AddSingleton<IEmailSender, SendMailService>();

// Register HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Register Identity
builder.Services.AddIdentity<AppUser, IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

// Configure IdentityOptions
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = false; // No digit required
    options.Password.RequireLowercase = false; // No lowercase letter required
    options.Password.RequireNonAlphanumeric = false; // No special character required
    options.Password.RequireUppercase = false; // No uppercase letter required
    options.Password.RequiredLength = 3; // Minimum length of password
    options.Password.RequiredUniqueChars = 1; // Minimum unique chars

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5); // Lockout for 5 minutes
    options.Lockout.MaxFailedAccessAttempts = 3; // 3 failed attempts to lockout
    options.Lockout.AllowedForNewUsers = true;

    options.User.AllowedUserNameCharacters = // User name allowed characters
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;  // Unique email

    options.SignIn.RequireConfirmedEmail = true;            // Require email confirmation
    options.SignIn.RequireConfirmedPhoneNumber = false;     // Require phone number confirmation
    options.SignIn.RequireConfirmedAccount = true;
});

// Configure ApplicationCookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login/";
    options.LogoutPath = "/logout/";
    options.AccessDeniedPath = "/khongduoctruycap.html";
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromSeconds(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure Authentication
builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            var gconfig = builder.Configuration.GetSection("Authentication:Google");
            options.ClientId = gconfig["ClientId"];
            options.ClientSecret = gconfig["ClientSecret"];
            options.CallbackPath = "/dang-nhap-tu-google";
        })
        .AddFacebook(options =>
        {
            var fconfig = builder.Configuration.GetSection("Authentication:Facebook");
            options.AppId = fconfig["AppId"];
            options.AppSecret = fconfig["AppSecret"];
            options.CallbackPath = "/dang-nhap-tu-facebook";
        });

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromSeconds(5);
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

builder.Services.AddControllers(
    options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);

// Replace the default Identity error describer
builder.Services.AddSingleton<IdentityErrorDescriber, AppIdentityErrorDescriber>();

// Configure authorization policies
builder.Services.AddAuthorization(option =>
{
    option.AddPolicy("ViewManageMenu", builder =>
    {
        builder.RequireAuthenticatedUser();
        builder.RequireRole(RoleName.Administrator);
    });
});

// Register CartService
builder.Services.AddTransient<CartService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// Initialize the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<AppDbContext>();
    var userManager = services.GetRequiredService<UserManager<AppUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    // Ensure the database is created and updated
    await dbContext.Database.MigrateAsync();

    // Seed data if necessary
    await SeedDataAsync(dbContext, userManager, roleManager);
}

app.Run();

static async Task SeedDataAsync(AppDbContext dbContext, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
{
    try
    {
        // Seed roles
        string[] roleNames = { RoleName.Administrator, RoleName.Editor, RoleName.Member };
        foreach (var roleName in roleNames)
        {
            var roleExist = await roleManager.RoleExistsAsync(roleName);

            if (!roleExist)
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // Seed admin user
        var useradmin = await userManager.FindByEmailAsync("admin@qlxd.com");
        if (useradmin == null)
        {
            useradmin = new AppUser()
            {
                UserName = "admin",
                Email = "admin@qlxd.com",
                EmailConfirmed = true,
            };
            await userManager.CreateAsync(useradmin, "admin123");
            await userManager.AddToRoleAsync(useradmin, RoleName.Administrator);
        }
    }
    catch (Exception ex)
    {
        // Handle exceptions here, e.g., log errors, show error messages, etc.
        Console.WriteLine($"Error while seeding data: {ex.Message}");
    }
}
