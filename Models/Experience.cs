namespace GenerateCV.Models
{
    public class Experience
    {
        public int Id { get; set; }
        public string Company { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public int UserId { get; set; }
        public virtual Users? Users { get; set; }
    }
} 