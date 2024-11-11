using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Fall2024_Assignment3_msingh9.Data;
using Fall2024_Assignment3_msingh9.Models;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using VaderSharp2;
using System.ClientModel;

namespace Fall2024_Assignment3_msingh9.Controllers
{
    public class ActorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ApiKeyCredential _apiCredential;
        private readonly string _apiEndpoint;
        private readonly string _aiDeployment;
        public ActorsController(ApplicationDbContext context, IConfiguration configuration)
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

        private async Task<List<TweetWithSentiment>> GenerateMovieReviews(string actorName)
        {
            var tweets = new List<TweetWithSentiment>();
            var analyzer = new SentimentIntensityAnalyzer();

            var chatClient = new AzureOpenAIClient(new Uri(_apiEndpoint), _apiCredential)
                .GetChatClient(_aiDeployment);

            var messages = new ChatMessage[]
            {
                new SystemChatMessage("You are a social media enthusiast who follows celebrities and entertainment news. Generate realistic tweets about actors that could appear on social media."),
                new UserChatMessage($"Generate 10 different tweets about {actorName}. Include a mix of comments about their recent work, public appearances, social media activity, and general fan reactions. Keep each tweet under 280 characters and make them feel authentic. Format each tweet as a separate line.")
    
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


        public async Task<IActionResult> GetActorPhoto(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var actor = await _context.Actor.FindAsync(id);
            if (actor == null || actor.Photo == null)
            {
                return NotFound();
            }

            var data = actor.Photo;
            return File(data, "image/jpg");
        }
        // GET: Actors
        public async Task<IActionResult> Index()
        {
            return View(await _context.Actor.ToListAsync());
        }

        // GET: Actors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var actor = await _context.Actor
                .FirstOrDefaultAsync(m => m.ActorID == id);
            if (actor == null)
            {
                return NotFound();
            }
            var movies = await _context.ActorMovie
               .Include(cs => cs.Movie)
               .Where(cs => cs.ActorID == actor.ActorID)
               .Select(cs => cs.Movie)
               .ToListAsync();
            if (TempData["HasNewReviews"]?.ToString() == "true")
            {
                var analyzer = new SentimentIntensityAnalyzer();
                var reviews = actor.AITweets.Select(tweet => new TweetWithSentiment
                {
                    Tweet = tweet,
                    Sentiment = analyzer.PolarityScores(tweet).Compound
                }).ToList();

                ViewData["Reviews"] = reviews;
            }
            // For existing actors, calculate sentiments from stored tweets
            else if (actor.AITweets?.Any() == true)
            {
                var analyzer = new SentimentIntensityAnalyzer();
                var reviews = actor.AITweets.Select(tweet => new TweetWithSentiment
                {
                    Tweet = tweet,
                    Sentiment = analyzer.PolarityScores(tweet).Compound
                }).ToList();

                ViewData["Reviews"] = reviews;
            }

            var vm = new ActorDetailsViewModel(actor, movies);


            return View(vm);

        }

        // GET: Actors/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Actors/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ActorID,Name,Gender,Age,IMDB,AITweets,AverageSentiment")] Actor actor, IFormFile? photo)
        {
            if (ModelState.IsValid)
            {
                if (photo != null && photo!.Length > 0)
                {
                    using MemoryStream memoryStream = new MemoryStream();
                    photo.CopyTo(memoryStream);
                    actor.Photo = memoryStream.ToArray();
                }
                var reviewsWithSentiment = await GenerateMovieReviews(actor.Name);

                // Store reviews in AITweets
                actor.AITweets = reviewsWithSentiment.Select(r => r.Tweet).ToList();

                // Calculate and store average sentiment
                actor.AverageSentiment = reviewsWithSentiment.Average(r => r.Sentiment);
                _context.Add(actor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(actor);
        }

        // GET: Actors/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var actor = await _context.Actor.FindAsync(id);
            if (actor == null)
            {
                return NotFound();
            }
            return View(actor);
        }

        // POST: Actors/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ActorID,Name,Gender,Age,IMDB,AITweets,AverageSentiment")] Actor actor, IFormFile? photo)
        {
            if (id != actor.ActorID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingActor = await _context.Actor
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.ActorID == id);
                    if (existingActor == null)
                    {
                        return NotFound();
                    }

                    // Keep existing photo if no new one uploaded
                    if (photo == null || photo.Length == 0)
                    {
                        actor.Photo = existingActor.Photo;
                    }
                    else
                    {
                        // Update with new photo
                        using MemoryStream memoryStream = new MemoryStream();
                        photo.CopyTo(memoryStream);
                        actor.Photo = memoryStream.ToArray();
                    }
                    _context.Update(actor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ActorExists(actor.ActorID))
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
            return View(actor);
        }

        // GET: Actors/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var actor = await _context.Actor
                .FirstOrDefaultAsync(m => m.ActorID == id);
            if (actor == null)
            {
                return NotFound();
            }

            return View(actor);
        }

        // POST: Actors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var actor = await _context.Actor.FindAsync(id);
            if (actor != null)
            {
                _context.Actor.Remove(actor);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ActorExists(int id)
        {
            return _context.Actor.Any(e => e.ActorID == id);
        }
    }
}
