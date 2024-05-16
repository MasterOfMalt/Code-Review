using System;

namespace Atom.Interview.Example.Data.Models
{
    public class OrderData
    {
        public int OrderId { get; set; }
        public int? CustomerId { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }
        public string PaymentToken { get; set; }
        public int PaymentTypeId { get; set; }
        public int DeliveryContactId { get; set; }
        public string DeliveryFirstName { get; set; }
        public string DeliveryLastName { get; set; }
        public string DeliverySalutation { get; set; }
        public string DeliveryEmailAddress { get; set; }
        public string DeliveryLine1 { get; set; }
        public string DeliveryLine2 { get; set; }
        public string DeliveryLine3 { get; set; }
        public string DeliveryCountry { get; set; }
        public string DeliveryCounty { get; set; }
        public string DeliveryPostCode { get; set; }
        public int? BillingContactId { get; set; }
        public string BillingFirstName { get; set; }
        public string BillingLastName { get; set; }
        public string BillingSalutation { get; set; }
        public string BillingEmailAddress { get; set; }
        public string BillingLine1 { get; set; }
        public string BillingLine2 { get; set; }
        public string BillingLine3 { get; set; }
        public string BillingCountry { get; set; }
        public string BillingCounty { get; set; }
        public string BillingPostCode { get; set; }
    }
}
