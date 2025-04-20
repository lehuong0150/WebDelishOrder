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
            if (id != customer.Id)
            {
                return BadRequest();
            }
            _context.Entry(customer).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
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
    }

}
