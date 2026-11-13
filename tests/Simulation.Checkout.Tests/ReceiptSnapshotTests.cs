using Moq;
using Simulation.Checkout.Application;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Simulation.Checkout.Tests;

public class ReceiptSnapshotTests
{
    private readonly Mock<IInventoryService> _inventoryMock = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<INotificationService> _notificationService = new();

    [Fact]
    public async Task Receipt_Snapshot_Matches_ExampleC()
    {
        // 1. Arrange - 
        var order = new Order
        {
            CustomerName = "Mostafa Kamal",
            CustomerEmail = "mostafa@example.com",
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    ProductId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    ProductName = "Laptop",
                    Quantity = 3,
                    UnitPrice = 500m
                }
            }
        };

        _inventoryMock.Setup(x => x.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new CheckoutService(_orderRepository.Object, _inventoryMock.Object, _notificationService.Object);

        // 2. Act
        var result = await service.CheckoutAsync(order);

        // 3. Assert (مكتبة Verify بتقارن الإيصال بالملف الثابت مع تجاهل المتغيرات)
        // حليتلي مشكله تكرار should 
        await Verify(result)
            .ScrubMember("OrderId")     // بنخفي أو بنثبت الـ OrderId عشان ما يضربش التست
            .ScrubMember("CreatedAt");  // بنخفي التاريخ والوقت لنفس السبب
    }
}
