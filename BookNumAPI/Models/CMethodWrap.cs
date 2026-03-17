using System.ComponentModel.DataAnnotations;

namespace BookNumAPI.Models
{
    public class CMethodWrap
    {
        public int MethodId { get; set; }

        [Required(ErrorMessage = "請輸入方案名稱")]
        public string MethodName { get; set; }

        [Required(ErrorMessage = "請輸入天數")]
        [Range(1, 365, ErrorMessage = "天數必須在 1 到 365 之間")]
        public int? DurationDay { get; set; }   // 注意要用 int? 才能配合 Required

        [Required(ErrorMessage = "請輸入定價")]
        [Range(1, 99999, ErrorMessage = "定價必須大於 0")]
        public int? Price { get; set; }
    }

}

