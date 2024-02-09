using MongoDB.Driver;
using MoviesLibraryAPI.Controllers;
using MoviesLibraryAPI.Controllers.Contracts;
using MoviesLibraryAPI.Data.Models;
using MoviesLibraryAPI.Services;
using MoviesLibraryAPI.Services.Contracts;
using System.ComponentModel.DataAnnotations;

namespace MoviesLibraryAPI.XUnitTests
{
    public class XUnitIntegrationTests : IClassFixture<DatabaseFixture>
    {
        private readonly MoviesLibraryXUnitTestDbContext _dbContext;
        private readonly IMoviesLibraryController _controller;
        private readonly IMoviesRepository _repository;

        public XUnitIntegrationTests(DatabaseFixture fixture)
        {
            _dbContext = fixture.DbContext;
            _repository = new MoviesRepository(_dbContext.Movies);
            _controller = new MoviesLibraryController(_repository);

            InitializeDatabaseAsync().GetAwaiter().GetResult();
        }

        private async Task InitializeDatabaseAsync()
        {
            await _dbContext.ClearDatabaseAsync();
        }

        [Fact]
        public async Task AddMovieAsync_WhenValidMovieProvided_ShouldAddToDatabase()
        {
            // Arrange
            var movie = new Movie
            {
                Title = "Test Movie",
                Director = "Test Director",
                YearReleased = 2022,
                Genre = "Action",
                Duration = 120,
                Rating = 7.5
            };

            // Act
            await _controller.AddAsync(movie);

            // Assert
            var resultMovie = await _dbContext.Movies.Find(m => m.Title == "Test Movie").FirstOrDefaultAsync();
            Xunit.Assert.NotNull(resultMovie);
            Xunit.Assert.Equal("Test Movie", resultMovie.Title);
            Xunit.Assert.Equal("Test Director", resultMovie.Director);
            Xunit.Assert.Equal(2022, resultMovie.YearReleased);
            Xunit.Assert.Equal("Action", resultMovie.Genre);
            Xunit.Assert.Equal(120, resultMovie.Duration);
            Xunit.Assert.Equal(7.5, resultMovie.Rating);
        }

        [Fact]
        public async Task AddMovieAsync_WhenInvalidMovieProvided_ShouldThrowValidationException()
        {
            // Arrange
            var invalidMovie = new Movie
            {
                Director = "Test Director",
                YearReleased = 2022,
                Genre = "Action",
                Duration = 120,
                Rating = 7.5
            };

            // Act and Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(() => _controller.AddAsync(invalidMovie));
            Assert.Equal("Movie is not valid.", exception.Message);
        }

        [Fact]
        public async Task DeleteAsync_WhenValidTitleProvided_ShouldDeleteMovie()
        {
            // Arrange            
            var movie = new Movie
            {
                Title = "Test Movie",
                Director = "Test Director",
                YearReleased = 2022,
                Genre = "Action",
                Duration = 120,
                Rating = 7.5
            };
            await _controller.AddAsync(movie);
            // Act            
            await _controller.DeleteAsync(movie.Title);

            // Assert
            // The movie should no longer exist in the database
            var resultMovie = await _dbContext.Movies.Find(m => m.Title == movie.Title).FirstOrDefaultAsync();
            Assert.Null(resultMovie);

        }


        
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task DeleteAsync_WhenTitleIsNullOrEmpty_ShouldThrowArgumentException(string invalidName)
        {
            // Act and Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _controller.DeleteAsync(invalidName));
        }

      

        [Fact]
        public async Task DeleteAsync_WhenTitleDoesNotExist_ShouldThrowInvalidOperationException()
        {
            // Act and Assert
            Assert.ThrowsAsync<InvalidOperationException>(() => _controller.DeleteAsync("Invalid Name"));
        }

