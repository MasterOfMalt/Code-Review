using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Atom.Interview.Example.Data.Models;
using Atom.Interview.Example.Models;
using Atom.Interview.Example.Utils;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.SqlServer.Server;

namespace Atom.Interview.Example.Data
{
    public class OrderRepository : IOrderRepository
    {
        private readonly IConfiguration config;

        public OrderRepository(IConfiguration config)
        {
            this.config = config;
        }

        public async Task<IReadOnlyCollection<Order>> GetOrders(string cutoff)
        {
            using (var conn = new SqlConnection(config.GetConnectionString("orders")))
            {
                var getOrderSql = @$"
                            SELECT
                                o.OrderId, 
                                o.CustomerId, 
                                o.OrderDate,
                                o.Status,
                                p.PaymentToken, 
                                p.PaymentTypeId, 
                                dc.ContactId as DeliveryContactId,
                                dc.FirstName as DeliveryFirstName,
                                dc.LastName as DeliveryLastName, 
                                dc.Salutation as DeliverySalutation,
                                dc.EmailAddress as DeliveryEmailAddress,
                                da.Line1 as DeliveryLine1,
                                da.Line2 as DeliveryLine2,
                                da.Line3 as DeliveryLine3,
                                da.Country as DeliveryCountry,
                                da.County as DeliveryCounty,
                                da.PostCode as DeliveryPostCode,
                                bc.ContactId as BillingContactId,
                                bc.FirstName as BillingFirstName,
                                bc.LastName as BillingLastName, 
                                bc.Salutation as BillingSalutation,
                                bc.EmailAddress as BillingEmailAddress,
                                ba.Line1 as BillingLine1,
                                ba.Line2 as BillingLine2,
                                ba.Line3 as BillingLine3,
                                ba.Country as BillingCountry,
                                ba.County as BillingCounty,
                                ba.PostCode as BillingPostCode
                            FROM 
                                [Orders] o
                            inner join 
                                [dbo].[Payment] p ON o.PaymentId = p.PaymentId
                            inner join
                                [dbo].[Contact] dc ON dc.ContactId = o.DeliveryContactId
                            inner join
                                [dbo].[Address] da ON dc.AddressId = da.AddressId
                            left join
                                [dbo].[Contact] bc ON bc.ContactId = o.BillingContactId
                            left join
                                [dbo].[Address] ba ON ba.AddressId = bc.AddressId
                            WHERE
                                o.[OrderDate] BETWEEN '{cutoff}' AND '{DateTime.Now}'
                        ";

                var orders = await conn.QueryAsync<OrderData>(getOrderSql);

                var orderIdsList = string.Join(", ", orders.Select(o => o.OrderId));

                var orderProductSql = @$"
                            SELECT
                                o.OrderId, 
                                op.ProductId, 
                                p.Name as [ProductName],
                                p.Price,
                                op.Adjustment
                            FROM 
                                [Orders] o
                            inner join 
                                [dbo].[OrderProduct] op ON o.OrderId = op.OrderId
                            inner join
                                [dbo].[Products] p ON op.ProductId = p.ProductId
                            left join
                                [dbo].[OrderProductAdjustments] opa ON opa.OrderId = o.OrderId AND opa.ProductId = p.ProductId                           
                            WHERE
                                o.[OrderId]  in ({orderIdsList})
                        ";

                var orderProducts = await conn.QueryAsync<OrderProductData>(orderProductSql);

                return await Task.WhenAll(orders.GroupJoin(orderProducts, o => o.OrderId, op => op.OrderId, Map));
            }
        }

        protected Task<Order> Map(OrderData order, IEnumerable<OrderProductData> products)
        {
            var delivery = new Contact
            {
                Salutation = order.DeliverySalutation,
                ContactId = order.DeliveryContactId,
                EmailAddress = order.DeliveryEmailAddress,
                FirstName = order.DeliveryFirstName,
                LastName = order.DeliveryLastName,
                Address = new Address
                {
                    Country = order.DeliveryCounty,
                    County = order.DeliveryCounty,
                    Line1 = order.DeliveryLine1,
                    Line2 = order.DeliveryLine2,
                    Line3 = order.DeliveryLine3,
                    PostCode = order.DeliveryPostCode
                }
            };

            Contact billing = null;
            if (!order.BillingContactId.HasValue)
            {
                billing = delivery;
            }
            else
            {
                billing = new Contact
                {
                    Salutation = order.BillingSalutation,
                    ContactId = order.BillingContactId.Value,
                    EmailAddress = order.BillingEmailAddress,
                    FirstName = order.BillingFirstName,
                    LastName = order.BillingLastName,
                    Address = new Address
                    {
                        Country = order.BillingCounty,
                        County = order.BillingCounty,
                        Line1 = order.BillingLine1,
                        Line2 = order.BillingLine2,
                        Line3 = order.BillingLine3,
                        PostCode = order.BillingPostCode
                    }
                };
            }

            return Task.FromResult(new Order
            {
                Billing = billing,
                Delivery = delivery,
                Payment = new Payment {Type = (PaymentType) order.PaymentTypeId, PaymentToken = order.PaymentToken},
                CustomerId = order.CustomerId.HasValue ? order.CustomerId.Value : -1,
                OrderId = order.OrderId,
                Products = products.Select(op => new OrderProduct {ProductId = op.ProductId, Price = op.Price, ProductName = op.ProductName, Adjustment = op.Adjustment ?? 0}).ToList()
            });
        }

