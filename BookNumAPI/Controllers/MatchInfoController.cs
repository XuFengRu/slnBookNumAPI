using BookNumAPI.Match.DTO;
using BookNumAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenCvSharp;

namespace BookNumAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MatchInfoController : ControllerBase
    {
        private readonly BookNumApiContext _context;

        public MatchInfoController(BookNumApiContext context)
        {
            _context = context;
        }

        // 取得交友資料
        [HttpGet("{userId}")]
        public async Task<ActionResult<MatchInfoDto?>> GetMatchInfo(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Infos)
                .Include(u => u.HobbyLists).ThenInclude(h => h.Hobby)
                .Include(u => u.CityPreferences)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return NotFound("使用者不存在");

            var info = user.Infos.FirstOrDefault();
            if (info == null)
            {
                // 使用者存在但沒有交友資料 → 回傳 null
                return Ok(null);
            }

            var matchInfo = new MatchInfoDto
            {
                UserId = user.UserId,
                Nickname = info.Nickname,
                Age = user.Birthdate.HasValue
                    ? DateTime.Now.Year - user.Birthdate.Value.Year
                      - (DateTime.Now.DayOfYear < user.Birthdate.Value.DayOfYear ? 1 : 0)
                    : 0,
                Bio = info.Bio,
                CurrentCity = info.CurrentCity,
                Photo = info.Photo,
                Job = info.Job,
                Hobbies = user.HobbyLists
                    .Select(h => new MatchHobbyDto
                    {
                        HobbyId = h.Hobby.HobbyId,
                        HobbyName = h.Hobby.HobbyName
                    }).ToList(),
                Preferences = new PreferenceDto
                {
                    Gender = info.GenderPrefer.HasValue
                        ? (info.GenderPrefer.Value ? "男性" : "女性") : "不限",
                    AgeMin = info.AgeMin,
                    AgeMax = info.AgeMax,
                    Cities = user.CityPreferences.Select(cp => cp.City).ToList()
                },
                Stats = new StatsDto
                {
                    Matches = _context.Matcheds.Count(m => m.User1Id == userId || m.User2Id == userId),
                    Likes = _context.Likeds.Count(l => l.LikedUserId == userId && l.IsLiked)
                }
            };

            return Ok(matchInfo);
        }

        // 新增交友資料 (第一次填寫)
        [HttpPost("{userId}")]
        public async Task<IActionResult> CreateMatchInfo(int userId, [FromBody] MatchUpdateInfoDto dto)
        {
            var user = await _context.Users
                .Include(u => u.Infos)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return NotFound("使用者不存在");
            if (user.Infos.Any()) return BadRequest("已經有交友資料，請用 PUT 更新");

            var info = new Info
            {
                UserId = userId,
                Nickname = dto.Nickname,
                Bio = dto.Bio,
                CurrentCity = dto.CurrentCity,
                Photo = dto.Photo,
                Job = dto.Job,
                GenderPrefer = dto.GenderPrefer == null ? (bool?)null : dto.GenderPrefer == 1,
                AgeMin = dto.AgeMin,
                AgeMax = dto.AgeMax
            };

            user.Infos.Add(info);

            // 興趣
            if (dto.Hobbies != null && dto.Hobbies.Any())
            {
                user.HobbyLists = dto.Hobbies
                    .Select(h => new HobbyList { UserId = userId, HobbyId = h.HobbyId })
                    .ToList();
            }

            // 城市偏好
            if (dto.Cities != null && dto.Cities.Any())
            {
                user.CityPreferences = dto.Cities
                    .Select(c => new CityPreference { UserId = userId, City = c })
                    .ToList();
            }

            await _context.SaveChangesAsync();
            return Ok("交友資料已建立");
        }

        // 編輯交友資料
        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateMatchInfo(int userId, [FromBody] MatchUpdateInfoDto dto)
        {
            var user = await _context.Users
                .Include(u => u.Infos)
                .Include(u => u.HobbyLists)
                .Include(u => u.CityPreferences)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return NotFound("使用者不存在");

            var info = user.Infos.FirstOrDefault();
            if (info == null) return NotFound("使用者資訊不存在");

            // 更新基本資料
            info.Nickname = dto.Nickname;
            info.Bio = dto.Bio;
            info.CurrentCity = dto.CurrentCity;
            info.Photo = dto.Photo;
            info.Job = dto.Job;

            // 更新興趣 (刪除舊的再新增)
            _context.HobbyLists.RemoveRange(user.HobbyLists);
            if (dto.Hobbies != null && dto.Hobbies.Any())
            {
                user.HobbyLists = dto.Hobbies
                    .Select(h => new HobbyList { UserId = userId, HobbyId = h.HobbyId })
                    .ToList();
            }

            // 更新偏好設定
            info.GenderPrefer = dto.GenderPrefer == null ? (bool?)null : dto.GenderPrefer == 1;
            info.AgeMin = dto.AgeMin;
            info.AgeMax = dto.AgeMax;

            // 更新城市偏好 (刪除舊的再新增)
            _context.CityPreferences.RemoveRange(user.CityPreferences);
            if (dto.Cities != null && dto.Cities.Any())
            {
                user.CityPreferences = dto.Cities
                    .Select(c => new CityPreference { UserId = userId, City = c })
                    .ToList();
            }

            await _context.SaveChangesAsync();
            return Ok("個人資料已更新");
        }

        // 取得所有興趣清單
        [HttpGet("hobbies")]
        public async Task<ActionResult<List<MatchHobbyDto>>> GetAllHobbies()
        {
            var hobbies = await _context.Hobbies
                .Where(h => h.IsActived == true || h.IsActived == null)
                .Select(h => new MatchHobbyDto
                {
                    HobbyId = h.HobbyId,
                    HobbyName = h.HobbyName
                })
                .ToListAsync();

            return Ok(hobbies);
        }



        [HttpPost("upload-photo")]
        public async Task<IActionResult> UploadPhoto(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("沒有檔案");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            //  使用 Azure Face API 偵測人臉
            var endpoint = "https://feng-facedetect.cognitiveservices.azure.com/";
            var subscriptionKey = ""; // 建議放在設定檔或環境變數

            var client = new HttpClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Ocp-Apim-Subscription-Key", subscriptionKey);

            // Face API detect，不加任何額外參數
            var requestUrl = new Uri(new Uri(endpoint), "face/v1.0/detect");

            byte[] imageBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            using var content = new ByteArrayContent(imageBytes);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            var response = await client.PostAsync(requestUrl, content);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                System.IO.File.Delete(filePath);
                return BadRequest($"偵測失敗: {json}");
            }

            //  只判斷是否有臉
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind != System.Text.Json.JsonValueKind.Array || root.GetArrayLength() == 0)
            {
                System.IO.File.Delete(filePath);
                return BadRequest("照片中未偵測到清晰人臉，請重新上傳");
            }

            // 有人臉 → 回傳完整 URL
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var fullUrl = $"{baseUrl}/uploads/{fileName}";

            return Ok(new { path = fullUrl });
        }
    }
}