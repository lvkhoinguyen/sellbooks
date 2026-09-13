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
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CartController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ==========================================
        // 1. READ: DANH SÁCH GIỎ HÀNG (Index)
        // ==========================================
        public IActionResult Index()
        {
            var userId = GetCurrentUserId();

            var shoppingCartList = _db.ShoppingCarts
                .Include(u => u.Book)
                .Where(u => u.ApplicationUserId == userId)
                .ToList();

            decimal orderTotal = 0;
            foreach (var cart in shoppingCartList)
            {
                cart.Price = cart.Book?.Price ?? 0;
                orderTotal += (cart.Price * cart.Count);
            }

            var sessionCoupon = HttpContext.Session.GetString(SD.SessionCoupon);
            decimal discountAmount = 0;
            Coupon? appliedCoupon = null;

            if (!string.IsNullOrEmpty(sessionCoupon))
            {
                var (coupon, discount, error) = ValidateAndCalculateCoupon(sessionCoupon, orderTotal);
                if (error == null && coupon != null)
                {
                    appliedCoupon = coupon;
                    discountAmount = discount;
                }
                else
                {
                    HttpContext.Session.Remove(SD.SessionCoupon);
                    TempData["warning"] = $"Mã giảm giá \"{sessionCoupon}\" đã bị hủy bỏ: {error}";
                }
            }

            var shoppingCartVM = new ShoppingCartVM
            {
                ShoppingCartList = shoppingCartList,
                OrderTotal = orderTotal,
                DiscountAmount = discountAmount,
                FinalTotal = Math.Max(0, orderTotal - discountAmount),
                CouponCode = appliedCoupon?.Code,
                AppliedCoupon = appliedCoupon
            };

            // Đồng bộ Session số loại mặt hàng trong giỏ
            HttpContext.Session.SetInt32(SD.SessionCart, shoppingCartList.Count);

            return View(shoppingCartVM);
        }

        // ==========================================
        // COUPON HELPER & ACTIONS
        // ==========================================
        private (Coupon? coupon, decimal discountAmount, string? errorMessage) ValidateAndCalculateCoupon(string? couponCode, decimal subTotal)
        {
            if (string.IsNullOrWhiteSpace(couponCode))
            {
                return (null, 0, "Vui lòng nhập mã giảm giá.");
            }

            var code = couponCode.Trim().ToUpper();
            var coupon = _db.Coupons.FirstOrDefault(c => c.Code.ToUpper() == code);

            if (coupon == null || !coupon.IsActive)
            {
                return (null, 0, "Mã giảm giá không tồn tại hoặc đã bị vô hiệu hóa!");
            }

            var now = DateTime.UtcNow;
            if (now < coupon.StartDate)
            {
                return (null, 0, $"Mã giảm giá chưa đến ngày áp dụng (bắt đầu từ {coupon.StartDate:dd/MM/yyyy})!");
            }

            if (now > coupon.EndDate)
            {
                return (null, 0, $"Mã giảm giá đã hết hạn sử dụng vào {coupon.EndDate:dd/MM/yyyy}!");
            }

            if (coupon.TimesUsed >= coupon.UsageLimit)
            {
                return (null, 0, "Mã giảm giá đã hết lượt sử dụng!");
            }

            if (subTotal < coupon.MinOrderAmount)
            {
                return (null, 0, $"Đơn hàng tối thiểu phải từ {coupon.MinOrderAmount:#,##0}₫ để áp dụng mã \"{coupon.Code}\"!");
            }

            decimal discountAmount = 0;
            if (coupon.DiscountType == DiscountType.Percentage)
            {
                discountAmount = Math.Round((subTotal * coupon.DiscountValue) / 100m);
            }
            else if (coupon.DiscountType == DiscountType.FixedAmount)
            {
                discountAmount = coupon.DiscountValue;
            }

            if (discountAmount > subTotal)
            {
                discountAmount = subTotal;
            }

            return (coupon, discountAmount, null);
        }

        [HttpPost]
        public IActionResult ApplyCoupon(string couponCode, string? returnUrl = null)
        {
            var userId = GetCurrentUserId();
            var shoppingCartList = _db.ShoppingCarts
                .Include(u => u.Book)
                .Where(u => u.ApplicationUserId == userId)
                .ToList();

            decimal subTotal = 0;
            foreach (var item in shoppingCartList)
            {
                item.Price = item.Book?.Price ?? 0;
                subTotal += (item.Price * item.Count);
            }

            var (coupon, discountAmount, errorMessage) = ValidateAndCalculateCoupon(couponCode, subTotal);

            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                          Request.Headers.Accept.ToString().Contains("application/json");

            if (errorMessage != null)
            {
                if (isAjax)
                {
                    return Json(new { success = false, message = errorMessage });
                }
                TempData["error"] = errorMessage;
            }
            else if (coupon != null)
            {
                HttpContext.Session.SetString(SD.SessionCoupon, coupon.Code);
                var successMessage = $"Áp dụng mã giảm giá \"{coupon.Code}\" thành công! Bạn được giảm {discountAmount:#,##0}₫.";

                if (isAjax)
                {
                    decimal shippingFee = subTotal >= 250000 ? 0 : 30000;
                    decimal finalTotal = Math.Max(0, subTotal + shippingFee - discountAmount);
                    return Json(new
                    {
                        success = true,
                        message = successMessage,
                        code = coupon.Code,
                        discountAmount = discountAmount,
                        subTotal = subTotal,
                        shippingFee = shippingFee,
                        finalTotal = finalTotal
                    });
                }
                TempData["success"] = successMessage;
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult RemoveCoupon(string? returnUrl = null)
        {
            HttpContext.Session.Remove(SD.SessionCoupon);
            TempData["success"] = "Đã hủy áp dụng mã giảm giá.";

            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                          Request.Headers.Accept.ToString().Contains("application/json");
            if (isAjax)
            {
                return Json(new { success = true, message = "Đã hủy áp dụng mã giảm giá." });
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // 2. ADD: THÊM SẢN PHẨM VÀO GIỎ (AddToCart)
        // ==========================================
        [HttpGet]
        [HttpPost]
        public IActionResult AddToCart(int bookId, int count = 1, string? returnUrl = null)
        {
            var userId = GetCurrentUserId();

            var book = _db.Books.Find(bookId);
            if (book == null)
            {
                TempData["error"] = "Không tìm thấy cuốn sách này trong hệ thống!";
                return RedirectToAction("Index", "Home");
            }

            if (count <= 0) count = 1;

            var cartFromDb = _db.ShoppingCarts.FirstOrDefault(u => u.ApplicationUserId == userId && u.BookId == bookId);
            int currentCountInCart = cartFromDb?.Count ?? 0;

            // Kiểm tra số lượng tồn kho
            if (currentCountInCart + count > book.StockQuantity)
            {
                TempData["error"] = $"Số lượng thêm vào vượt quá tồn kho (Kho còn {book.StockQuantity} cuốn, giỏ của bạn đã có {currentCountInCart} cuốn)!";
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return LocalRedirect(returnUrl);
                }
                return RedirectToAction(nameof(Index));
            }

            if (cartFromDb == null)
            {
                // Tạo mới record ShoppingCart
                var newCart = new ShoppingCart
                {
                    ApplicationUserId = userId,
                    BookId = bookId,
                    Count = count
                };
                _db.ShoppingCarts.Add(newCart);
            }
            else
            {
                // Cộng dồn số lượng
                cartFromDb.Count += count;
                _db.ShoppingCarts.Update(cartFromDb);
            }

            _db.SaveChanges();

            // Cập nhật số lượng mặt hàng trong Session
            int cartCount = _db.ShoppingCarts.Count(u => u.ApplicationUserId == userId);
            HttpContext.Session.SetInt32(SD.SessionCart, cartCount);

            TempData["success"] = $"Đã thêm {count} cuốn \"{book.Title}\" vào giỏ hàng thành công!";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        // Hỗ trợ GET AddToCart nhanh (ví dụ click từ nút mua nhanh)
        [HttpGet]
        public IActionResult AddToCartGet(int bookId, int count = 1, string? returnUrl = null)
        {
            return AddToCart(bookId, count, returnUrl);
        }

        // ==========================================
        // 3. PLUS: TĂNG SỐ LƯỢNG (Plus)
        // ==========================================
        public IActionResult Plus(int cartId)
        {
            var userId = GetCurrentUserId();
            var cartFromDb = _db.ShoppingCarts.Include(u => u.Book).FirstOrDefault(u => u.Id == cartId && u.ApplicationUserId == userId);

            if (cartFromDb != null)
            {
                if (cartFromDb.Book != null && cartFromDb.Count + 1 > cartFromDb.Book.StockQuantity)
                {
                    TempData["error"] = $"Số lượng yêu cầu vượt quá số lượng trong kho (Hiện chỉ còn {cartFromDb.Book.StockQuantity} cuốn)!";
                }
                else
                {
                    cartFromDb.Count += 1;
                    _db.ShoppingCarts.Update(cartFromDb);
                    _db.SaveChanges();
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // 4. MINUS: GIẢM SỐ LƯỢNG (Minus)
        // ==========================================
        public IActionResult Minus(int cartId)
        {
            var userId = GetCurrentUserId();
            var cartFromDb = _db.ShoppingCarts.FirstOrDefault(u => u.Id == cartId && u.ApplicationUserId == userId);

            if (cartFromDb != null)
            {
                if (cartFromDb.Count <= 1)
                {
                    // Giảm về 0 thì tự động xóa khỏi giỏ
                    _db.ShoppingCarts.Remove(cartFromDb);
                    _db.SaveChanges();

                    int cartCount = _db.ShoppingCarts.Count(u => u.ApplicationUserId == userId);
                    HttpContext.Session.SetInt32(SD.SessionCart, cartCount);

                    TempData["success"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
                }
                else
                {
                    cartFromDb.Count -= 1;
                    _db.ShoppingCarts.Update(cartFromDb);
                    _db.SaveChanges();
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // 5. REMOVE: XÓA HẲN MẶT HÀNG (Remove)
        // ==========================================
        public IActionResult Remove(int cartId)
        {
            var userId = GetCurrentUserId();
            var cartFromDb = _db.ShoppingCarts.FirstOrDefault(u => u.Id == cartId && u.ApplicationUserId == userId);

            if (cartFromDb != null)
            {
                _db.ShoppingCarts.Remove(cartFromDb);
                _db.SaveChanges();

                int cartCount = _db.ShoppingCarts.Count(u => u.ApplicationUserId == userId);
                HttpContext.Session.SetInt32(SD.SessionCart, cartCount);

                TempData["success"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // 6. SUMMARY: XÁC NHẬN ĐƠN HÀNG (GET)
        // ==========================================
        [HttpGet]
        public IActionResult Summary()
        {
            var userId = GetCurrentUserId();

            var shoppingCartList = _db.ShoppingCarts
                .Include(u => u.Book)
                .Where(u => u.ApplicationUserId == userId)
                .ToList();

            if (!shoppingCartList.Any())
            {
                TempData["error"] = "Giỏ hàng của bạn đang trống! Vui lòng chọn sách trước khi thanh toán.";
                return RedirectToAction(nameof(Index));
            }

            var applicationUser = _db.ApplicationUsers.FirstOrDefault(u => u.Id == userId);

            decimal subTotal = 0;
            foreach (var cart in shoppingCartList)
            {
                cart.Price = cart.Book?.Price ?? 0;
                subTotal += (cart.Price * cart.Count);
            }

            var sessionCoupon = HttpContext.Session.GetString(SD.SessionCoupon);
            decimal discountAmount = 0;
            Coupon? appliedCoupon = null;

            if (!string.IsNullOrEmpty(sessionCoupon))
            {
                var (coupon, discount, error) = ValidateAndCalculateCoupon(sessionCoupon, subTotal);
                if (error == null && coupon != null)
                {
                    appliedCoupon = coupon;
                    discountAmount = discount;
                }
                else
                {
                    HttpContext.Session.Remove(SD.SessionCoupon);
                    TempData["warning"] = $"Mã giảm giá \"{sessionCoupon}\" không còn hợp lệ: {error}";
                }
            }

            // Quy định giao hàng: Miễn phí cho đơn từ 250.000₫, dưới 250.000₫ là 30.000₫
            decimal shippingFee = subTotal >= 250000 ? 0 : 30000;
            decimal finalTotal = Math.Max(0, subTotal + shippingFee - discountAmount);

            var shoppingCartVM = new ShoppingCartVM
            {
                ShoppingCartList = shoppingCartList,
                OrderHeader = new OrderHeader(),
                OrderTotal = subTotal,
                DiscountAmount = discountAmount,
                FinalTotal = finalTotal,
                CouponCode = appliedCoupon?.Code,
                AppliedCoupon = appliedCoupon
            };

            shoppingCartVM.OrderHeader.OrderTotal = finalTotal;
            shoppingCartVM.OrderHeader.DiscountAmount = discountAmount;
            shoppingCartVM.OrderHeader.CouponCode = appliedCoupon?.Code;

            if (applicationUser != null)
            {
                shoppingCartVM.OrderHeader.Name = applicationUser.FullName ?? string.Empty;
                shoppingCartVM.OrderHeader.PhoneNumber = applicationUser.PhoneNumber ?? string.Empty;
                shoppingCartVM.OrderHeader.StreetAddress = applicationUser.Address ?? string.Empty;
                shoppingCartVM.OrderHeader.City = applicationUser.City ?? string.Empty;
            }

            return View(shoppingCartVM);
        }

        // ==========================================
        // 7. SUMMARY: ĐẶT HÀNG (POST)
        // ==========================================
        [HttpPost]
        [ActionName("Summary")]
        [ValidateAntiForgeryToken]
        public IActionResult SummaryPOST(ShoppingCartVM shoppingCartVM)
        {
            var userId = GetCurrentUserId();

            var shoppingCartList = _db.ShoppingCarts
                .Include(u => u.Book)
                .Where(u => u.ApplicationUserId == userId)
                .ToList();

            if (!shoppingCartList.Any())
            {
                TempData["error"] = "Giỏ hàng của bạn đang trống!";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra tồn kho trước khi tạo đơn
            foreach (var cart in shoppingCartList)
            {
                if (cart.Book == null || cart.Count > cart.Book.StockQuantity)
                {
                    TempData["error"] = $"Rất tiếc, cuốn sách \"{cart.Book?.Title ?? "Ấn phẩm"}\" hiện chỉ còn {cart.Book?.StockQuantity ?? 0} cuốn trong kho! Vui lòng điều chỉnh lại số lượng.";
                    return RedirectToAction(nameof(Index));
                }
            }

            decimal subTotal = 0;
            foreach (var cart in shoppingCartList)
            {
                cart.Price = cart.Book?.Price ?? 0;
                subTotal += (cart.Price * cart.Count);
            }

            // Kiểm tra và áp dụng mã giảm giá từ Session
            var sessionCoupon = HttpContext.Session.GetString(SD.SessionCoupon);
            decimal discountAmount = 0;
            Coupon? appliedCoupon = null;

            if (!string.IsNullOrEmpty(sessionCoupon))
            {
                var (coupon, discount, error) = ValidateAndCalculateCoupon(sessionCoupon, subTotal);
                if (error == null && coupon != null)
                {
                    appliedCoupon = coupon;
                    discountAmount = discount;
                }
                else
                {
                    HttpContext.Session.Remove(SD.SessionCoupon);
                }
            }

            decimal shippingFee = subTotal >= 250000 ? 0 : 30000;
            decimal finalTotal = Math.Max(0, subTotal + shippingFee - discountAmount);

            // Gán dữ liệu cho OrderHeader
            shoppingCartVM.OrderHeader.ApplicationUserId = userId;
            shoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
            shoppingCartVM.OrderHeader.OrderTotal = finalTotal;
            shoppingCartVM.OrderHeader.DiscountAmount = discountAmount;
            shoppingCartVM.OrderHeader.CouponCode = appliedCoupon?.Code;
            shoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
            shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;

            // Kiểm tra thông tin người nhận
            if (string.IsNullOrWhiteSpace(shoppingCartVM.OrderHeader.Name) ||
                string.IsNullOrWhiteSpace(shoppingCartVM.OrderHeader.PhoneNumber) ||
                string.IsNullOrWhiteSpace(shoppingCartVM.OrderHeader.StreetAddress) ||
                string.IsNullOrWhiteSpace(shoppingCartVM.OrderHeader.City))
            {
                TempData["error"] = "Vui lòng điền đầy đủ họ tên, số điện thoại, địa chỉ và thành phố nhận hàng!";
                shoppingCartVM.ShoppingCartList = shoppingCartList;
                shoppingCartVM.OrderTotal = subTotal;
                shoppingCartVM.DiscountAmount = discountAmount;
                shoppingCartVM.FinalTotal = finalTotal;
                shoppingCartVM.CouponCode = appliedCoupon?.Code;
                shoppingCartVM.AppliedCoupon = appliedCoupon;
                return View("Summary", shoppingCartVM);
            }

            _db.OrderHeaders.Add(shoppingCartVM.OrderHeader);
            _db.SaveChanges(); // Tạo Id cho OrderHeader

            // Cập nhật lượt dùng coupon nếu có
            if (appliedCoupon != null)
            {
                appliedCoupon.TimesUsed += 1;
                _db.Coupons.Update(appliedCoupon);
            }

            // Tạo các dòng OrderDetail và trừ tồn kho sách
            foreach (var cart in shoppingCartList)
            {
                var orderDetail = new OrderDetail
                {
                    OrderHeaderId = shoppingCartVM.OrderHeader.Id,
                    BookId = cart.BookId,
                    Count = cart.Count,
                    Price = cart.Book!.Price
                };
                _db.OrderDetails.Add(orderDetail);

                // Trừ số lượng trong kho
                cart.Book.StockQuantity -= cart.Count;
                _db.Books.Update(cart.Book);
            }

            // Xóa sạch giỏ hàng của user
            _db.ShoppingCarts.RemoveRange(shoppingCartList);
            _db.SaveChanges();

            // Cập nhật lại Session badge giỏ hàng về 0 và xóa Session Coupon
            HttpContext.Session.SetInt32(SD.SessionCart, 0);
            HttpContext.Session.Remove(SD.SessionCoupon);

            TempData["success"] = $"Đơn hàng #{shoppingCartVM.OrderHeader.Id} đã được đặt thành công!";
            return RedirectToAction(nameof(OrderConfirmation), new { id = shoppingCartVM.OrderHeader.Id });
        }

        // ==========================================
        // 8. ORDER CONFIRMATION: XÁC NHẬN THÀNH CÔNG (GET)
        // ==========================================
        [HttpGet]
        public IActionResult OrderConfirmation(int id)
        {
            var userId = GetCurrentUserId();
            var orderHeader = _db.OrderHeaders
                .FirstOrDefault(u => u.Id == id && (u.ApplicationUserId == userId || User.IsInRole(SD.Role_Admin)));

            if (orderHeader == null)
            {
                TempData["error"] = "Không tìm thấy đơn hàng yêu cầu!";
                return RedirectToAction("Index", "Home");
            }

            var orderDetails = _db.OrderDetails
                .Include(u => u.Book)
                .Where(u => u.OrderHeaderId == id)
                .ToList();

            var orderVM = new OrderVM
            {
                OrderHeader = orderHeader,
                OrderDetail = orderDetails
            };

            return View(orderVM);
        }

        // Helper: Lấy UserId hiện tại
        private string GetCurrentUserId()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity!;
            return claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        }
    }
}
