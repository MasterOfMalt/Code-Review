using Api.Client;
using Api.Models;
using Api.Models.RPC;
using System.Collections.Concurrent;

namespace Api.Logic
{
    public interface IBasketQuerier
    {
        Task<IReadOnlyCollection<Basket>> GetBaskets(BasketFilters filters);
        Task<Basket> GetBasket(int id);
    }

    public class BasketQuerier : IBasketQuerier
    {
        private readonly ConcurrentDictionary<int, Basket> basketCache = new ConcurrentDictionary<int, Basket>();
        private readonly IRpcClient client;

        public BasketQuerier(IRpcClient client)
        {
            this.client = client;
        }

        public async Task<IReadOnlyCollection<Basket>> GetBaskets(BasketFilters filters)
        {
            var baskets = await client.CallGetBasketsAsync(filters.Since);

            foreach (var basket in baskets)
            {
                basketCache.TryUpdate(basket.BasketId, basket, basket);
            }

            return baskets;
        }

        public async Task<Basket> GetBasket(int id)
        {
            try
            {
                if (basketCache.TryGetValue(id, out var basket)) return basket;

                var baskets = await GetBaskets(new BasketFilters {Since = DateTime.MinValue.ToString("u")});

                return baskets.First(o => o.BasketId == id);
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
