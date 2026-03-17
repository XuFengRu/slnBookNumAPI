using BookNumAPI.Models;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BookNumAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly BookNumApiContext _context;
        private readonly IConfiguration _configuration;
        public AuthController(BookNumApiContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // 前端傳來的 JSON
        public class LoginRequestDto
        {
            [Required(ErrorMessage = "請輸入電子信箱")]
            [EmailAddress(ErrorMessage = "電子信箱格式錯誤")]
            public string Email { get; set; } = string.Empty;
            [Required(ErrorMessage = "請輸入密碼")]
            public string Password { get; set; } = string.Empty;
            public bool RememberMe { get; set; }
            public decimal? Latitude { get; set; }
            public decimal? Longitude { get; set; }
            public string? CurrentCity { get; set; }
        }

        // POST: api/Auth/Login
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "請輸入帳號與密碼" });
            }

            var dbAccount = await _context.Accounts
                            .Include(a => a.Users)
                            .FirstOrDefaultAsync(x => x.Account1 == request.Email);

            if (dbAccount == null)
            {
                return Unauthorized(new { message = "帳號或密碼錯誤" });
            }

            // 驗證密碼
            bool isPasswordCorrect = false;
            try
            {
                // 若資料庫密碼為 null 或非 BCrypt 格式拋出例外
                isPasswordCorrect = !string.IsNullOrEmpty(dbAccount.Password) &&
                                    BCrypt.Net.BCrypt.Verify(request.Password, dbAccount.Password);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                isPasswordCorrect = false;
            }

            if (!isPasswordCorrect)
            {
                return Unauthorized(new { message = "帳號或密碼錯誤" });
            }

            // 檢查帳號狀態 (0停用, 1啟用)
            if (dbAccount.Status == 0)
            {
                return StatusCode(403, new { message = "此帳號已被停用，請聯繫管理員" });
            }

            // 取得真實 IP Address
            var ipAddress = Request.Headers.ContainsKey("X-Forwarded-For")
                            ? Request.Headers["X-Forwarded-For"].ToString()
                            : HttpContext.Connection.RemoteIpAddress?.ToString();

            // 更新 Account 資料表的最後登入時間與 IP
            dbAccount.LastLoginAt = DateTime.Now;
            dbAccount.LastLoginIp = ipAddress;

            // 更新 Info 資料表 (經緯度與城市)
            var user = dbAccount.Users.FirstOrDefault();
            if (user != null && request.Latitude.HasValue && request.Longitude.HasValue)
            {
                var info = await _context.Infos.FirstOrDefaultAsync(i => i.UserId == user.UserId);

                // 只有當交友檔案 (Info) "已經存在" 時才更新。
                if (info != null)
                {
                    info.Latitude = request.Latitude.Value;
                    info.Longitude = request.Longitude.Value;

                    if (!string.IsNullOrEmpty(request.CurrentCity))
                    {
                        info.CurrentCity = request.CurrentCity;
                    }
                }
            }

            await _context.SaveChangesAsync();

            // 寫入 JWT 的使用者資訊 (Claims)
            string roleName = dbAccount.Role == 1 ? "Admin" : "Member";
            string displayName = user?.Name ?? "會員";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, displayName),
                new Claim(ClaimTypes.NameIdentifier, dbAccount.AccountId.ToString()),
                new Claim(ClaimTypes.Role, roleName),
                new Claim(ClaimTypes.Email, dbAccount.Account1)
            };

            // JWT 憑證
            var token = GenerateJwtToken(claims, request.RememberMe);

            // 取得使用者的 Info
            var infoForPhoto = user != null
                ? await _context.Infos.FirstOrDefaultAsync(i => i.UserId == user.UserId)
                : null;
            string? photoUrl = infoForPhoto?.Photo;
            bool hasInfo = infoForPhoto != null;
            // 回傳狀態與 Token 給 Vue
            return Ok(new
            {
                message = "登入成功",
                token = token,
                user = new
                {
                    id = dbAccount.AccountId,
                    name = displayName,
                    role = roleName,
                    email = dbAccount.Account1,
                    photo = photoUrl,
                    hasInfo = hasInfo,

                }
            });
        }

        // Google 登入
        public class GoogleLoginDto
        {
            [Required(ErrorMessage = "缺少 Google 憑證")]
            public string Credential { get; set; } = string.Empty;
            public decimal? Latitude { get; set; }
            public decimal? Longitude { get; set; }
            public string? CurrentCity { get; set; }
        }


        [HttpPost("GoogleLogin")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto request)
        {
            try
            {
                // 驗證 Google 傳來的 Token
                var payload = await GoogleJsonWebSignature.ValidateAsync(request.Credential);

                var email = payload.Email;
                var name = payload.Name;

                // 檢查資料庫是否有這個 Email
                var dbAccount = await _context.Accounts
                                .Include(a => a.Users)
                                .FirstOrDefaultAsync(x => x.Account1 == email);

                // 如果找不到帳號，回傳 200 OK，但附帶一個 isRegistered = false 的旗標，讓前端去註冊！
                if (dbAccount == null)
                {
                    return Ok(new
                    {
                        isRegistered = false,
                        message = "尚未註冊",
                        email = email,
                        name = name
                    });
                }

                // 已註冊
                if (dbAccount.Status == 0) return StatusCode(403, new { message = "此帳號已被停用，請聯繫管理員" });

                // 記錄 IP 與經緯度
                var ipAddress = Request.Headers.ContainsKey("X-Forwarded-For")
                                ? Request.Headers["X-Forwarded-For"].ToString()
                                : HttpContext.Connection.RemoteIpAddress?.ToString();

                dbAccount.LastLoginAt = DateTime.Now;
                dbAccount.LastLoginIp = ipAddress;

                var user = dbAccount.Users.FirstOrDefault();
                if (user != null && request.Latitude.HasValue && request.Longitude.HasValue)
                {
                    var info = await _context.Infos.FirstOrDefaultAsync(i => i.UserId == user.UserId);
                    if (info != null)
                    {
                        info.Latitude = request.Latitude.Value;
                        info.Longitude = request.Longitude.Value;
                        if (!string.IsNullOrEmpty(request.CurrentCity)) info.CurrentCity = request.CurrentCity;
                    }
                }
                await _context.SaveChangesAsync();

                // 產生 JWT Token
                string roleName = dbAccount.Role == 1 ? "Admin" : "Member";
                string displayName = user?.Name ?? "會員";

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, displayName),
                    new Claim(ClaimTypes.NameIdentifier, dbAccount.AccountId.ToString()),
                    new Claim(ClaimTypes.Role, roleName),
                    new Claim(ClaimTypes.Email, dbAccount.Account1)
                };

                // Google 登入預設給 7 天免登入 (RememberMe = true)
                var token = GenerateJwtToken(claims, true);

                var infoForPhoto = user != null
                    ? await _context.Infos.FirstOrDefaultAsync(i => i.UserId == user.UserId)
                    : null;
                string? photoUrl = infoForPhoto?.Photo;
                bool hasInfo = infoForPhoto != null;
                // 回傳 isRegistered = true，並附上 Token
                return Ok(new
                {
                    isRegistered = true,
                    message = "Google 登入成功",
                    token = token,
                    user = new { 
                        id = dbAccount.AccountId, 
                        name = displayName, 
                        role = roleName, 
                        email = dbAccount.Account1, 
                        photo = photoUrl,
                        hasInfo = hasInfo
                    }
                });
            }
            catch (InvalidJwtException)
            {
                return BadRequest(new { message = "無效的 Google 驗證憑證" });
            }
        }

        private string GenerateJwtToken(IEnumerable<Claim> claims, bool rememberMe)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings.GetValue<string>("SecretKey");
            var issuer = jwtSettings.GetValue<string>("Issuer");
            var audience = jwtSettings.GetValue<string>("Audience");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 有勾選記住我就是 7 天，沒有就是 60 分鐘
            var expires = rememberMe ? DateTime.UtcNow.AddDays(7) : DateTime.UtcNow.AddMinutes(60);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto request)
        {
            // Email
            var account = await _context.Accounts
                .Include(a => a.Users)
                .FirstOrDefaultAsync(x => x.Account1 == request.Email);

            if (account == null)
            {
                return BadRequest(new { message = "找不到此 Email 帳號" });
            }

            // 重設 Token (TokenType = 2 代表忘記密碼)
            string tokenStr = Guid.NewGuid().ToString();

            var resetToken = new Token
            {
                AccountId = account.AccountId,
                Token1 = tokenStr,
                TokenType = 2,
                CreateAt = DateTime.Now,
                ExpiryAt = DateTime.Now.AddMinutes(30), // 30 分鐘有效
                UsedAt = null
            };

            _context.Tokens.Add(resetToken);
            await _context.SaveChangesAsync();

            // 重設連結 要連到 Vue 的 /reset-password 路由
            var resetUrl = $"http://localhost:5173/reset-password?token={tokenStr}";

            string displayName = account.Users.FirstOrDefault()?.Name ?? "會員";
            string subject = "Book仁 - 重設密碼通知";
            string body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h3 style='color: #0D47A1;'>重設您的密碼</h3>
                    <p>親愛的 {displayName} 您好：</p>
                    <p>我們收到了您重設密碼的請求。請點擊下方連結設定新密碼：</p>
                    <p><a href='{resetUrl}' style='display: inline-block; padding: 10px 20px; background-color: #FF8A65; color: white; text-decoration: none; border-radius: 5px; font-weight: bold;'>重設密碼</a></p>
                    <p style='margin-top: 20px; color: #888; font-size: 12px;'>此連結 30 分鐘內有效。</p>
                </div>";

            //await SendEmailAsync(request.Email, subject, body);
            _ = Task.Run(() => SendEmailAsync(request.Email, subject, body));
            return Ok(new { message = "驗證信已寄出" });
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpHost = _configuration["EmailSettings:Host"];
            var smtpPort = int.Parse(_configuration["EmailSettings:Port"] ?? "587");
            var smtpUser = _configuration["EmailSettings:User"];
            var smtpPass = _configuration["EmailSettings:Password"];

            using var client = new System.Net.Mail.SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Credentials = new System.Net.NetworkCredential(smtpUser, smtpPass)
            };

            var mailMessage = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress(smtpUser!, "Book仁 系統通知"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);
            await client.SendMailAsync(mailMessage);
        }

        // DTO 類別
        public class ForgotPasswordDto
        {
            [Required(ErrorMessage = "請輸入電子信箱")]
            [EmailAddress(ErrorMessage = "電子信箱格式錯誤")]
            public string Email { get; set; } = string.Empty;
        }
        // 重設密碼的 DTO
        public class ResetPasswordDto
        {
            [Required(ErrorMessage = "缺少 Token，無法重設密碼")]
            public string Token { get; set; } = string.Empty;
            [Required(ErrorMessage = "請輸入新密碼")]
            // 重設密碼也必須符合高強度規定
            [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,}$", ErrorMessage = "新密碼必須至少 8 個字元，且包含英文字母與數字")]
            public string NewPassword { get; set; } = string.Empty;
        }

        // POST: api/Auth/ResetPassword
        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
        {
            // 1. 驗證 Token 是否存在且為重設密碼類型 (2)
            var dbToken = await _context.Tokens
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.Token1 == request.Token && t.TokenType == 2);

            if (dbToken == null || dbToken.ExpiryAt < DateTime.Now || dbToken.UsedAt != null)
            {
                return BadRequest(new { message = "連結已失效或已過期，請重新申請" });
            }

            // 2. 更新密碼 (加密) 並標記 Token 已使用
            dbToken.Account.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            dbToken.UsedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new { message = "密碼重設成功，請使用新密碼登入" });
        }

        public class RegisterRequestDto
        {
            [Required(ErrorMessage = "請輸入電子信箱")]
            [EmailAddress(ErrorMessage = "電子信箱格式錯誤")]
            public string Email { get; set; } = string.Empty;
            [Required(ErrorMessage = "請輸入密碼")]
            // 至少8碼，包含英數字
            [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,}$", ErrorMessage = "密碼必須至少 8 個字元，且包含英文字母與數字")]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "請輸入姓名")]
            public string Name { get; set; } = string.Empty;
            [Required(ErrorMessage = "請輸入手機號碼")]
            [RegularExpression(@"^09\d{8}$", ErrorMessage = "手機號碼格式錯誤 (需為 09 開頭共 10 碼)")]
            public string Phone { get; set; } = string.Empty;
            public string Gender { get; set; } = string.Empty;
            public DateOnly? Birthday { get; set; }
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            // 1. 檢查 Email 是否已被註冊
            if (await _context.Accounts.AnyAsync(x => x.Account1 == request.Email))
            {
                return BadRequest(new { message = "此 Email 已被註冊" });
            }

            // 2. 準備 Token (不再需要先存檔拿到 AccountId)
            string tokenStr = Guid.NewGuid().ToString();

            // 3. 組合所有資料 (利用 EF Core 的關聯特性)
            var newAccount = new Account
            {
                Account1 = request.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = 2,
                Status = 0,
                IsEmailVerified = false,
                CreateAt = DateTime.Now,

                // 直接在這裡把 User 掛上去
                Users = new List<User>
                {
                    new User
                    {
                        Name = string.IsNullOrEmpty(request.Name) ? "新會員" : request.Name,
                        Phone = request.Phone,
                        Gender = request.Gender,
                        Birthdate = request.Birthday
                    }
                },

                // 直接在這裡把 Token 掛上去
                Tokens = new List<Token>
                {
                    new Token
                    {
                        Token1 = tokenStr,
                        TokenType = 1,
                        CreateAt = DateTime.Now,
                        ExpiryAt = DateTime.Now.AddDays(1)
                    }
                }
            };

            // 4. 全部加入 Context
            _context.Accounts.Add(newAccount);

            // 5. 只需要呼叫一次 SaveChangesAsync！EF Core 會自動搞定所有關聯的 ID
            await _context.SaveChangesAsync();

            // 6. 非同步寄送驗證信 (射後不理)
            var verifyUrl = $"http://localhost:5173/verify-email?token={tokenStr}";
            string subject = "Book仁交友平台 - 請啟用您的帳號";

            // 下方寄信邊睛不變...
            string body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2 style='color: #0D47A1;'>歡迎加入 Book仁！</h2>
                    <p>親愛的 {newAccount.Users.First().Name} 您好：</p>
                    <p>感謝您註冊。請點擊下方按鈕以驗證您的 Email 並啟用帳號：</p>
                    <p><a href='{verifyUrl}' style='display: inline-block; padding: 10px 20px; background-color: #FF8A65; color: white; text-decoration: none; border-radius: 5px; font-weight: bold;'>啟用帳號</a></p>
                    <p style='margin-top: 20px; color: #888; font-size: 12px;'>若連結無法點擊，請複製以下網址至瀏覽器開啟：<br>{verifyUrl}</p>
                </div>";

            _ = Task.Run(() => SendEmailAsync(request.Email, subject, body));

            return Ok(new { message = "註冊成功，驗證信已寄出" });
        }

        // POST: api/Auth/ResendVerifyEmail (重新發送驗證信)
        [HttpPost("ResendVerifyEmail")]
        public async Task<IActionResult> ResendVerifyEmail([FromBody] ForgotPasswordDto request)
        {
            var account = await _context.Accounts
                .Include(a => a.Users)
                .FirstOrDefaultAsync(x => x.Account1 == request.Email);

            if (account == null) return BadRequest(new { message = "找不到此帳號" });
            if (account.IsEmailVerified == true) return BadRequest(new { message = "此帳號已完成驗證，請直接登入" });

            string tokenStr = Guid.NewGuid().ToString();
            _context.Tokens.Add(new Token
            {
                AccountId = account.AccountId,
                Token1 = tokenStr,
                TokenType = 1,
                CreateAt = DateTime.Now,
                ExpiryAt = DateTime.Now.AddDays(1)
            });
            await _context.SaveChangesAsync();

            var verifyUrl = $"http://localhost:5173/verify-email?token={tokenStr}";
            string displayName = account.Users.FirstOrDefault()?.Name ?? "會員";
            string subject = "Book仁 - 重新發送驗證信";
            string body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2 style='color: #0D47A1;'>歡迎加入 Book仁！</h2>
                    <p>親愛的 {displayName} 您好：</p>
                    <p>這是一封重新發送的驗證信。請點擊下方按鈕以驗證您的 Email 並啟用帳號：</p>
                    <p><a href='{verifyUrl}' style='display: inline-block; padding: 10px 20px; background-color: #FF8A65; color: white; text-decoration: none; border-radius: 5px; font-weight: bold;'>啟用帳號</a></p>
                    <p style='margin-top: 20px; color: #888; font-size: 12px;'>若連結無法點擊，請複製以下網址至瀏覽器開啟：<br>{verifyUrl}</p>
                </div>";

            _ = Task.Run(() => SendEmailAsync(request.Email, subject, body));
            return Ok(new { message = "驗證信已重新發送" });
        }

        public class VerifyEmailDto
        {
            [Required(ErrorMessage = "缺少 Token，無法驗證")]
            public string Token { get; set; } = string.Empty;
        }

        [HttpPost("VerifyEmail")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto request)
        {
            var dbToken = await _context.Tokens
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.Token1 == request.Token && t.TokenType == 1);

            if (dbToken == null) return BadRequest(new { message = "驗證失敗：無效的連結" });
            if (dbToken.ExpiryAt < DateTime.Now) return BadRequest(new { message = "驗證失敗：連結已過期，請重新申請驗證信" });
            if (dbToken.UsedAt != null) return BadRequest(new { message = "此帳號已完成驗證，請直接登入" });

            dbToken.Account.IsEmailVerified = true;
            dbToken.Account.Status = 1; // 改回 1 (啟用)
            dbToken.UsedAt = DateTime.Now; // 標記 Token 已使用

            await _context.SaveChangesAsync();
            return Ok(new { message = "Email 驗證成功！帳號已啟用。" });
        }
    }
}

