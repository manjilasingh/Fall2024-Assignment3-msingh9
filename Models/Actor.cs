using System.ComponentModel.DataAnnotations;

namespace Fall2024_Assignment3_msingh9.Models
{
    public class Actor
    {
        [Key]
        public int ActorID { get; set; }
        public string Name { get; set; }
        public string Gender { get; set; }
        public int Age { get; set; }
        public string IMDB { get; set; }

        public byte[]? Photo { get; set; }

        public List<string> AITweets { get; set; } = new List<string>();
        public double AverageSentiment { get; set; }
    }
}
