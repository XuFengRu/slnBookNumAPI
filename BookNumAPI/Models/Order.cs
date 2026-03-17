using System;
using System.Collections.Generic;

namespace BookNumAPI.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public string OrderNo { get; set; } = null!;

    public int UserId { get; set; }

    public int RenterId { get; set; }

    public int ServiceId { get; set; }

    public DateOnly AppointmentDate { get; set; }

    public TimeOnly StartAt { get; set; }

    public TimeOnly EndAt { get; set; }

    public int TotalAmount { get; set; }

    public string PayMethod { get; set; } = null!;

    public int PayStatus { get; set; }

    public DateTime? PayAt { get; set; }

    public string? TransactionNo { get; set; }

    public int OrderStatus { get; set; }

    public DateTime CreateAt { get; set; }

    public virtual ICollection<OrderLog> OrderLogs { get; set; } = new List<OrderLog>();

    public virtual Renter Renter { get; set; } = null!;

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual Service Service { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
