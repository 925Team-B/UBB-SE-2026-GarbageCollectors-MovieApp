using System;
using Microsoft.Data.SqlClient;
using MovieApp.Core.Models;
using MovieApp.Core.Repositories;
using Xunit;

namespace MovieApp.Tests.Integration.Repositories
{
    public class ReviewRepositoryIntegrationTests : IDisposable
    {
        private readonly string databaseName;
        private readonly string connectionString;
        private readonly ReviewRepository repo;
        private readonly MovieRepository movieRepo;

        public ReviewRepositoryIntegrationTests()
        {
            databaseName = "MovieAppTestDb_Review_" + Guid.NewGuid().ToString("N");

            connectionString =
                $"Server=.\\SQLEXPRESS;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;";

            var initializer = new DatabaseInitializer(connectionString);
            initializer.EnsureCreatedAndSeeded();

            repo = new ReviewRepository(connectionString);
            movieRepo = new MovieRepository(connectionString);

            ClearTables();
        }

        private void ClearTables()
        {
            using var conn = new SqlConnection(connectionString);
            conn.Open();

            new SqlCommand("DELETE FROM Bet", conn).ExecuteNonQuery();
            new SqlCommand("DELETE FROM Comment", conn).ExecuteNonQuery();
            new SqlCommand("DELETE FROM Review", conn).ExecuteNonQuery();
            new SqlCommand("DELETE FROM Battle", conn).ExecuteNonQuery();
            new SqlCommand("DELETE FROM Movie", conn).ExecuteNonQuery();
        }

        private Movie CreateMovie(string title, int year, string genre, double rating)
        {
            var movie = new Movie
            {
                Title = title,
                Year = year,
                PosterUrl = title.Replace(" ", string.Empty) + ".jpg",
                Genre = genre,
                AverageRating = rating
            };

            int id = movieRepo.Insert(movie);
            movie.MovieId = id;
            return movie;
        }

        private User ExistingUser(int userId = 1)
        {
            return new User { UserId = userId };
        }

        public void Dispose()
        {
            var masterConnectionString =
                "Server=.\\SQLEXPRESS;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

            using var conn = new SqlConnection(masterConnectionString);
            conn.Open();

            using var cmd = new SqlCommand($@"
IF DB_ID('{databaseName}') IS NOT NULL
BEGIN
    ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{databaseName}];
END", conn);

            cmd.ExecuteNonQuery();
        }

        [Fact]
        public void Insert_ValidReview_ReturnsNewId()
        {
            var movie = CreateMovie("Review Movie", 2020, "Drama", 8.0);

            var review = new Review
            {
                User = ExistingUser(1),
                Movie = movie,
                StarRating = 8.5f,
                Content = "Very good movie",
                CreatedAt = new DateTime(2025, 1, 1, 10, 0, 0),
                IsExtraReview = false
            };

            int id = repo.Insert(review);

            Assert.True(id > 0);

            var insertedReview = repo.GetById(id);
            Assert.NotNull(insertedReview);
            Assert.Equal("Very good movie", insertedReview!.Content);
            Assert.Equal(8.5f, insertedReview.StarRating);
            Assert.Equal(1, insertedReview.User!.UserId);
            Assert.Equal(movie.MovieId, insertedReview.Movie!.MovieId);
            Assert.False(insertedReview.IsExtraReview);
        }

        [Fact]
        public void Insert_ExtraReviewWithDetails_ReturnsNewId()
        {
            var movie = CreateMovie("Detailed Review Movie", 2021, "Sci-Fi", 9.0);

            var review = new Review
            {
                User = ExistingUser(2),
                Movie = movie,
                StarRating = 9.5f,
                Content = "Excellent",
                CreatedAt = new DateTime(2025, 1, 2, 12, 0, 0),
                IsExtraReview = true,
                CinematographyRating = 10,
                CinematographyText = "Amazing visuals",
                ActingRating = 9,
                ActingText = "Great acting",
                CgiRating = 8,
                CgiText = "Solid CGI",
                PlotRating = 9,
                PlotText = "Very interesting plot",
                SoundRating = 10,
                SoundText = "Outstanding soundtrack"
            };

            int id = repo.Insert(review);

            var insertedReview = repo.GetById(id);

            Assert.NotNull(insertedReview);
            Assert.True(insertedReview!.IsExtraReview);
            Assert.Equal(10, insertedReview.CinematographyRating);
            Assert.Equal("Amazing visuals", insertedReview.CinematographyText);
            Assert.Equal(9, insertedReview.ActingRating);
            Assert.Equal("Great acting", insertedReview.ActingText);
            Assert.Equal(8, insertedReview.CgiRating);
            Assert.Equal("Solid CGI", insertedReview.CgiText);
            Assert.Equal(9, insertedReview.PlotRating);
            Assert.Equal("Very interesting plot", insertedReview.PlotText);
            Assert.Equal(10, insertedReview.SoundRating);
            Assert.Equal("Outstanding soundtrack", insertedReview.SoundText);
        }

