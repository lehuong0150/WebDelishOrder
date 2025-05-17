using System.Text.Json.Serialization;
using DinkToPdf.Contracts;
using DinkToPdf;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WebDelishOrder.Controllers;
using WebDelishOrder.Models;
using System.Runtime.InteropServices;
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

// Thêm SignalR
// Thêm dịch vụ SignalR
builder.Services.AddControllersWithViews();
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
    serverOptions.Listen(System.Net.IPAddress.Parse("192.168.1.70"), 7010); // HTTP
});

builder.Services.AddControllers().AddJsonOptions(x =>
{
    x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});


FirebaseApp.Create(new AppOptions()
{
    Credential = GoogleCredential.FromFile(@"D:\DoAn\Web\WebDelishOrder\Firebase\firebase-adminsdk.json")
});
// Đường dẫn đến tệp libwkhtmltox.dll
var dllPath = Path.Combine(AppContext.BaseDirectory, "libwkhtmltox.dll");
if (!File.Exists(dllPath))
{
    throw new FileNotFoundException("Không tìm thấy tệp libwkhtmltox.dll tại đường dẫn: " + dllPath);
}

// Tải thư viện unmanaged
var context = new CustomAssemblyLoadContext();
context.LoadUnmanagedLibrary(dllPath);

// Đăng ký dịch vụ DinkToPdf
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));


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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// Cấu hình SignalR Hub
app.MapHub<OrderHub>("/orderHub");

app.Run();
