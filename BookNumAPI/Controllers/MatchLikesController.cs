using BookNumAPI.Match.DTO;
using BookNumAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookNumAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MatchLikesController : ControllerBase
    {
        private readonly BookNumApiContext _context;

        public MatchLikesController(BookNumApiContext context)
        {
            _context = context;
        }

        // 我喜歡誰
        [HttpGet("ILike/{userId}")]
        public async Task<ActionResult<MatchLikesResultDto>> GetILike(int userId)
        {
            var cutoffDate = DateTime.Now.AddDays(-28);

            var likedUsers = await _context.Likeds
                .Where(l => l.LikerUserId == userId && l.IsLiked && l.CreateAt >= cutoffDate)
                // 排除已經在 Matched 表的
                .Where(l => !_context.Matcheds
                    .Any(m => (m.User1Id == l.LikerUserId && m.User2Id == l.LikedUserId) ||
                              (m.User1Id == l.LikedUserId && m.User2Id == l.LikerUserId)))
                // 排除對方已經按不喜歡
                .Where(l => !_context.Likeds
                    .Any(r => r.LikerUserId == l.LikedUserId &&
                              r.LikedUserId == l.LikerUserId &&
                              r.IsLiked == false))
                .Include(l => l.LikedUser).ThenInclude(u => u.Infos)
                .Include(l => l.LikedUser.HobbyLists).ThenInclude(hl => hl.Hobby)
                .ToListAsync();

            var result = likedUsers.Select(l => MapToDto(l.LikedUser, l.Score)).ToList();

            return Ok(new MatchLikesResultDto
            {
                Count = result.Count,
                Candidates = result
            });
        }


        // 誰喜歡我
        [HttpGet("WhoLikesMe/{userId}")]
        public async Task<ActionResult<MatchLikesResultDto>> GetWhoLikesMe(int userId)
        {
            var cutoffDate = DateTime.Now.AddDays(-28);

            var likerUsers = await _context.Likeds
                .Where(l => l.LikedUserId == userId && l.IsLiked && l.CreateAt >= cutoffDate)
                // 排除已經配對成功
                .Where(l => !_context.Matcheds
                    .Any(m => (m.User1Id == l.LikerUserId && m.User2Id == l.LikedUserId) ||
                              (m.User1Id == l.LikedUserId && m.User2Id == l.LikerUserId)))
                // 排除對方已經按不喜歡
                .Where(l => !_context.Likeds
                    .Any(r => r.LikerUserId == l.LikedUserId &&
                              r.LikedUserId == l.LikerUserId &&
                              r.IsLiked == false))
                .Include(l => l.LikerUser).ThenInclude(u => u.Infos)
                .Include(l => l.LikerUser.HobbyLists).ThenInclude(hl => hl.Hobby)
                .ToListAsync();

            var result = likerUsers.Select(l => MapToDto(l.LikerUser, l.Score)).ToList();

            return Ok(new MatchLikesResultDto
            {
                Count = result.Count,
                Candidates = result
                // 不再帶 IsPremium
            });
        }

        // 將 User 轉換成 MatchCandidateDto
        private MatchCandidateDto MapToDto(User user, double score)
        {
            var info = user.Infos.First();
            return new MatchCandidateDto
            {
                UserId = user.UserId,
                Age = user.Birthdate.HasValue
                    ? DateTime.Now.Year - user.Birthdate.Value.Year
                      - (DateTime.Now.DayOfYear < user.Birthdate.Value.DayOfYear ? 1 : 0)
                    : 0,
                Nickname = info.Nickname,
                Bio = info.Bio,
                CurrentCity = info.CurrentCity,
                Photo = info.Photo,
                Job = info.Job,
                Hobbies = user.HobbyLists
                    .Select(h => h.Hobby.HobbyName)
                    .ToList(),
                Probability = score
            };
        }
    }

    // 新增 DTO，讓 Swagger 文件顯示正確
    public class MatchLikesResultDto
    {
        public int Count { get; set; }
        public List<MatchCandidateDto> Candidates { get; set; }

    }
}