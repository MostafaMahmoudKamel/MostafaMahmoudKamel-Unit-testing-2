using FluentAssertions;
using Moq;
using Simulation.Checkout.Application;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;
using Simulation.Checkout.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Simulation.Checkout.Tests;

public class CheckoutServiceTests
{
    //moq library
    //i used Mock علشان منبعتش الاوبجكت الحقيقي ونستهلك مثلا داتا بيز حقيقيه احنا بنعمل تيست من اسمها كده فيك اصلا 
    private readonly Mock<IInventoryService> _inventoryMock = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<INotificationService> _notificationService = new();
    [Fact]
    public async Task CheckoutAsync_OrderWithNoItems_ThrowException()
    {
        //arranage  خلي بالك انا بعت القيم هنا لانها required
        Order order = new Order
        {
            CustomerName = "Mostafa",        //dummyData
            CustomerEmail = "mk@gmail.com",
            Items = new List<OrderItem>()
        };
        var service = new CheckoutService(_orderRepository.Object, _inventoryMock.Object, _notificationService.Object);

        //act 
        Func<Task> act = async () => await service.CheckoutAsync(order);

        //asert
        await act.Should().ThrowAsync<EmptyOrderException>();

    }

    //Quantity must be greater than 0, otherwise InvalidQuantityException (message contains the product name).
    [Fact]
    public async Task CheckoutAsync_Quantity_LessThanZero_ThrowInvalidQuantityException()
    {
        //arrange
        Order order = new Order
        {
            CustomerName = "Mostafa",        //dummyData
            CustomerEmail = "mk@gmail.com",
            Items = new List<OrderItem>()
            {
                new OrderItem
                {
                    ProductId=new Guid(),
                    ProductName = "Laptop",
                Quantity = -1, // الكمية بالسالب عشان تضرب الإكسيبشن المطلوب
                UnitPrice = 1000
                }
            },
        };
        var service = new CheckoutService(_orderRepository.Object, _inventoryMock.Object, _notificationService.Object);

        //act 
        Func<Task> act = async () => await service.CheckoutAsync(order);

        //assert
        var exceptionAssert = await act.Should().ThrowAsync<InvalidQuantityException>();
        exceptionAssert.And.Message.Should().Contain("Laptop");

    }

    //BR-03 Price cannot be negative, otherwise InvalidPriceException. Price 0 is allowed.
    [Fact]
    public async Task CheckoutAsync_PriceLessThanZero_ThrowInvalidPriceException()
    {
        //arrange
        Order order = new Order
        {
            CustomerName = "Mostafa",        //dummyData
            CustomerEmail = "mk@gmail.com",
            Items = new List<OrderItem>()
            {
                new OrderItem
                {
                    ProductId=new Guid(),
                    ProductName = "Laptop",
                    Quantity = 1, 
                    UnitPrice = -10
                }
            },

        };
        var service = new CheckoutService(_orderRepository.Object, _inventoryMock.Object, _notificationService.Object);


        //act 
        Func<Task> act = async () => await service.CheckoutAsync(order);

        //Assert
        var exceptionAssert = await act.Should().ThrowAsync<InvalidPriceException>();


    }

    // [  BR-03]: price 0 is allowed test
    [Fact]
    public async Task CheckoutAsync_PriceIsZero_ShouldPassSuccessfully()
    {
        // Arrange
        Order order = new Order
        {
            CustomerName = "Mostafa",
            CustomerEmail = "mk@gmail.com",
            Items = new List<OrderItem>()
            {
                new OrderItem
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "FreeGift",
                    Quantity = 1,
                    UnitPrice = 0 // السعر صفر مسموح   
                }
            },
        };

        //TestDouble type is stub => return fixed values
        _inventoryMock.Setup(x => x.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new CheckoutService(_orderRepository.Object, _inventoryMock.Object, _notificationService.Object);

        // Act
        var result = await service.CheckoutAsync(order);

        // Assert
        result.Subtotal.Should().Be(0);
        result.Total.Should().Be(0);
    }

