using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayEdu.Data;
using PlayEdu.Models;
using PlayEdu.Services;
namespace PlayEdu.Controllers;
[Authorize(Roles = "2")]
public class AdminController(GameDbContext db, CoinService coins) : AppController {
    public async Task<IActionResult> Index(string? search) {
        var users = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) users=users.Where(x => x.Email.Contains(search) || x.FullName.Contains(search));
        ViewData["Search"]=search;
        return View(new AdminVm {
            Users=await users.OrderByDescending(x => x.UserId).Take(100).ToListAsync(),
            Calls=await db.AICalls.AsNoTracking().OrderByDescending(x => x.AICallId).Take(200).ToListAsync(),
            Payments=await db.Payments.AsNoTracking().OrderByDescending(x => x.PaymentId).Take(200).ToListAsync(),
            Logs=await db.AdminLogs.AsNoTracking().OrderByDescending(x => x.AdminLogId).Take(100).ToListAsync(),
            FileCount=await db.Files.CountAsync(),RunCount=await db.Runs.CountAsync()
        });
    }
    [HttpPost] public async Task<IActionResult> ToggleLock(int id, string? reason) {
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 300 || id == UserId) {
            ErrorNotice("Cần lý do tối đa 300 ký tự; không thể tự khóa tài khoản."); return RedirectToAction(nameof(Index));
        }
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var user = await db.Users.SingleOrDefaultAsync(x => x.UserId == id);
        if (user == null) return NotFound();
        // Preserve administrator availability.
        if (user.Role == 2) { ErrorNotice("Bản MVP chỉ hỗ trợ khóa tài khoản người học."); return RedirectToAction(nameof(Index)); }
        user.IsLocked=!user.IsLocked;
        db.AdminLogs.Add(new() { AdminUserId=UserId,TargetUserId=id,Action=user.IsLocked ? "LockUser" : "UnlockUser",Description=reason.Trim() });
        await db.SaveChangesAsync(); await tx.CommitAsync();
        Notice("Đã cập nhật trạng thái tài khoản."); return RedirectToAction(nameof(Index));
    }
    [HttpPost] public async Task<IActionResult> AdjustCoins(int id, int amount, string? reason) {
        if (amount == 0 || amount is < -100000 or > 100000 || string.IsNullOrWhiteSpace(reason) || reason.Length > 200) {
            ErrorNotice("Số coin cần khác 0, trong ±100.000 và phải có lý do tối đa 200 ký tự."); return RedirectToAction(nameof(Index));
        }
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == id && x.Role == 1);
        if (user == null) return NotFound();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try {
            await coins.Change(id,amount,"AdminAdjustment",reason.Trim());
            db.AdminLogs.Add(new() { AdminUserId=UserId,TargetUserId=id,Action="AdjustCoins",Description=$"{amount:+#;-#} coin: {reason.Trim()}" });
            await db.SaveChangesAsync(); await tx.CommitAsync(); Notice("Đã cập nhật coin và ghi sổ giao dịch/audit.");
        } catch (InvalidOperationException ex) { await tx.RollbackAsync(); ErrorNotice(ex.Message); }
        return RedirectToAction(nameof(Index));
    }
}

