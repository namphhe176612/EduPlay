using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PlayEdu.Data;
using PlayEdu.Models;

namespace PlayEdu.Controllers;
public class AccountController(GameDbContext db, IPasswordHasher<AppUser> hasher) : AppController {
    [HttpGet] public IActionResult Login(string? returnUrl) => View(new LoginForm { ReturnUrl = returnUrl });
    [HttpGet] public IActionResult Register(string? returnUrl) => View(new AuthForm { ReturnUrl = returnUrl });
    [HttpPost, EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(AuthForm form) {
        if (!ModelState.IsValid) return View(form);
        form.Email = form.Email.Trim().ToLowerInvariant();
        form.FullName = form.FullName.Trim();
        if (string.IsNullOrWhiteSpace(form.FullName)) ModelState.AddModelError("", "Nhập họ tên của bạn.");
        if (await db.Users.AnyAsync(x => x.Email == form.Email)) ModelState.AddModelError("", "Email đã được sử dụng.");
        if (!ModelState.IsValid) return View(form);
        await using var tx = await db.Database.BeginTransactionAsync();
        var user = new AppUser { Email = form.Email, FullName = form.FullName, Role = 1, Coin = 20 };
        user.PasswordHash = hasher.HashPassword(user, form.Password);
        db.Users.Add(user);
        try {
            await db.SaveChangesAsync();
            db.CoinTransactions.Add(new() { UserId = user.UserId, Amount = 20, TransactionType = "Signup",
                Description = "20 coin chào mừng", BalanceAfter = 20 });
            await db.SaveChangesAsync(); await tx.CommitAsync();
        } catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 }) {
            await tx.RollbackAsync(); ModelState.AddModelError("", "Email đã được sử dụng."); return View(form);
        }
        await SignIn(user); Notice("Chào mừng bạn! Ví đã nhận 20 coin.");
        return LocalRedirect(Url.IsLocalUrl(form.ReturnUrl) ? form.ReturnUrl! : "/Home/Dashboard");
    }
    [HttpPost, EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginForm form) {
        if (!ModelState.IsValid) return View(form);
        var email = form.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email);
        var valid = false;
        if (user != null && !user.IsLocked && !user.PasswordHash.StartsWith("DEMO", StringComparison.Ordinal)) {
            try {
                var result = hasher.VerifyHashedPassword(user, user.PasswordHash, form.Password);
                valid = result != PasswordVerificationResult.Failed;
                if (result == PasswordVerificationResult.SuccessRehashNeeded) {
                    user.PasswordHash = hasher.HashPassword(user, form.Password); await db.SaveChangesAsync();
                }
            } catch (FormatException) { }
        }
        if (!valid) { ModelState.AddModelError("", "Email hoặc mật khẩu chưa đúng, hoặc tài khoản đã bị khóa."); return View(form); }
        await SignIn(user!);
        return LocalRedirect(Url.IsLocalUrl(form.ReturnUrl) ? form.ReturnUrl! : user!.Role == 2 ? "/Admin" : "/Home/Dashboard");
    }
    private Task SignIn(AppUser user) => HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()), new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        }, CookieAuthenticationDefaults.AuthenticationScheme)));
    [Authorize, HttpPost] public async Task<IActionResult> Logout() {
        await HttpContext.SignOutAsync(); return RedirectToAction("Index","Home");
    }
    public IActionResult Denied() { Response.StatusCode = 403; return View(); }
}

