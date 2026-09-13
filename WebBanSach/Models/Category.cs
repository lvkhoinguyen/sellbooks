using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WebBanSach.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống!")]
        [MaxLength(50, ErrorMessage = "Tên danh mục không được vượt quá 50 ký tự!")]
        [DisplayName("Tên danh mục")]
        public string Name { get; set; } = string.Empty;

        [DisplayName("Thứ tự hiển thị")]
        [Range(1, 100, ErrorMessage = "Thứ tự hiển thị phải nằm trong khoảng từ 1 đến 100!")]
        public int DisplayOrder { get; set; }

        public DateTime CreatedDateTime { get; set; } = DateTime.Now;
    }
}
