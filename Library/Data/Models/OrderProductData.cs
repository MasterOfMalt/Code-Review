namespace Atom.Interview.Example.Data.Models
{
    public class OrderProductData
    {
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public double Price { get; set; }
        public double? Adjustment { get; set; }
    }
}
