using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookNumAPI.Models;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Json;

namespace BookNumAPI.Controllers
{
    [Route("api/activities")]
    [ApiController]
    public class ActivityController : ControllerBase
    {
        private readonly BookNumApiContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpClientFactory _httpClientFactory;

        public ActivityController(
            BookNumApiContext context,
            IWebHostEnvironment env,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _env = env;
            _httpClientFactory = httpClientFactory;
        }

        // ===========================
        // DTO：新增活動
        // ===========================
        public class ActivityCreateDto
        {
            public int UserId { get; set; }
            public int CategoryId { get; set; }
            public string Title { get; set; } = "";
            public string Description { get; set; } = "";
            public DateTime EventDate { get; set; }
            public DateTime StartAt { get; set; }
            public DateTime EndAt { get; set; }
            public int MaxPeople { get; set; }
            public string Location { get; set; } = "";
            public int Status { get; set; } = 1;
            public IFormFile? Image { get; set; }
        }

        // ===========================
        // DTO：更新活動
        // ===========================
        public class ActivityUpdateDto
        {
            public int CategoryId { get; set; }
            public string Title { get; set; } = "";
            public string Description { get; set; } = "";
            public DateTime EventDate { get; set; }
            public DateTime StartAt { get; set; }
            public DateTime EndAt { get; set; }
            public int MaxPeople { get; set; }
            public string Location { get; set; } = "";
            public int Status { get; set; } = 1;
        }

        private string? MakeImageUrl(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            var p = path.TrimStart('/');
            return $"{Request.Scheme}://{Request.Host}/{p}";
        }

        // ===========================
        // 內部呼叫同步 Google
        // ===========================
        private async Task<(bool ok, string? error, object? payload)> TrySyncGoogleAsync(int activityId, int userId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var url = $"{baseUrl}/api/activities/{activityId}/sync-google?userId={userId}";

                using var resp = await client.PostAsJsonAsync(url, new { });

                if (!resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    return (false, $"Sync Google 失敗：HTTP {(int)resp.StatusCode} - {body}", null);
                }

                var json = await resp.Content.ReadFromJsonAsync<object>();
                return (true, null, json);
            }
            catch (Exception ex)
            {
                return (false, $"Sync Google 例外：{ex.Message}", null);
            }
        }

        // ===========================
        // 取得全部活動（原本 /api/activities）
        // ✅ 追加 joinedCount（真實報名人數）
        // ===========================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var raw = await _context.Activities
                .Include(a => a.Category)
                .OrderByDescending(a => a.CreateAt)
                .ToListAsync();

            // 一次抓出每個 activity 的報名人數（避免 N+1）
            var ids = raw.Select(x => x.ActivityId).ToList();
            var joinedDict = await _context.Participants
                .Where(p => ids.Contains(p.ActivityId))
                .GroupBy(p => p.ActivityId)
                .Select(g => new { ActivityId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ActivityId, x => x.Count);

            var list = raw.Select(a => new
            {
                a.ActivityId,
                a.UserId,
                a.CategoryId,
                CategoryName = a.Category.CategoryName,
                a.Title,
                a.Description,
                a.EventDate,
                a.StartAt,
                a.EndAt,
                a.MaxPeople,
                a.Location,
                a.Status,
                a.CreateAt,
                a.Image,
                ImageUrl = MakeImageUrl(a.Image),

                // ✅ 新增：真實報名人數（列表頁用得到）
                joinedCount = joinedDict.TryGetValue(a.ActivityId, out var c) ? c : 0,
                remaining = Math.Max(0, a.MaxPeople - (joinedDict.TryGetValue(a.ActivityId, out var c2) ? c2 : 0))
            });

            return Ok(list);
        }

