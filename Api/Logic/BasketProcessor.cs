using Api.Client;
using Api.Models;
using Api.Models.RPC;
using Api.Utils;
using Address = Api.Models.RPC.Address;
using Contact = Api.Models.RPC.Contact;
using Payment = Api.Models.RPC.Payment;

namespace Api.Logic
{
    public interface IBasketProcessor
    {
        Task ProcessBasket(CreateBasketRequest order);
    }

    public class BasketProcessor : IBasketProcessor
    {
        private static readonly HttpApi Http = new HttpApi();
        private readonly IRpcClient rpcClient;
        private readonly IConfiguration configuration;
        private readonly ILogger<object> logger;

        public BasketProcessor(IRpcClient rpcClient, ILogger<object> logger, IConfiguration configuration)
        {
            this.rpcClient = rpcClient;
            this.logger = logger;
            this.configuration = configuration;
        }

        public async Task ProcessBasket(CreateBasketRequest basket)
        {
            int basketId = -1;
            try
            {
                var deliveryContact = new Contact
                {
                    Salutation = basket.DeliveryAddress.Salutation,
                    EmailAddress = basket.DeliveryAddress.Email,
                    FirstName = basket.DeliveryAddress.FirstName,
                    LastName = basket.DeliveryAddress.Surname,
                    Address = new Address
                    {
                        Country = basket.DeliveryAddress.Address.Country,
                        County = basket.DeliveryAddress.Address.Lines[3],
                        Line1 = basket.DeliveryAddress.Address.Lines[0],
                        Line2 = basket.DeliveryAddress.Address.Lines[1],
                        Line3 = basket.DeliveryAddress.Address.Lines[2],
                        PostCode = basket.DeliveryAddress.Address.PostCode
                    }
                };

                Contact billing = null;
                if (basket.Billing != null)
                {
                    billing = new Contact
                    {
                        Salutation = basket.Billing.Salutation,
                        EmailAddress = basket.Billing.Email,
                        FirstName = basket.Billing.FirstName,
                        LastName = basket.Billing.Surname,
                        Address = new Address
                        {
                            Country = basket.Billing.Address.Country,
                            County = basket.Billing.Address.Lines[3],
                            Line1 = basket.Billing.Address.Lines[0],
                            Line2 = basket.Billing.Address.Lines[1],
                            Line3 = basket.Billing.Address.Lines[2],
                            PostCode = basket.Billing.Address.PostCode
                        }
                    };
                }

                var newBasket = new Basket
                {
                    CustomerId = basket.CustomerId ?? -1,
                    Billing = billing,
                    Delivery = deliveryContact,
                    Payment = new Payment
                    {
                        Type = Enum.Parse<PaymentType>(basket.Payment.PaymentType),
                        PaymentToken = basket.Payment.Token
                    },
                    Products = basket.Products.SelectMany(op => Enumerable.Range(0, op.Quantity).Select(m => new BasketProduct {Adjustment = 0.0, ProductId = op.ProductId})).ToList()
                };

                basketId = await rpcClient.CallCreateBasketAsync(newBasket);
            }
            catch (Exception)
            {
                logger.LogInformation("Basket failed to create");
                throw new Exception("Basket is not valid.");
            }
            finally
            {
                await Http.Send(new HttpRequestMessage(HttpMethod.Put, configuration.GetValue<string>("api:basketcreated:url") + basketId));
            }
        }
    }
}
