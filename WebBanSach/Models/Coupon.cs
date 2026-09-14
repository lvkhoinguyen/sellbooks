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
        [StringLength(50, ErrorMessage = "Mã giảm giá không được quá 50 ký tự")]
        [Display(Name = "Mã giảm giá")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn loại giảm giá")]
        [Display(Name = "Loại giảm giá")]
        public DiscountType DiscountType { get; set; } = DiscountType.Percentage;

        [Required(ErrorMessage = "Vui lòng nhập giá trị giảm")]
        [Range(0.01, 100000000, ErrorMessage = "Giá trị giảm phải lớn hơn 0")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Giá trị giảm")]
        public decimal DiscountValue { get; set; }

        [Range(0, 100000000, ErrorMessage = "Đơn tối thiểu không được âm")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Giá trị đơn hàng tối thiểu")]
        public decimal MinOrderAmount { get; set; } = 0;

        [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu")]
        [Display(Name = "Ngày bắt đầu")]
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc")]
        [Display(Name = "Ngày kết thúc")]
        public DateTime EndDate { get; set; } = DateTime.UtcNow.AddMonths(1);

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;

        [Range(1, 1000000, ErrorMessage = "Giới hạn lượt dùng phải từ 1 trở lên")]
        [Display(Name = "Giới hạn số lượt dùng")]
        public int UsageLimit { get; set; } = 100;

        [Display(Name = "Số lượt đã dùng")]
        public int TimesUsed { get; set; } = 0;
    }
}
