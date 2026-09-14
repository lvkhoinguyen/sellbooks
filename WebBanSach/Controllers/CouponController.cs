using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanSach.Data;
using WebBanSach.Models;
using WebBanSach.Utility;

namespace WebBanSach.Controllers
{
    [Authorize(Roles = SD.Role_Admin)]
    public class CouponController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CouponController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ==========================================
        // 1. READ: DANH SÁCH MÃ GIẢM GIÁ (Index)
        // ==========================================
        public IActionResult Index()
        {
            var couponList = _db.Coupons.OrderByDescending(c => c.Id).ToList();
            return View(couponList);
        }

        // ==========================================
        // 2. CREATE & EDIT: THÊM MỚI HOẶC SỬA (Upsert)
        // ==========================================
        [HttpGet]
        public IActionResult Upsert(int? id)
        {
            if (id == null || id == 0)
            {
                // Create
                var newCoupon = new Coupon
                {
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddMonths(1),
                    IsActive = true,
                    UsageLimit = 100,
                    MinOrderAmount = 0
                };
                return View(newCoupon);
            }

            // Edit
            var couponFromDb = _db.Coupons.Find(id);
            if (couponFromDb == null)
            {
                TempData["error"] = "Không tìm thấy mã giảm giá yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            return View(couponFromDb);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Coupon obj)
        {
            // Chuẩn hóa mã viết hoa
            if (!string.IsNullOrWhiteSpace(obj.Code))
            {
                obj.Code = obj.Code.Trim().ToUpper();
            }

            // Kiểm tra trùng mã code với coupon khác
            bool isDuplicate = _db.Coupons.Any(c => c.Code.ToUpper() == obj.Code.ToUpper() && c.Id != obj.Id);
            if (isDuplicate)
            {
                ModelState.AddModelError("Code", "Mã giảm giá này đã tồn tại trong hệ thống! Vui lòng chọn mã khác.");
            }

            // Kiểm tra ngày kết thúc phải sau ngày bắt đầu
            if (obj.EndDate <= obj.StartDate)
            {
                ModelState.AddModelError("EndDate", "Ngày kết thúc phải diễn ra sau ngày bắt đầu!");
            }

            // Kiểm tra giá trị giảm theo % không vượt quá 100%
            if (obj.DiscountType == DiscountType.Percentage && obj.DiscountValue > 100)
            {
                ModelState.AddModelError("DiscountValue", "Giảm theo phần trăm không được vượt quá 100%!");
            }

            if (ModelState.IsValid)
            {
                if (obj.Id == 0)
                {
                    _db.Coupons.Add(obj);
                    TempData["success"] = $"Tạo mới mã giảm giá \"{obj.Code}\" thành công!";
                }
                else
                {
                    _db.Coupons.Update(obj);
                    TempData["success"] = $"Cập nhật mã giảm giá \"{obj.Code}\" thành công!";
                }

                _db.SaveChanges();
                return RedirectToAction(nameof(Index));
            }

            TempData["error"] = "Dữ liệu chưa hợp lệ, vui lòng kiểm tra lại các trường!";
            return View(obj);
        }

        // ==========================================
        // 3. TOGGLE STATUS: BẬT / TẮT KÍCH HOẠT NHANH
        // ==========================================
        [HttpPost]
        public IActionResult ToggleStatus(int id)
        {
            var coupon = _db.Coupons.Find(id);
            if (coupon == null)
            {
                return Json(new { success = false, message = "Không tìm thấy mã giảm giá!" });
            }

            coupon.IsActive = !coupon.IsActive;
            _db.Coupons.Update(coupon);
            _db.SaveChanges();

            return Json(new
            {
                success = true,
                isActive = coupon.IsActive,
                message = $"Đã {(coupon.IsActive ? "kích hoạt" : "vô hiệu hóa")} mã \"{coupon.Code}\" thành công!"
            });
        }

        // ==========================================
        // 4. DELETE: XÓA MÃ GIẢM GIÁ
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var coupon = _db.Coupons.Find(id);
            if (coupon == null)
            {
                TempData["error"] = "Không tìm thấy mã giảm giá để xóa!";
                return RedirectToAction(nameof(Index));
            }

            _db.Coupons.Remove(coupon);
            _db.SaveChanges();

            TempData["success"] = $"Đã xóa thành công mã giảm giá \"{coupon.Code}\"!";
            return RedirectToAction(nameof(Index));
        }
    }
}
