namespace Fall2024_Assignment3_msingh9.Models
{
    public class MovieDetailsViewModel
    {
        public Movie Movie { get; set; }
        public IEnumerable<Actor> Actors { get; set; }

        public MovieDetailsViewModel(Movie movie, IEnumerable<Actor> actors)
        {
            Movie = movie;
            Actors = actors;
        }
    }
}
