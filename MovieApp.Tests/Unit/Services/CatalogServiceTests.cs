#nullable enable
using Moq;
using MovieApp.Core.Interfaces.Repository;
using MovieApp.Core.Models;
using MovieApp.Core.Services;

namespace MovieApp.Tests.Unit.Services;

public class CatalogServiceTests
{
    private readonly Mock<IMovieRepository> movieRepoMock;
    private readonly CatalogService sut;

    public CatalogServiceTests()
    {
        movieRepoMock = new Mock<IMovieRepository>();
        sut = new CatalogService(movieRepoMock.Object);
    }

    // --- GetAllMovies ---
    [Fact]
    public async Task GetAllMovies_WhenMoviesExist_ReturnsMoviesOrderedByTitle()
    {
        var movies = new List<Movie>
        {
            new () { MovieId = 1, Title = "Zoolander", Genre = "Comedy", AverageRating = 3.5 },
            new () { MovieId = 2, Title = "Inception", Genre = "Sci-Fi", AverageRating = 4.8 },
            new () { MovieId = 3, Title = "Avatar", Genre = "Action", AverageRating = 4.0 }
        };
        movieRepoMock.Setup(r => r.GetAll()).Returns(movies);

        var result = await sut.GetAllMovies();

        Assert.Equal(3, result.Count);
        Assert.Equal("Avatar", result[0].Title);
        Assert.Equal("Inception", result[1].Title);
        Assert.Equal("Zoolander", result[2].Title);
    }

    [Fact]
    public async Task GetAllMovies_WhenNoMoviesExist_ReturnsEmptyList()
    {
        movieRepoMock.Setup(r => r.GetAll()).Returns(new List<Movie>());

        var result = await sut.GetAllMovies();

        Assert.Empty(result);
    }

    // --- GetMovieById ---
    [Fact]
    public async Task GetMovieById_WhenMovieExists_ReturnsMovie()
    {
        var movie = new Movie { MovieId = 42, Title = "Inception" };
        movieRepoMock.Setup(r => r.GetById(42)).Returns(movie);

        var result = await sut.GetMovieById(42);

        Assert.Equal(42, result.MovieId);
        Assert.Equal("Inception", result.Title);
    }

    [Fact]
    public async Task GetMovieById_WhenMovieNotFound_ThrowsInvalidOperationException()
    {
        movieRepoMock.Setup(r => r.GetById(It.IsAny<int>())).Returns((Movie?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetMovieById(999));
    }

    // --- SearchMovies ---
    [Fact]
    public async Task SearchMovies_WithMatchingQuery_ReturnsOnlyMatchingMovies()
    {
        var movies = new List<Movie>
        {
            new () { MovieId = 1, Title = "Inception" },
            new () { MovieId = 2, Title = "Interstellar" },
            new () { MovieId = 3, Title = "The Dark Knight" }
        };
        movieRepoMock.Setup(r => r.GetAll()).Returns(movies);

        var result = await sut.SearchMovies("in");

        Assert.Equal(2, result.Count);
        Assert.All(result, m => Assert.Contains("in", m.Title, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchMovies_WithEmptyQuery_ReturnsAllMovies()
    {
        var movies = new List<Movie>
        {
            new () { MovieId = 1, Title = "Inception" },
            new () { MovieId = 2, Title = "Avatar" }
        };
        movieRepoMock.Setup(r => r.GetAll()).Returns(movies);

        var result = await sut.SearchMovies(string.Empty);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task SearchMovies_WithCaseMismatch_ReturnsMatchingMovies()
    {
        var movies = new List<Movie>
        {
            new () { MovieId = 1, Title = "INCEPTION" },
            new () { MovieId = 2, Title = "Avatar" }
        };
        movieRepoMock.Setup(r => r.GetAll()).Returns(movies);

        var result = await sut.SearchMovies("inception");

        Assert.Single(result);
        Assert.Equal("INCEPTION", result[0].Title);
    }

    [Fact]
    public async Task SearchMovies_WithQueryMatchingNoMovies_ReturnsEmptyList()
    {
        var movies = new List<Movie>
        {
            new () { MovieId = 1, Title = "Inception" }
        };
        movieRepoMock.Setup(r => r.GetAll()).Returns(movies);

        var result = await sut.SearchMovies("zzz");

        Assert.Empty(result);
    }

    // --- FilterMovies ---
    [Fact]
    public async Task FilterMovies_WithGenreAndMinRating_ReturnsOnlyMatchingMovies()
    {
        var movies = new List<Movie>
        {
            new () { MovieId = 1, Title = "A", Genre = "Comedy", AverageRating = 4.0 },
            new () { MovieId = 2, Title = "B", Genre = "Comedy", AverageRating = 2.0 },
            new () { MovieId = 3, Title = "C", Genre = "Drama", AverageRating = 4.5 }
        };
        movieRepoMock.Setup(r => r.GetAll()).Returns(movies);

        var result = await sut.FilterMovies("Comedy", 3.0f);

        Assert.Single(result);
        Assert.Equal(1, result[0].MovieId);
    }

    [Fact]
    public async Task FilterMovies_WithNoGenreFilter_ReturnsAllMoviesAboveMinRating()
    {
        var movies = new List<Movie>
        {
            new () { MovieId = 1, Title = "A", Genre = "Comedy", AverageRating = 4.0 },
            new () { MovieId = 2, Title = "B", Genre = "Drama", AverageRating = 2.0 }
        };
        movieRepoMock.Setup(r => r.GetAll()).Returns(movies);

        var result = await sut.FilterMovies(string.Empty, 3.0f);

        Assert.Single(result);
        Assert.Equal(1, result[0].MovieId);
    }

    [Fact]
    public async Task FilterMovies_WithCaseInsensitiveGenre_ReturnsMatchingMovies()
    {
        var movies = new List<Movie>
        {
            new () { MovieId = 1, Title = "A", Genre = "COMEDY", AverageRating = 4.0 }
        };
        movieRepoMock.Setup(r => r.GetAll()).Returns(movies);

        var result = await sut.FilterMovies("comedy", 0f);

        Assert.Single(result);
    }
}
