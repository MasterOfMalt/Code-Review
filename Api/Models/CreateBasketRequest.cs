using System.Collections.Generic;

namespace Api.Models
{
    public class CreateBasketRequest
    {
        public Payment Payment { get; set; }
        public Contact DeliveryAddress { get; set; }
        public Contact Billing { get; set; }
        public int? CustomerId { get; set; }
        public List<BasketItem> Products { get; set; }
    }
}