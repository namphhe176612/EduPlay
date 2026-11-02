using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace PlayEdu.Models;

public class AppUser {
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public int Role { get; set; } = 1;
    public int Coin { get; set; } = 20;
    public bool IsLocked { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class LectureFile {
    public int FileId { get; set; }
    public int UserId { get; set; }
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public int TotalPages { get; set; }
    public string? Language { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public List<Slide> Slides { get; set; } = [];
}
public class Slide {
    public int SlideId { get; set; }
    public int FileId { get; set; }
    public int SlideNumber { get; set; }
    public string? SlideText { get; set; }
    public string? ImagePath { get; set; }
}
public class Scenario {
    public int ScenarioId { get; set; }
    public int UserId { get; set; }
    public int FileId { get; set; }
    public string Title { get; set; } = "";
    public string? RoleName { get; set; }
    public string? CompanyName { get; set; }
    public string? Description { get; set; }
    public string? ScenarioJson { get; set; }
    public string? AiModel { get; set; }
    public int CoinCost { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ScenarioData Data => JsonSerializer.Deserialize<ScenarioData>(ScenarioJson ?? "{}", JsonDefaults.Options) ?? new();
}
public class PlayRun {
    public int RunId { get; set; }
    public int UserId { get; set; }
    public int ScenarioId { get; set; }
    public decimal? Score { get; set; }
    public string Status { get; set; } = "Playing";
    public int? Rating { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public List<RunChoice> Choices { get; set; } = [];
}
public class RunChoice {
    public int RunChoiceId { get; set; }
    public int RunId { get; set; }
    public int StepNumber { get; set; }
    public string Choice { get; set; } = "";
    public string? ChoiceText { get; set; }
    public string? ResultText { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class CoinTransaction {
    public int TransactionId { get; set; }
    public int UserId { get; set; }
    public int Amount { get; set; }
    public string TransactionType { get; set; } = "";
    public string? Description { get; set; }
    public int BalanceAfter { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class AiCall {
    public int AICallId { get; set; }
    public int UserId { get; set; }
    public string FeatureName { get; set; } = "";
    public string? AiProvider { get; set; }
    public string? AiModel { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal CostUSD { get; set; }
    public int CoinCharged { get; set; }
    public bool IsSuccess { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class Payment {
    public int PaymentId { get; set; }
    public int UserId { get; set; }
    public string PackageName { get; set; } = "";
    public int AmountVND { get; set; }
    public int CoinAmount { get; set; }
    public string? PaymentMethod { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class AdminLog {
    public int AdminLogId { get; set; }
    public int AdminUserId { get; set; }
    public string Action { get; set; } = "";
    public int? TargetUserId { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class DeepPractice {
    public int DeepPracticeId { get; set; }
    public int UserId { get; set; }
    public int ScenarioId { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public int CoinCost { get; set; }
    public string? ResultJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class SavedRun {
    public int SaveId { get; set; }
    public int UserId { get; set; }
    public int RunId { get; set; }
    public string Title { get; set; } = "";
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
}
public class ScenarioMessage {
    public int MessageId { get; set; }
    public int UserId { get; set; }
    public int ScenarioId { get; set; }
    public string Question { get; set; } = "";
    public string Answer { get; set; } = "";
    public int SourceSlide { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public static class JsonDefaults {
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
public class ScenarioData {
    public string Title { get; set; } = "";
    public string Role { get; set; } = "";
    public string Company { get; set; } = "";
    public string Premise { get; set; } = "";
    public bool IsDemo { get; set; }
    public List<Metric> Metrics { get; set; } = [];
    public List<Scene> Scenes { get; set; } = [];
}
public class Metric {
    public string Name { get; set; } = "";
    public int Initial { get; set; } = 50;
}
public class Scene {
    public string Topic { get; set; } = "";
    public string Sender { get; set; } = "";
    public string Channel { get; set; } = "";
    public string Message { get; set; } = "";
    public int SourceSlide { get; set; }
    public string SourceQuote { get; set; } = "";
    public List<Decision> Choices { get; set; } = [];
}
public class Decision {
    public string Key { get; set; } = "";
    public string Text { get; set; } = "";
    public string Outcome { get; set; } = "";
    public string Why { get; set; } = "";
    public string Quality { get; set; } = "";
    public List<int> Deltas { get; set; } = [];
}
public class AnswerData {
    public string Answer { get; set; } = "";
    public int SourceSlide { get; set; }
}
public class QuizData {
    public List<QuizQuestion> Questions { get; set; } = [];
    public List<int>? Answers { get; set; }
    public bool Submitted { get; set; }
}
public class QuizQuestion {
    public string Question { get; set; } = "";
    public List<string> Options { get; set; } = [];
    public int Correct { get; set; }
    public string Why { get; set; } = "";
    public int SourceSlide { get; set; }
}
public class AuthForm {
    [Required, StringLength(100)] public string FullName { get; set; } = "";
    [Required, EmailAddress, StringLength(255)] public string Email { get; set; } = "";
    [Required, MinLength(10), MaxLength(128)] public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
}
public class LoginForm {
    [Required, EmailAddress, StringLength(255)] public string Email { get; set; } = "";
    [Required, MaxLength(128)] public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
}
public class DashboardVm {
    public AppUser User { get; set; } = new();
    public List<LectureFile> Files { get; set; } = [];
    public List<Scenario> Scenarios { get; set; } = [];
    public List<PlayRun> Runs { get; set; } = [];
}
public class PlayVm {
    public Scenario Scenario { get; set; } = new();
    public PlayRun Run { get; set; } = new();
    public int Step { get; set; }
    public int[] Metrics { get; set; } = [];
    public bool ShowOutcome { get; set; }
    public Decision? Selected { get; set; }
}
public class ResultsVm {
    public Scenario Scenario { get; set; } = new();
    public PlayRun Run { get; set; } = new();
    public int[] Metrics { get; set; } = [];
    public decimal DecisionScore { get; set; }
    public decimal BusinessScore { get; set; }
    public List<ScenarioMessage> Messages { get; set; } = [];
    public bool IsSaved { get; set; }
}
public class WalletVm {
    public int Coin { get; set; }
    public List<CoinTransaction> Transactions { get; set; } = [];
}
public class AdminVm {
    public List<AppUser> Users { get; set; } = [];
    public List<AiCall> Calls { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
    public List<AdminLog> Logs { get; set; } = [];
    public int FileCount { get; set; }
    public int RunCount { get; set; }
}
public class VaultVm {
    public List<SavedRun> Saves { get; set; } = [];
    public List<PlayRun> Runs { get; set; } = [];
}

