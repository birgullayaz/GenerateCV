namespace GenerateCV.Models
{
    public class School
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? FinishDate { get; set; }
        
        public int UserId { get; set; }
        public virtual Users? Users { get; set; }
     
    }
} 