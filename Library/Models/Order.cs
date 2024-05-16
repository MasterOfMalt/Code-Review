using System.Collections.Generic;

namespace Atom.Interview.Example.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public Contact Billing { get; set; }
        public Contact Delivery { get; set; }
        public int CustomerId { get; set; } = -1;
        public List<OrderProduct> Products { get; set; }
        public Payment Payment { get; set; }
    }
}