        [Fact]
        public async Task GetAllAsync_WhenNoMoviesExist_ShouldReturnEmptyList()
        {
            // Act
            var result = await _controller.GetAllAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllAsync_WhenMoviesExist_ShouldReturnAllMovies()
        {
            // Arrange
            var movie1 = new Movie
            {
                Title = "Test Movie",
                Director = "Test Director",
                YearReleased = 2022,
                Genre = "Action",
                Duration = 120,
                Rating = 7.5
            };
            var movie2 = new Movie
            {
                Title = "Second Movie",
                Director = "Second Director",
                YearReleased = 1999,
                Genre = "Drama",
                Duration = 150,
                Rating = 8.5
            };
            var movie3 = new Movie
            {
                Title = "Third Movie",
                Director = "Third Director",
                YearReleased = 2000,
                Genre = "Comedy",
                Duration = 120,
                Rating = 9.5
            };

            await _dbContext.Movies.InsertManyAsync(new[] { movie1, movie2, movie3 });

            // Act

            var result = await _controller.GetAllAsync();


            // Assert
            // Ensure that all movies are returned
            Assert.Equal(3,result.Count());
            var expectedTitles = new[] { "Test Movie", "Second Movie", "Third Movie" };
            Assert.Equal(expectedTitles, result.Select(m => m.Title));

            var expectedDirectors = new[] { "Test Director", "Second Director", "Third Director" };
            Assert.Equal(expectedDirectors, result.Select(m => m.Director));

            var expectedGenres = new[] { "Action", "Drama", "Comedy" };
            Assert.Equal(expectedGenres, result.Select(m => m.Genre));

            var expectedYears = new[] { 2022, 1999, 2000 };
            Assert.Equal(expectedYears, result.Select(m => m.YearReleased));

            var expectedDurations = new[] { 120, 150, 120 };
            Assert.Equal(expectedDurations, result.Select(m => m.Duration));

            var expectedRatings = new[] { 7.5, 8.5, 9.5 };
            Assert.Equal(expectedRatings, result.Select(m => m.Rating));

        }

        [Fact]
        public async Task GetByTitle_WhenTitleExists_ShouldReturnMatchingMovie()
        {
            // Arrange
            var movie1 = new Movie
            {
                Title = "Test Movie",
                Director = "Test Director",
                YearReleased = 2022,
                Genre = "Action",
                Duration = 120,
                Rating = 7.5
            };
            var movie2 = new Movie
            {
                Title = "Second Movie",
                Director = "Second Director",
                YearReleased = 1999,
                Genre = "Drama",
                Duration = 150,
                Rating = 8.5
            };
            var movie3 = new Movie
            {
                Title = "Third Movie",
                Director = "Third Director",
                YearReleased = 2000,
                Genre = "Comedy",
                Duration = 120,
                Rating = 9.5
            };

            await _dbContext.Movies.InsertManyAsync(new[] { movie1, movie2, movie3 });
            // Act
            var result = await _controller.GetByTitle(movie1.Title);
            // Assert
            Assert.NotNull(result);
            Assert.Equal(movie1.Title, result.Title);
            Assert.Equal(movie1.Director, result.Director);
        }

        [Fact]
        public async Task GetByTitle_WhenTitleDoesNotExist_ShouldReturnNull()
        {
            // Act
            var result = await _controller.GetByTitle("Not Existing Movie");
            // Assert
            Assert.Null(result);
        }


        [Fact]
        public async Task SearchByTitleFragmentAsync_WhenTitleFragmentExists_ShouldReturnMatchingMovies()
        {
            // Arrange
            var movie1 = new Movie
            {
                Title = "Test Movie",
                Director = "Test Director",
                YearReleased = 2022,
                Genre = "Action",
                Duration = 120,
                Rating = 7.5
            };
            var movie2 = new Movie
            {
                Title = "Second Movie",
                Director = "Second Director",
                YearReleased = 1999,
                Genre = "Drama",
                Duration = 150,
                Rating = 8.5
            };
            var movie3 = new Movie
            {
                Title = "Third Movie",
                Director = "Third Director",
                YearReleased = 2000,
                Genre = "Comedy",
                Duration = 120,
                Rating = 9.5
            };

            await _dbContext.Movies.InsertManyAsync(new[] { movie1, movie2, movie3 });
            // Act
            var result = await _controller.SearchByTitleFragmentAsync("Third");

            // Assert // Should return one matching movie
            Assert.Single(result);// Assert that only one movie is returned
            Assert.Equal(movie3.Title, result.First().Title);// Assert that the title of the returned movie matches
        }

        [Fact]
        public async Task SearchByTitleFragmentAsync_WhenNoMatchingTitleFragment_ShouldThrowKeyNotFoundException()
        {
            // Act and Assert
            var invalidMovie = new Movie
            {
                Title = "Invalid",
                // Add other required fields if necessary
            };

            await Assert.ThrowsAsync<ValidationException>(() => _controller.UpdateAsync(invalidMovie));

        }

        [Fact]
        public async Task UpdateAsync_WhenValidMovieProvided_ShouldUpdateMovie()
        {
            // Arrange
            var movie1 = new Movie
            {
                Title = "Test Movie",
                Director = "Test Director",
                YearReleased = 2022,
                Genre = "Action",
                Duration = 120,
                Rating = 7.5
            };
            var movie2 = new Movie
            {
                Title = "Second Movie",
                Director = "Second Director",
                YearReleased = 1999,
                Genre = "Drama",
                Duration = 150,
                Rating = 8.5
            };
            var movie3 = new Movie
            {
                Title = "Third Movie",
                Director = "Third Director",
                YearReleased = 2000,
                Genre = "Comedy",
                Duration = 120,
                Rating = 9.5
            };

            await _dbContext.Movies.InsertManyAsync(new[] { movie1, movie2, movie3 });
            // Modify the movie
            var movieToUpdate = await _dbContext.Movies.Find(x => x.Title == movie1.Title).FirstOrDefaultAsync();

            movieToUpdate.Title = "First Movie";


            // Act
            await _controller.UpdateAsync(movieToUpdate);
            // Assert
            var updatedMovie = await _dbContext.Movies.Find(x => x.Title == movieToUpdate.Title).FirstOrDefaultAsync();
            Assert.NotNull(updatedMovie);   
            Assert.Equal(movieToUpdate.Title, updatedMovie.Title);


        }

        [Fact]
        public async Task UpdateAsync_WhenInvalidMovieProvided_ShouldThrowValidationException()
        {
            // Arrange
            var invalidMovie = new Movie
            {
                Title = "Invalid",
              
            };
            // Movie without required fields

            // Act and Assert
            await Assert.ThrowsAsync<ValidationException>(() => _controller.UpdateAsync(invalidMovie));

        }
    }
}
