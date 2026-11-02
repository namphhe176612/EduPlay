using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayEdu.Data;
using PlayEdu.Models;
namespace PlayEdu.Controllers;
[Authorize(Roles = "1")]
public class ProfileController(GameDbContext db) : AppController {
    public async Task<IActionResult> Index() => View(new DashboardVm {
        User=await db.Users.AsNoTracking().SingleAsync(x => x.UserId == UserId),
        Files=await db.Files.AsNoTracking().Where(x => x.UserId == UserId).ToListAsync(),
        Scenarios=await db.Scenarios.AsNoTracking().Where(x => x.UserId == UserId).ToListAsync(),
        Runs=await db.Runs.AsNoTracking().Where(x => x.UserId == UserId).OrderByDescending(x => x.StartedAt).ToListAsync()
    });
    [HttpPost] public async Task<IActionResult> Update(string? fullName) {
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length > 100) ErrorNotice("Họ tên cần có từ 1 đến 100 ký tự.");
        else { await db.Users.Where(x => x.UserId == UserId).ExecuteUpdateAsync(s => s.SetProperty(x => x.FullName,fullName.Trim())); Notice("Đã cập nhật hồ sơ. Tên ở thanh điều hướng sẽ đổi sau khi đăng nhập lại."); }
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Vault() => View(new VaultVm {
        Saves=await db.Saves.AsNoTracking().Where(x => x.UserId == UserId).OrderByDescending(x => x.SavedAt).ToListAsync(),
        Runs=await db.Runs.AsNoTracking().Where(x => x.UserId == UserId).ToListAsync()
    });
    [HttpPost] public async Task<IActionResult> DeleteSave(int id, bool confirm) {
        if (!confirm) { ErrorNotice("Hãy xác nhận trước khi xóa mục lưu."); return RedirectToAction(nameof(Vault)); }
        await db.Saves.Where(x => x.SaveId == id && x.UserId == UserId).ExecuteDeleteAsync();
        Notice("Đã xóa mục lưu. Lịch sử lượt chơi vẫn được giữ.");
        return RedirectToAction(nameof(Vault));
    }
}

