using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebBanSach.Data;
using WebBanSach.Models;
using WebBanSach.Models.ViewModels;
using WebBanSach.Utility;

namespace WebBanSach.Controllers
{
    [Authorize(Roles = SD.Role_Admin)]
    public class BookController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public BookController(ApplicationDbContext db, IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
        }

        // ==========================================
        // 1. READ: DANH SÁCH SÁCH (Index)
        // ==========================================
        public IActionResult Index()
        {
            List<Book> objBookList = _db.Books
                .Include(u => u.Category)
                .OrderByDescending(u => u.Id)
                .ToList();

            return View(objBookList);
        }

        // ==========================================
        // 2. UPSERT: TẠO MỚI HOẶC CẬP NHẬT (GET)
        // ==========================================
        public IActionResult Upsert(int? id)
        {
            BookVM bookVM = new()
            {
                CategoryList = _db.Categories.Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                }),
                Book = new Book()
            };

            if (id == null || id == 0)
            {
                // Create mode: Thêm mới sách
                return View(bookVM);
            }
            else
            {
                // Update mode: Sửa sách đã có
                var bookFromDb = _db.Books.FirstOrDefault(u => u.Id == id);
                if (bookFromDb == null)
                {
                    return NotFound();
                }
                bookVM.Book = bookFromDb;
                return View(bookVM);
            }
        }

        // ==========================================
        // 2. UPSERT: TẠO MỚI HOẶC CẬP NHẬT (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(BookVM bookVM, IFormFile? file)
        {
            // Nghiệp vụ 1: Bắt buộc chọn Danh mục hợp lệ
            if (bookVM.Book.CategoryId <= 0)
            {
                ModelState.AddModelError("Book.CategoryId", "Vui lòng chọn danh mục cho cuốn sách!");
            }

            // Nghiệp vụ 2: Giá bán ưu đãi không được lớn hơn giá niêm yết
            if (bookVM.Book.Price > bookVM.Book.ListPrice)
            {
                ModelState.AddModelError("Book.Price", "Giá bán ưu đãi không được vượt quá giá niêm yết gốc!");
            }

            if (ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;

                // Xử lý upload hình ảnh bìa
                if (file != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string bookPath = Path.Combine(wwwRootPath, @"images\books");

                    if (!Directory.Exists(bookPath))
                    {
                        Directory.CreateDirectory(bookPath);
                    }

                    // Nếu đang cập nhật và đã có ảnh cũ -> Xóa ảnh vật lý cũ
                    if (!string.IsNullOrEmpty(bookVM.Book.ImageUrl))
                    {
                        var trimmedPath = bookVM.Book.ImageUrl.TrimStart('\\', '/').Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
                        var oldImagePath = Path.Combine(wwwRootPath, trimmedPath);
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    // Lưu file ảnh mới vào wwwroot/images/books/
                    using (var fileStream = new FileStream(Path.Combine(bookPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }

                    bookVM.Book.ImageUrl = @"/images/books/" + fileName;
                }

                if (bookVM.Book.Id == 0)
                {
                    // Thêm mới
                    _db.Books.Add(bookVM.Book);
                    TempData["success"] = $"Thêm sách \"{bookVM.Book.Title}\" thành công!";
                }
                else
                {
                    // Cập nhật
                    _db.Books.Update(bookVM.Book);
                    TempData["success"] = $"Cập nhật sách \"{bookVM.Book.Title}\" thành công!";
                }

                _db.SaveChanges();
                return RedirectToAction(nameof(Index));
            }

            // Nếu dữ liệu không hợp lệ, nạp lại danh sách danh mục dropdown
            bookVM.CategoryList = _db.Categories.Select(u => new SelectListItem
            {
                Text = u.Name,
                Value = u.Id.ToString()
            });

            TempData["error"] = "Dữ liệu chưa hợp lệ, vui lòng kiểm tra lại thông tin!";
            return View(bookVM);
        }

        // ==========================================
        // 3. DELETE: XÓA SÁCH (GET)
        // ==========================================
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Book? bookFromDb = _db.Books
                .Include(u => u.Category)
                .FirstOrDefault(u => u.Id == id);

            if (bookFromDb == null)
            {
                return NotFound();
            }

            return View(bookFromDb);
        }

        // ==========================================
        // 3. DELETE: XÓA SÁCH (POST)
        // ==========================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {
            Book? obj = _db.Books.Find(id);
            if (obj == null)
            {
                return NotFound();
            }

            // Xóa file ảnh vật lý tương ứng nếu tồn tại
            if (!string.IsNullOrEmpty(obj.ImageUrl))
            {
                var trimmedPath = obj.ImageUrl.TrimStart('\\', '/').Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
                var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, trimmedPath);
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }

            string title = obj.Title;
            _db.Books.Remove(obj);
            _db.SaveChanges();

            TempData["success"] = $"Đã xóa cuốn sách \"{title}\" thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
