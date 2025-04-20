using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebDelishOrder.Models;

[Route("api/[controller]")]
[ApiController]
public class AccountApiController : ControllerBase
{
    private readonly AppDbContext _context;

    public AccountApiController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Account>>> GetAccounts()
    {
        var accounts = await _context.Accounts
       .Select(a => new
       {
           Email = a.Email,
           Password = a.Password,
           Fullname = a.Fullname
       })
       .ToListAsync();

        return Ok(accounts);
    }

    [HttpGet("{email}")]
    public async Task<ActionResult<Account>> GetAccount(string email)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == email);
        if (account == null)
        {
            return NotFound();
        }
        return account;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        // Kiểm tra tài khoản có tồn tại không
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == loginRequest.Email);

        if (account == null)
        {
            // Nếu email không tồn tại, trả về lỗi
            return BadRequest(new { error = "Email không tồn tại." });
        }

        // Kiểm tra mật khẩu
        if (account.Password != loginRequest.Password)
        {
            // Nếu mật khẩu sai, trả về lỗi
            return BadRequest(new { error = "Sai mật khẩu." });
        }

        // Nếu đúng, trả về token hoặc thông tin xác thực khác
        // Giả sử sử dụng JWT token, hoặc có thể chỉ trả về thông tin đơn giản.
        var token = "dummy-jwt-token"; // Bạn có thể tạo JWT thực tế ở đây

        return Ok(new { token = token, message = "Đăng nhập thành công.", fullname = account.Fullname });
    }


    [HttpPost("register")]
    public async Task<ActionResult<Account>> PostAccount(Account account)
    {
        // Kiểm tra tài khoản đã tồn tại hay chưa
        if (await _context.Accounts.AnyAsync(a => a.Email == account.Email))
        {
            return BadRequest(new { error = "Email đã tồn tại." });
        }

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetAccount", new { email = account.Email }, account);
    }


    [HttpPut("{email}")]
    public async Task<IActionResult> PutAccount(string email, Account account)
    {
        if (email != account.Email)
        {
            return BadRequest();
        }

        _context.Entry(account).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!AccountExists(email))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }
        return NoContent();
    }

    [HttpDelete("{email}")]
    public async Task<IActionResult> DeleteAccount(string email)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == email);
        if (account == null)
        {
            return NotFound();
        }

        _context.Accounts.Remove(account);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool AccountExists(string email)
    {
        return _context.Accounts.Any(e => e.Email == email);
    }
}
