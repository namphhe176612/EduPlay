using System.Data;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayEdu.Data;
using PlayEdu.Models;
using PlayEdu.Services;

namespace PlayEdu.Controllers;
[Authorize(Roles = "1")]
public class ScenariosController(GameDbContext db, AiGateway ai, AiOperation operation, ILogger<ScenariosController> logger) : AppController {
    private Task<Scenario?> Owned(int id) => db.Scenarios.AsNoTracking().FirstOrDefaultAsync(x => x.ScenarioId == id && x.UserId == UserId);
    [HttpPost]
    public async Task<IActionResult> Build(int fileId, string? context, CancellationToken cancellationToken) {
        if ((context?.Length ?? 0) > 1000) { ErrorNotice("Bối cảnh tối đa 1.000 ký tự."); return RedirectToAction("Details","Documents",new { id = fileId }); }
        var file = await db.Files.AsNoTracking().Include(x => x.Slides).FirstOrDefaultAsync(x => x.FileId == fileId && x.UserId == UserId);
        if (file == null) return NotFound();
        var slides = file.Slides.Where(x => !string.IsNullOrWhiteSpace(x.SlideText)).OrderBy(x => x.SlideNumber).ToList();
        if (slides.Count == 0) { ErrorNotice("Tài liệu chưa có nội dung văn bản để dựng sa bàn."); return RedirectToAction("Details","Documents",new { id=fileId }); }
        Scenario? scenario = null;
        try {
            await operation.Execute(UserId,"Build",() => Task.FromResult(3),
                () => ai.Build(slides,context ?? "",file.Language ?? "vi",cancellationToken), data => {
                    scenario = new() { UserId = UserId, FileId = fileId, Title = data.Title, RoleName = data.Role,
                        CompanyName = data.Company, Description = data.Premise, AiModel = ai.Model, CoinCost = ai.IsDemo ? 0 : 3,
                        ScenarioJson = JsonSerializer.Serialize(data,JsonDefaults.Options) };
                    db.Scenarios.Add(scenario); return Task.CompletedTask;
                });
            return RedirectToAction(nameof(Details),new { id = scenario!.ScenarioId });
        } catch (Exception ex) {
            logger.LogWarning("Build failed: {Type}",ex.GetType().Name);
            ErrorNotice(ex is InvalidOperationException ? ex.Message : "Không dựng được sa bàn. Coin đã được giữ nguyên. Hãy thử lại hoặc kiểm tra cấu hình AI.");
            return RedirectToAction("Details","Documents",new { id=fileId });
        }
    }
    [HttpGet] public async Task<IActionResult> Details(int id) {
        var scenario = await Owned(id);
        if (scenario == null) return NotFound();
        if (string.IsNullOrWhiteSpace(scenario.ScenarioJson)) { ErrorNotice("Kịch bản mẫu trong SQL chưa có nội dung chơi. Hãy dựng sa bàn từ PDF của bạn."); return RedirectToAction("Dashboard","Home"); }
        return View(scenario);
    }
    [HttpPost] public async Task<IActionResult> Start(int id) {
        var scenario = await Owned(id);
        if (scenario == null || string.IsNullOrWhiteSpace(scenario.ScenarioJson)) return NotFound();
        var run = new PlayRun { UserId = UserId, ScenarioId = id };
        db.Runs.Add(run); await db.SaveChangesAsync();
        return RedirectToAction(nameof(Play),new { id=run.RunId });
    }
    [HttpGet] public async Task<IActionResult> Play(int id, bool outcome = false) {
        var run = await db.Runs.AsNoTracking().Include(x => x.Choices).FirstOrDefaultAsync(x => x.RunId == id && x.UserId == UserId);
        if (run == null) return NotFound();
        var scenario = await Owned(run.ScenarioId);
        if (scenario == null || string.IsNullOrWhiteSpace(scenario.ScenarioJson)) return NotFound();
        if (run.Status == "Completed" && !outcome) return RedirectToAction(nameof(Results),new { id });
        var data = scenario.Data;
        var step = outcome && run.Choices.Count > 0 ? run.Choices.Count-1 : run.Choices.Count;
        if (step >= data.Scenes.Count) return RedirectToAction(nameof(Results),new { id });
        var chosen = run.Choices.FirstOrDefault(c => c.StepNumber == step+1);
        return View(new PlayVm { Scenario=scenario, Run=run, Step=step, ShowOutcome=chosen != null,
            Selected=chosen == null ? null : data.Scenes[step].Choices.Single(c => c.Key == chosen.Choice),
            Metrics=Simulation.Metrics(data,run.Choices) });
    }
    [HttpPost] public async Task<IActionResult> Choose(int id, int step, string choice) {
        if (choice is not ("A" or "B" or "C")) return BadRequest();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        // Acquire a write lock before reading choices so simultaneous requests cannot double-submit.
        var locked = await db.Runs.Where(x => x.RunId == id && x.UserId == UserId && x.Status == "Playing")
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status,x => x.Status));
        if (locked == 0) return RedirectToAction(nameof(Play),new { id });
        var run = await db.Runs.Include(x => x.Choices).SingleAsync(x => x.RunId == id && x.UserId == UserId);
        var scenario = await Owned(run.ScenarioId);
        if (scenario == null) return NotFound();
        var data = scenario.Data;
        if (step != run.Choices.Count+1 || step < 1 || step > data.Scenes.Count)
            return RedirectToAction(nameof(Play),new { id, outcome=true });
        var decision = data.Scenes[step-1].Choices.Single(c => c.Key == choice);
        run.Choices.Add(new() { StepNumber=step, Choice=choice, ChoiceText=decision.Text, ResultText=decision.Outcome });
        if (step == data.Scenes.Count) {
            var scores = Simulation.Score(data,run.Choices);
            run.Score = scores.Decision + scores.Business; run.Status="Completed"; run.CompletedAt=DateTime.UtcNow;
        }
        await db.SaveChangesAsync(); await tx.CommitAsync();
        return RedirectToAction(nameof(Play),new { id, outcome=true });
    }
    [HttpGet] public async Task<IActionResult> Results(int id) {
        var run = await db.Runs.AsNoTracking().Include(x => x.Choices).FirstOrDefaultAsync(x => x.RunId == id && x.UserId == UserId);
        if (run == null) return NotFound();
        if (run.Status != "Completed") return RedirectToAction(nameof(Play),new { id });
        var scenario = await Owned(run.ScenarioId);
        if (scenario == null || string.IsNullOrWhiteSpace(scenario.ScenarioJson)) {
            ErrorNotice("Lượt chơi mẫu chưa có kịch bản JSON. Hãy tạo lượt chơi mới."); return RedirectToAction("Dashboard","Home");
        }
        var scores = Simulation.Score(scenario.Data,run.Choices);
        return View(new ResultsVm { Scenario=scenario,Run=run,Metrics=Simulation.Metrics(scenario.Data,run.Choices),
            DecisionScore=scores.Decision,BusinessScore=scores.Business,
            Messages=await db.ScenarioMessages.AsNoTracking().Where(x => x.UserId == UserId && x.ScenarioId == scenario.ScenarioId).OrderBy(x => x.CreatedAt).ToListAsync(),
            IsSaved=await db.Saves.AnyAsync(x => x.UserId == UserId && x.RunId == id) });
    }
    [HttpPost] public async Task<IActionResult> Rate(int id, int rating) {
        if (rating is < 1 or > 5) return BadRequest();
        await db.Runs.Where(x => x.RunId == id && x.UserId == UserId && x.Status == "Completed").ExecuteUpdateAsync(s => s.SetProperty(x => x.Rating,rating));
        return RedirectToAction(nameof(Results),new { id });
    }
    [HttpPost] public async Task<IActionResult> Ask(int id, string? question, CancellationToken cancellationToken) {
        var run = await db.Runs.AsNoTracking().FirstOrDefaultAsync(x => x.RunId == id && x.UserId == UserId && x.Status == "Completed");
        if (run == null) return NotFound();
        var scenario = await Owned(run.ScenarioId); if (scenario == null) return NotFound();
        if (string.IsNullOrWhiteSpace(question) || question.Length > 1000) { ErrorNotice("Câu hỏi cần từ 1 đến 1.000 ký tự."); return RedirectToAction(nameof(Results),new { id }); }
        var slides = await db.Slides.AsNoTracking().Where(x => x.FileId == scenario.FileId).OrderBy(x => x.SlideNumber).ToListAsync();
        try {
            await operation.Execute(UserId,$"Ask:{scenario.ScenarioId}",
                async () => await db.ScenarioMessages.CountAsync(x => x.UserId == UserId && x.ScenarioId == scenario.ScenarioId) < 3 ? 0 : 1,
                () => ai.Ask(slides,question,cancellationToken),
                answer => { db.ScenarioMessages.Add(new() { UserId=UserId,ScenarioId=scenario.ScenarioId,
                    Question=question.Trim(), Answer=answer.Answer, SourceSlide=answer.SourceSlide }); return Task.CompletedTask; });
        } catch (Exception ex) { logger.LogWarning("Ask failed: {Type}",ex.GetType().Name); ErrorNotice(ex is InvalidOperationException ? ex.Message : "Chưa lấy được câu trả lời; coin không bị trừ."); }
        return RedirectToAction(nameof(Results),new { id });
    }
    [HttpPost] public async Task<IActionResult> Save(int id) {
        var run = await db.Runs.AsNoTracking().FirstOrDefaultAsync(x => x.RunId == id && x.UserId == UserId && x.Status == "Completed");
        if (run == null) return NotFound();
        var scenario = await Owned(run.ScenarioId); if (scenario == null) return NotFound();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await db.Users.Where(x => x.UserId == UserId).ExecuteUpdateAsync(s => s.SetProperty(x => x.Coin,x => x.Coin));
        if (!await db.Saves.AnyAsync(x => x.UserId == UserId && x.RunId == id)) {
            if (await db.Saves.CountAsync(x => x.UserId == UserId) >= 3) ErrorNotice("Bạn đã dùng đủ 3 chỗ lưu miễn phí. Hãy xóa một mục trong kho để lưu mục mới.");
            else { db.Saves.Add(new() { UserId=UserId,RunId=id,Title=scenario.Title }); await db.SaveChangesAsync(); Notice("Đã lưu kết quả vào kho."); }
        }
        await tx.CommitAsync(); return RedirectToAction(nameof(Results),new { id });
    }
}

