using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanSach.Data;
using WebBanSach.Models;
using WebBanSach.Models.ViewModels;
using WebBanSach.Utility;

namespace WebBanSach.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;

        public OrderController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ==========================================
        // 1. READ: DANH SÁCH ĐƠN HÀNG (Index)
        // ==========================================
        public IActionResult Index(string? status)
        {
            var userId = GetCurrentUserId();
            IEnumerable<OrderHeader> orderHeaders;

            if (User.IsInRole(SD.Role_Admin))
            {
                orderHeaders = _db.OrderHeaders.Include(u => u.ApplicationUser).ToList();
            }
            else
            {
                orderHeaders = _db.OrderHeaders
                    .Where(u => u.ApplicationUserId == userId)
                    .Include(u => u.ApplicationUser)
                    .ToList();
            }

            status = status?.ToLower() ?? "all";
            switch (status)
            {
                case "pending":
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == SD.StatusPending);
                    break;
                case "inprocess":
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == SD.StatusInProcess);
                    break;
                case "completed":
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == SD.StatusShipped);
                    break;
                case "cancelled":
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == SD.StatusCancelled);
                    break;
                default:
                    break;
            }

            ViewData["CurrentStatus"] = status;
            return View(orderHeaders.OrderByDescending(u => u.Id));
        }

        // ==========================================
        // 2. READ: CHI TIẾT ĐƠN HÀNG (Details)
        // ==========================================
        public IActionResult Details(int orderId)
        {
            var userId = GetCurrentUserId();
            var orderHeader = _db.OrderHeaders
                .Include(u => u.ApplicationUser)
                .FirstOrDefault(u => u.Id == orderId);

            if (orderHeader == null)
            {
                TempData["error"] = "Không tìm thấy đơn hàng trong hệ thống!";
                return RedirectToAction(nameof(Index));
            }

            // Độc giả chỉ được xem đơn hàng của chính mình
            if (!User.IsInRole(SD.Role_Admin) && orderHeader.ApplicationUserId != userId)
            {
                TempData["error"] = "Bạn không có quyền truy cập vào đơn hàng này!";
                return RedirectToAction(nameof(Index));
            }

            var orderDetails = _db.OrderDetails
                .Include(u => u.Book)
                .Where(u => u.OrderHeaderId == orderId)
                .ToList();

            var orderVM = new OrderVM
            {
                OrderHeader = orderHeader,
                OrderDetail = orderDetails
            };

            return View(orderVM);
        }

        // ==========================================
        // 3. ADMIN: CẬP NHẬT THÔNG TIN GIAO NHẬN
        // ==========================================
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateOrderDetail(OrderVM orderVM)
        {
            var orderHeaderFromDb = _db.OrderHeaders.FirstOrDefault(u => u.Id == orderVM.OrderHeader.Id);
            if (orderHeaderFromDb != null)
            {
                orderHeaderFromDb.Name = orderVM.OrderHeader.Name;
                orderHeaderFromDb.PhoneNumber = orderVM.OrderHeader.PhoneNumber;
                orderHeaderFromDb.StreetAddress = orderVM.OrderHeader.StreetAddress;
                orderHeaderFromDb.City = orderVM.OrderHeader.City;

                if (!string.IsNullOrEmpty(orderVM.OrderHeader.Carrier))
                {
                    orderHeaderFromDb.Carrier = orderVM.OrderHeader.Carrier;
                }
                if (!string.IsNullOrEmpty(orderVM.OrderHeader.TrackingNumber))
                {
                    orderHeaderFromDb.TrackingNumber = orderVM.OrderHeader.TrackingNumber;
                }

                _db.OrderHeaders.Update(orderHeaderFromDb);
                _db.SaveChanges();

                TempData["success"] = "Cập nhật thông tin giao nhận thành công!";
            }
            else
            {
                TempData["error"] = "Không tìm thấy đơn hàng cần cập nhật!";
            }

            return RedirectToAction(nameof(Details), new { orderId = orderVM.OrderHeader.Id });
        }

        // ==========================================
        // 4. ADMIN: BẮT ĐẦU XỬ LÝ / ĐÓNG GÓI
        // ==========================================
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        [ValidateAntiForgeryToken]
        public IActionResult StartProcessing(int orderId)
        {
            var orderHeaderFromDb = _db.OrderHeaders.FirstOrDefault(u => u.Id == orderId);
            if (orderHeaderFromDb != null)
            {
                orderHeaderFromDb.OrderStatus = SD.StatusInProcess;
                _db.OrderHeaders.Update(orderHeaderFromDb);
                _db.SaveChanges();

                TempData["success"] = $"Đơn hàng #{orderId} đã chuyển sang trạng thái: Đang Xử Lý!";
            }

            return RedirectToAction(nameof(Details), new { orderId = orderId });
        }

        // ==========================================
        // 5. ADMIN: GIAO HÀNG (SHIP ORDER)
        // ==========================================
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        [ValidateAntiForgeryToken]
        public IActionResult ShipOrder(int orderId, string? carrier, string? trackingNumber)
        {
            var orderHeaderFromDb = _db.OrderHeaders.FirstOrDefault(u => u.Id == orderId);
            if (orderHeaderFromDb != null)
            {
                orderHeaderFromDb.Carrier = string.IsNullOrWhiteSpace(carrier) ? "PageCraft Express" : carrier;
                orderHeaderFromDb.TrackingNumber = string.IsNullOrWhiteSpace(trackingNumber) ? $"PC{DateTime.Now:yyyyMMddHHmm}" : trackingNumber;
                orderHeaderFromDb.OrderStatus = SD.StatusShipped;
                orderHeaderFromDb.ShippingDate = DateTime.Now;

                _db.OrderHeaders.Update(orderHeaderFromDb);
                _db.SaveChanges();

                TempData["success"] = $"Đơn hàng #{orderId} đã được xuất kho và giao cho đơn vị vận chuyển!";
            }

            return RedirectToAction(nameof(Details), new { orderId = orderId });
        }

        // ==========================================
        // 6. ADMIN: HỦY ĐƠN HÀNG (CANCEL ORDER & HOÀN KHO)
        // ==========================================
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        [ValidateAntiForgeryToken]
        public IActionResult CancelOrder(int orderId)
        {
            var orderHeaderFromDb = _db.OrderHeaders.FirstOrDefault(u => u.Id == orderId);
            if (orderHeaderFromDb != null)
            {
                // Hoàn lại số lượng tồn kho cho từng cuốn sách
                var orderDetails = _db.OrderDetails.Include(u => u.Book).Where(u => u.OrderHeaderId == orderId).ToList();
                foreach (var detail in orderDetails)
                {
                    if (detail.Book != null)
                    {
                        detail.Book.StockQuantity += detail.Count;
                        _db.Books.Update(detail.Book);
                    }
                }

                orderHeaderFromDb.OrderStatus = SD.StatusCancelled;
                _db.OrderHeaders.Update(orderHeaderFromDb);
                _db.SaveChanges();

                TempData["success"] = $"Đã hủy đơn hàng #{orderId} và tự động hoàn trả số lượng sách về lại kho thành công!";
            }

            return RedirectToAction(nameof(Details), new { orderId = orderId });
        }

        // Helper: Lấy UserId hiện tại
        private string GetCurrentUserId()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity!;
            return claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        }
    }
}
