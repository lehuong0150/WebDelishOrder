using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WebDelishOrder.Controllers;
using WebDelishOrder.Hubs;
using WebDelishOrder.Models;
using WebDelishOrder.Service;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình AppDbContext sử dụng MySQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
    new MySqlServerVersion(new Version(8, 0, 21)))
    .EnableSensitiveDataLogging()
        .EnableDetailedErrors());

// Cấu hình Authentication và Cookie
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.LoginPath = "/Account/Login";  // Đường dẫn đến trang đăng nhập
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);  // Thời gian sống của cookie
    });

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();//// Thêm SignalR
builder.Services.AddScoped<OrderHubService>();
// Cấu hình CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Listen(System.Net.IPAddress.Parse("172.19.201.61"), 7010); // HTTP
});

var app = builder.Build();
// Bật middleware CORS
app.UseCors("AllowAllOrigins");
// Cấu hình middleware
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Kích hoạt Authentication Middleware để xử lý cookie
app.UseAuthentication();

// Kích hoạt Authorization Middleware để kiểm tra quyền truy cập
app.UseAuthorization();
app.MapHub<OrderHub>("/orderHub");


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");  // Điều hướng đến trang Login khi cần

app.Run();
