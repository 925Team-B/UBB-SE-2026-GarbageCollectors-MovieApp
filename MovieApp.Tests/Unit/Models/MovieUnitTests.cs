using MovieApp.Core.Models;
using Xunit;

namespace MovieApp.Tests.Unit.Models
{
    public class MovieUnitTests
    {
        [Fact]
        public void DefaultConstructor_SetsExpectedDefaults()
        {
            var movie = new Movie();

            Assert.Equal(0, movie.MovieId);
            Assert.Equal(string.Empty, movie.Title);
            Assert.Equal(0, movie.Year);
            Assert.Equal(string.Empty, movie.PosterUrl);
            Assert.Equal(string.Empty, movie.Genre);
            Assert.Equal(0.0, movie.AverageRating);
            Assert.NotNull(movie.Reviews);
            Assert.Empty(movie.Reviews);
            Assert.NotNull(movie.Comments);
            Assert.Empty(movie.Comments);
        }

        [Fact]
        public void Properties_CanBeSetAndRead()
        {
            var movie = new Movie
            {
                MovieId = 1,
                Title = "Inception",
                Year = 2010,
                PosterUrl = "https://example.com/inception.jpg",
                Genre = "Sci-Fi",
                AverageRating = 8.8
            };

            Assert.Equal(1, movie.MovieId);
            Assert.Equal("Inception", movie.Title);
            Assert.Equal(2010, movie.Year);
            Assert.Equal("https://example.com/inception.jpg", movie.PosterUrl);
            Assert.Equal("Sci-Fi", movie.Genre);
            Assert.Equal(8.8, movie.AverageRating);
        }

        [Fact]
        public void ToString_ReturnsTitle()
        {
            var movie = new Movie { Title = "Interstellar" };
            Assert.Equal("Interstellar", movie.ToString());
        }

        [Fact]
        public void ToString_WhenTitleIsEmpty_ReturnsEmptyString()
        {
            var movie = new Movie { Title = string.Empty };
            Assert.Equal(string.Empty, movie.ToString());
        }

        [Fact]
        public void Reviews_CanAddItems()
        {
            var movie = new Movie();
            movie.Reviews.Add(new Review());
            movie.Reviews.Add(new Review());

            Assert.Equal(2, movie.Reviews.Count);
        }

        [Fact]
        public void Comments_CanAddItems()
        {
            var movie = new Movie();
            movie.Comments.Add(new Comment());

            Assert.Single(movie.Comments);
        }
    }
}