        // ===========================
        // 取得單筆活動
        // GET /api/activities/{id}?userId=83 (可選)
        // ✅ joinedCount / isJoinedByMe / remaining
        // ===========================
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, [FromQuery] int? userId = null)
        {
            var a = await _context.Activities
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.ActivityId == id);

            if (a == null)
                return NotFound(new { message = "找不到活動" });

            var joinedCount = await _context.Participants.CountAsync(p => p.ActivityId == id);

            bool isJoinedByMe = false;
            if (userId.HasValue && userId.Value > 0)
            {
                isJoinedByMe = await _context.Participants
                    .AnyAsync(p => p.ActivityId == id && p.UserId == userId.Value);
            }

            return Ok(new
            {
                a.ActivityId,
                a.UserId, // 這個就是 ownerId（你前端會拿來判斷自己發布）
                a.CategoryId,
                CategoryName = a.Category.CategoryName,
                a.Title,
                a.Description,
                a.EventDate,
                a.StartAt,
                a.EndAt,
                a.MaxPeople,
                a.Location,
                a.Status,
                a.CreateAt,
                a.Image,
                ImageUrl = MakeImageUrl(a.Image),

                joinedCount,
                remaining = Math.Max(0, a.MaxPeople - joinedCount),
                isJoinedByMe
            });
        }

        // ===========================
        // 新增活動（FromForm + 檔案上傳）
        // ===========================
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] ActivityCreateDto dto, [FromQuery] bool syncGoogle = false)
        {
            var categoryExists = await _context.ActivityCategories
                .AnyAsync(c => c.CategoryId == dto.CategoryId);

            if (!categoryExists)
                return BadRequest(new { message = "分類不存在" });

            string? imagePath = null;

            if (dto.Image != null && dto.Image.Length > 0)
            {
                if (string.IsNullOrWhiteSpace(_env.WebRootPath))
                    return StatusCode(500, new { message = "WebRootPath 未設定，請確認專案有 wwwroot 且已啟用靜態檔案。" });

                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsDir);

                var ext = Path.GetExtension(dto.Image.FileName);
                var fileName = $"{Guid.NewGuid()}{ext}";
                var savePath = Path.Combine(uploadsDir, fileName);

                using var stream = System.IO.File.Create(savePath);
                await dto.Image.CopyToAsync(stream);

                imagePath = $"uploads/{fileName}";
            }

            var activity = new Activity
            {
                UserId = dto.UserId,
                CategoryId = dto.CategoryId,
                Title = dto.Title ?? "",
                Description = dto.Description ?? "",
                EventDate = dto.EventDate,
                StartAt = dto.StartAt,
                EndAt = dto.EndAt,
                MaxPeople = dto.MaxPeople,
                Location = dto.Location ?? "",
                Status = dto.Status,
                CreateAt = DateTime.Now,
                Image = imagePath
            };

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            object? googleSync = null;
            if (syncGoogle)
            {
                var (ok, error, payload) = await TrySyncGoogleAsync(activity.ActivityId, activity.UserId);
                if (!ok) return StatusCode(500, new { message = error });
                googleSync = payload;
            }

            var a = await _context.Activities
                .Include(x => x.Category)
                .FirstAsync(x => x.ActivityId == activity.ActivityId);

            return CreatedAtAction(nameof(GetById), new { id = a.ActivityId }, new
            {
                a.ActivityId,
                a.UserId,
                a.CategoryId,
                CategoryName = a.Category.CategoryName,
                a.Title,
                a.Description,
                a.EventDate,
                a.StartAt,
                a.EndAt,
                a.MaxPeople,
                a.Location,
                a.Status,
                a.CreateAt,
                a.Image,
                ImageUrl = MakeImageUrl(a.Image),
                googleSync
            });
        }

        // ===========================
        // 修改活動
        // ===========================
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ActivityUpdateDto dto, [FromQuery] bool syncGoogle = false)
        {
            var activity = await _context.Activities.FirstOrDefaultAsync(a => a.ActivityId == id);
            if (activity == null)
                return NotFound(new { message = "找不到活動" });

            var categoryExists = await _context.ActivityCategories
                .AnyAsync(c => c.CategoryId == dto.CategoryId);

            if (!categoryExists)
                return BadRequest(new { message = "分類不存在" });

            activity.CategoryId = dto.CategoryId;
            activity.Title = dto.Title ?? "";
            activity.Description = dto.Description ?? "";
            activity.EventDate = dto.EventDate;
            activity.StartAt = dto.StartAt;
            activity.EndAt = dto.EndAt;
            activity.MaxPeople = dto.MaxPeople;
            activity.Location = dto.Location ?? "";
            activity.Status = dto.Status;

            await _context.SaveChangesAsync();

            object? googleSync = null;
            if (syncGoogle)
            {
                var (ok, error, payload) = await TrySyncGoogleAsync(activity.ActivityId, activity.UserId);
                if (!ok) return StatusCode(500, new { message = error });
                googleSync = payload;
            }

            return Ok(new
            {
                success = true,
                activityId = activity.ActivityId,
                googleSync
            });
        }

        // ===========================
        // 刪除活動
        // ===========================
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var activity = await _context.Activities.FindAsync(id);

            if (activity == null)
                return NotFound(new { message = "找不到活動" });

            _context.Activities.Remove(activity);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ===========================
        // ✅ 報名活動
        // POST /api/activities/{activityId}/join?userId=1
        // ✅ 新增：不能報名自己發布的活動
        // ===========================
        [HttpPost("{activityId:int}/join")]
        public async Task<IActionResult> JoinActivity(int activityId, [FromQuery] int userId)
        {
            // 0) userId 基本檢查
            if (userId <= 0)
                return BadRequest(new { message = "userId 不正確" });

            // 1) 先確認 User 存在（避免 FK 爆炸）
            var userExists = await _context.Users.AnyAsync(u => u.UserId == userId);
            if (!userExists)
                return BadRequest(new { message = "使用者不存在，請先建立使用者或確認 userId" });

            // 2) 活動存在
            var activity = await _context.Activities.FindAsync(activityId);
            if (activity == null)
                return NotFound(new { message = "找不到活動" });

            // ✅ 3) 不能報名自己發布的活動（重點）
            if (activity.UserId == userId)
                return BadRequest(new { message = "不能報名自己發布的活動" });

            // 4) 是否已報名
            var alreadyJoined = await _context.Participants
                .AnyAsync(p => p.ActivityId == activityId && p.UserId == userId);

            if (alreadyJoined)
                return BadRequest(new { message = "已經報名過" });

            // 5) 檢查是否額滿（用 Participants 真實人數）
            var joinedCount = await _context.Participants.CountAsync(p => p.ActivityId == activityId);
            if (joinedCount >= activity.MaxPeople)
                return Conflict(new { message = "活動已額滿" });

            // 6) 新增 Participant
            var participant = new Participant
            {
                ActivityId = activityId,
                UserId = userId,
                JoinStatus = 1,
                CreateAt = DateTime.Now
            };

            _context.Participants.Add(participant);
            await _context.SaveChangesAsync();

            // 7) 自動同步 Google（用報名者 userId）
            var (ok, error, payload) = await TrySyncGoogleAsync(activityId, userId);
            if (!ok)
            {
                return Ok(new
                {
                    success = true,
                    googleSynced = false,
                    googleError = error
                });
            }

            return Ok(new
            {
                success = true,
                googleSynced = true,
                google = payload
            });
        }

        // ===========================
        // ✅ 取消報名
        // DELETE /api/activities/{activityId}/join?userId=1
        // ===========================
        [HttpDelete("{activityId:int}/join")]
        public async Task<IActionResult> CancelJoin(int activityId, [FromQuery] int userId)
        {
            if (userId <= 0)
                return BadRequest(new { message = "userId 不正確" });

            var participant = await _context.Participants
                .FirstOrDefaultAsync(p => p.ActivityId == activityId && p.UserId == userId);

            if (participant == null)
            {
                // 冪等：沒報名也回成功
                return Ok(new { success = true, removed = false });
            }

            _context.Participants.Remove(participant);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, removed = true });
        }

        // ===========================
        // 活動列表頁專用 API（/api/activities/list）
        // ✅ 追加 joinedCount / remaining
        // ===========================
        [HttpGet("list")]
        public async Task<IActionResult> GetList(
            int page = 1,
            int pageSize = 10,
            int? categoryId = null,
            string? keyword = null,
            bool upcoming = true
        )
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            var query = _context.Activities.Include(a => a.Category).AsQueryable();

            if (upcoming)
            {
                var today = DateTime.Today;
                query = query.Where(a => a.EventDate >= today);
            }

            if (categoryId.HasValue)
                query = query.Where(a => a.CategoryId == categoryId.Value);

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(a => a.Title.Contains(keyword) || a.Description.Contains(keyword));

            query = query.OrderBy(a => a.EventDate);

            var total = await query.CountAsync();

            var raw = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var ids = raw.Select(x => x.ActivityId).ToList();
            var joinedDict = await _context.Participants
                .Where(p => ids.Contains(p.ActivityId))
                .GroupBy(p => p.ActivityId)
                .Select(g => new { ActivityId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ActivityId, x => x.Count);

            var items = raw.Select(a => new
            {
                a.ActivityId,
                a.UserId,
                a.Title,
                a.Description,
                a.EventDate,
                a.StartAt,
                a.EndAt,
                a.Location,
                a.MaxPeople,
                a.Status,
                a.CategoryId,
                CategoryName = a.Category.CategoryName,
                a.Image,
                ImageUrl = MakeImageUrl(a.Image),

                joinedCount = joinedDict.TryGetValue(a.ActivityId, out var c) ? c : 0,
                remaining = Math.Max(0, a.MaxPeople - (joinedDict.TryGetValue(a.ActivityId, out var c2) ? c2 : 0))
            });

            return Ok(new
            {
                page,
                pageSize,
                total,
                totalPages = (int)Math.Ceiling(total / (double)pageSize),
                items
            });
        }
    }
}