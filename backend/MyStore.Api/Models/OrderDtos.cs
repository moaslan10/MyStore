namespace MyStore.Api.Models;
public record OrderItemRequest(int ProductId,int Quantity);
public record CreateOrderRequest(string FullName,string Phone,string Address,string City,string PaymentMethod,List<OrderItemRequest> Items);