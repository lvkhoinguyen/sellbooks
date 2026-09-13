using WebBanSach.Utility;

namespace WebBanSach.Models.ViewModels
{
    public class BookListVM
    {
        public PaginatedList<Book> Books { get; set; } = null!;
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();

        // Filter & Search state persistence
        public string? SearchString { get; set; }
        public int? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? SortBy { get; set; }
        public int PageSize { get; set; } = 12;
    }
}