        [Fact]
        public void Insert_NullUser_ThrowsInvalidOperationException()
        {
            var movie = CreateMovie("Review Movie", 2020, "Drama", 8.0);

            var review = new Review
            {
                User = null,
                Movie = movie,
                StarRating = 7.0f,
                Content = "Invalid review",
                CreatedAt = new DateTime(2025, 1, 1, 10, 0, 0),
                IsExtraReview = false
            };

            Assert.Throws<InvalidOperationException>(() => repo.Insert(review));
        }

        [Fact]
        public void GetAll_WhenReviewsExist_ReturnsReviews()
        {
            var movie = CreateMovie("Review Movie", 2020, "Drama", 8.0);

            repo.Insert(new Review
            {
                User = ExistingUser(1),
                Movie = movie,
                StarRating = 7.5f,
                Content = "First review",
                CreatedAt = new DateTime(2025, 2, 1, 10, 0, 0),
                IsExtraReview = false
            });

            repo.Insert(new Review
            {
                User = ExistingUser(2),
                Movie = movie,
                StarRating = 8.0f,
                Content = "Second review",
                CreatedAt = new DateTime(2025, 2, 1, 11, 0, 0),
                IsExtraReview = true
            });

            var reviews = repo.GetAll();

            Assert.Equal(2, reviews.Count);
        }

        [Fact]
        public void GetById_ExistingReview_ReturnsReview()
        {
            var movie = CreateMovie("Review Movie", 2020, "Drama", 8.0);

            var review = new Review
            {
                User = ExistingUser(1),
                Movie = movie,
                StarRating = 6.5f,
                Content = "Find this review",
                CreatedAt = new DateTime(2025, 3, 1, 9, 0, 0),
                IsExtraReview = false
            };

            int id = repo.Insert(review);

            var result = repo.GetById(id);

            Assert.NotNull(result);
            Assert.Equal(id, result!.ReviewId);
            Assert.Equal("Find this review", result.Content);
            Assert.Equal(6.5f, result.StarRating);
            Assert.Equal(1, result.User!.UserId);
            Assert.Equal(movie.MovieId, result.Movie!.MovieId);
        }

        [Fact]
        public void GetById_NonExistingReview_ReturnsNull()
        {
            var result = repo.GetById(999999);

            Assert.Null(result);
        }

        [Fact]
        public void Update_ExistingReview_ReturnsTrueAndUpdatesReview()
        {
            var movie1 = CreateMovie("Review Movie 1", 2020, "Drama", 8.0);
            var movie2 = CreateMovie("Review Movie 2", 2021, "Action", 7.5);

            var review = new Review
            {
                User = ExistingUser(1),
                Movie = movie1,
                StarRating = 6.0f,
                Content = "Old content",
                CreatedAt = new DateTime(2025, 4, 1, 8, 0, 0),
                IsExtraReview = false
            };

            int id = repo.Insert(review);

            review.ReviewId = id;
            review.User = ExistingUser(2);
            review.Movie = movie2;
            review.StarRating = 9.0f;
            review.Content = "New content";
            review.CreatedAt = new DateTime(2025, 4, 2, 8, 0, 0);
            review.IsExtraReview = true;
            review.CinematographyRating = 9;
            review.CinematographyText = "Updated cinematography";

            bool updated = repo.Update(review);

            Assert.True(updated);

            var updatedReview = repo.GetById(id);
            Assert.NotNull(updatedReview);
            Assert.Equal(2, updatedReview!.User!.UserId);
            Assert.Equal(movie2.MovieId, updatedReview.Movie!.MovieId);
            Assert.Equal(9.0f, updatedReview.StarRating);
            Assert.Equal("New content", updatedReview.Content);
            Assert.True(updatedReview.IsExtraReview);
            Assert.Equal(9, updatedReview.CinematographyRating);
            Assert.Equal("Updated cinematography", updatedReview.CinematographyText);
        }

        [Fact]
        public void Update_NonExistingReview_ReturnsFalse()
        {
            var movie = CreateMovie("Review Movie", 2020, "Drama", 8.0);

            var review = new Review
            {
                ReviewId = 999999,
                User = ExistingUser(1),
                Movie = movie,
                StarRating = 5.0f,
                Content = "Ghost review",
                CreatedAt = new DateTime(2025, 4, 1, 8, 0, 0),
                IsExtraReview = false
            };

            bool updated = repo.Update(review);

            Assert.False(updated);
        }

        [Fact]
        public void Delete_ExistingReview_ReturnsTrueAndRemovesReview()
        {
            var movie = CreateMovie("Review Movie", 2020, "Drama", 8.0);

            var review = new Review
            {
                User = ExistingUser(1),
                Movie = movie,
                StarRating = 7.0f,
                Content = "Delete me",
                CreatedAt = new DateTime(2025, 5, 1, 8, 0, 0),
                IsExtraReview = false
            };

            int id = repo.Insert(review);

            bool deleted = repo.Delete(id);

            Assert.True(deleted);
            Assert.Null(repo.GetById(id));
        }

        [Fact]
        public void Delete_NonExistingReview_ReturnsFalse()
        {
            bool deleted = repo.Delete(999999);

            Assert.False(deleted);
        }
    }
}