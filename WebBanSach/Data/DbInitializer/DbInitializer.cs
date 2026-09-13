using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebBanSach.Models;
using WebBanSach.Utility;

namespace WebBanSach.Data.DbInitializer
{
    public class DbInitializer : IDbInitializer
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;

        public DbInitializer(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
        }

        public void Initialize()
        {
            // 1. Áp dụng các migration chưa chạy nếu có
            try
            {
                if (_db.Database.GetPendingMigrations().Any())
                {
                    _db.Database.Migrate();
                }
            }
            catch (Exception)
            {
                // Bỏ qua nếu đã migrate
            }

            // 2. Tạo Roles nếu chưa tồn tại
            if (!_roleManager.RoleExistsAsync(SD.Role_Admin).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin)).GetAwaiter().GetResult();
            }
            if (!_roleManager.RoleExistsAsync(SD.Role_Customer).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Customer)).GetAwaiter().GetResult();
            }

            // 3. Tạo tài khoản Quản Trị Viên (Admin) mặc định
            var adminFromDb = _userManager.FindByEmailAsync("admin@pagecraft.com").GetAwaiter().GetResult();
            if (adminFromDb == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = "admin@pagecraft.com",
                    Email = "admin@pagecraft.com",
                    FullName = "Quản Trị Viên PageCraft",
                    PhoneNumber = "0988888888",
                    Address = "742 Botanical Way, Suite 4",
                    City = "Hồ Chí Minh",
                    PostalCode = "700000",
                    EmailConfirmed = true
                };

                var createResult = _userManager.CreateAsync(adminUser, "Admin@123456").GetAwaiter().GetResult();
                if (createResult.Succeeded)
                {
                    _userManager.AddToRoleAsync(adminUser, SD.Role_Admin).GetAwaiter().GetResult();
                }
            }

            // 4. Tạo tài khoản Độc Giả (Customer / User) mặc định
            var userFromDb = _userManager.FindByEmailAsync("user@pagecraft.com").GetAwaiter().GetResult();
            if (userFromDb == null)
            {
                var normalUser = new ApplicationUser
                {
                    UserName = "user@pagecraft.com",
                    Email = "user@pagecraft.com",
                    FullName = "Nguyễn Văn Độc Giả",
                    PhoneNumber = "0912345678",
                    Address = "123 Phố Sách Tràng Tiền",
                    City = "Hà Nội",
                    PostalCode = "100000",
                    EmailConfirmed = true
                };

                var createResult = _userManager.CreateAsync(normalUser, "User@123456").GetAwaiter().GetResult();
                if (createResult.Succeeded)
                {
                    _userManager.AddToRoleAsync(normalUser, SD.Role_Customer).GetAwaiter().GetResult();
                }
            }

            // 5. Tạo các mã giảm giá mẫu (Coupons) nếu chưa có
            if (!_db.Coupons.Any(c => c.Code == "READMORE"))
            {
                _db.Coupons.Add(new Coupon
                {
                    Code = "READMORE",
                    DiscountType = DiscountType.Percentage,
                    DiscountValue = 10,
                    MinOrderAmount = 100000,
                    StartDate = DateTime.UtcNow.AddDays(-1),
                    EndDate = DateTime.UtcNow.AddYears(1),
                    IsActive = true,
                    UsageLimit = 100,
                    TimesUsed = 0
                });
            }

            if (!_db.Coupons.Any(c => c.Code == "PAGECRAFT50"))
            {
                _db.Coupons.Add(new Coupon
                {
                    Code = "PAGECRAFT50",
                    DiscountType = DiscountType.FixedAmount,
                    DiscountValue = 50000,
                    MinOrderAmount = 200000,
                    StartDate = DateTime.UtcNow.AddDays(-1),
                    EndDate = DateTime.UtcNow.AddYears(1),
                    IsActive = true,
                    UsageLimit = 50,
                    TimesUsed = 0
                });
            }

            _db.SaveChanges();

            // 6. Seed Thể Loại (Categories) phong phú
            var catClassics = GetOrCreateCategory("Văn Học Cổ Điển", 1);
            var catPhilosophy = GetOrCreateCategory("Triết Học & Lịch Sử", 2);
            var catMystery = GetOrCreateCategory("Trinh Thám & Giật Gân", 3);
            var catSciFi = GetOrCreateCategory("Khoa Học & Viễn Tưởng", 4);
            var catPsychology = GetOrCreateCategory("Tâm Lý & Kỹ Năng Sống", 5);
            var catBusiness = GetOrCreateCategory("Kinh Doanh & Khởi Nghiệp", 6);
            var catContemporary = GetOrCreateCategory("Văn Học Đương Đại", 7);

            _db.SaveChanges();

            // Cập nhật thể loại cho 2 cuốn sách sẵn có
            var bookNhaGiaKim = _db.Books.FirstOrDefault(b => b.Title == "Nhà Giả Kim");
            if (bookNhaGiaKim != null)
            {
                bookNhaGiaKim.CategoryId = catPhilosophy.Id;
                bookNhaGiaKim.Description = "Câu chuyện ngụ ngôn triết lý đầy mê hoặc về hành trình theo đuổi vận mệnh của chàng trai chăn cừu Santiago, vượt qua sa mạc để tìm kho báu đích thực tại Kim Tự Tháp Ai Cập.";
                _db.Books.Update(bookNhaGiaKim);
            }

            var bookChienTranh = _db.Books.FirstOrDefault(b => b.Title == "Chiến Tranh và Hòa Bình");
            if (bookChienTranh != null)
            {
                bookChienTranh.CategoryId = catClassics.Id;
                bookChienTranh.Description = "Đại sử thi bất hủ của Lev Tolstoy tái hiện nước Nga đầu thế kỷ 19 qua cuộc xâm lược của Napoleon, số phận những gia tộc quý tộc và triết lý sâu sắc về tự do ý chí của con người.";
                _db.Books.Update(bookChienTranh);
            }

            // 7. Seed thêm các tác phẩm sách tiêu biểu đa dạng thể loại
            var booksToSeed = new List<Book>
            {
                new Book
                {
                    Title = "Tội Ác và Trừng Phạt",
                    Author = "Fyodor Dostoevsky",
                    ISBN = "978-604-1-12345-1",
                    ListPrice = 220000,
                    Price = 175000,
                    StockQuantity = 45,
                    ImageUrl = "/images/books/cover_crime_punishment.jpg",
                    CategoryId = catClassics.Id,
                    CreatedDate = DateTime.Now.AddDays(-20),
                    Description = "Kiệt tác văn học Nga thế kỷ 19 khắc họa sâu sắc cuộc giằng xé tâm lý và sám hối của chàng cựu sinh viên nghèo Raskolnikov sau quyết định định mệnh thử thách luân lý xã hội."
                },
                new Book
                {
                    Title = "Đại Gia Gatsby",
                    Author = "F. Scott Fitzgerald",
                    ISBN = "978-604-1-67890-2",
                    ListPrice = 160000,
                    Price = 128000,
                    StockQuantity = 60,
                    ImageUrl = "/images/books/cover_great_gatsby.jpg",
                    CategoryId = catClassics.Id,
                    CreatedDate = DateTime.Now.AddDays(-18),
                    Description = "Bản tình ca hoài niệm lộng lẫy và bi kịch về Giấc Mơ Mỹ trong thập niên 20 rực rỡ, ánh đèn xanh xa xăm nơi bến tàu và mối tình si tuyệt vọng của triệu phú bí ẩn Jay Gatsby."
                },
                new Book
                {
                    Title = "Hoàng Tử Bé",
                    Author = "Antoine de Saint-Exupéry",
                    ISBN = "978-604-2-99881-0",
                    ListPrice = 110000,
                    Price = 85000,
                    StockQuantity = 80,
                    ImageUrl = "/images/books/cover_little_prince.jpg",
                    CategoryId = catClassics.Id,
                    CreatedDate = DateTime.Now.AddDays(-15),
                    Description = "Tác phẩm thơ mộng bất hủ về cậu bé tóc vàng đến từ tiểu tinh cầu B-612, bài học về tình yêu thương với đóa hồng duy nhất và nhìn nhận cuộc đời bằng trái tim."
                },
                new Book
                {
                    Title = "Sapiens: Lược Sử Loài Người",
                    Author = "Yuval Noah Harari",
                    ISBN = "978-604-5-33441-2",
                    ListPrice = 260000,
                    Price = 215000,
                    StockQuantity = 50,
                    ImageUrl = "/images/books/cover_sapiens.jpg",
                    CategoryId = catPhilosophy.Id,
                    CreatedDate = DateTime.Now.AddDays(-14),
                    Description = "Hành trình vĩ mô khám phá sự tiến hóa của giống loài Homo Sapiens từ động vật không có gì đặc biệt trở thành kẻ thống trị hành tinh qua ba cuộc cách mạng: Nhận thức, Nông nghiệp và Khoa học."
                },
                new Book
                {
                    Title = "Sherlock Holmes Toàn Tập",
                    Author = "Arthur Conan Doyle",
                    ISBN = "978-604-7-88992-3",
                    ListPrice = 320000,
                    Price = 265000,
                    StockQuantity = 35,
                    ImageUrl = "/images/books/cover_sherlock_holmes.jpg",
                    CategoryId = catMystery.Id,
                    CreatedDate = DateTime.Now.AddDays(-12),
                    Description = "Tuyển tập những vụ kỳ án đỉnh cao dưới ngòi bút bậc thầy trinh thám thế giới, nghệ thuật suy luận quan sát phi thường của thám tử số 221B phố Baker cùng bác sĩ Watson."
                },
                new Book
                {
                    Title = "Dune - Xứ Cát",
                    Author = "Frank Herbert",
                    ISBN = "978-604-3-44556-7",
                    ListPrice = 280000,
                    Price = 229000,
                    StockQuantity = 40,
                    ImageUrl = "/images/books/cover_dune.jpg",
                    CategoryId = catSciFi.Id,
                    CreatedDate = DateTime.Now.AddDays(-10),
                    Description = "Bộ sử thi khoa học viễn tưởng hoành tráng bậc nhất về hành tinh sa mạc Arrakis nguy hiểm nhưng chứa đựng mỏ hương dược vô giá, cùng hành trình trưởng thành của người lãnh tụ Paul Atreides."
                },
                new Book
                {
                    Title = "1984",
                    Author = "George Orwell",
                    ISBN = "978-604-8-12123-4",
                    ListPrice = 150000,
                    Price = 119000,
                    StockQuantity = 55,
                    ImageUrl = "/images/books/cover_1984.jpg",
                    CategoryId = catSciFi.Id,
                    CreatedDate = DateTime.Now.AddDays(-8),
                    Description = "Kiệt tác phản địa đàng kinh điển dự báo về bộ máy kiểm soát tư tưởng toàn năng Big Brother, ngôn ngữ Tân ngữ và sự kháng cự kiên cường bảo vệ ký ức tự do của Winston Smith."
                },
                new Book
                {
                    Title = "Tư Duy Nhanh và Chậm",
                    Author = "Daniel Kahneman",
                    ISBN = "978-604-9-77665-8",
                    ListPrice = 250000,
                    Price = 195000,
                    StockQuantity = 65,
                    ImageUrl = "/images/books/cover_thinking_fast_slow.jpg",
                    CategoryId = catPsychology.Id,
                    CreatedDate = DateTime.Now.AddDays(-6),
                    Description = "Công trình đột phá của nhà tâm lý học đoạt giải Nobel Kinh tế giải mã hai hệ thống tư duy: Hệ thống 1 phản ứng nhanh trực giác và Hệ thống 2 suy xét chậm chạp, cẩn trọng."
                },
                new Book
                {
                    Title = "Tiểu Sử Steve Jobs",
                    Author = "Walter Isaacson",
                    ISBN = "978-604-6-55443-9",
                    ListPrice = 350000,
                    Price = 289000,
                    StockQuantity = 30,
                    ImageUrl = "/images/books/cover_steve_jobs.jpg",
                    CategoryId = catBusiness.Id,
                    CreatedDate = DateTime.Now.AddDays(-4),
                    Description = "Cuốn tiểu sử chính thức và chân thực nhất về cuộc đời, cá tính mãnh liệt, đam mê hoàn mỹ và triết lý giao thoa giữa công nghệ với nhân văn của người đồng sáng lập Apple."
                },
                new Book
                {
                    Title = "Rừng Na Uy",
                    Author = "Haruki Murakami",
                    ISBN = "978-604-4-99887-1",
                    ListPrice = 180000,
                    Price = 145000,
                    StockQuantity = 70,
                    ImageUrl = "/images/books/cover_norwegian_wood.jpg",
                    CategoryId = catContemporary.Id,
                    CreatedDate = DateTime.Now.AddDays(-2),
                    Description = "Bản tình ca da diết, trầm lặng và đầy chất thơ về tình yêu, sự cô độc và nỗi mất mát của tuổi trẻ Nhật Bản những năm cuối thập niên 1960."
                }
            };

            foreach (var book in booksToSeed)
            {
                if (!_db.Books.Any(b => b.Title == book.Title))
                {
                    _db.Books.Add(book);
                }
            }

            _db.SaveChanges();
        }

        private Category GetOrCreateCategory(string name, int displayOrder)
        {
            var category = _db.Categories.FirstOrDefault(c => c.Name == name);
            if (category == null)
            {
                category = new Category
                {
                    Name = name,
                    DisplayOrder = displayOrder,
                    CreatedDateTime = DateTime.Now
                };
                _db.Categories.Add(category);
                _db.SaveChanges();
            }
            return category;
        }
    }
}
