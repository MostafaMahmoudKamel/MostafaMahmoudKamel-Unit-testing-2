using FluentAssertions;
using Moq;
using Simulation.Checkout.Application;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Simulation.Checkout.Tests.TestData;

public class Task3Test
{
    private readonly Mock<IInventoryService> _inventoryMock = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<INotificationService> _notificationService = new();

    [Fact]
    public async Task CreateFixtureOrder_CheckQuantityAndPrice_Succeeds()
    {
        //arrange
        var order = OrderDataHelper.CreateFixtureOrder();

        _inventoryMock.Setup(x => x.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new CheckoutService(_orderRepository.Object, _inventoryMock.Object, _notificationService.Object);

        // 2. Act
        var result = await service.CheckoutAsync(order);

        // 3. Assert (العملية نجحت والأوردر اتحفظ مرة واحدة)
        result.Should().NotBeNull();
        _orderRepository.Verify(r => r.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckoutAsync_UsingBogusSeederHelper_ReceiptHasSameNameAndEmail()
    {
        // 1. Arrange (بنجيب الأوردر ببيانات Bogus الثابتة من الهيلبر)
        var order = OrderDataHelper.CreateBogousSeeder();

        string expectedName = order.CustomerName;
        string expectedEmail = order.CustomerEmail;

        _inventoryMock.Setup(x => x.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new CheckoutService(_orderRepository.Object, _inventoryMock.Object, _notificationService.Object);

        // 2. Act
        var result = await service.CheckoutAsync(order);

        // 3. Assert (التأكد إن الإيصال جواه نفس الاسم والإيميل اللي تم توليدهم)
        result.CustomerName.Should().Be(expectedName);
        result.CustomerEmail.Should().Be(expectedEmail);
    }
}
