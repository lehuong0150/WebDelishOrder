using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebDelishOrder.Models;
using Microsoft.EntityFrameworkCore;

namespace WebDelishOrder.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }
       

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra thông tin đăng nhập trong cơ sở dữ liệu
                var isValidUser = await _context.Accounts
                    .Where(a => a.Email == model.Email && a.Password == model.Password)
                    .FirstOrDefaultAsync();

                if (isValidUser != null)
                {
                    // Nếu thông tin đăng nhập đúng, tạo Claim và đăng nhập
                    var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, model.Email)
                };

                    var identity = new ClaimsIdentity(claims, "CookieAuth");
                    var principal = new ClaimsPrincipal(identity);

                    // Đăng nhập bằng cookie
                    await HttpContext.SignInAsync("CookieAuth", principal);

                    return RedirectToAction("Index", "Home"); // Điều hướng tới trang chính
                }

                // Nếu thông tin đăng nhập sai
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
            }

            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            // Đăng xuất người dùng
            await HttpContext.SignOutAsync("CookieAuth");

            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }

}
