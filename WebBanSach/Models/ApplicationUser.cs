using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace WebBanSach.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Họ và tên không được để trống!")]
        [MaxLength(100, ErrorMessage = "Họ và tên không được vượt quá 100 ký tự!")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(255)]
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [MaxLength(100)]
        [Display(Name = "Tỉnh / Thành phố")]
        public string? City { get; set; }

        [MaxLength(20)]
        [Display(Name = "Mã bưu chính")]
        public string? PostalCode { get; set; }
    }
}
