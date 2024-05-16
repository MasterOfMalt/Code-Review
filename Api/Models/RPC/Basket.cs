namespace Api.Models.RPC
{
    public class Basket
    {
        public int BasketId { get; set; }
        public Contact Billing { get; set; }
        public Contact Delivery { get; set; }
        public int CustomerId { get; set; } = -1;
        public List<BasketProduct> Products { get; set; }
        public Payment Payment { get; set; }
    }
}
