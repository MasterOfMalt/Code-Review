namespace Api.Models.RPC
{
    public class Payment
    {
        public string PaymentToken { get; set; }
        public PaymentType Type { get; set; }
    }
}
