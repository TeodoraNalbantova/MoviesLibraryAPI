using DnsClient;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using MoviesLibraryAPI.Controllers;
using MoviesLibraryAPI.Controllers.Contracts;
using MoviesLibraryAPI.Data.Models;
using MoviesLibraryAPI.Services;
using MoviesLibraryAPI.Services.Contracts;
using System.ComponentModel.DataAnnotations;

namespace MoviesLibraryAPI.Tests
{
    [TestFixture]
    public class NUnitIntegrationTests
    {
        private MoviesLibraryNUnitTestDbContext _dbContext;
        private IMoviesLibraryController _controller;
        private IMoviesRepository _repository;
        IConfiguration _configuration;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
        }

        [SetUp]
        public async Task Setup()
        {
            string dbName = $"MoviesLibraryTestDb_{Guid.NewGuid()}";
            _dbContext = new MoviesLibraryNUnitTestDbContext(_configuration, dbName);

            _repository = new MoviesRepository(_dbContext.Movies);
            _controller = new MoviesLibraryController(_repository);
        }

        [TearDown]
        public async Task TearDown()
        {
            await _dbContext.ClearDatabaseAsync();
        }

        [Test]
        public async Task AddMovieAsync_WhenValidMovieProvided_ShouldAddToDatabase()
        {
            // Arrange
            var movie = new Movie
            {
                Title = "Test Movie",
                Director = "Test Director",
                YearReleased = 2022,
                Genre = "Action",
                Duration = 86,
                Rating = 7.5
            };

            // Act
            await _controller.AddAsync(movie);

            // Assert
            var resultMovie = await _dbContext.Movies.Find(m => m.Title == "Test Movie").FirstOrDefaultAsync();
            //всеки метод, който завършва на async изисква await.
            Assert.IsNotNull(resultMovie);
        }

        [Test]
        public async Task AddMovieAsync_WhenInvalidMovieProvided_ShouldThrowValidationException()
        {
            // Arrange
            var invalidMovie = new Movie
            {
                // Provide an invalid movie object, for example, missing required fields like 'Title'
                // Assuming 'Title' is a required field, do not set it
                Director = "Test Director",
                YearReleased = 2022,
                Genre = "Action",
                Duration = 86,
                Rating = 7.5
            };

            // Act and Assert
            // Expect a ValidationException because the movie is missing a required field
            var exception = Assert.ThrowsAsync<ValidationException>(() => _controller.AddAsync(invalidMovie));
        }

        [Test]
        public async Task DeleteAsync_WhenValidTitleProvided_ShouldDeleteMovie()
        {
            // Arrange            
            var movie = new Movie
            {
                Title = "Test Movie",
                Director = "Test Director",
                YearReleased = 2022,
                Genre = "Action",
                Duration = 86,
                Rating = 7.5
            };
            await _controller.AddAsync(movie);

            // Act            
            await _controller.DeleteAsync(movie.Title);
            // Assert
            // The movie should no longer exist in the database
            var result = await _dbContext.Movies.Find(m => m.Title == movie.Title).FirstOrDefaultAsync();
            Assert.IsNull(result);
        }


        [Test]
        public async Task DeleteAsync_WhenTitleIsNull_ShouldThrowArgumentException()
        {
            // Act and Assert

            Assert.ThrowsAsync<ArgumentException>(() => _controller.DeleteAsync(null));
        }

        [Test]
        public async Task DeleteAsync_WhenTitleIsEmpty_ShouldThrowArgumentException()
        {
            // Act and Assert
            Assert.ThrowsAsync<ArgumentException>(() => _controller.DeleteAsync(""));
        }

        [Test]
        public async Task DeleteAsync_WhenTitleDoesNotExist_ShouldThrowInvalidOperationException()
        {
            // Act and Assert
           Assert.ThrowsAsync<InvalidOperationException>(() => _controller.DeleteAsync("NotExistingMovie"));
        }

        [Test]
        public async Task GetAllAsync_WhenNoMoviesExist_ShouldReturnEmptyList()
        {
            // Act
            var result = await _controller.GetAllAsync();

            // Assert
            Assert.IsEmpty(result);
        }

        [Test]
        public async Task GetAllAsync_WhenMoviesExist_ShouldReturnAllMovies()
        {
            // Arrange

            var movie = new Movie
            {
                Title = "Test Movie",
                Director = "Test Director",
                YearReleased = 2022,
                Genre = "Action",
                Duration = 86,
                Rating = 7.5
            };
            await _controller.AddAsync(movie);

            var movie2 = new Movie
            {
                Title = "Another Life",
                Director = "Teddy",
                YearReleased = 2023,
                Genre = "Drama",
                Duration = 120,
                Rating = 6.5
            };

            await _controller.AddAsync(movie2);


            var movie3 = new Movie
            {
                Title = "ScaryMovie",
                Director = "Nical Cage",
                YearReleased = 1999,
                Genre = "Horror",
                Duration = 101,
                Rating = 9.8
            };

            await _controller.AddAsync(movie3);

            // Другият начин за добавяна е чрез пълнене от базата от данни, но не е препоръчително 
            //_dbContext.Movies.InsertMany();

            // Act
            var allMovies = await _controller.GetAllAsync();
            // Assert
            // Ensure that all movies are returned
            Assert.IsNotEmpty(allMovies);
            Assert.That(allMovies.Count(), Is.EqualTo(3));

            var hasFirstMovie = allMovies.Any(x => x.Title == movie.Title);
            Assert.IsTrue(hasFirstMovie);

            var hasSecondMovie = allMovies.Any(x => x.Title == movie2.Title);
            Assert.IsTrue(hasSecondMovie);

            var hasThirdMovie = allMovies.Any(x => x.Title == movie3.Title);
            Assert.IsTrue(hasThirdMovie);
        }

