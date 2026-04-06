using MovieApp.Core.Models;

namespace MovieApp.Core.Interfaces.Repository;

public interface IMovieRepository
{
    public List<Movie> GetAll();
    public Movie? GetById(int id);
    public int Insert(Movie movie);
    public bool Update(Movie movie);
    public bool Delete(int id);
}