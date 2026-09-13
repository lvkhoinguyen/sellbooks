using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanSach.Models
{
    public enum DiscountType
    {
        Percentage,  // Giảm theo % (ví dụ 10 = 10%)
        FixedAmount  // Giảm số tiền cố định (ví dụ 50,000đ)
    }

    public class Coupon
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã giảm giá")]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        public DiscountType DiscountType { get; set; } = DiscountType.Percentage;

        [Required]
        [Range(0, 100000000, ErrorMessage = "Giá trị giảm không hợp lệ")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        [Range(0, 100000000, ErrorMessage = "Đơn tối thiểu không hợp lệ")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MinOrderAmount { get; set; } = 0;

        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        public DateTime EndDate { get; set; } = DateTime.UtcNow.AddMonths(1);

        public bool IsActive { get; set; } = true;

        public int UsageLimit { get; set; } = 100;

        public int TimesUsed { get; set; } = 0;
    }
}
