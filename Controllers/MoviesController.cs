using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Fall2024_Assignment3_msingh9.Data;
using Fall2024_Assignment3_msingh9.Models;
using System.Numerics;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using VaderSharp2;
using Humanizer.Localisation;
using System.Configuration;

namespace Fall2024_Assignment3_msingh9.Controllers
{
    public class MoviesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ApiKeyCredential _apiCredential;
        private readonly string _apiEndpoint;
        private readonly string _aiDeployment;
        public MoviesController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            var ApiKey = _configuration["AZURE_OPENAI_API_KEY"] ?? throw new Exception("AZURE_OPENAI_API_KEY not found in configuration");
            _apiEndpoint = "https://fall2024-msingh9-openai.openai.azure.com/";
            _aiDeployment = "gpt-35-turbo";
            _apiCredential = new ApiKeyCredential(ApiKey);
        }
        public class TweetWithSentiment
        {
            public string? Tweet { get; set; }
            public double Sentiment { get; set; }
        }

        private async Task<List<TweetWithSentiment>> GenerateMovieReviews(string movieName, int year)
        {
            var tweets = new List<TweetWithSentiment>();
            var analyzer = new SentimentIntensityAnalyzer();

            var chatClient = new AzureOpenAIClient(new Uri(_apiEndpoint), _apiCredential)
                .GetChatClient(_aiDeployment);

            var messages = new ChatMessage[]
            {
                 new SystemChatMessage("You are an experienced film critic with deep knowledge of cinema history and different genres. Generate authentic, thoughtful movie reviews that reflect the cinema standards and cultural context of the specified release year."),
                 new UserChatMessage($"Generate 10 different movie reviews for {movieName} movie released in {year}. Include a mix of positive and negative reviews, focusing on aspects like cinematography, plot, acting, special effects, and cultural impact. Consider the technical and artistic capabilities of that era. Keep each review under 100 words and make them feel authentic to the time period. Format each review as a separate line. Reviews should analyze the artistic and technical merits of the films while considering the standards and expectations of {year} audiences.")


             };

            var result = await chatClient.CompleteChatAsync(messages);
            var tweetTexts = result.Value.Content[0].Text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var reviewText in tweetTexts.Take(10))
            {
                var sentiment = analyzer.PolarityScores(reviewText);
                tweets.Add(new TweetWithSentiment
                {
                    Tweet = reviewText.Trim(),
                    Sentiment = sentiment.Compound
                });
            }

            return tweets;
        }

        public async Task<IActionResult> GetMoviePhoto(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie.FindAsync(id);
            if (movie == null && movie.Photo == null)
            {
                return NotFound();
            }

            var data = movie.Photo;
            return File(data, "image/jpg");
        }

        // GET: Movies
        public async Task<IActionResult> Index()
        {
            return View(await _context.Movie.ToListAsync());
        }

        // GET: Movies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie
                .FirstOrDefaultAsync(m => m.MovieID == id);
            if (movie == null)
            {
                return NotFound();
            }
            var actors = await _context.ActorMovie
               .Include(cs => cs.Actor)
               .Where(cs => cs.MovieID == movie.MovieID)
               .Select(cs => cs.Actor)
               .ToListAsync();
            if (TempData["HasNewReviews"]?.ToString() == "true")
            {
                var analyzer = new SentimentIntensityAnalyzer();
                var reviews = movie.AIReviews.Select(tweet => new TweetWithSentiment
                {
                    Tweet = tweet,
                    Sentiment = analyzer.PolarityScores(tweet).Compound
                }).ToList();

                ViewData["Reviews"] = reviews;
            }
            // For existing actors, calculate sentiments from stored tweets
            else if (movie.AIReviews?.Any() == true)
            {
                var analyzer = new SentimentIntensityAnalyzer();
                var reviews = movie.AIReviews.Select(tweet => new TweetWithSentiment
                {
                    Tweet = tweet,
                    Sentiment = analyzer.PolarityScores(tweet).Compound
                }).ToList();

                ViewData["Reviews"] = reviews;
            }

            var vm = new MovieDetailsViewModel(movie, actors);

            return View(vm);
        }

        // GET: Movies/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Movies/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MovieID,Name,IMDB,Genre,Year,AIReviews,AverageSentiment")] Movie movie, IFormFile? photo)
        {
            if (ModelState.IsValid)
            {
                if (photo != null && photo!.Length > 0)
                {
                    using MemoryStream memoryStream = new MemoryStream();
                    photo.CopyTo(memoryStream);
                    movie.Photo = memoryStream.ToArray();
                }
                var reviewsWithSentiment = await GenerateMovieReviews(movie.Name, movie.Year );

                // Store reviews in AITweets
                movie.AIReviews= reviewsWithSentiment.Select(r => r.Tweet).ToList();

                // Calculate and store average sentiment
                movie.AverageSentiment = reviewsWithSentiment.Average(r => r.Sentiment);
                _context.Add(movie);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(movie);
        }

        // GET: Movies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie.FindAsync(id);
            if (movie == null)
            {
                return NotFound();
            }
            return View(movie);
        }

        // POST: Movies/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MovieID,Name,IMDB,Genre,Year,AIReviews,AverageSentiment")] Movie movie, IFormFile? photo)
        {
            if (id != movie.MovieID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingMovie = await _context.Movie
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.MovieID == id);
                    if (existingMovie == null)
                    {
                        return NotFound();
                    }

                    // Keep existing photo if no new one uploaded
                    if (photo == null || photo.Length == 0)
                    {
                        movie.Photo = existingMovie.Photo;
                    }
                    else
                    {
                        // Update with new photo
                        using MemoryStream memoryStream = new MemoryStream();
                        photo.CopyTo(memoryStream);
                        movie.Photo = memoryStream.ToArray();
                    }
                    _context.Update(movie);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MovieExists(movie.MovieID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(movie);
        }

        // GET: Movies/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie
                .FirstOrDefaultAsync(m => m.MovieID == id);
            if (movie == null)
            {
                return NotFound();
            }

            return View(movie);
        }

        // POST: Movies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movie = await _context.Movie.FindAsync(id);
            if (movie != null)
            {
                _context.Movie.Remove(movie);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MovieExists(int id)
        {
            return _context.Movie.Any(e => e.MovieID == id);
        }
    }
}
