using ClothingStore.Data;
using ClothingStore.Repositories;
using ClothingStore.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── MVC ───────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();

// ── Security & Options ─────────────────────────────────────────
builder.Services.Configure<ClothingStore.Models.Security.SecurityOptions>(
    builder.Configuration.GetSection("SecurityOptions"));

// ── Database ──────────────────────────────────────────────────
builder.Services.AddDbContext<StoreDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Session (for guest cart) ──────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.Cookie.Name     = ".ClothingStore.Cart";
    options.IdleTimeout     = TimeSpan.FromDays(14);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ── [BUG-03 FIX] Cookie Authentication ───────────────────────
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath        = "/Account/Login";
        options.LogoutPath       = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan   = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Cookie.Name      = ".ClothingStore.Auth";
        options.Cookie.HttpOnly  = true;
        options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
    })
    .AddCookie("ExternalCookie")
    .AddGoogle(options =>
    {
        options.SignInScheme = "ExternalCookie";
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "placeholder";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "placeholder";
        
        // Force Google to always ask the user to select an account
        options.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            context.Response.Redirect(context.RedirectUri + "&prompt=select_account");
            return Task.CompletedTask;
        };
    })
    .AddFacebook(options =>
    {
        options.SignInScheme = "ExternalCookie";
        options.AppId = builder.Configuration["Authentication:Facebook:AppId"] ?? "placeholder";
        options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"] ?? "placeholder";
    });

builder.Services.AddAuthorization();

// ── Repositories ──────────────────────────────────────────────
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();

// ── Services ──────────────────────────────────────────────────
builder.Services.AddScoped<ICurrentCustomerService, CurrentCustomerService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ICheckoutService, CheckoutService>();
builder.Services.AddScoped<ICouponService, CouponService>();
builder.Services.AddScoped<IAdminProductService, AdminProductService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<ICustomerAccountService, CustomerAccountService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();

var app = builder.Build();

// Seed data
await ClothingStore.Data.TestAccountSeeder.SeedAsync(app.Services);

// ── Pipeline ──────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// [BUG-03 FIX] Authentication MUST come before Authorization
app.UseAuthentication();
app.UseAuthorization();

// ── Permission Middleware ─────────────────────────────────────
app.UseMiddleware<ClothingStore.Middleware.PermissionMiddleware>();

// ── Routes ────────────────────────────────────────────────────
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
