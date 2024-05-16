using System.Collections.Generic;
using System.Threading.Tasks;
using Atom.Interview.Example.Models;

namespace Atom.Interview.Example.Data
{
    public interface IOrderRepository
    {
        Task<IReadOnlyCollection<Order>> GetOrders(string cutoff);
        Task<int> CreateOrder(Order order);
    }
}
