using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Xunit;
using MovieApp.UI.ViewModels;
using MovieApp.Core.Models;
using MovieApp.Core.Interfaces;

namespace MovieApp.Tests.Unit.ViewModels
{
    public class CatalogViewModelTests
    {
        private readonly Mock<ICatalogService> mockCatalogService;

        public CatalogViewModelTests()
        {
            mockCatalogService = new Mock<ICatalogService>();

            mockCatalogService
                .Setup(s => s.GetAllMovies())
                .ReturnsAsync(new List<Movie>());

            mockCatalogService
                .Setup(s => s.SearchMovies(It.IsAny<string>()))
                .ReturnsAsync(new List<Movie>());
        }

        private CatalogViewModel CreateViewModel() =>
            new (mockCatalogService.Object);
        [Fact]
        public async Task LoadMoviesAsync_WhenCalled_CallsGetAllMovies()
        {
            CatalogViewModel vm = CreateViewModel();

            await vm.LoadMoviesAsync();

            mockCatalogService.Verify(s => s.GetAllMovies(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task LoadMoviesAsync_WhenCalled_PopulatesMoviesCollection()
        {
            List<Movie> movies = new List<Movie>
            {
                new () { MovieId = 1, Title = "Inception" },
                new () { MovieId = 2, Title = "Interstellar" }
            };
            mockCatalogService.Setup(s => s.GetAllMovies()).ReturnsAsync(movies);

            CatalogViewModel vm = CreateViewModel();
            await vm.LoadMoviesAsync();

            Assert.Equal(2, vm.Movies.Count);
        }

        [Fact]
        public async Task LoadMoviesAsync_WhenCalledTwice_ReplacesMoviesNotAppends()
        {
            mockCatalogService.Setup(s => s.GetAllMovies())
                .ReturnsAsync(new List<Movie> { new () { MovieId = 1, Title = "Movie A" } });

            CatalogViewModel vm = CreateViewModel();
            await vm.LoadMoviesAsync();
            await vm.LoadMoviesAsync();

            Assert.Single(vm.Movies);
        }

        [Fact]
        public async Task LoadMoviesCommand_WhenExecuted_CallsGetAllMovies()
        {
            CatalogViewModel vm = CreateViewModel();

            vm.LoadMoviesCommand.Execute(null);
            await Task.Delay(100);

            mockCatalogService.Verify(s => s.GetAllMovies(), Times.AtLeastOnce);
        }
        [Fact]
        public async Task SearchQuery_WhenSetToNonEmpty_CallsSearchMovies()
        {
            CatalogViewModel vm = CreateViewModel();

            vm.SearchQuery = "Inception";
            await Task.Delay(100);

            mockCatalogService.Verify(s => s.SearchMovies("Inception"), Times.AtLeastOnce);
        }

        [Fact]
        public async Task SearchQuery_WhenSetToEmpty_CallsGetAllMovies()
        {
            CatalogViewModel vm = CreateViewModel();
            vm.SearchQuery = "Inception";
            await Task.Delay(50);

            vm.SearchQuery = string.Empty;
            await Task.Delay(100);

            mockCatalogService.Verify(s => s.GetAllMovies(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task SearchQuery_WhenResultsReturned_PopulatesMoviesCollection()
        {
            mockCatalogService
                .Setup(s => s.SearchMovies("Inception"))
                .ReturnsAsync(new List<Movie> { new () { MovieId = 1, Title = "Inception" } });

            CatalogViewModel vm = CreateViewModel();
            vm.SearchQuery = "Inception";
            await Task.Delay(100);

            Assert.Single(vm.Movies);
            Assert.Equal("Inception", vm.Movies[0].Title);
        }
        [Fact]
        public async Task SelectedGenre_WhenSetToSpecificGenre_FiltersMoviesByGenre()
        {
            mockCatalogService.Setup(s => s.GetAllMovies()).ReturnsAsync(new List<Movie>
            {
                new () { MovieId = 1, Title = "Inception",     Genre = "Sci-Fi" },
                new () { MovieId = 2, Title = "The Godfather", Genre = "Crime" }
            });

            CatalogViewModel vm = CreateViewModel();
            vm.SelectedGenre = "Sci-Fi";
            await Task.Delay(100);

            Assert.Single(vm.Movies);
            Assert.Equal("Inception", vm.Movies[0].Title);
        }

        [Fact]
        public async Task SelectedGenre_WhenSetToAllGenres_ShowsAllMovies()
        {
            List<Movie> allMovies = new List<Movie>
            {
                new () { MovieId = 1, Genre = "Sci-Fi", Title = "Movie 1" },
                new () { MovieId = 2, Genre = "Crime",  Title = "Movie 2" }
            };
            mockCatalogService.Setup(s => s.GetAllMovies()).ReturnsAsync(allMovies);

            CatalogViewModel vm = CreateViewModel();

            vm.SelectedGenre = "Sci-Fi";
            await Task.Delay(200);
            vm.SelectedGenre = "All Genres";
            await Task.Delay(300);

            Assert.Equal(2, vm.Movies.Count);
        }
        [Fact]
        public async Task MinimumRating_WhenSet_FiltersMoviesBelowThreshold()
        {
            mockCatalogService.Setup(s => s.GetAllMovies()).ReturnsAsync(new List<Movie>
            {
                new () { MovieId = 1, Title = "High Rated", AverageRating = 8.5 },
                new () { MovieId = 2, Title = "Low Rated",  AverageRating = 4.0 }
            });

            CatalogViewModel vm = CreateViewModel();
            vm.MinimumRating = 7.0;
            await Task.Delay(100);

            Assert.Single(vm.Movies);
            Assert.Equal("High Rated", vm.Movies[0].Title);
        }

        [Fact]
        public async Task MinimumRating_WhenSetToZero_ShowsAllMovies()
        {
            // Arrange
            List<Movie> allMovies = new List<Movie>
            {
                new () { MovieId = 1, Title = "Movie 1", AverageRating = 8.5, Genre = "Sci-Fi" },
                new () { MovieId = 2, Title = "Movie 2", AverageRating = 2.0, Genre = "Drama" }
            };
            mockCatalogService.Setup(s => s.GetAllMovies()).ReturnsAsync(allMovies);

            CatalogViewModel vm = CreateViewModel();
            vm.SelectedGenre = "All Genres";
            await vm.LoadMoviesAsync();
            vm.MinimumRating = 9.0;
            await Task.Delay(200);
            Assert.Empty(vm.Movies);

            vm.MinimumRating = 0;
            await Task.Delay(300);

            Assert.Equal(2, vm.Movies.Count);
        }
        [Fact]
        public void SelectMovieCommand_WhenMoviePassed_SetsSelectedMovie()
        {
            Movie movie = new Movie { MovieId = 1, Title = "Inception" };
            CatalogViewModel vm = CreateViewModel();

            vm.SelectMovieCommand.Execute(movie);

            Assert.Equal(movie, vm.SelectedMovie);
        }

        [Fact]
        public void SelectMovieCommand_WhenMoviePassed_RaisesMovieSelectedEvent()
        {
            Movie movie = new Movie { MovieId = 1, Title = "Inception" };
            CatalogViewModel vm = CreateViewModel();
            Movie? raisedMovie = null;
            vm.MovieSelected += m => raisedMovie = m;

            vm.SelectMovieCommand.Execute(movie);

            Assert.Equal(movie, raisedMovie);
        }

        [Fact]
        public void SelectMovieCommand_WhenNullPassed_DoesNotRaiseMovieSelectedEvent()
        {
            CatalogViewModel vm = CreateViewModel();
            bool raised = false;
            vm.MovieSelected += _ => raised = true;

            vm.SelectMovieCommand.Execute(null);

            Assert.False(raised);
        }
        [Fact]
        public async Task ClearFiltersCommand_WhenFiltersActive_ResetsSearchQueryToEmpty()
        {
            CatalogViewModel vm = CreateViewModel();
            vm.SearchQuery = "something";
            await Task.Delay(50);

            vm.ClearFiltersCommand.Execute(null);
            await Task.Delay(100);

            Assert.Equal(string.Empty, vm.SearchQuery);
        }

        [Fact]
        public async Task ClearFiltersCommand_WhenFiltersActive_ResetsSelectedGenreToAllGenres()
        {
            CatalogViewModel vm = CreateViewModel();
            vm.SelectedGenre = "Action";
            await Task.Delay(50);

            vm.ClearFiltersCommand.Execute(null);
            await Task.Delay(100);

            Assert.Equal("All Genres", vm.SelectedGenre);
        }

        [Fact]
        public async Task ClearFiltersCommand_WhenFiltersActive_ResetsMinimumRatingToZero()
        {
            CatalogViewModel vm = CreateViewModel();
            vm.MinimumRating = 7.0;
            await Task.Delay(50);

            vm.ClearFiltersCommand.Execute(null);
            await Task.Delay(100);

            Assert.Equal(0, vm.MinimumRating);
        }
        [Fact]
        public void SearchQuery_WhenSet_RaisesPropertyChangedEvent()
        {
            CatalogViewModel vm = CreateViewModel();
            bool changed = false;
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(vm.SearchQuery))
                {
                    changed = true;
                }
            };

            vm.SearchQuery = "new query";

            Assert.True(changed);
        }

        [Fact]
        public void SelectedGenre_WhenSet_RaisesPropertyChangedEvent()
        {
            CatalogViewModel vm = CreateViewModel();
            bool changed = false;
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(vm.SelectedGenre))
                {
                    changed = true;
                }
            };

            vm.SelectedGenre = "Action";

            Assert.True(changed);
        }
    }
}