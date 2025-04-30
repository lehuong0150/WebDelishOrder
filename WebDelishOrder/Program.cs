using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WebDelishOrder.Controllers;
using WebDelishOrder.Models;
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

// Thêm SignalR
builder.Services.AddSignalR();

// Thêm Order Notification Service - đăng ký singleton để duy trì trạng thái
//builder.Services.AddSingleton<OrderNotificationService>();

// Đăng ký Order Hub Service với scope
//builder.Services.AddScoped<OrderNotificationService>();

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
    serverOptions.Listen(System.Net.IPAddress.Parse("192.168.1.8"), 7010); // HTTP
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

// Cấu hình Endpoints
app.UseEndpoints(endpoints =>
{
    // Map SignalR hub
   // endpoints.MapHub<OrderNotificationHub>("/orderNotificationHub");

    // Map controller routes
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Account}/{action=Login}/{id?}");
});

app.Run();
