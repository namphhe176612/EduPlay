using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayEdu.Data;
using PlayEdu.Models;
using PlayEdu.Services;
using UglyToad.PdfPig;

namespace PlayEdu.Controllers;
[Authorize(Roles = "1")]
public class DocumentsController(GameDbContext db, IWebHostEnvironment env, CoinService coins, ILogger<DocumentsController> logger) : AppController {
    [HttpGet] public async Task<IActionResult> Index() => View(await db.Files.AsNoTracking().Where(x => x.UserId == UserId).OrderByDescending(x => x.UploadedAt).ToListAsync());
    [HttpGet] public IActionResult Upload() => View();
    [HttpPost, RequestSizeLimit(22 * 1024 * 1024), RequestFormLimits(MultipartBodyLengthLimit = 22 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile? pdf, string language = "vi", bool unlockPages = false) {
        if (pdf == null || pdf.Length == 0 || pdf.Length > 20 * 1024 * 1024 ||
            !Path.GetExtension(pdf.FileName).Equals(".pdf",StringComparison.OrdinalIgnoreCase) || pdf.ContentType != "application/pdf") {
            ModelState.AddModelError("", "Chọn file PDF hợp lệ, tối đa 20 MB. Với Word/PowerPoint, hãy xuất sang PDF trước.");
            return View();
        }
        if (language is not ("vi" or "en" or "auto" or "vi-en")) {
            ModelState.AddModelError("", "Ngôn ngữ không hợp lệ."); return View();
        }
        var directory = Path.Combine(env.ContentRootPath,"App_Data","documents",UserId.ToString());
        Directory.CreateDirectory(directory);
        var key = Guid.NewGuid().ToString("N") + ".pdf";
        var path = Path.Combine(directory,key);
        try {
            await using (var output = new FileStream(path,FileMode.CreateNew)) await pdf.CopyToAsync(output);
            var signature = new byte[5];
            await using (var input = System.IO.File.OpenRead(path)) await input.ReadExactlyAsync(signature);
            if (!signature.SequenceEqual("%PDF-"u8.ToArray())) throw new InvalidDataException("File không có chữ ký PDF hợp lệ.");
            List<Slide> slides = [];
            using (var document = PdfDocument.Open(path)) {
                if (document.NumberOfPages < 1 || document.NumberOfPages > 40)
                    throw new InvalidDataException("PDF cần có từ 1 đến 40 trang.");
                if (document.NumberOfPages > 15 && !unlockPages)
                    throw new InvalidDataException("PDF hơn 15 trang: xác nhận mở rộng tới 40 trang với 15 coin, hoặc tải PDF ngắn hơn.");
                foreach (var page in document.GetPages()) {
                    var text = page.Text;
                    if (text.Length > 100000) throw new InvalidDataException("Trang PDF chứa quá nhiều văn bản.");
                    slides.Add(new() { SlideNumber = page.Number, SlideText = text });
                }
            }
            if (slides.All(x => string.IsNullOrWhiteSpace(x.SlideText)))
                throw new InvalidDataException("PDF là ảnh scan hoặc không có văn bản. Hãy OCR thành PDF có thể chọn chữ rồi tải lại.");
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            if (slides.Count > 15) await coins.Change(UserId,-15,"PageUnlock","Mở rộng PDF tới 40 trang");
            var file = new LectureFile { UserId = UserId, FileName = Path.GetFileName(pdf.FileName)[..Math.Min(Path.GetFileName(pdf.FileName).Length,255)],
                FilePath = Path.Combine(UserId.ToString(),key), Language = language, TotalPages = slides.Count, Slides = slides };
            db.Files.Add(file); await db.SaveChangesAsync(); await tx.CommitAsync();
            Notice("Đã nạp PDF và trích xuất nội dung từng trang.");
            return RedirectToAction(nameof(Details),new { id = file.FileId });
        } catch (Exception ex) {
            System.IO.File.Delete(path);
            logger.LogWarning("PDF processing failed: {Type}",ex.GetType().Name);
            ModelState.AddModelError("", ex is InvalidDataException or InvalidOperationException ? ex.Message : "Không đọc được PDF. File có thể hỏng hoặc được mã hóa. Hãy xuất lại PDF.");
            return View();
        }
    }
    [HttpGet] public async Task<IActionResult> Details(int id, int page = 1) {
        var file = await db.Files.AsNoTracking().Include(x => x.Slides).FirstOrDefaultAsync(x => x.FileId == id && x.UserId == UserId);
        if (file == null) return NotFound();
        ViewData["Page"] = Math.Clamp(page,1,file.TotalPages);
        ViewData["FileExists"] = ResolvePath(file) is string path && System.IO.File.Exists(path);
        return View(file);
    }
    [HttpGet] public async Task<IActionResult> Pdf(int id) {
        var file = await db.Files.AsNoTracking().FirstOrDefaultAsync(x => x.FileId == id && x.UserId == UserId);
        if (file == null) return NotFound();
        var path = ResolvePath(file);
        if (path == null || !System.IO.File.Exists(path)) return NotFound("File mẫu chưa có PDF thật. Hãy tải tài liệu của bạn.");
        Response.Headers["Content-Disposition"] = "inline";
        return PhysicalFile(path,"application/pdf",enableRangeProcessing:true);
    }
    private string? ResolvePath(LectureFile file) {
        var root = Path.GetFullPath(Path.Combine(env.ContentRootPath,"App_Data","documents")) + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(root,file.FilePath));
        return path.StartsWith(root,StringComparison.OrdinalIgnoreCase) ? path : null;
    }
}

