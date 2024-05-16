namespace Atom.Interview.Example.Models
{
    public class Contact
    {
        public int ContactId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Salutation { get; set; }
        public string EmailAddress { get; set; }
        public Address Address { get; set; }
    }
}
