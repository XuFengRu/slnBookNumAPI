using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BookNumAPI.Models;

namespace BookNumAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MatchPayPalController : ControllerBase
    {
        private readonly BookNumApiContext _context;

        // ✅ 你的金鑰與密碼
        private readonly string clientId = "";
        private readonly string clientSecret = "";

        public MatchPayPalController(BookNumApiContext context)
        {
            _context = context;
        }

        private async Task<string> GetAccessToken()
        {
            using var client = new HttpClient();
            var byteArray = Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            var body = new Dictionary<string, string> { { "grant_type", "client_credentials" } };
            var response = await client.PostAsync("https://api-m.sandbox.paypal.com/v1/oauth2/token", new FormUrlEncodedContent(body));
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<JsonElement>(json).GetProperty("access_token").GetString()!;
        }

        [HttpPost("create-product")]
        public async Task<ActionResult> CreateProduct()
        {
            var token = await GetAccessToken();
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var product = new
            {
                name = "BookPremium",
                description = "交友平台付費會員",
                type = "SERVICE",
                category = "SOFTWARE"
            };

            var response = await client.PostAsync("https://api-m.sandbox.paypal.com/v1/catalogs/products",
                new StringContent(JsonSerializer.Serialize(product), Encoding.UTF8, "application/json"));

            var json = await response.Content.ReadAsStringAsync();
            var productId = JsonSerializer.Deserialize<JsonElement>(json).GetProperty("id").GetString();
            return Ok(new { productId });
        }

        [HttpPost("create-plans")]
        public async Task<ActionResult> CreatePlans([FromBody] string productId)
        {
            var token = await GetAccessToken();
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var methods = await _context.Methods.ToListAsync();
            var results = new List<object>();

            foreach (var m in methods)
            {
                // ✅ 改成只支援 1 天、7 天、30 天
                string unit;
                int count;

                if (m.DurationDay == 1)
                {
                    unit = "DAY";
                    count = 1;
                }
                else if (m.DurationDay == 7)
                {
                    unit = "WEEK";
                    count = 1;
                }
                else if (m.DurationDay == 30)
                {
                    unit = "MONTH";
                    count = 1;
                }
                else
                {
                    // 如果資料庫有其他天數，直接丟錯避免建立不合規的 Plan
                    return BadRequest($"不支援的 DurationDay: {m.DurationDay}");
                }

                var plan = new
                {
                    product_id = productId,
                    name = m.MethodName,
                    description = $"{m.MethodName} - NT${m.Price}",
                    billing_cycles = new[]
                    {
                new {
                    frequency = new { interval_unit = unit, interval_count = count },
                    tenure_type = "REGULAR",
                    sequence = 1,
                    total_cycles = 0,
                    pricing_scheme = new {
                        fixed_price = new { value = m.Price.ToString(), currency_code = "TWD" }
                    }
                }
            },
                    payment_preferences = new
                    {
                        auto_bill_outstanding = true,
                        setup_fee_failure_action = "CONTINUE",
                        payment_failure_threshold = 1
                    }
                };

                var response = await client.PostAsync("https://api-m.sandbox.paypal.com/v1/billing/plans",
                    new StringContent(JsonSerializer.Serialize(plan), Encoding.UTF8, "application/json"));

                var json = await response.Content.ReadAsStringAsync();
                var planId = JsonSerializer.Deserialize<JsonElement>(json).GetProperty("id").GetString();

                m.PayPalId = planId;
                _context.Methods.Update(m);
                await _context.SaveChangesAsync();

                results.Add(new
                {
                    m.MethodId,
                    m.MethodName,
                    planId,
                    m.Price,
                    m.DurationDay
                });
            }

            return Ok(results);
        }

        [HttpPost("create-subscription")]
        public async Task<ActionResult> CreateSubscription([FromBody] CreateSubscriptionDto dto)
        {
            var method = await _context.Methods.FindAsync(dto.MethodId);
            if (method == null || string.IsNullOrEmpty(method.PayPalId))
                return NotFound("方案不存在或尚未建立 PayPal Plan");

            var token = await GetAccessToken();
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var subscription = new
            {
                plan_id = method.PayPalId,
                application_context = new
                {
                    brand_name = "BookPremium",
                    locale = "zh-TW",
                    user_action = "SUBSCRIBE_NOW",
                    return_url = "http://localhost:5173/member/dating/bookpremium?status=success",
                    cancel_url = "http://localhost:5173/member/dating/bookpremium?status=cancel"
                },
                custom_id = $"{dto.UserId}:{dto.MethodId}"
            };

            var response = await client.PostAsync("https://api-m.sandbox.paypal.com/v1/billing/subscriptions",
                new StringContent(JsonSerializer.Serialize(subscription), Encoding.UTF8, "application/json"));

            var json = await response.Content.ReadAsStringAsync();
            var links = JsonSerializer.Deserialize<JsonElement>(json).GetProperty("links");
            var approveLink = links.EnumerateArray().First(l => l.GetProperty("rel").GetString() == "approve").GetProperty("href").GetString();

            return Ok(new { approveLink });
        }

        public class CreateSubscriptionDto
        {
            public int MethodId { get; set; }
            public int UserId { get; set; }
        }

        [HttpPost("confirm-subscription")]
        public async Task<ActionResult> ConfirmSubscription([FromBody] string subscriptionId)
        {
            var token = await GetAccessToken();
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"https://api-m.sandbox.paypal.com/v1/billing/subscriptions/{subscriptionId}");
            var json = await response.Content.ReadAsStringAsync();
            var detail = JsonSerializer.Deserialize<JsonElement>(json);

            var status = detail.GetProperty("status").GetString();
            var customId = detail.TryGetProperty("custom_id", out var customProp) ? customProp.GetString() : null;

            if (status == "ACTIVE" && !string.IsNullOrEmpty(customId))
            {
                // ✅ 先檢查是否已存在
                var existingPremium = await _context.Premia.FirstOrDefaultAsync(p => p.SubscriptionId == subscriptionId);
                if (existingPremium != null)
                {
                    return Ok(new { success = true, message = "訂閱已存在", expiryTime = existingPremium.EndAt });
                }

                var parts = customId.Split(':');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out var userId) &&
                    int.TryParse(parts[1], out var methodId))
                {
                    var method = await _context.Methods.FindAsync(methodId);
                    if (method != null)
                    {
                        var taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
                        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, taiwanTimeZone);

                        var lastPremium = await _context.Premia
                            .Where(p => p.UserId == userId && p.EndAt > now)
                            .OrderByDescending(p => p.EndAt)
                            .FirstOrDefaultAsync();

                        DateTime startAt = lastPremium?.EndAt ?? now;

                        var premium = new Premium
                        {
                            UserId = userId,
                            MethodId = method.MethodId,
                            Price = method.Price,
                            StartAt = startAt,
                            EndAt = startAt.AddDays(method.DurationDay),
                            AutoRenew = true,
                            SubscriptionId = subscriptionId
                        };

                        _context.Premia.Add(premium);
                        await _context.SaveChangesAsync();

                        return Ok(new { success = true, message = "Premium 已建立", expiryTime = premium.EndAt });
                    }
                }
            }

            return BadRequest(new { success = false, message = $"訂閱狀態={status}，尚未啟用" });
        
    }
    }
}
