namespace GenerateCV.Models
{
    public class Users
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telephone { get; set; } = string.Empty;
        public DateTime Created_at { get; set; } = DateTime.UtcNow;
        public int DownloadCount { get; set; } = 0;  // CV indirme sayısı
        
        // Navigation properties
        public virtual ICollection<School> Schools { get; set; } = new List<School>();
        public virtual ICollection<Experience> Experiences { get; set; } = new List<Experience>();
    }
} 