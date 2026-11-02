using System.Data;
using Microsoft.EntityFrameworkCore;
using PlayEdu.Data;
using PlayEdu.Models;
namespace PlayEdu.Services;
public class AiOperation(GameDbContext db, CoinService coins, AiGateway ai, ILogger<AiOperation> logger) {
    public async Task<T> Execute<T>(int userId, string feature, Func<Task<int>> price, Func<Task<T>> generate, Func<T,Task> persist) {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try {
            var found = await db.Users.Where(u => u.UserId == userId && !u.IsLocked)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Coin,u => u.Coin));
            if (found == 0) throw new InvalidOperationException("Tài khoản không khả dụng.");
            var cost = ai.IsDemo ? 0 : await price();
            if (cost > 0) await coins.Change(userId,-cost,feature,feature);
            var result = await generate();
            await persist(result);
            db.AICalls.Add(new() { UserId = userId, FeatureName = feature, AiProvider = ai.Provider,
                AiModel = ai.Model, CoinCharged = cost, InputTokens = ai.InputTokens,
                OutputTokens = ai.OutputTokens, CostUSD = ai.Cost, IsSuccess = true });
            await db.SaveChangesAsync(); await tx.CommitAsync();
            logger.LogInformation("AI feature {Feature} completed in {Ms}ms",feature,ai.DurationMs);
            return result;
        } catch {
            await tx.RollbackAsync(); db.ChangeTracker.Clear();
            db.AICalls.Add(new() { UserId = userId, FeatureName = feature, AiProvider = ai.Provider, AiModel = ai.Model,
                IsSuccess = false, InputTokens = ai.InputTokens, OutputTokens = ai.OutputTokens, CostUSD = ai.Cost, CoinCharged = 0 });
            await db.SaveChangesAsync();
            throw;
        }
    }
}