    //BR-04 Stock is checked for every item. If one is not available → InsufficientInventoryException.
    [Fact]
    public async Task CheckOutAsync_IsnnotAvaliable_ThrowInsufficientInventoryException()
    {
        //arrange
        Order order = new Order
        {
            CustomerName = "Mostafa",
            CustomerEmail = "mk@gmail.com",
            Items = new List<OrderItem>
        {
            new OrderItem
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Laptop",
                Quantity = 5,
                UnitPrice = 1000
            }
        }
        };
                            //.setup() => is moq library  = >if not available return false
        _inventoryMock.Setup(x => x.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var service = new CheckoutService(_orderRepository.Object, _inventoryMock.Object, _notificationService.Object);
        //act
        Func<Task> act = async () => await service.CheckoutAsync(order);

        //
        await act.Should().ThrowAsync<InsufficientInventoryException>();

    }

    //BR-05 Subtotal = sum of UnitPrice × Quantity.
    [Fact]
    public async Task CheckOutAsync_ValidSummation_Pass()
    {
        //arrange
        Order order = new Order
        {
            CustomerName = "Mostafa",
            CustomerEmail = "mk@gmail.com",
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Laptop",
                    Quantity = 2,
                    UnitPrice = 500 // 2 * 500 = 1000
                },
                new OrderItem
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Mouse",
                    Quantity = 3,
                    UnitPrice = 50 // 3 * 50 = 150
                }
            }
        };
        // بنظبط الـ Mock هنا إنه يرجع true عادي لأن الأوردر سليم والمخزن مكفي
        _inventoryMock.Setup(x => x.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new CheckoutService(_orderRepository.Object, _inventoryMock.Object, _notificationService.Object);

        //act
        var result = await service.CheckoutAsync(order);

        //assert
        result.Subtotal.Should().Be(1150);
    }

    //BR-06 Subtotal below 1,000 → discount 0. Subtotal 1,000 or more → 10% discount, but max 100.
    //this business have to Test 1) <1000  , 2) >10000
    [Fact]
    public async Task CheckOutAsync_TestSubTotalDiscount_GetDiscount()
    {
        //arrange
        Order order = new Order
        {
            CustomerName = "Mostafa",
            CustomerEmail = "mk@gmail.com",
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Laptop",
                    Quantity = 2,
                    UnitPrice = 500 // 2 * 500 = 1000
                },
                new OrderItem
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Mouse",
                    Quantity = 3,
                    UnitPrice = 50 // 3 * 50 = 150
                }
            }
        };
        // بنظبط الـ Mock هنا إنه يرجع true عادي لأن الأوردر سليم والمخزن مكفي
        _inventoryMock.Setup(x => x.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new CheckoutService(_orderRepository.Object, _inventoryMock.Object, _notificationService.Object);

        //act
        var result = await service.CheckoutAsync(order);

        //assert
        result.Subtotal.Should().Be(1150);
     
        result.Discount.Should().Be(100); // 10% من 1500 هي 150، بس عشان الـ Max 100، فهتقف عند 100 بالظبط!

    }

    [Fact]
    public async Task CheckOutAsync_ZeroDiscount_WhenSubTotalBelow1000()
    {
        //arrange
        Order order = new Order
        {
            CustomerName = "Mostafa",
            CustomerEmail = "mk@gmail.com",
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Laptop",
                    Quantity = 1,
                    UnitPrice = 500 // 1 * 500 = 500
                },
                new OrderItem
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Mouse",
                    Quantity = 3,
                    UnitPrice = 50 // 3 * 50 = 150
                }
            }
        };
        // بنظبط الـ Mock هنا إنه يرجع true عادي لأن الأوردر سليم والمخزن مكفي
        _inventoryMock.Setup(x => x.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new CheckoutService(_orderRepository.Object, _inventoryMock.Object, _notificationService.Object);

        //act
        var result = await service.CheckoutAsync(order);

        //assert
        result.Subtotal.Should().Be(650);

        result.Discount.Should().Be(0); // 0


    }

    //BR-07 Total = Subtotal − Discount.
    [Fact]
    public async Task CheckOutAsync_TestTotal_ReturnTotal()
    {
        //arrange
        Order order = new Order
        {
            CustomerName = "Mostafa",
            CustomerEmail = "mk@gmail.com",
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Laptop",
                    Quantity = 1,
                    UnitPrice = 500 // 1 * 500 = 500
                },
                new OrderItem
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Mouse",
                    Quantity = 3,
                    UnitPrice = 50 // 3 * 50 = 150
                }
            }
        };
        // بنظبط الـ Mock هنا إنه يرجع true عادي لأن الأوردر سليم والمخزن مكفي
        _inventoryMock.Setup(x => x.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new CheckoutService(_orderRepository.Object, _inventoryMock.Object, _notificationService.Object);

        //act
        var result = await service.CheckoutAsync(order);

        //assert
        result.Subtotal.Should().Be(650);
        result.Discount.Should().Be(0);
        result.Total.Should().Be(650);//subtotal-discount


    }

    //BR-08 On success: the order is saved once and one confirmation is sent with the receipt.
    // BR-08: On success: the order is saved once and one confirmation is sent with the receipt.
    [Fact]
    public async Task CheckoutAsync_SavesOrderAndSendsNotification_WhenOrderIsSuccessful()
    {
        // Arrange
        Order order = new Order
        {
            CustomerName = "Mostafa",
            CustomerEmail = "mk@gmail.com",
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Laptop",
                    Quantity = 1,
                    UnitPrice = 1000
                }
            }
        };

        _inventoryMock.Setup(x => x.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new CheckoutService(_orderRepository.Object, _inventoryMock.Object, _notificationService.Object);

        // Act
        await service.CheckoutAsync(order);

        // Assert - التأكد إنه اتحفظ مرة واحدة وتبعت إيميل واحد
        _orderRepository.Verify(r => r.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
        _notificationService.Verify(n => n.SendOrderConfirmationAsync(It.IsAny<CheckoutReceipt>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // BR-09: On any failure: the order is not saved and no confirmation is sent.
    [Fact]
    public async Task CheckoutAsync_DoesNotSaveOrderOrSendNotification_WhenOrderFails()
    {
        // Arrange
        Order order = new Order
        {
            CustomerName = "Mostafa",
            CustomerEmail = "mk@gmail.com",
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Laptop",
                    Quantity = 5,
                    UnitPrice = 1000
                }
            }
        };

        // بنخليه يفشل (المخزن مش متاح) عشان يضرب إكسيبشن
        _inventoryMock.Setup(x => x.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new CheckoutService(_orderRepository.Object, _inventoryMock.Object, _notificationService.Object);

        // Act
        Func<Task> act = async () => await service.CheckoutAsync(order);

        // Assert - التأكد إن الإكسيبشن اتضرب، وإن الحفظ والإيميل متموش خالص
        await act.Should().ThrowAsync<InsufficientInventoryException>();

        //mock verify
        _orderRepository.Verify(r => r.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never); //orderSaved
        _notificationService.Verify(n => n.SendOrderConfirmationAsync(It.IsAny<CheckoutReceipt>(), It.IsAny<CancellationToken>()), Times.Never); //notification
    }


    //======================================================= Task 2 02 Tasks — Exactly What to Write=======================================
    //1.1 Order with an empty item list EmptyOrderException 

    //NOTE: method with MemberData must Return IEnumerable
    public static IEnumerable<object[]> GetEmptyOrder()
    {
        yield return new object[]{
            new Order { CustomerEmail="mk@gmail.com",CustomerName="MostafaKamel",Items=new List<OrderItem>()}
        };
    }
    [Theory]
    [MemberData(nameof(GetEmptyOrder))]
    public async Task ValidateOrder_WhenOrderHasNoItems_ThrowsEmptyOrderException(Order order)
    {
        var service = new CheckoutService(_orderRepository.Object, _inventoryMock.Object, _notificationService.Object);


        //act 
        Func<Task> act = async () => await service.CheckoutAsync(order);

        //Assert
        var exceptionAssert = await act.Should().ThrowAsync<EmptyOrderException>();
    }

    //1.2 [Theory] quantity 0 and -1 InvalidQuantityException, message has product name

    [Theory]
    [InlineData(0)]//quanitity 0
    [InlineData(-1)]
    public async Task CheckoutAsync_CheckInvalidQunainity_ThrowInvalidQuantityException(int quantity)
    {
        //arrange
        Order order = new Order
        {
            CustomerName = "Mostafa",        //dummyData
            CustomerEmail = "mk@gmail.com",
            Items = new List<OrderItem>()
            {
                new OrderItem
                {
                    ProductId=new Guid(),
                    ProductName = "Laptop",
                Quantity = quantity, // الكمية بالسالب عشان تضرب الإكسيبشن المطلوب
                UnitPrice = 1000
                }
            },
        };
        var service = new CheckoutService(_orderRepository.Object, _inventoryMock.Object, _notificationService.Object);

        //act 
        Func<Task> act = async () => await service.CheckoutAsync(order);

        //assert
        var exceptionAssert = await act.Should().ThrowAsync<InvalidQuantityException>();
        exceptionAssert.And.Message.Should().Contain("Laptop");
    }

    //1.3 Price -0.01 InvalidPriceException
    public static IEnumerable<object[]> InvalidPirces()
    {
        yield return new object[] { -0.01m };
    }

    [Theory]
    [MemberData(nameof(InvalidPirces))]
    
    public async Task CheckoutAsync_FirstInvalidPrice_ThrowInvalidPriceException(decimal price)
    {
        //arrange
        Order order = new Order
        {
            CustomerName = "Mostafa",        //dummyData
            CustomerEmail = "mk@gmail.com",
            Items = new List<OrderItem>()
            {
                new OrderItem
                {
                    ProductId=new Guid(),
                    ProductName = "Laptop",
                    Quantity = 1,
                    UnitPrice = price
                }
            },

        };
        var service = new CheckoutService(_orderRepository.Object, _inventoryMock.Object, _notificationService.Object);


        //act 
        Func<Task> act = async () => await service.CheckoutAsync(order);

        //Assert
        var exceptionAssert = await act.Should().ThrowAsync<InvalidPriceException>();


    }

    [Theory]
    [InlineData(0)]
    public async Task CheckoutAsync_FirstValidPrice_ShouldPassSuccessfully(int price)
    {
        // Arrange
        Order order = new Order
        {
            CustomerName = "Mostafa",
            CustomerEmail = "mk@gmail.com",
            Items = new List<OrderItem>()
            {
                new OrderItem
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "FreeGift",
                    Quantity = 1,
                    UnitPrice = price //   valid price =0   
                }
            },
        };

        //TestDouble type is stub => return fixed values
        _inventoryMock.Setup(x => x.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new CheckoutService(_orderRepository.Object, _inventoryMock.Object, _notificationService.Object);

        // Act
        var result = await service.CheckoutAsync(order);

        // Assert
        result.Subtotal.Should().Be(0);
        result.Total.Should().Be(0);
    }

    //1.5 [Theory] discount for subtotal 999.99, 1000, 1500 0, 100, 100
    public static IEnumerable<object[]> TestSubtotal()
    {
        yield return new object[] { 999.99m, 0m };

        yield return new object[] { 1000.00m, 100m };

        yield return new object[] { 1500.00m, 100m };
    }
    [Theory]
    [MemberData(nameof(TestSubtotal))]
    public async Task CheckoutAsync_CalculatesCorrectDiscount_BasedOnSubtotal(decimal unitPrice, decimal expectedDiscount)
    {
        // Arrange
        Order order = new Order
        {
            CustomerName = "Mostafa",
            CustomerEmail = "mk@gmail.com",
            Items = new List<OrderItem>
        {
            new OrderItem
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Product",
                Quantity = 1,
                UnitPrice = unitPrice // 
            }
        }
        };

        _inventoryMock.Setup(x => x.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new CheckoutService(_orderRepository.Object, _inventoryMock.Object, _notificationService.Object);

        // Act
        var result = await service.CheckoutAsync(order);

        // Assert
        result.Discount.Should().Be(expectedDiscount); //compare
    }

    //1.6 Example A and C (above) Correct subtotal, discount and total
    // 1. مصدر البيانات اللي بيبعت الأوردر والنتائج المتوقعة بناءً على الجدول
    public static IEnumerable<object[]> OrderCalculationTestCases()
    {
        // === مثال A: عنصرين (2 × 150 + 1 × 400) ===
        var orderA = new Order
        {
            CustomerName = "Mostafa",
            CustomerEmail = "mk@gmail.com",
            Items = new List<OrderItem>
        {
            new OrderItem { ProductId = Guid.NewGuid(), ProductName = "Item 1", Quantity = 2, UnitPrice = 150m },
            new OrderItem { ProductId = Guid.NewGuid(), ProductName = "Item 2", Quantity = 1, UnitPrice = 400m }
        }
        };
        yield return new object[] { orderA, 700m, 0m, 700m }; // (Order, Expected Subtotal, Expected Discount, Expected Total)


        // === مثال C: عنصر واحد بقيمة كبيرة (3 × 500) ===
        var orderC = new Order
        {
            CustomerName = "Mostafa",
            CustomerEmail = "mk@gmail.com",
            Items = new List<OrderItem>
        {
            new OrderItem { ProductId = Guid.NewGuid(), ProductName = "Laptop", Quantity = 3, UnitPrice = 500m }
        }
        };
        yield return new object[] { orderC, 1500m, 100m, 1400m }; // (Order, Expected Subtotal, Expected Discount, Expected Total)
    }

    [Theory]
    [MemberData(nameof(OrderCalculationTestCases))]
    public async Task CheckOutAsync_TestTotal_ReturnTotalAndSubToatl(Order order,
    decimal expectedSubtotal,
    decimal expectedDiscount,
    decimal expectedTotal)
    {
        //arrange

        // بنظبط الـ Mock هنا إنه يرجع true عادي لأن الأوردر سليم والمخزن مكفي
        _inventoryMock.Setup(x => x.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new CheckoutService(_orderRepository.Object, _inventoryMock.Object, _notificationService.Object);

        //act
        var result = await service.CheckoutAsync(order);

        //assert
        result.Subtotal.Should().Be(expectedSubtotal);
        result.Discount.Should().Be(expectedDiscount);
        result.Total.Should().Be(expectedTotal);
     


    }

    [Fact]
    public async Task CheckoutAsync_WhenOrderFailsValidation_DoesNotCallInventorySaveOrNotification()
    {
        // Arrange 
        Order order = new Order
        {
            CustomerName = "Mostafa",
            CustomerEmail = "mk@gmail.com",
            Items = new List<OrderItem>
        {
            new OrderItem
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Laptop",
                Quantity = 0, 
                UnitPrice = 1000
            }
        }
        };

        var service = new CheckoutService(_orderRepository.Object, _inventoryMock.Object, _notificationService.Object);

        // Act
        Func<Task> act = async () => await service.CheckoutAsync(order);

        // Assert 
        await act.Should().ThrowAsync<InvalidQuantityException>();

        //3-Mocks : to check if method is call or not call
        // التأكد إن مفيش ولا خدمة من الـ 3 اتندت  (Times.Never)
        _inventoryMock.Verify(i => i.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _orderRepository.Verify(r => r.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _notificationService.Verify(n => n.SendOrderConfirmationAsync(It.IsAny<CheckoutReceipt>(), It.IsAny<CancellationToken>()), Times.Never);
    }






}
