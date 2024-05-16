namespace Api.Models
{
    public class Contact
    {
        public string Salutation { get; set; }
        public string FirstName { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public AddressInfo Address { get; set; }

        public class AddressInfo
        {
            public string[] Lines { get; set; }
            public string Country { get; set; }
            public string PostCode { get; set; }
        }
    }
}