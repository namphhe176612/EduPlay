using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayEdu.Data;
using PlayEdu.Models;
using PlayEdu.Services;

var builder = WebApplication.CreateBuilder(args);
var keyDirectory = new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath,"App_Data","keys"));
keyDirectory.Create();
var protection = builder.Services.AddDataProtection().SetApplicationName("SaBan").PersistKeysToFileSystem(keyDirectory);
if (OperatingSystem.IsWindows()) protection.ProtectKeysWithDpapi();
builder.Services.AddDbContext<GameDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("GameDB")));
builder.Services.AddControllersWithViews(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddScoped<CoinService>();
builder.Services.AddScoped<AiGateway>();
builder.Services.AddScoped<AiOperation>();
builder.Services.AddScoped<EduStore>();
builder.Services.AddScoped<EduAdminSecurity>();
builder.Services.AddHttpClient("ai", c => c.Timeout = TimeSpan.FromSeconds(85));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o => {
    o.LoginPath = "/Account/Login"; o.AccessDeniedPath = "/Account/Denied";
    o.Cookie.Name = "SaBan.Session"; o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax; o.ExpireTimeSpan = TimeSpan.FromHours(8);
    o.Events.OnValidatePrincipal = async ctx => {
        var id = int.TryParse(ctx.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var n) ? n : 0;
        var db = ctx.HttpContext.RequestServices.GetRequiredService<GameDbContext>();
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);
        if (user == null || user.IsLocked || ctx.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value != user.Role.ToString())
            ctx.RejectPrincipal();
    };
}).AddCookie("EduV2", o => {
    o.LoginPath="/edu/login"; o.AccessDeniedPath="/edu/login";
    o.Cookie.Name="EduPlay.V2.Session"; o.Cookie.HttpOnly=true; o.Cookie.SameSite=SameSiteMode.Lax;
    o.Events.OnValidatePrincipal=async ctx => {
        var id=long.TryParse(ctx.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,out var n)?n:0;
        var store=ctx.HttpContext.RequestServices.GetRequiredService<EduStore>();
        var user=await store.Query("SELECT UserId FROM dbo.Users WHERE UserId=@u AND Status='Active'",("@u",id));
        if(user.Count==0){ctx.RejectPrincipal();return;}
        var roles=await store.Query("SELECT r.Code FROM dbo.UserRoles ur JOIN dbo.Roles r ON r.RoleId=ur.RoleId WHERE ur.UserId=@u",("@u",id));
        var expected=roles.Select(r=>EduPage.Text(r,"Code")).OrderBy(x=>x).ToArray();
        var actual=ctx.Principal!.FindAll(System.Security.Claims.ClaimTypes.Role).Select(x=>x.Value).OrderBy(x=>x).ToArray();
        if(!actual.SequenceEqual(expected))ctx.RejectPrincipal();
    };
});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(o => {
    o.RejectionStatusCode = 429;
    o.AddPolicy("auth", ctx => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new() {
            PermitLimit = 15, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
        }));
});
var app = builder.Build();
if(args.Contains("--create-edu-admin")) {
    using var scope=app.Services.CreateScope();
    var store=scope.ServiceProvider.GetRequiredService<EduStore>();
    var email=Environment.GetEnvironmentVariable("EDUPLAY_ADMIN_EMAIL");
    var password=Environment.GetEnvironmentVariable("EDUPLAY_ADMIN_PASSWORD");
    if(string.IsNullOrWhiteSpace(email)||string.IsNullOrEmpty(password)||password.Length<12)throw new InvalidOperationException("Set EDUPLAY_ADMIN_EMAIL and EDUPLAY_ADMIN_PASSWORD (12+ characters).");
    var id=await store.Register(new EduRegistration{FullName="Quản trị EduPlay",Email=email,AcceptTerms=true},new PasswordHasher<string>().HashPassword(email,password));
    await store.Execute("SET XACT_ABORT ON;BEGIN TRAN;DELETE dbo.UserRoles WHERE UserId=@u;INSERT dbo.UserRoles(UserId,RoleId) SELECT @u,RoleId FROM dbo.Roles WHERE Code='SuperAdmin';INSERT dbo.AdminSecurity(UserId) VALUES(@u);COMMIT;",("@u",id));
    Console.WriteLine("Admin created. Sign in with email and password at /edu/login.");return;
}
if (args.Contains("--create-admin")) {
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    var email = Environment.GetEnvironmentVariable("SABAN_ADMIN_EMAIL")?.Trim().ToLowerInvariant();
    var password = Environment.GetEnvironmentVariable("SABAN_ADMIN_PASSWORD");
    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || password.Length < 12)
        throw new InvalidOperationException("Set SABAN_ADMIN_EMAIL and SABAN_ADMIN_PASSWORD (12+ characters).");
    if (await db.Users.AnyAsync(x => x.Email == email))
        throw new InvalidOperationException("Email already exists; existing accounts are never overwritten.");
    var user = new AppUser { Email = email, FullName = "Quản trị Play edu", Role = 2, Coin = 0 };
    user.PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>().HashPassword(user, password);
    db.Users.Add(user); await db.SaveChangesAsync(); Console.WriteLine("Administrator created."); return;
}
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Home/Error");
app.Use(async (ctx, next) => {
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    ctx.Response.Headers["Referrer-Policy"] = "same-origin";
    if(ctx.Request.Path.StartsWithSegments("/edu"))ctx.Response.Headers.CacheControl="no-store";
    ctx.Response.Headers["Content-Security-Policy"] = "default-src 'self'; style-src 'self'; script-src 'self'; img-src 'self' data:; frame-src 'self'; object-src 'self'; base-uri 'self'; form-action 'self'; frame-ancestors 'self'";
    if (ctx.Request.Path.StartsWithSegments("/Documents") || ctx.Request.Path.StartsWithSegments("/Scenarios") || ctx.Request.Path.StartsWithSegments("/Admin") || ctx.Request.Path.StartsWithSegments("/Profile") || ctx.Request.Path.StartsWithSegments("/Wallet") || ctx.Request.Path.StartsWithSegments("/Home/Dashboard"))
        ctx.Response.Headers.CacheControl = "no-store";
    await next();
});
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.MapGet("/", () => Results.Redirect("/edu"));
app.Run();




