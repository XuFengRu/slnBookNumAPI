using BookNumAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace BookNumAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MemberController : ControllerBase
    {
        private readonly BookNumApiContext _context;

        public MemberController(BookNumApiContext context)
        {
            _context = context;
        }

        private int GetCurrentAccountId()
        {
            var accountIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(accountIdStr, out int id) ? id : 0;
        }

        // 取得個人資料 (包含系統紀錄)

        [HttpGet("Profile")]
        public async Task<IActionResult> GetProfile()
        {
            int accountId = GetCurrentAccountId();

            var account = await _context.Accounts
                .Include(a => a.Users)
                .FirstOrDefaultAsync(a => a.AccountId == accountId);

            if (account == null) return NotFound(new { message = "找不到帳號資訊" });

            var user = account.Users.FirstOrDefault();

            return Ok(new
            {
                email = account.Account1,
                name = user?.Name ?? "",
                phone = user?.Phone ?? "",
                gender = user?.Gender ?? "",
                birthday = user?.Birthdate?.ToString("yyyy-MM-dd") ?? "",
                lastLogin = account.LastLoginAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "無紀錄",
                lastIp = account.LastLoginIp ?? "無紀錄"
            });
        }

        // 更新個人資料
        public class UpdateProfileDto
        {
            public string Name { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string Gender { get; set; } = string.Empty;
            public DateOnly? Birthday { get; set; }
        }

        [HttpPut("Profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto request)
        {
            int accountId = GetCurrentAccountId();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.AccountId == accountId);

            if (user == null) return NotFound(new { message = "找不到會員資料" });

            user.Name = request.Name;
            user.Phone = request.Phone;
            user.Gender = request.Gender;
            user.Birthdate = request.Birthday;

            await _context.SaveChangesAsync();
            return Ok(new { message = "個人資料更新成功" });
        }

        // 停用 / 刪除帳號 (假刪除)
        [HttpPost("Deactivate")]
        public async Task<IActionResult> DeactivateAccount()
        {
            int accountId = GetCurrentAccountId();
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == accountId);

            if (account == null) return NotFound(new { message = "找不到帳號資訊" });

            // 🌟 實作「假刪除」：不管是按停用還是刪除，都只是把 Status 改成 0
            account.Status = 0;
            await _context.SaveChangesAsync();

            return Ok(new { message = "帳號已成功停用" });
        }

        // 修改密碼
        public class ChangePasswordDto
        {
            [Required(ErrorMessage = "請輸入目前密碼")]
            public string CurrentPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "請輸入新密碼")]
            [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,}$", ErrorMessage = "新密碼必須至少 8 個字元，且包含英文字母與數字")]
            public string NewPassword { get; set; } = string.Empty;
        }

        [HttpPut("Password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
        {
            int accountId = GetCurrentAccountId();
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == accountId);

            if (account == null) return NotFound(new { message = "找不到帳號資訊" });

            // 驗證「目前密碼」是否輸入正確
            bool isPasswordCorrect = false;
            try
            {
                isPasswordCorrect = !string.IsNullOrEmpty(account.Password) &&
                                    BCrypt.Net.BCrypt.Verify(request.CurrentPassword, account.Password);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                isPasswordCorrect = false;
            }

            if (!isPasswordCorrect)
            {
                return BadRequest(new { message = "目前密碼輸入錯誤，請重新確認" });
            }

            // hash update
            account.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "密碼已成功修改！下次請使用新密碼登入。" });
        }
    }
}