        public async Task<int> CreateOrder(Order order)
        {
            using (var conn = new SqlConnection(config.GetConnectionString("orders")))
            {
                var addressArgs = new
                {
                    order.Delivery.Address.Line1,
                    order.Delivery.Address.Line2,
                    order.Delivery.Address.Line3,
                    order.Delivery.Address.Country,
                    order.Delivery.Address.County,
                    order.Delivery.Address.PostCode
                };

                var addressId = await conn.ExecuteScalarAsync<int>("[dbo].[CreateAddress]", addressArgs, commandType: CommandType.StoredProcedure);

                var contactArgs = new
                {
                    order.Delivery.EmailAddress,
                    order.Delivery.FirstName,
                    order.Delivery.LastName,
                    AddressId = addressId
                };

                var deliveryContactId = await conn.ExecuteScalarAsync<int>("[dbo].[CreateContact]", contactArgs, commandType: CommandType.StoredProcedure);
                await HttpApi.Instance.Send(new HttpRequestMessage(HttpMethod.Put, config.GetValue<string>("api:contactcreated:url") + deliveryContactId));
                int billingContactId = deliveryContactId;

                if (order.Billing != null)
                {
                    addressArgs = new
                    {
                        order.Billing.Address.Line1,
                        order.Billing.Address.Line2,
                        order.Billing.Address.Line3,
                        order.Billing.Address.Country,
                        order.Billing.Address.County,
                        order.Billing.Address.PostCode
                    };

                    addressId = await conn.ExecuteScalarAsync<int>("[dbo].[CreateAddress]", addressArgs, commandType: CommandType.StoredProcedure);

                    contactArgs = new
                    {
                        order.Billing.EmailAddress,
                        order.Billing.FirstName,
                        order.Billing.LastName,
                        AddressId = addressId
                    };

                    billingContactId = await conn.ExecuteScalarAsync<int>("[dbo].[CreateContact]", contactArgs, commandType: CommandType.StoredProcedure);
                    await HttpApi.Instance.Send(new HttpRequestMessage(HttpMethod.Put, config.GetValue<string>("api:contactcreated:url")+ billingContactId));
                }

                var paymentArgs = new
                {
                    PaymentTypeName = order.Payment.Type.ToString(),
                    Token = order.Payment.PaymentToken
                };

                var paymentId = await conn.ExecuteScalarAsync<int>("[dbo].[LookupPayment]", paymentArgs, commandType: CommandType.StoredProcedure);

                var orderArgs = new
                {
                    CustomerId = order.CustomerId == -1 ? (int?) null : order.CustomerId,
                    OrderDate = DateTime.UtcNow,
                    Status = "new",
                    PaymentId = paymentId,
                    BillingContactId = billingContactId,
                    DeliveryContactId = deliveryContactId
                };

                var orderId = await conn.ExecuteScalarAsync<int>("[dbo].[InsertOrder]", orderArgs, commandType: CommandType.StoredProcedure);

                var orderProducts = CreateOrderProductArgs(order.Products, orderId).AsTableValuedParameter("TT_OrderProduct");
                await conn.ExecuteAsync("[dbo].[InsertProductsToOrder]", new {Products = orderProducts}, commandType: CommandType.StoredProcedure);

                return orderId;
            }
        }

        public static IEnumerable<SqlDataRecord> CreateOrderProductArgs(IEnumerable<OrderProduct> products, int orderId)
        {
            var orderIdMeta = new SqlMetaData("OrderId", SqlDbType.Int);
            var productIdMeta = new SqlMetaData("ProductId", SqlDbType.Int);
            var adjustmentMeta = new SqlMetaData("Adjustment", SqlDbType.Decimal);
            var record = new SqlDataRecord(orderIdMeta,productIdMeta,adjustmentMeta);
            foreach (var orderProduct in products)
            {
                record.SetInt32(0, orderId);
                record.SetInt32(1, orderProduct.ProductId);
                record.SetDouble(2, orderProduct.Adjustment);
                yield return record;
            }
        }
    }
}
