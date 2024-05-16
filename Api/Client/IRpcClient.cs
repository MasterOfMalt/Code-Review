using Api.Models.RPC;

namespace Api.Client
{
    public interface IRpcClient
    {
        public Task<int> CallCreateBasketAsync(Basket newBasket, CancellationToken cancellationToken = default);

        public Task<IReadOnlyCollection<Basket>> CallGetBasketsAsync(string since, CancellationToken cancellationToken = default);
    }
}
