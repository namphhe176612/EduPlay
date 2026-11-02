using System.ComponentModel.DataAnnotations;
namespace PlayEdu.Models;

public class EduPage {
 public string Page { get; set; } = "today";
 public bool Preview { get; set; }
 public long UserId { get; set; }
 public string Name { get; set; } = "Trang";
 public int Balance { get; set; }
 public bool IsAdmin { get; set; }
 public List<Dictionary<string,object?>> Courses { get; set; } = [];
 public List<Dictionary<string,object?>> Chapters { get; set; } = [];
 public List<Dictionary<string,object?>> Items { get; set; } = [];
 public List<Dictionary<string,object?>> Extra { get; set; } = [];
 public Dictionary<string,object?> Data { get; set; } = [];
 public List<Dictionary<string,object?>> Packages { get; set; } = [];
 public long Id { get; set; }
 public string? Query { get; set; }
 public string S(string key, string fallback="") => Data.TryGetValue(key,out var x) && x != null ? Convert.ToString(x)! : fallback;
 public static string Text(Dictionary<string,object?> row,string key,string fallback="") => row.TryGetValue(key,out var x) && x!=null ? Convert.ToString(x)! : fallback;
 public static long Number(Dictionary<string,object?> row,string key) => row.TryGetValue(key,out var x) && x!=null ? Convert.ToInt64(x) : 0;
}
public class EduRegistration {
 [Required,StringLength(100)] public string FullName { get; set; }="";
 [Required,EmailAddress,StringLength(255)] public string Email { get; set; }="";
 [Required,MinLength(8),MaxLength(128)] public string Password { get; set; }="";
 [Required,Compare(nameof(Password))] public string ConfirmPassword { get; set; }="";
 public int Semester { get; set; }=4;
 public bool Marketing { get; set; }=true;
 public string Source { get; set; }="Facebook";
 public string? Referral { get; set; }
 public bool MarketingConsent { get; set; }
 [Range(typeof(bool),"true","true",ErrorMessage="Bạn cần đồng ý điều khoản sử dụng.")] public bool AcceptTerms { get; set; }
}
public class EduLogin {
 [Required,EmailAddress] public string Email { get; set; }="";
 [Required,MaxLength(128)] public string Password { get; set; }="";
 public bool Remember { get; set; }
}
