using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayEdu.Data;
using PlayEdu.Models;
namespace PlayEdu.Controllers;
[Authorize(Roles = "1")]
public class WalletController(GameDbContext db) : AppController {
    public async Task<IActionResult> Index() => View(new WalletVm {
        Coin=await db.Users.Where(x => x.UserId == UserId).Select(x => x.Coin).SingleAsync(),
        Transactions=await db.CoinTransactions.AsNoTracking().Where(x => x.UserId == UserId).OrderByDescending(x => x.TransactionId).Take(100).ToListAsync()
    });
}

