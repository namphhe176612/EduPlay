using Microsoft.EntityFrameworkCore;
using PlayEdu.Models;

namespace PlayEdu.Data;

public class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options) {
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<LectureFile> Files => Set<LectureFile>();
    public DbSet<Slide> Slides => Set<Slide>();
    public DbSet<Scenario> Scenarios => Set<Scenario>();
    public DbSet<PlayRun> Runs => Set<PlayRun>();
    public DbSet<RunChoice> RunChoices => Set<RunChoice>();
    public DbSet<CoinTransaction> CoinTransactions => Set<CoinTransaction>();
    public DbSet<AiCall> AICalls => Set<AiCall>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<AdminLog> AdminLogs => Set<AdminLog>();
    public DbSet<DeepPractice> DeepPractices => Set<DeepPractice>();
    public DbSet<SavedRun> Saves => Set<SavedRun>();
    public DbSet<ScenarioMessage> ScenarioMessages => Set<ScenarioMessage>();
    protected override void OnModelCreating(ModelBuilder b) {
        b.Entity<AppUser>().ToTable("Users").HasKey(x => x.UserId);
        b.Entity<LectureFile>().ToTable("Files").HasKey(x => x.FileId);
        b.Entity<Slide>().ToTable("Slides").HasKey(x => x.SlideId);
        b.Entity<Scenario>().ToTable("Scenarios").HasKey(x => x.ScenarioId);
        b.Entity<Scenario>().Ignore(x => x.Data);
        b.Entity<PlayRun>().ToTable("Runs").HasKey(x => x.RunId);
        b.Entity<RunChoice>().ToTable("RunChoices").HasKey(x => x.RunChoiceId);
        b.Entity<CoinTransaction>().ToTable("CoinTransactions").HasKey(x => x.TransactionId);
        b.Entity<AiCall>().ToTable("AICalls").HasKey(x => x.AICallId);
        b.Entity<Payment>().ToTable("Payments").HasKey(x => x.PaymentId);
        b.Entity<AdminLog>().ToTable("AdminLogs").HasKey(x => x.AdminLogId);
        b.Entity<DeepPractice>().ToTable("DeepPractices").HasKey(x => x.DeepPracticeId);
        b.Entity<SavedRun>().ToTable("Saves").HasKey(x => x.SaveId);
        b.Entity<ScenarioMessage>().ToTable("ScenarioMessages").HasKey(x => x.MessageId);
        b.Entity<LectureFile>().HasMany(x => x.Slides).WithOne().HasForeignKey(x => x.FileId);
        b.Entity<PlayRun>().HasMany(x => x.Choices).WithOne().HasForeignKey(x => x.RunId);
        b.Entity<PlayRun>().Property(x => x.Score).HasPrecision(5, 2);
        b.Entity<AiCall>().Property(x => x.CostUSD).HasPrecision(18, 6);
    }
}

