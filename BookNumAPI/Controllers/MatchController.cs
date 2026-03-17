using BookNumAPI.Match.DTO;
using BookNumAPI.Match.Events;
using BookNumAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookNumAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MatchController : ControllerBase
    {
        private readonly BookNumApiContext _context;
        private readonly IEventPublisher _eventPublisher; 
        public MatchController(BookNumApiContext context, IEventPublisher eventPublisher)
        {   _context = context;
            _eventPublisher = eventPublisher;
        }

        // Step 1: 查詢候選人清單
        [HttpGet("predict/{userId}")]
        public async Task<IActionResult> Predict(int userId)
        {
            var info = await _context.Infos
                .Include(i => i.User)
                .FirstOrDefaultAsync(i => i.UserId == userId);

            if (info == null) return NotFound("使用者不存在");

            var preferredCities = await _context.CityPreferences
                .Where(cp => cp.UserId == userId)
                .Select(cp => cp.City)
                .ToListAsync();

            var userHobbies = await _context.HobbyLists
                .Where(h => h.UserId == userId)
                .Select(h => h.HobbyId)
                .ToListAsync();

            var interactedUserIds = await _context.Likeds
                .Where(l => l.LikerUserId == userId)
                .Select(l => l.LikedUserId)
                .ToListAsync();

            var candidates = await _context.Users
                .Where(c => c.UserId != userId && !interactedUserIds.Contains(c.UserId))
                .ToListAsync();

            var results = new List<MatchCandidateDto>();

            foreach (var c in candidates)
            {
                var candidateInfo = await _context.Infos
                    .Include(i => i.User)
                    .FirstOrDefaultAsync(i => i.UserId == c.UserId);

                if (candidateInfo == null) continue;

                int candidateAge = CalculateAge(candidateInfo.User?.Birthdate);

                var candidateHobbies = await _context.HobbyLists
                    .Where(h => h.UserId == c.UserId)
                    .Include(h => h.Hobby)
                    .Select(h => h.Hobby.HobbyName)
                    .ToListAsync();

                int genderScore = 0;
                if (info.GenderPrefer.HasValue)
                {
                    if (info.GenderPrefer == false && c.Gender != "F") genderScore = 1;
                    if (info.GenderPrefer == true && c.Gender != "M") genderScore = 1;
                }

                int ageScore = 0;
                if (info.AgeMin.HasValue && candidateAge < info.AgeMin.Value) ageScore = 1;
                if (info.AgeMax.HasValue && candidateAge > info.AgeMax.Value) ageScore = 1;

                int cityScore = 0;
                if (preferredCities.Any() && !preferredCities.Contains(candidateInfo.CurrentCity)) cityScore = 1;

                double interestSim = 0.5;
                if (userHobbies.Any())
                {
                    var candidateHobbyIds = await _context.HobbyLists
                        .Where(h => h.UserId == c.UserId)
                        .Select(h => h.HobbyId)
                        .ToListAsync();

                    int intersection = userHobbies.Intersect(candidateHobbyIds).Count();
                    int union = userHobbies.Union(candidateHobbyIds).Count();
                    interestSim = union > 0 ? (double)intersection / union : 0.0;
                }

                double selfDescFeature = (candidateInfo?.Bio?.Length ?? 0) / 50.0;

                DateTime cutoff = DateTime.Now.AddDays(-28);

                bool hasAnyInteraction = await _context.Likeds
                    .AnyAsync(l => l.LikerUserId == c.UserId || l.LikedUserId == c.UserId);

                bool hasRecentInteraction = await _context.Likeds
                    .AnyAsync(l => l.LikerUserId == c.UserId && l.CreateAt >= cutoff);

                bool hasRecentChat = await _context.Chats
                    .AnyAsync(chat => (chat.SenderId == c.UserId || chat.ReceiverId == c.UserId) && chat.SendAt >= cutoff);

                int inactiveScore = hasAnyInteraction && (!hasRecentInteraction && !hasRecentChat) ? 1 : 0;

                double linear = (-0.5 * ageScore)
                                + (-0.3 * cityScore)
                                + (-1.0 * genderScore)
                                + (-0.5 * inactiveScore)
                                + (1.50 * interestSim)
                                + (0.50 * selfDescFeature);

                double probability = 1.0 / (1.0 + Math.Exp(-linear));
                probability = Math.Round(probability, 3);

                results.Add(new MatchCandidateDto
                {
                    UserId = c.UserId,
                    Age = candidateAge,
                    Nickname = candidateInfo.Nickname,
                    Bio = candidateInfo.Bio,
                    CurrentCity = candidateInfo.CurrentCity,
                    Photo = candidateInfo.Photo,
                    Job = candidateInfo.Job,
                    Hobbies = candidateHobbies,
                    Probability = probability
                });
            }

            var bestCandidate = results
                .OrderByDescending(r => r.Probability)
                .FirstOrDefault();

            return Ok(bestCandidate);
        }

        // Step 2: 喜歡 / 拒絕
        [HttpPost("interact")]
        public async Task<IActionResult> Interact([FromBody] LikedRequest request)
        {
            // 檢查是否有有效訂閱
            var now = DateTime.Now;
            bool hasPremium = await _context.Premia
                .AnyAsync(p => p.UserId == request.LikerUserId
                            && p.StartAt <= now
                            && p.EndAt >= now);

            if (!hasPremium)
            {
                // 檢查當天互動次數
                var today = DateTime.Today;
                int todayInteractions = await _context.Likeds
                    .CountAsync(l => l.LikerUserId == request.LikerUserId
                                  && l.CreateAt.Date == today);

                if (todayInteractions >= 5)
                {
                    return BadRequest(new { success = false, message = "今日互動次數已達上限 (5 次)，訂閱會員可享不限次數！" });
                }
            }

            // 檢查是否已經有互動紀錄
            var existingInteraction = await _context.Likeds
                .FirstOrDefaultAsync(l => l.LikerUserId == request.LikerUserId &&
                                          l.LikedUserId == request.LikedUserId);

            if (existingInteraction != null)
            {
                return BadRequest(new { success = false, message = "已經按過喜歡或拒絕，不能重複操作" });
            }

            var liked = new Liked
            {
                LikerUserId = request.LikerUserId,
                LikedUserId = request.LikedUserId,
                IsLiked = request.IsLiked,
                CreateAt = DateTime.Now,
                Score = request.Score
            };

            _context.Likeds.Add(liked);
            await _context.SaveChangesAsync();

            // 配對邏輯
            if (request.IsLiked)
            {
                var reciprocal = await _context.Likeds
                    .FirstOrDefaultAsync(l => l.LikerUserId == request.LikedUserId &&
                                              l.LikedUserId == request.LikerUserId &&
                                              l.IsLiked == true);

                if (reciprocal != null)
                {
                    var matched = new Matched
                    {
                        User1Id = request.LikerUserId,
                        User2Id = request.LikedUserId,
                        CreateAt = DateTime.Now,
                        Score = (request.Score + reciprocal.Score) / 2.0
                    };

                    _context.Matcheds.Add(matched);
                    await _context.SaveChangesAsync();

                    var matchEvent = new MatchChatRoomCreatedEvent
                    {
                        RoomId = matched.MatchedId,
                        User1Id = matched.User1Id,
                        User2Id = matched.User2Id
                    };

                    await _eventPublisher.Publish(matchEvent);

                    // ✅ 回傳 isMatchSuccess = true
                    return Ok(new { success = true, likedId = liked.LikedId, isMatchSuccess = true });
                }
            }

            // 如果沒有 reciprocal，回傳 isMatchSuccess = false
            return Ok(new { success = true, likedId = liked.LikedId, isMatchSuccess = false });
        }


        private static int CalculateAge(DateOnly? birthdate)
        {
            if (!birthdate.HasValue) return 0;

            var today = DateTime.Now.Year;
            var bday = birthdate.Value.Year;

            return today - bday
                - (DateTime.Now.DayOfYear < birthdate.Value.DayOfYear ? 1 : 0);
        }
    }
}

