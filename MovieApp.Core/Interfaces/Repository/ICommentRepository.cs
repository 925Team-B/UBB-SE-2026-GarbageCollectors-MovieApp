using MovieApp.Core.Models;

namespace MovieApp.Core.Interfaces.Repository;

public interface ICommentRepository
{
    public List<Comment> GetAll();
    public Comment? GetById(int id);
    public int Insert(Comment comment);
    public bool Update(Comment comment);
    public bool Delete(int id);
}