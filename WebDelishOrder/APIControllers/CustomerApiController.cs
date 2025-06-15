using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebDelishOrder.Models;

namespace WebDelishOrder.APIControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CustomerApiController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<Customer>> getCustomerInformation(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email không được để trống");
            }

            var customer = await _context.Customers
                .Where(c => c.AccountEmail == email)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Phone,
                    Avatar = c.Avatar ?? "default-avatar-url.jpg",
                    c.Address,
                    c.Gender,
                    c.Birthdate,
                    c.AccountEmail
                })
                .FirstOrDefaultAsync();

            if (customer == null)
            {
                return NotFound($"Không tìm thấy khách hàng với email: {email}");
            }

            return Ok(customer);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Customer>> GetCustomer(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }
            return customer;
        }

        [HttpPost]
        public async Task<ActionResult<Customer>> PostCustomer(Customer customer)
        {
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetCustomer", new { id = customer.Id }, customer);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutCustomer(int id, Customer customer)
        {
            try
            {
                if (id != customer.Id)
                {
                    return BadRequest(new { message = "ID không khớp" });
                }

                var existingCustomer = await _context.Customers.FindAsync(id);
                if (existingCustomer == null)
                {
                    return NotFound(new { message = "Không tìm thấy khách hàng" });
                }

                // Cập nhật thông tin cá nhân
                existingCustomer.Name = customer.Name;
                existingCustomer.Phone = customer.Phone;
                existingCustomer.Address = customer.Address;
                existingCustomer.AccountEmail = customer.AccountEmail;
                existingCustomer.Birthdate = customer.Birthdate;
                existingCustomer.Gender = customer.Gender;
                existingCustomer.Avatar = customer.Avatar;

                await _context.SaveChangesAsync();
                return Ok(new { message = "Cập nhật thành công", customer = existingCustomer });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi: " + ex.Message });
            }
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        [HttpPost("create")]
        public async Task<IActionResult> CreateCustomer([FromBody] Customer customer)
        {
            if (customer == null || string.IsNullOrEmpty(customer.AccountEmail))
            {
                return BadRequest(new { message = "Dữ liệu khách hàng không hợp lệ hoặc thiếu email." });
            }

            // Kiểm tra email đã tồn tại chưa
            var existingCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.AccountEmail == customer.AccountEmail);

            if (existingCustomer != null)
            {
                return Conflict(new { message = "Email đã tồn tại trong hệ thống." });
            }

            // Gán avatar mặc định nếu chưa có
            if (string.IsNullOrEmpty(customer.Avatar))
            {
                customer.Avatar = "default-avatar-url.jpg";
            }

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customer);
        }

    }

}
