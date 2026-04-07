using System;
using MovieApp.Core.Models;
using Xunit;

namespace MovieApp.Tests.Unit.Models
{
    public class ReviewUnitTests
    {
        [Fact]
        public void DefaultConstructor_SetsExpectedDefaults()
        {
            var review = new Review();

            Assert.Equal(0, review.ReviewId);
            Assert.Equal(0f, review.StarRating);
            Assert.Equal(string.Empty, review.Content);
            Assert.False(review.IsExtraReview);
            Assert.Equal(0, review.CinematographyRating);
            Assert.Null(review.CinematographyText);
            Assert.Equal(0, review.ActingRating);
            Assert.Null(review.ActingText);
            Assert.Equal(0, review.CgiRating);
            Assert.Null(review.CgiText);
            Assert.Equal(0, review.PlotRating);
            Assert.Null(review.PlotText);
            Assert.Equal(0, review.SoundRating);
            Assert.Null(review.SoundText);
            Assert.Null(review.User);
            Assert.Null(review.Movie);
        }

        [Fact]
        public void Properties_CanBeSetAndRead()
        {
            var now = DateTime.UtcNow;
            var user = new User { UserId = 3 };
            var movie = new Movie { MovieId = 5 };

            var review = new Review
            {
                ReviewId = 1,
                StarRating = 4.5f,
                Content = "Outstanding film.",
                CreatedAt = now,
                IsExtraReview = true,
                CinematographyRating = 5,
                CinematographyText = "Beautiful shots",
                ActingRating = 4,
                ActingText = "Stellar performances",
                CgiRating = 3,
                CgiText = "Decent effects",
                PlotRating = 5,
                PlotText = "Gripping story",
                SoundRating = 4,
                SoundText = "Immersive audio",
                User = user,
                Movie = movie
            };

            Assert.Equal(1, review.ReviewId);
            Assert.Equal(4.5f, review.StarRating);
            Assert.Equal("Outstanding film.", review.Content);
            Assert.Equal(now, review.CreatedAt);
            Assert.True(review.IsExtraReview);
            Assert.Equal(5, review.CinematographyRating);
            Assert.Equal("Beautiful shots", review.CinematographyText);
            Assert.Equal(4, review.ActingRating);
            Assert.Equal("Stellar performances", review.ActingText);
            Assert.Equal(3, review.CgiRating);
            Assert.Equal("Decent effects", review.CgiText);
            Assert.Equal(5, review.PlotRating);
            Assert.Equal("Gripping story", review.PlotText);
            Assert.Equal(4, review.SoundRating);
            Assert.Equal("Immersive audio", review.SoundText);
            Assert.Same(user, review.User);
            Assert.Same(movie, review.Movie);
        }
        [Fact]
        public void UserDisplayId_WhenUserIsSet_ReturnsUserId()
        {
            var review = new Review { User = new User { UserId = 12 } };
            Assert.Equal(12, review.UserDisplayId);
        }

        [Fact]
        public void UserDisplayId_WhenUserIsNull_ReturnsZero()
        {
            var review = new Review { User = null };
            Assert.Equal(0, review.UserDisplayId);
        }
    }
}