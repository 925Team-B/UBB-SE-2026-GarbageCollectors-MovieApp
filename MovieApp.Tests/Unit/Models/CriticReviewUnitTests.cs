using MovieApp.Core.Models;
using Xunit;

namespace MovieApp.Tests.Unit.Models
{
    public class CriticReviewUnitTests
    {
        [Fact]
        public void DefaultConstructor_SetsExpectedDefaults()
        {
            var review = new CriticReview();

            Assert.Equal(string.Empty, review.Source);
            Assert.Equal(0.0, review.Score);
            Assert.Equal(string.Empty, review.Headline);
            Assert.Equal(string.Empty, review.Snippet);
            Assert.Equal(string.Empty, review.Url);
        }

        [Fact]
        public void Properties_CanBeSetAndRead()
        {
            var review = new CriticReview
            {
                Source = "NYT",
                Score = 8.5,
                Headline = "A triumph of cinema",
                Snippet = "Breathtaking visuals...",
                Url = "https://nytimes.com/review/1"
            };

            Assert.Equal("NYT", review.Source);
            Assert.Equal(8.5, review.Score);
            Assert.Equal("A triumph of cinema", review.Headline);
            Assert.Equal("Breathtaking visuals...", review.Snippet);
            Assert.Equal("https://nytimes.com/review/1", review.Url);
        }
        [Fact]
        public void ScoreDisplay_WhenScoreIsPositive_ReturnsFormattedString()
        {
            var review = new CriticReview { Score = 8.5 };
            Assert.Equal("8.5", review.ScoreDisplay);
        }

        [Fact]
        public void ScoreDisplay_WhenScoreIsZero_ReturnsEmptyString()
        {
            var review = new CriticReview { Score = 0 };
            Assert.Equal(string.Empty, review.ScoreDisplay);
        }

        [Fact]
        public void ScoreDisplay_WhenScoreIsNegative_ReturnsEmptyString()
        {
            var review = new CriticReview { Score = -1 };
            Assert.Equal(string.Empty, review.ScoreDisplay);
        }

        [Fact]
        public void ScoreDisplay_FormatsToOneDecimalPlace()
        {
            var review = new CriticReview { Score = 9.0 };
            Assert.Equal("9.0", review.ScoreDisplay);
        }

        [Fact]
        public void HasScore_WhenScoreIsPositive_ReturnsTrue()
        {
            var review = new CriticReview { Score = 7.2 };
            Assert.True(review.HasScore);
        }

        [Fact]
        public void HasScore_WhenScoreIsZero_ReturnsFalse()
        {
            var review = new CriticReview { Score = 0 };
            Assert.False(review.HasScore);
        }

        [Fact]
        public void HasScore_WhenScoreIsNegative_ReturnsFalse()
        {
            var review = new CriticReview { Score = -5 };
            Assert.False(review.HasScore);
        }
    }
}