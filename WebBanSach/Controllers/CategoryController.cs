using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBanSach.Data;
using WebBanSach.Models;
using WebBanSach.Utility;

namespace WebBanSach.Controllers
{
    [Authorize(Roles = SD.Role_Admin)]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CategoryController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ==========================================
        // 1. READ: HIỂN THỊ DANH SÁCH (Index)
        // ==========================================
        public IActionResult Index()
        {
            List<Category> objCategoryList = _db.Categories.ToList();
            return View(objCategoryList);
        }

        // ==========================================
        // 2. CREATE: TẠO MỚI CATEGORY
        // ==========================================
        // GET: Hiển thị form tạo mới
        public IActionResult Create()
        {
            return View();
        }

        // POST: Nhận dữ liệu submit và lưu vào CSDL
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Category obj)
        {
            // Custom Validation 1: Tên không được trùng với thứ tự hiển thị
            if (obj.Name == obj.DisplayOrder.ToString())
            {
                ModelState.AddModelError("Name", "Tên danh mục không được trùng với Thứ tự hiển thị.");
            }

            // Custom Validation 2: Tên danh mục không được trùng với danh mục đã có trong CSDL
            if (!string.IsNullOrEmpty(obj.Name) && _db.Categories.Any(u => u.Name.ToLower() == obj.Name.ToLower()))
            {
                ModelState.AddModelError("Name", "Tên danh mục này đã tồn tại trong hệ thống!");
            }

            if (ModelState.IsValid)
            {
                _db.Categories.Add(obj);
                _db.SaveChanges();
                TempData["success"] = "Thêm danh mục thành công!";
                return RedirectToAction("Index");
            }

            TempData["error"] = "Dữ liệu không hợp lệ, vui lòng kiểm tra lại!";
            return View(obj);
        }

        // ==========================================
        // 3. UPDATE: CHỈNH SỬA CATEGORY (Edit)
        // ==========================================
        // GET: Lấy thông tin Category theo Id và hiển thị lên form sửa
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Category? categoryFromDb = _db.Categories.Find(id);

            if (categoryFromDb == null)
            {
                return NotFound();
            }

            return View(categoryFromDb);
        }

        // POST: Cập nhật thông tin đã sửa vào CSDL
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Category obj)
        {
            if (obj.Name == obj.DisplayOrder.ToString())
            {
                ModelState.AddModelError("Name", "Tên danh mục không được trùng với Thứ tự hiển thị.");
            }

            // Kiểm tra trùng tên với danh mục khác (trừ chính nó)
            if (!string.IsNullOrEmpty(obj.Name) && _db.Categories.Any(u => u.Name.ToLower() == obj.Name.ToLower() && u.Id != obj.Id))
            {
                ModelState.AddModelError("Name", "Tên danh mục này đã được sử dụng bởi danh mục khác!");
            }

            if (ModelState.IsValid)
            {
                _db.Categories.Update(obj);
                _db.SaveChanges();
                TempData["success"] = "Cập nhật danh mục thành công!";
                return RedirectToAction("Index");
            }

            TempData["error"] = "Cập nhật thất bại, vui lòng kiểm tra lại thông tin!";
            return View(obj);
        }

        // ==========================================
        // 4. DELETE: XÓA CATEGORY
        // ==========================================
        // GET: Hiển thị trang xác nhận xóa
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Category? categoryFromDb = _db.Categories.Find(id);

            if (categoryFromDb == null)
            {
                return NotFound();
            }

            return View(categoryFromDb);
        }

        // POST: Thực hiện xóa Category khỏi CSDL
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {
            Category? obj = _db.Categories.Find(id);
            if (obj == null)
            {
                return NotFound();
            }

            _db.Categories.Remove(obj);
            _db.SaveChanges();
            TempData["success"] = "Xóa danh mục thành công!";
            return RedirectToAction("Index");
        }
    }
}
