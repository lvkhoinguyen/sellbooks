using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebBanSach.Models
{
    public class OrderHeader
    {
        [Key]
        public int Id { get; set; }

        public string ApplicationUserId { get; set; } = string.Empty;

        [ForeignKey("ApplicationUserId")]
        [ValidateNever]
        public ApplicationUser? ApplicationUser { get; set; }

        public DateTime OrderDate { get; set; }

        public DateTime? ShippingDate { get; set; }

        public decimal OrderTotal { get; set; }
        public decimal DiscountAmount { get; set; } = 0;
        public string? CouponCode { get; set; }

        public string? OrderStatus { get; set; }

        public string? PaymentStatus { get; set; }

        public string? TrackingNumber { get; set; }

        public string? Carrier { get; set; }

        // Receiver Information
        [Required(ErrorMessage = "Vui lòng nhập họ và tên người nhận!")]
        [MaxLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự!")]
        [Display(Name = "Họ và tên người nhận")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại nhận hàng!")]
        [MaxLength(20, ErrorMessage = "Số điện thoại không hợp lệ!")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng!")]
        [MaxLength(255, ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự!")]
        [Display(Name = "Địa chỉ nhận hàng")]
        public string StreetAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập Tỉnh / Thành phố!")]
        [MaxLength(100, ErrorMessage = "Tỉnh/Thành phố không được vượt quá 100 ký tự!")]
        [Display(Name = "Tỉnh / Thành phố")]
        public string City { get; set; } = string.Empty;
    }
}
