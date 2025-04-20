namespace WebDelishOrder.Models
{
    public class CommentDTO
    {
        public string AccountEmail { get; set; } = null!;
        public int? ProductId { get; set; }
        public string? Descript { get; set; }
        public int? Evaluate { get; set; }
    }
}
