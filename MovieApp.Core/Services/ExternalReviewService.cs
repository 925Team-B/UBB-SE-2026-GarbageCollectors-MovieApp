#nullable enable
using MovieApp.Core.Interfaces;
using MovieApp.Core.Models;

namespace MovieApp.Core.Services;

/// <summary>
/// Service for fetching external critic reviews from third-party sources.
/// Currently uses mock data; designed for easy swap to real API endpoints.
/// </summary>
public class ExternalReviewService
{
    private readonly IEnumerable<IExternalReviewProvider> _providers;

    // Common stop words to filter out in lexicon analysis
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for",
        "of", "with", "by", "from", "is", "it", "this", "that", "was", "are",
        "be", "has", "have", "had", "not", "no", "as", "its", "so", "than",
        "into", "about", "out", "up", "what", "which", "who", "when", "where",
        "how", "all", "each", "every", "both", "few", "more", "most", "other",
        "some", "such", "only", "own", "same", "very", "can", "will", "just",
        "do", "does", "did", "been", "being", "would", "could", "should",
        "may", "might", "must", "shall", "we", "they", "he", "she", "you",
        "i", "me", "my", "your", "his", "her", "our", "their"
    };

    /// <summary>
    /// Initializes a new instance of <see cref="ExternalReviewService"/>.
    /// </summary>
    /// <param name="providers">External review providers.</param>
    public ExternalReviewService(IEnumerable<IExternalReviewProvider> providers)
    {
        _providers = providers;
    }

    /// <summary>
    /// Gets external critic reviews for a movie from all configured providers.
    /// </summary>
    /// <param name="movieTitle">The movie title to search for.</param>
    /// <param name="releaseYear">The movie release year.</param>
    /// <returns>A list of critic reviews from various sources.</returns>
    public async Task<List<CriticReview>> GetExternalReviews(string movieTitle, int releaseYear)
    {
        var tasks = _providers.Select(async provider =>
        {
            try
            {
                return await provider.GetReviewAsync(movieTitle, releaseYear);
            }
            catch
            {
                return null;
            }
        });

        var results = await Task.WhenAll(tasks);

        return results
            .Where(r => r is not null)
            .Select(r => r!)
            .ToList();
    }

    /// <summary>
    /// Gets aggregate critic and audience scores for a movie (mock data).
    /// </summary>
    /// <param name="movieTitle">The movie title.</param>
    /// <returns>A tuple with CriticScore and AudienceScore.</returns>
    public async Task<(double CriticScore, double AudienceScore)> GetAggregateScores(string movieTitle)
    {
        await Task.Delay(200);

        // Mock aggregate scores — use wider ranges so polarization is visible in demos
        var hash = Math.Abs(movieTitle.GetHashCode());
        double criticScore  = 1.5 + (hash % 35) / 10.0;   // range 1.5 – 5.0
        double audienceScore = 1.0 + (hash % 40) / 10.0;  // range 1.0 – 5.0

        criticScore  = Math.Min(5.0, Math.Round(criticScore,  1));
        audienceScore = Math.Min(5.0, Math.Round(audienceScore, 1));

        return (criticScore, audienceScore);
    }

    /// <summary>
    /// Analyzes the lexicon of critic reviews, returning top 10 non-stop-words.
    /// </summary>
    /// <param name="reviews">The list of critic reviews to analyze.</param>
    /// <returns>Top 10 words by frequency.</returns>
    public List<(string Word, int Count)> AnalyseLexicon(List<CriticReview> reviews)
    {
        var wordCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var review in reviews)
        {
            var words = (review.Snippet + " " + review.Headline)
                .Split(new[] { ' ', '.', ',', '!', '?', ';', ':', '"', '\'', '(', ')', '-', '—', '\n', '\r', '\t' },
                    StringSplitOptions.RemoveEmptyEntries);

            foreach (var word in words)
            {
                var cleanWord = word.Trim().ToLower();
                if (cleanWord.Length < 3 || StopWords.Contains(cleanWord))
                    continue;

                if (wordCounts.ContainsKey(cleanWord))
                    wordCounts[cleanWord]++;
                else
                    wordCounts[cleanWord] = 1;
            }
        }

        return wordCounts
            .OrderByDescending(kv => kv.Value)
            .Take(10)
            .Select(kv => (kv.Key, kv.Value))
            .ToList();
    }

    /// <summary>
    /// Determines whether the critic and audience scores are polarized.
    /// </summary>
    /// <param name="criticScore">The critic score.</param>
    /// <param name="audienceScore">The audience score.</param>
    /// <param name="threshold">The threshold for polarization (default 2.0).</param>
    /// <returns>True if scores differ by more than the threshold.</returns>
    public bool IsPolarized(double criticScore, double audienceScore, double threshold = 2.0)
    {
        return Math.Abs(criticScore - audienceScore) > threshold;
    }
}
