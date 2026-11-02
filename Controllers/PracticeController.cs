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
public class PracticeController(GameDbContext db, AiOperation operation, AiGateway ai, ILogger<PracticeController> logger) : AppController {
    [HttpPost] public async Task<IActionResult> Create(int scenarioId, int count = 6, string? focus = null, CancellationToken cancellationToken = default) {
        if (count is < 3 or > 20 || (focus?.Length ?? 0) > 200) return BadRequest();
        var scenario = await db.Scenarios.AsNoTracking().FirstOrDefaultAsync(x => x.ScenarioId == scenarioId && x.UserId == UserId);
        if (scenario == null) return NotFound();
        var slides = await db.Slides.AsNoTracking().Where(x => x.FileId == scenario.FileId && x.SlideText != null && x.SlideText != "").OrderBy(x => x.SlideNumber).ToListAsync();
        if (slides.Count == 0) return BadRequest();
        DeepPractice? session = null;
        var history = await db.DeepPractices.AsNoTracking().Where(x => x.UserId == UserId && x.ScenarioId == scenarioId)
            .OrderByDescending(x => x.CreatedAt).Take(5).ToListAsync();
        var recent = string.Join("\n", history.SelectMany(h => JsonSerializer.Deserialize<QuizData>(h.ResultJson ?? "{}",JsonDefaults.Options)?.Questions ?? []).Select(q => q.Question));
        var cost = 0;
        try {
            await operation.Execute(UserId,"Deep",async () => {
                var today = DateTime.UtcNow.Date;
                var freeUsed = await db.DeepPractices.AnyAsync(x => x.UserId == UserId && x.CreatedAt >= today);
                cost = !freeUsed && count <= 6 ? 0 : (int)Math.Ceiling(count/3m);
                return cost;
            },() => ai.Practice(slides,focus ?? "",recent,count,cancellationToken), quiz => {
                session = new() { UserId=UserId, ScenarioId=scenarioId, TotalQuestions=quiz.Questions.Count,
                    CoinCost=ai.IsDemo ? 0 : cost, ResultJson=JsonSerializer.Serialize(quiz,JsonDefaults.Options) };
                db.DeepPractices.Add(session); return Task.CompletedTask;
            });
            return RedirectToAction(nameof(Play),new { id=session!.DeepPracticeId });
        } catch (Exception ex) {
            logger.LogWarning("Practice failed: {Type}",ex.GetType().Name);
            ErrorNotice(ex is InvalidOperationException ? ex.Message : "Không tạo được bài luyện tập. Coin không bị trừ.");
            return RedirectToAction("Details","Scenarios",new { id=scenarioId });
        }
    }
    [HttpGet] public async Task<IActionResult> Play(int id) {
        var session = await db.DeepPractices.AsNoTracking().FirstOrDefaultAsync(x => x.DeepPracticeId == id && x.UserId == UserId);
        if (session == null) return NotFound();
        var scenario = await db.Scenarios.AsNoTracking().FirstOrDefaultAsync(x => x.ScenarioId == session.ScenarioId && x.UserId == UserId);
        if (scenario == null) return NotFound();
        ViewData["FileId"] = scenario.FileId;
        return View(session);
    }
    [HttpPost] public async Task<IActionResult> Submit(int id, int[] answers) {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await db.DeepPractices.Where(x => x.DeepPracticeId == id && x.UserId == UserId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.CorrectAnswers,x => x.CorrectAnswers));
        var session = await db.DeepPractices.FirstOrDefaultAsync(x => x.DeepPracticeId == id && x.UserId == UserId);
        if (session == null) return NotFound();
        var data = JsonSerializer.Deserialize<QuizData>(session.ResultJson ?? "{}",JsonDefaults.Options)!;
        if (data.Submitted) return RedirectToAction(nameof(Play),new { id });
        if (answers.Length != data.Questions.Count || answers.Any(a => a is < 0 or > 2)) {
            ErrorNotice("Hãy trả lời đủ các câu hỏi."); return RedirectToAction(nameof(Play),new { id });
        }
        data.Answers=answers.ToList(); data.Submitted=true;
        session.CorrectAnswers=data.Questions.Select((q,i) => q.Correct == answers[i] ? 1 : 0).Sum();
        session.ResultJson=JsonSerializer.Serialize(data,JsonDefaults.Options);
        await db.SaveChangesAsync(); await tx.CommitAsync();
        return RedirectToAction(nameof(Play),new { id });
    }
}

