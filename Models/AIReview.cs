namespace Fall2024_Assignment3_msingh9.Models
{
    public class AIReview
    {
        public int Id { get; set; }
        public string ReviewText { get; set; }
        public double SentimentScore { get; set; }

        // Foreign keys
        public int? ActorId { get; set; }
        public int? MovieId { get; set; }

        // Navigation properties
        public Actor? Actor { get; set; }
        public Movie? Movie { get; set; }
    }
}
