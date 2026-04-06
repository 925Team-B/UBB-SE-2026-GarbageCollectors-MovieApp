using Microsoft.Data.SqlClient;
using MovieApp.Core.Models;
using MovieApp.Core.Repositories;
using System;
using Xunit;

namespace MovieApp.Tests.Integration.Repositories
{
    public class MovieRepositoryIntegrationTests : IDisposable
    {
        private readonly string _databaseName;
        private readonly string _connectionString;
        private readonly MovieRepository _repo;

        public MovieRepositoryIntegrationTests()
        {
            _databaseName = "MovieAppTestDb_Movie_" + Guid.NewGuid().ToString("N");

            _connectionString =
                $"Server=.\\SQLEXPRESS;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True;";

            var initializer = new DatabaseInitializer(_connectionString);
            initializer.EnsureCreatedAndSeeded();

            _repo = new MovieRepository(_connectionString);
            ClearMovieTable();
        }

        private void ClearMovieTable()
        {
            using var conn = new SqlConnection(_connectionString);
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
IF DB_ID('{_databaseName}') IS NOT NULL
BEGIN
    ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{_databaseName}];
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

            int id = _repo.Insert(movie);

            Assert.True(id > 0);

            var insertedMovie = _repo.GetById(id);
            Assert.NotNull(insertedMovie);
            Assert.Equal("Test Movie", insertedMovie!.Title);
        }

        [Fact]
        public void GetAll_WhenMoviesExist_ReturnsMovies()
        {
            _repo.Insert(new Movie
            {
                Title = "Movie 1",
                Year = 2021,
                PosterUrl = "a.jpg",
                Genre = "Action",
                AverageRating = 7.2
            });

            _repo.Insert(new Movie
            {
                Title = "Movie 2",
                Year = 2022,
                PosterUrl = "b.jpg",
                Genre = "Comedy",
                AverageRating = 6.8
            });

            var movies = _repo.GetAll();

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

            int id = _repo.Insert(movie);

            var result = _repo.GetById(id);

            Assert.NotNull(result);
            Assert.Equal(id, result!.MovieId);
            Assert.Equal("In Test", result.Title);
        }

        [Fact]
        public void GetById_NonExistingMovie_ReturnsNull()
        {
            var result = _repo.GetById(999999);

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

            int id = _repo.Insert(movie);

            movie.MovieId = id;
            movie.Title = "New Title";
            movie.Year = 2023;
            movie.PosterUrl = "new.jpg";
            movie.Genre = "Thriller";
            movie.AverageRating = 8.9;

            bool updated = _repo.Update(movie);

            Assert.True(updated);

            var updatedMovie = _repo.GetById(id);
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

            bool updated = _repo.Update(movie);

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

            int id = _repo.Insert(movie);

            bool deleted = _repo.Delete(id);

            Assert.True(deleted);
            Assert.Null(_repo.GetById(id));
        }

        [Fact]
        public void Delete_NonExistingMovie_ReturnsFalse()
        {
            bool deleted = _repo.Delete(999999);

            Assert.False(deleted);
        }
    }
}