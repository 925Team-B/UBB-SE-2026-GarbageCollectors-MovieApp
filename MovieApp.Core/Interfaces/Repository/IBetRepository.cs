using MovieApp.Core.Models;

namespace MovieApp.Core.Interfaces.Repository;

public interface IBetRepository
{
    public List<Bet> GetAll();
    public Bet? GetById(int userId, int battleId);
    public bool Insert(Bet bet);
    public bool Update(Bet bet);
    public bool Delete(int userId, int battleId);
   public void DeleteByBattleId(int battleId);
}