        [Test]
        public async Task GetByTitle_WhenTitleExists_ShouldReturnMatchingMovie()
        {
            // Arrange
            var movie = new Movie
            {
                Title = "Titanic",
                Director = "James Cameron",
                YearReleased = 1977,
                Genre = "DramaRomance",
                Duration = 86,
                Rating = 7.9
            };
            await _controller.AddAsync(movie);

            // Act
            var result = await _controller.GetByTitle(movie.Title);

            // Assert
            Assert.IsNotNull(result);
            Assert.That(result.Title, Is.EqualTo(movie.Title));
            Assert.That(result.Director, Is.EqualTo(movie.Director));
            Assert.That(result.Rating, Is.EqualTo(movie.Rating));
            Assert.That(result.YearReleased, Is.EqualTo(movie.YearReleased));
            Assert.That(result.Genre, Is.EqualTo(movie.Genre));
            Assert.That(result.Duration, Is.EqualTo(movie.Duration));
        }

        [Test]
        public async Task GetByTitle_WhenTitleDoesNotExist_ShouldReturnNull()
        {
            // Act
            var result = await _controller.GetByTitle("Fake Title");
            // Assert
            Assert.IsNull(result);
        }


        [Test]
        public async Task SearchByTitleFragmentAsync_WhenTitleFragmentExists_ShouldReturnMatchingMovies()
        {
            // Arrange
            var movie = new Movie
            {
                Title = "Titanic",
                Director = "James Cameron",
                YearReleased = 1977,
                Genre = "DramaRomance",
                Duration = 86,
                Rating = 7.9
            };
            var movie2 = new Movie
            {
                Title = "Another Life",
                Director = "Teddy",
                YearReleased = 2023,
                Genre = "Drama",
                Duration = 120,
                Rating = 6.5
            };

            await _dbContext.Movies.InsertManyAsync(new[] { movie, movie2 });

            // Act
            var result = await _controller.SearchByTitleFragmentAsync("Life");
            // Assert // Should return one matching movie
            Assert.IsNotEmpty(result);
            Assert.That(result.Count(), Is.EqualTo(1));
            var resultMovie = result.Single();
            Assert.That(resultMovie.Title, Is.Not.EqualTo(movie.Title));
            Assert.That(resultMovie.YearReleased, Is.Not.EqualTo(movie.YearReleased));
        


        }

        [Test]
        public async Task SearchByTitleFragmentAsync_WhenNoMatchingTitleFragment_ShouldThrowKeyNotFoundException()
        {
            // Act and Assert
            Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.SearchByTitleFragmentAsync("Does not exist"));
        }

        [Test]
        public async Task UpdateAsync_WhenValidMovieProvided_ShouldUpdateMovie()
        {
            // Arrange
            var movie = new Movie
            {
                Title = "Titanic",
                Director = "James Cameron",
                YearReleased = 1977,
                Genre = "DramaRomance",
                Duration = 86,
                Rating = 7.9
            };
            var movie2 = new Movie
            {
                Title = "Another Life",
                Director = "Teddy",
                YearReleased = 2023,
                Genre = "Drama",
                Duration = 120,
                Rating = 6.5
            };

            await _dbContext.Movies.InsertManyAsync(new[] { movie, movie2 });
            // Modify the movie
            var movieToUpdate = await _dbContext.Movies.Find(x => x.Title == movie.Title).FirstOrDefaultAsync();
            movieToUpdate.Title = "Titanic2";
            // Act
             await _controller.UpdateAsync(movieToUpdate);

            // Assert
            var result = await _dbContext.Movies.Find(x => x.Title == movieToUpdate.Title).FirstOrDefaultAsync();
            Assert.IsNotNull(result);
            Assert.That(result.Title, Is.EqualTo("Titanic2"));
        }

        [Test]
        public async Task UpdateAsync_WhenInvalidMovieProvided_ShouldThrowValidationException()
        {
            // Arrange
            // Movie without required fields
            var invalidMovie = new Movie 
          {
                Title = "Invalid Life",
                YearReleased = 2023,
                Genre = "Drama",
                Duration = 120,
                Rating = 6.5
            };


            // Act and Assert
            Assert.ThrowsAsync<ValidationException>(() => _controller.UpdateAsync(invalidMovie));
        }


        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await _dbContext.ClearDatabaseAsync();
        }
    }
}
