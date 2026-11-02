using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayEdu.Data;
using PlayEdu.Models;
using PlayEdu.Services;
namespace PlayEdu.Controllers;
public class HomeController(GameDbContext db) : AppController {
    public IActionResult Index() => View();
    [Authorize(Roles = "1")]
    public async Task<IActionResult> Dashboard() => View(new DashboardVm {
        User = await db.Users.AsNoTracking().SingleAsync(x => x.UserId == UserId),
        Files = await db.Files.AsNoTracking().Where(x => x.UserId == UserId).OrderByDescending(x => x.UploadedAt).ToListAsync(),
        Scenarios = await db.Scenarios.AsNoTracking().Where(x => x.UserId == UserId).OrderByDescending(x => x.CreatedAt).ToListAsync(),
        Runs = await db.Runs.AsNoTracking().Where(x => x.UserId == UserId).OrderByDescending(x => x.StartedAt).ToListAsync()
    });
    public IActionResult Sample() {
        var slides = new List<Slide> {
            new() { SlideNumber = 1, SlideText = "Phân khúc khách hàng: chia thị trường thành các nhóm có nhu cầu tương đồng." },
            new() { SlideNumber = 2, SlideText = "Định vị: xây dựng vị trí rõ ràng của thương hiệu trong tâm trí khách hàng mục tiêu." },
            new() { SlideNumber = 3, SlideText = "Giá trị đề xuất cần gắn với nhu cầu của phân khúc được lựa chọn." },
            new() { SlideNumber = 4, SlideText = "Đo lường hiệu quả chiến dịch dựa trên mục tiêu và chỉ số đã xác định trước." }
        };
        return View(AiGateway.Demo(slides));
    }
    public IActionResult Help() => View();
    public IActionResult Error() { Response.StatusCode = 500; return View(); }
}

