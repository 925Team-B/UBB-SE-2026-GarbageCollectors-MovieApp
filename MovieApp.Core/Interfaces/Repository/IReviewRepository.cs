using MovieApp.Core.Models;

namespace MovieApp.Core.Interfaces.Repository;

public interface IReviewRepository
{
    public List<Review> GetAll();
    public Review? GetById(int id);
    public int Insert(Review review);
    public bool Update(Review review);
    public bool Delete(int id);
    
}