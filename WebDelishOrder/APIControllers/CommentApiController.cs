using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebDelishOrder.Models;

[Route("api/[controller]")]
[ApiController]
public class CommentApiController : ControllerBase
{
    private readonly AppDbContext _context;

    public CommentApiController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/CommentApi
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Comment>>> GetComments()
    {
        var comments = await _context.Comments
    .Join(
        _context.Products,
        comment => comment.ProductId,
        product => product.Id,
        (comment, product) => new
        {
            Comment = comment,
            Product = product
        }
    )
    .Join(
        _context.Customers, // Join with the Customers table
        combined => combined.Comment.AccountEmail,
        customer => customer.AccountEmail, // Assuming this is the email field in Customers table
        (combined, customer) => new
        {
            AccountEmail = combined.Comment.AccountEmail,
            ProductId = combined.Product.Id,
            RegTime = combined.Comment.RegTime,
            Descript = combined.Comment.Descript,
            Evaluate = combined.Comment.Evaluate,
            CustomerName = customer.Name != null ? customer.Name : string.Empty, // Handle null name
            CustomerAvatar = customer.Avatar != null ? customer.Avatar : string.Empty // Handle null avatar
        }
    )
    .ToListAsync();

        return Ok(comments);
    }

    // GET: api/CommentApi/product/{id}
    [HttpGet("product/{id}")]
    public async Task<ActionResult<IEnumerable<object>>> GetCommentsByProductId(int id)
    {
        // Kiểm tra sản phẩm có tồn tại không
        var productExists = await _context.Products.AnyAsync(p => p.Id == id);
        if (!productExists)
        {
            return NotFound($"Không tìm thấy sản phẩm có ID = {id}");
        }

        var comments = await _context.Comments
            .Where(c => c.ProductId == id) // Lọc comments theo Product ID
            .Join(
                _context.Products,
                comment => comment.ProductId,
                product => product.Id,
                (comment, product) => new
                {
                    Comment = comment,
                    Product = product
                }
            )
            .Join(
                _context.Customers,
                combined => combined.Comment.AccountEmail,
                customer => customer.AccountEmail,
                (combined, customer) => new
                {
                    AccountEmail = combined.Comment.AccountEmail,
                    ProductId = combined.Product.Id,
                    RegTime = combined.Comment.RegTime,
                    Descript = combined.Comment.Descript,
                    Evaluate = combined.Comment.Evaluate,
                    CustomerName = customer.Name ?? string.Empty, // Sử dụng null coalescing operator
                    CustomerAvatar = customer.Avatar ?? string.Empty // Sử dụng null coalescing operator
                }
            )
            .ToListAsync();

        // Nếu không có comment nào, trả về danh sách rỗng
        if (!comments.Any())
        {
            return Ok(new List<object>()); // Trả về mảng rỗng thay vì null
        }

        return Ok(comments);
    }



    // GET: api/CommentApi/{accountEmail}

    [HttpGet("{accountEmail}")]
    public async Task<ActionResult<Comment>> GetComment(string accountEmail)
    {
        var comment = await _context.Comments
            .Include(c => c.AccountEmailNavigation) // Include Account relationship
            .Include(c => c.Product) // Include Product relationship
            .FirstOrDefaultAsync(c => c.AccountEmail == accountEmail);

        if (comment == null)
        {
            return NotFound();
        }

        return comment;
    }

    // POST: api/CommentApi
    // POST: api/CommentApi
    [HttpPost]
    public async Task<ActionResult<Comment>> addComment([FromBody] CommentDTO dto)
    {
        if (string.IsNullOrEmpty(dto.AccountEmail))
        {
            return BadRequest("AccountEmail is required.");
        }

        if (dto.ProductId == null)
        {
            return BadRequest("ProductId is required.");
        }

        if (string.IsNullOrEmpty(dto.Descript))
        {
            return BadRequest("Comment description is required.");
        }

        if (dto.Evaluate == null || dto.Evaluate < 1 || dto.Evaluate > 5)
        {
            return BadRequest("Evaluate must be between 1 and 5.");
        }

        var productExists = await _context.Products.AnyAsync(p => p.Id == dto.ProductId);
        if (!productExists)
        {
            return NotFound($"Product with ID {dto.ProductId} does not exist.");
        }

        var accountExists = await _context.Accounts.AnyAsync(a => a.Email == dto.AccountEmail);
        if (!accountExists)
        {
            return NotFound($"Account with email {dto.AccountEmail} does not exist.");
        }

        var comment = new Comment
        {
            AccountEmail = dto.AccountEmail,
            ProductId = dto.ProductId,
            RegTime = DateTime.UtcNow,
            Descript = dto.Descript,
            Evaluate = dto.Evaluate,
            
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetComment", new { id = comment.ProductId, accountEmail = comment.AccountEmail }, comment);
    }
    // PUT: api/CommentApi/{accountEmail}
    [HttpPut("{accountEmail}")]
    public async Task<IActionResult> PutComment(string accountEmail, Comment comment)
    {
        if (accountEmail != comment.AccountEmail)
        {
            return BadRequest("AccountEmail không khớp.");
        }

        _context.Entry(comment).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!CommentExists(accountEmail))
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

    // DELETE: api/CommentApi/{accountEmail}
    [HttpDelete("{accountEmail}")]
    public async Task<IActionResult> DeleteComment(string accountEmail)
    {
        var comment = await _context.Comments.FirstOrDefaultAsync(c => c.AccountEmail == accountEmail);
        if (comment == null)
        {
            return NotFound();
        }

        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool CommentExists(string accountEmail)
    {
        return _context.Comments.Any(c => c.AccountEmail == accountEmail);
    }
}
