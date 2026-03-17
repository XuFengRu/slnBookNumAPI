using Microsoft.AspNetCore.Mvc;
using BookNumAPI.Services;
using BookNumAPI.Models;
using System.Text.Json;

namespace BookNumAPI.Controllers
{
    [ApiController]
    [Route("Payment")]
    public class PaymentController : ControllerBase
    {
        private readonly NewebPayService _newebPayService;
        private readonly BookNumApiContext _db;

        public PaymentController(BookNumApiContext db)
        {
            _newebPayService = new NewebPayService();
            _db = db;
        }

        [HttpPost("CreateOrder")]
        public IActionResult CreateOrder([FromBody] CreateOrderRequest request)
        {
            if (request == null || request.Amount <= 0 || string.IsNullOrEmpty(request.OrderNo))
            {
                return BadRequest("Invalid order data");
            }

            try
            {
                // 建立訂單並存入資料庫
                var order = new Order
                {
                    OrderNo = request.OrderNo,
                    UserId = 6,
                    RenterId = 6,
                    ServiceId = 11,
                    AppointmentDate = DateOnly.FromDateTime(DateTime.Now.Date),
                    StartAt = TimeOnly.FromDateTime(DateTime.Now),
                    EndAt = TimeOnly.FromDateTime(DateTime.Now.AddHours(2)),
                    TotalAmount = request.Amount,
                    PayMethod = request.Method,
                    PayStatus = 0,
                    OrderStatus = 0,
                    CreateAt = DateTime.Now
                };

                _db.Orders.Add(order);
                _db.SaveChanges();

                // 呼叫藍新金流 Service 生成加密字串
                var (tradeInfo, tradeSha) = _newebPayService.CreateOrder(request.OrderNo, request.Amount, request.ItemDesc);

                // 建立表單，送到藍新金流
                var html = $@"
                    <form id='newebpay' method='post' action='https://ccore.newebpay.com/MPG/mpg_gateway'>
                        <input type='hidden' name='MerchantID' value='MS158311749' />
                        <input type='hidden' name='TradeInfo' value='{tradeInfo}' />
                        <input type='hidden' name='TradeSha' value='{tradeSha}' />
                        <input type='hidden' name='Version' value='2.0' />
                    </form>
                    <script>document.getElementById('newebpay').submit();</script>
                ";

                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                return BadRequest("CreateOrder failed: " + ex.Message);
            }
        }

        [HttpPost("Return")]
        public IActionResult Return([FromForm] string TradeInfo)
        {
            var decrypted = _newebPayService.DecryptTradeInfo(TradeInfo);
            var tradeResult = JsonSerializer.Deserialize<Dictionary<string, object>>(decrypted);

            string orderNo = tradeResult["MerchantOrderNo"].ToString();
            string status = tradeResult["Status"].ToString();

            var order = _db.Orders.FirstOrDefault(o => o.OrderNo == orderNo);
            if (order != null)
            {
                var oldStatus = order.PayStatus;
                order.PayStatus = status == "SUCCESS" ? 1 : 0;
                order.PayAt = DateTime.Now;
                order.TransactionNo = tradeResult["TradeNo"].ToString();

                var log = new OrderLog
                {
                    OrderId = order.OrderId,
                    OldStatus = oldStatus.ToString(),
                    NewStatus = order.PayStatus.ToString(),
                    UpdateAt = DateTime.Now
                };
                _db.OrderLogs.Add(log);
                _db.SaveChanges();
            }

            return Content("Payment Return Received");
        }

        [HttpPost("Notify")]
        public IActionResult Notify([FromForm] string TradeInfo)
        {
            var decrypted = _newebPayService.DecryptTradeInfo(TradeInfo);
            var tradeResult = JsonSerializer.Deserialize<Dictionary<string, object>>(decrypted);

            string orderNo = tradeResult["MerchantOrderNo"].ToString();
            string status = tradeResult["Status"].ToString();

            var order = _db.Orders.FirstOrDefault(o => o.OrderNo == orderNo);
            if (order != null)
            {
                var oldStatus = order.PayStatus;
                order.PayStatus = status == "SUCCESS" ? 1 : 0;
                order.PayAt = DateTime.Now;
                order.TransactionNo = tradeResult["TradeNo"].ToString();

                var log = new OrderLog
                {
                    OrderId = order.OrderId,
                    OldStatus = oldStatus.ToString(),
                    NewStatus = order.PayStatus.ToString(),
                    UpdateAt = DateTime.Now
                };
                _db.OrderLogs.Add(log);
                _db.SaveChanges();
            }

            return Ok();
        }
    }
}