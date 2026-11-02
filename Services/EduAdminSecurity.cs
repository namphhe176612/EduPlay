using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;
namespace PlayEdu.Services;
public class EduAdminSecurity(IDataProtectionProvider provider) {
 readonly IDataProtector protector=provider.CreateProtector("EduPlay.V2.AdminTotp.v1");
 public byte[] Encrypt(byte[] secret)=>protector.Protect(secret);
 public byte[] Decrypt(byte[] secret)=>protector.Unprotect(secret);
 public static string Base32(byte[] bytes) {
  const string alphabet="ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";var output=new System.Text.StringBuilder();int buffer=0,bits=0;
  foreach(var b in bytes){buffer=(buffer<<8)|b;bits+=8;while(bits>=5){bits-=5;output.Append(alphabet[(buffer>>bits)&31]);}}
  if(bits>0)output.Append(alphabet[(buffer<<(5-bits))&31]);return output.ToString();
 }
 public static string Code(byte[] secret,long counter) {
  var bytes=BitConverter.GetBytes(counter);if(BitConverter.IsLittleEndian)Array.Reverse(bytes);
  var hash=HMACSHA1.HashData(secret,bytes);int offset=hash[^1]&15;
  int number=((hash[offset]&127)<<24)|(hash[offset+1]<<16)|(hash[offset+2]<<8)|hash[offset+3];return (number%1000000).ToString("D6");
 }
 public long? Verify(byte[] encrypted,string code,long lastCounter) {
  if(code.Length!=6||!code.All(char.IsAsciiDigit))return null;
  var secret=Decrypt(encrypted);long now=DateTimeOffset.UtcNow.ToUnixTimeSeconds()/30;
  try{for(long c=now-1;c<=now+1;c++)if(c>lastCounter&&CryptographicOperations.FixedTimeEquals(System.Text.Encoding.ASCII.GetBytes(Code(secret,c)),System.Text.Encoding.ASCII.GetBytes(code)))return c;return null;}
  finally{CryptographicOperations.ZeroMemory(secret);}
 }
}
