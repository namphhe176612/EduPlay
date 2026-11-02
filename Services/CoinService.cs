using Microsoft.EntityFrameworkCore;
using PlayEdu.Data;
using PlayEdu.Models;
namespace PlayEdu.Services;
public class CoinService(GameDbContext db) {
    // Call inside a transaction. SQL updates and ledger insertion commit together.
    public async Task Change(int userId, int amount, string type, string reason) {
        var changed = await db.Users.Where(x => x.UserId == userId && !x.IsLocked && (long)x.Coin + amount >= 0 && (long)x.Coin + amount <= int.MaxValue)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Coin, x => x.Coin + amount));
        if (changed != 1) throw new InvalidOperationException("Không đủ coin hoặc tài khoản bị khóa.");
        var balance = await db.Users.AsNoTracking().Where(x => x.UserId == userId).Select(x => x.Coin).SingleAsync();
        db.CoinTransactions.Add(new CoinTransaction { UserId = userId, Amount = amount,
            TransactionType = type, Description = reason[..Math.Min(reason.Length,255)], BalanceAfter = balance });
        await db.SaveChangesAsync();
    }
}

