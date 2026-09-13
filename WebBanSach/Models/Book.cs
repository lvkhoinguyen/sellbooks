using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebBanSach.Models
{
    public class Book
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tiêu đề sách không được để trống!")]
        [MaxLength(150, ErrorMessage = "Tiêu đề không được vượt quá 150 ký tự!")]
        [DisplayName("Tiêu đề sách")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên tác giả không được để trống!")]
        [MaxLength(100, ErrorMessage = "Tác giả không được vượt quá 100 ký tự!")]
        [DisplayName("Tác giả")]
        public string Author { get; set; } = string.Empty;

        [DisplayName("Mô tả nội dung")]
        public string? Description { get; set; }

        [MaxLength(20, ErrorMessage = "Mã ISBN không được vượt quá 20 ký tự!")]
        [DisplayName("Mã ISBN")]
        public string? ISBN { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá niêm yết!")]
        [Range(1000, 10000000, ErrorMessage = "Giá niêm yết phải từ 1.000đ đến 10.000.000đ!")]
        [DisplayName("Giá niêm yết")]
        public decimal ListPrice { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá bán ưu đãi!")]
        [Range(1000, 10000000, ErrorMessage = "Giá bán ưu đãi phải nằm trong khoảng từ 1.000đ đến 10.000.000đ!")]
        [DisplayName("Giá bán ưu đãi")]
        public decimal Price { get; set; }

        [ValidateNever]
        [DisplayName("Hình ảnh bìa")]
        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục sách!")]
        [DisplayName("Danh mục")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        [ValidateNever]
        public Category? Category { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng tồn kho!")]
        [Range(0, 100000, ErrorMessage = "Số lượng tồn kho không được âm!")]
        [DisplayName("Số lượng tồn kho")]
        public int StockQuantity { get; set; } = 0;

        [DisplayName("Ngày tạo")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
