using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebBanSach.Data;
using WebBanSach.Models;
using WebBanSach.Models.ViewModels;
using WebBanSach.Utility;

namespace WebBanSach.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _db = db;
        }

        // ==========================================
        // 1. REGISTER (ĐĂNG KÝ)
        // ==========================================
        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            var registerVM = new RegisterVM
            {
                RoleList = _roleManager.Roles.Select(x => x.Name).Select(i => new SelectListItem
                {
                    Text = i,
                    Value = i
                })
            };
            return View(registerVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM model, string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address,
                    City = model.City,
                    PostalCode = model.PostalCode,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Gán vai trò: nếu có chọn và hợp lệ thì gán, ngược lại mặc định là Customer
                    if (!string.IsNullOrEmpty(model.Role) && await _roleManager.RoleExistsAsync(model.Role))
                    {
                        await _userManager.AddToRoleAsync(user, model.Role);
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user, SD.Role_Customer);
                    }

                    // Tự động đăng nhập sau khi tạo tài khoản thành công
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    TempData["success"] = $"Đăng ký thành công! Chào mừng {user.FullName} đến với PageCraft.";

                    if (Url.IsLocalUrl(returnUrl))
                    {
                        return LocalRedirect(returnUrl);
                    }
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            model.RoleList = _roleManager.Roles.Select(x => x.Name).Select(i => new SelectListItem
            {
                Text = i,
                Value = i
            });
            ViewData["ReturnUrl"] = returnUrl;
            TempData["error"] = "Đăng ký không thành công, vui lòng kiểm tra lại thông tin!";
            return View(model);
        }

        // ==========================================
        // 2. LOGIN (ĐĂNG NHẬP)
        // ==========================================
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginVM { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM model, string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    model.Email,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    if (user != null)
                    {
                        int cartCount = _db.ShoppingCarts.Count(u => u.ApplicationUserId == user.Id);
                        HttpContext.Session.SetInt32(SD.SessionCart, cartCount);
                    }
                    TempData["success"] = $"Đăng nhập thành công! Chào mừng trở lại, {user?.FullName ?? model.Email}.";

                    if (Url.IsLocalUrl(returnUrl))
                    {
                        return LocalRedirect(returnUrl);
                    }
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác!");
                TempData["error"] = "Đăng nhập thất bại, vui lòng kiểm tra lại thông tin!";
            }

            return View(model);
        }

        // ==========================================
        // 3. LOGOUT (ĐĂNG XUẤT)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            TempData["success"] = "Bạn đã đăng xuất tài khoản thành công.";
            return RedirectToAction("Index", "Home");
        }

        // Hỗ trợ GET logout nếu người dùng click link trực tiếp
        [HttpGet]
        public async Task<IActionResult> LogoutGet()
        {
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            TempData["success"] = "Bạn đã đăng xuất tài khoản thành công.";
            return RedirectToAction("Index", "Home");
        }

        // ==========================================
        // 4. ACCESS DENIED (TỪ CHỐI TRUY CẬP)
        // ==========================================
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
