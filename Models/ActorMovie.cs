using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Fall2024_Assignment3_msingh9.Models
{
    public class ActorMovie
    {
        [Key]
        public int ActorMovieID { get; set; }

        [ForeignKey("Actor")]
        public int ActorID { get; set; }

        public Actor? Actor { get; set; }

        [ForeignKey("Movie")]
        public int MovieID { get; set; }

        public Movie? Movie { get; set; }

    }
}
