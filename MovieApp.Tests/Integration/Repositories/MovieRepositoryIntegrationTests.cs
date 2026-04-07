using System;
using Microsoft.Data.SqlClient;
using MovieApp.Core.Models;
using MovieApp.Core.Repositories;
using Xunit;

namespace MovieApp.Tests.Integration.Repositories
{
    public class MovieRepositoryIntegrationTests : IDisposable
    {
        private readonly string databaseName;
        private readonly string connectionString;
        private readonly MovieRepository repo;

        public MovieRepositoryIntegrationTests()
        {
            databaseName = "MovieAppTestDb_Movie_" + Guid.NewGuid().ToString("N");

            connectionString =
                $"Server=.\\SQLEXPRESS;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;";

            var initializer = new DatabaseInitializer(connectionString);
            initializer.EnsureCreatedAndSeeded();

            repo = new MovieRepository(connectionString);
            ClearMovieTable();
        }

        private void ClearMovieTable()
        {
            using var conn = new SqlConnection(connectionString);
            conn.Open();

            new SqlCommand("DELETE FROM Bet", conn).ExecuteNonQuery();
            new SqlCommand("DELETE FROM Comment", conn).ExecuteNonQuery();
            new SqlCommand("DELETE FROM Review", conn).ExecuteNonQuery();
            new SqlCommand("DELETE FROM Battle", conn).ExecuteNonQuery();
            new SqlCommand("DELETE FROM Movie", conn).ExecuteNonQuery();
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
        public void Insert_ValidMovie_ReturnsNewId()
        {
            var movie = new Movie
            {
                Title = "Test Movie",
                Year = 2020,
                PosterUrl = "test.jpg",
                Genre = "Drama",
                AverageRating = 8.5
            };

            int id = repo.Insert(movie);

            Assert.True(id > 0);

            var insertedMovie = repo.GetById(id);
            Assert.NotNull(insertedMovie);
            Assert.Equal("Test Movie", insertedMovie!.Title);
        }

        [Fact]
        public void GetAll_WhenMoviesExist_ReturnsMovies()
        {
            repo.Insert(new Movie
            {
                Title = "Movie 1",
                Year = 2021,
                PosterUrl = "a.jpg",
                Genre = "Action",
                AverageRating = 7.2
            });

            repo.Insert(new Movie
            {
                Title = "Movie 2",
                Year = 2022,
                PosterUrl = "b.jpg",
                Genre = "Comedy",
                AverageRating = 6.8
            });

            var movies = repo.GetAll();

            Assert.Equal(2, movies.Count);
        }

        [Fact]
        public void GetById_ExistingMovie_ReturnsMovie()
        {
            var movie = new Movie
            {
                Title = "In Test",
                Year = 2019,
                PosterUrl = "poster.png",
                Genre = "Sci-Fi",
                AverageRating = 9.1
            };

            int id = repo.Insert(movie);

            var result = repo.GetById(id);

            Assert.NotNull(result);
            Assert.Equal(id, result!.MovieId);
            Assert.Equal("In Test", result.Title);
        }

        [Fact]
        public void GetById_NonExistingMovie_ReturnsNull()
        {
            var result = repo.GetById(999999);

            Assert.Null(result);
        }

        [Fact]
        public void Update_ExistingMovie_ReturnsTrueAndUpdatesMovie()
        {
            var movie = new Movie
            {
                Title = "Old Title",
                Year = 2018,
                PosterUrl = "old.jpg",
                Genre = "Drama",
                AverageRating = 5.5
            };

            int id = repo.Insert(movie);

            movie.MovieId = id;
            movie.Title = "New Title";
            movie.Year = 2023;
            movie.PosterUrl = "new.jpg";
            movie.Genre = "Thriller";
            movie.AverageRating = 8.9;

            bool updated = repo.Update(movie);

            Assert.True(updated);

            var updatedMovie = repo.GetById(id);
            Assert.NotNull(updatedMovie);
            Assert.Equal("New Title", updatedMovie!.Title);
            Assert.Equal(2023, updatedMovie.Year);
            Assert.Equal("new.jpg", updatedMovie.PosterUrl);
            Assert.Equal("Thriller", updatedMovie.Genre);
            Assert.Equal(8.9, updatedMovie.AverageRating);
        }

        [Fact]
        public void Update_NonExistingMovie_ReturnsFalse()
        {
            var movie = new Movie
            {
                MovieId = 999999,
                Title = "Ghost Movie",
                Year = 2020,
                PosterUrl = "ghost.jpg",
                Genre = "Horror",
                AverageRating = 4.4
            };

            bool updated = repo.Update(movie);

            Assert.False(updated);
        }

        [Fact]
        public void Delete_ExistingMovie_ReturnsTrueAndRemovesMovie()
        {
            var movie = new Movie
            {
                Title = "Delete Me",
                Year = 2021,
                PosterUrl = "delete.jpg",
                Genre = "Action",
                AverageRating = 6.5
            };

            int id = repo.Insert(movie);

            bool deleted = repo.Delete(id);

            Assert.True(deleted);
            Assert.Null(repo.GetById(id));
        }

        [Fact]
        public void Delete_NonExistingMovie_ReturnsFalse()
        {
            bool deleted = repo.Delete(999999);

            Assert.False(deleted);
        }
    }
}