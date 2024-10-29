using Microsoft.VisualBasic;
using System.ComponentModel.DataAnnotations;
namespace Fall2024_Assignment3_msingh9.Models
{
    public class Movie
    {
        [Key]
        public int MovieID { get; set; }
        public string Name { get; set; }
        public string IMDB { get; set; }

        public string Genre { get; set; }
        public int Year { get; set; }
        
        public byte[]? Photo { get; set; }

        public List<string> AIReviews { get; set; } = new List<string>();
        public double AverageSentiment { get; set; }
    }
}
