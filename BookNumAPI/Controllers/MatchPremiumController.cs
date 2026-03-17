using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookNumAPI.Models;
using BookNumAPI.Match.DTO;

namespace BookNumAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MatchPremiumController : ControllerBase
    {
        private readonly BookNumApiContext _context;

        public MatchPremiumController(BookNumApiContext context)
        {
            _context = context;
        }

        [HttpPost("cancel")]
        public async Task<ActionResult> Cancel([FromBody] int userId)
        {
            var lastPremium = await _context.Premia
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.EndAt)
                .FirstOrDefaultAsync();

            if (lastPremium == null) return NotFound("沒有找到訂閱紀錄");

            lastPremium.AutoRenew = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = "已取消續訂", lastPremium.PremiumId });
        }

        [HttpGet("check/{userId}")]
        public async Task<ActionResult> CheckPremium(int userId)
        {
            var now = DateTime.UtcNow;

            var lastPremium = await _context.Premia
                .Include(p => p.Method)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.EndAt)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                PremiumId = lastPremium?.PremiumId,
                EndAt = lastPremium?.EndAt,
            });
        }

        [HttpGet("isActive/{userId}")]
        public async Task<ActionResult<bool>> IsActive(int userId)
        {
            var now = DateTime.UtcNow;
            var lastPremium = await _context.Premia
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.EndAt)
                .FirstOrDefaultAsync();

            return Ok(lastPremium != null && lastPremium.EndAt > now);
        }
    }
    }