namespace BookNumAPI.Models
{
    public class CreateOrderRequest
    {
        public string OrderNo { get; set; }
        public int Amount { get; set; }
        public string ItemDesc { get; set; }
        public string Method { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CardNumber { get; set; }
    }
}