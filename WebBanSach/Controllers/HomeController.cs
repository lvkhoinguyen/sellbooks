using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanSach.Data;
using WebBanSach.Models;
using WebBanSach.Models.ViewModels;
using WebBanSach.Utility;

namespace WebBanSach.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ==========================================
        // 1. TRANG CHỦ (Home/Index)
        // ==========================================
        public IActionResult Index()
        {
            List<Category> categoryList = _db.Categories.OrderBy(c => c.DisplayOrder).ToList();
            ViewBag.BookList = _db.Books.Include(u => u.Category).OrderByDescending(u => u.Id).ToList();
            return View(categoryList);
        }

        // ==========================================
        // 2. CỬA HÀNG SÁCH, TÌM KIẾM, LỌC & PHÂN TRANG (Home/Shop)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Shop(
            string? searchString,
            int? categoryId,
            decimal? minPrice,
            decimal? maxPrice,
            string? sortBy,
            int page = 1,
            int pageSize = 12)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 12;

            var query = BuildBookQuery(searchString, categoryId, minPrice, maxPrice, sortBy);

            // Chỉ thực thi truy vấn CountAsync và Skip/Take ở bước cuối cùng
            var paginatedBooks = await PaginatedList<Book>.CreateAsync(query.AsNoTracking(), page, pageSize);
            var categories = await _db.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();

            var viewModel = new BookListVM
            {
                Books = paginatedBooks,
                Categories = categories,
                SearchString = searchString,
                CategoryId = categoryId,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SortBy = sortBy,
                PageSize = pageSize
            };

            return View(viewModel);
        }

        // ==========================================
        // 3. API TÌM KIẾM & PHÂN TRANG CHO CLIENT / AJAX (Home/ApiBooks)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ApiBooks(
            string? searchString,
            int? categoryId,
            decimal? minPrice,
            decimal? maxPrice,
            string? sortBy,
            int page = 1,
            int pageSize = 12)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 12;

            var query = BuildBookQuery(searchString, categoryId, minPrice, maxPrice, sortBy);
            var paginatedBooks = await PaginatedList<Book>.CreateAsync(query.AsNoTracking(), page, pageSize);

            return Json(new
            {
                success = true,
                pageIndex = paginatedBooks.PageIndex,
                totalPages = paginatedBooks.TotalPages,
                totalCount = paginatedBooks.TotalCount,
                hasPreviousPage = paginatedBooks.HasPreviousPage,
                hasNextPage = paginatedBooks.HasNextPage,
                items = paginatedBooks.Items.Select(b => new
                {
                    b.Id,
                    b.Title,
                    b.Author,
                    b.Price,
                    b.ListPrice,
                    b.ImageUrl,
                    b.StockQuantity,
                    CategoryName = b.Category?.Name
                })
            });
        }

        // Helper: Xây dựng truy vấn động IQueryable<Book>
        private IQueryable<Book> BuildBookQuery(
            string? searchString,
            int? categoryId,
            decimal? minPrice,
            decimal? maxPrice,
            string? sortBy)
        {
            IQueryable<Book> query = _db.Books.Include(u => u.Category);

            // 1. Tìm kiếm theo Tựa sách, Tác giả hoặc ISBN
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var term = searchString.Trim();
                query = query.Where(b =>
                    b.Title.Contains(term) ||
                    b.Author.Contains(term) ||
                    (b.ISBN != null && b.ISBN.Contains(term)));
            }

            // 2. Lọc theo Thể loại danh mục
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(b => b.CategoryId == categoryId.Value);
            }

            // 3. Lọc theo Khoảng giá
            if (minPrice.HasValue && minPrice.Value >= 0)
            {
                query = query.Where(b => b.Price >= minPrice.Value);
            }
            if (maxPrice.HasValue && maxPrice.Value > 0)
            {
                query = query.Where(b => b.Price <= maxPrice.Value);
            }

            // 4. Sắp xếp kết quả
            query = sortBy?.ToLower() switch
            {
                "price_asc" => query.OrderBy(b => b.Price),
                "price_desc" => query.OrderByDescending(b => b.Price),
                "bestseller" => query.OrderByDescending(b => b.StockQuantity <= 10 ? 1 : 0).ThenByDescending(b => b.Id),
                "newest" => query.OrderByDescending(b => b.CreatedDate).ThenByDescending(b => b.Id),
                _ => query.OrderByDescending(b => b.CreatedDate).ThenByDescending(b => b.Id)
            };

            return query;
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
