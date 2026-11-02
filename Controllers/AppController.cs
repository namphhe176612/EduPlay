using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
namespace PlayEdu.Controllers;
public abstract class AppController : Controller {
    protected int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    protected void Notice(string message) => TempData["Notice"] = message;
    protected void ErrorNotice(string message) => TempData["Error"] = message;
}

