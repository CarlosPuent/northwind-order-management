using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Northwind.Api.Controllers;
using Northwind.Application.Common;
using Northwind.Application.Orders;
using Northwind.Application.Orders.Commands;
using Northwind.Application.Orders.Dtos;
using Northwind.Domain.Common;
using Xunit;

namespace Northwind.Api.Tests.Controllers;

public class OrdersControllerTests
{
    private readonly Mock<IOrderService> _service = new();
    private readonly OrdersController _sut;

    public OrdersControllerTests()
    {
        _sut = new OrdersController(_service.Object);
    }

    // ---- GetPaged ----

    [Fact]
    public async Task GetPaged_ShouldReturnOk_WithPagedResult()
    {
        var paged = new PagedResult<OrderDto>(
            new List<OrderDto>().AsReadOnly(), 1, 20, 0);

        _service.Setup(s => s.GetPagedAsync(
            1, 20, null, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var result = await _sut.GetPaged();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPaged_WithFilters_ShouldPassThemToService()
    {
        var paged = new PagedResult<OrderDto>(
            new List<OrderDto>().AsReadOnly(), 1, 10, 0);

        _service.Setup(s => s.GetPagedAsync(
            1, 10, "ALFKI", "Germany", true, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var result = await _sut.GetPaged(
            page: 1, pageSize: 10,
            customerId: "ALFKI", region: "Germany", isShipped: true);

        result.Should().BeOfType<OkObjectResult>();
    }

    // ---- GetById ----

    [Fact]
    public async Task GetById_WhenOrderExists_ShouldReturnOk()
    {
        var dto = new OrderDto { Id = 1, CustomerId = "ALFKI" };
        _service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _sut.GetById(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        ((OkObjectResult)result).Value.Should().Be(dto);
    }

    [Fact]
    public async Task GetById_WhenOrderNotFound_ShouldReturnProblemDetails()
    {
        _service.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.NotFound("Order.NotFound", "Not found."));

        var result = await _sut.GetById(999, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>();
        ((ObjectResult)result).StatusCode.Should().Be(404);
    }

    // ---- Create ----

    [Fact]
    public async Task Create_WithValidCommand_ShouldReturnCreated()
    {
        var dto = new OrderDto { Id = 100, CustomerId = "ALFKI" };
        var cmd = new CreateOrderCommand(
            "ALFKI", 1, DateTime.UtcNow, "John", "123 St", "NYC",
            null, null, "USA", 0,
            new List<OrderLineCommand> { new(1, 1, 0f) });

        _service.Setup(s => s.CreateAsync(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _sut.Create(cmd, CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
        ((CreatedAtActionResult)result).StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Create_WithInvalidCommand_ShouldReturnUnprocessable()
    {
        var cmd = new CreateOrderCommand(
            "", 0, DateTime.UtcNow, "", "", "", null, null, "", 0,
            new List<OrderLineCommand>());

        _service.Setup(s => s.CreateAsync(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Validation("Order.NoLines", "No lines."));

        var result = await _sut.Create(cmd, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>();
        ((ObjectResult)result).StatusCode.Should().Be(422);
    }

    // ---- Update ----

    [Fact]
    public async Task Update_WhenIdMismatch_ShouldReturnBadRequest()
    {
        var cmd = new UpdateOrderCommand(
            999, "ALFKI", 1, DateTime.UtcNow, null, "John", "123 St", "NYC",
            null, null, "USA", 0,
            new List<OrderLineCommand> { new(1, 1, 0f) });

        var result = await _sut.Update(1, cmd, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        ((ObjectResult)result).StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Update_WithValidCommand_ShouldReturnOk()
    {
        var dto = new OrderDto { Id = 1 };
        var cmd = new UpdateOrderCommand(
            1, "ALFKI", 1, DateTime.UtcNow, null, "John", "123 St", "NYC",
            null, null, "USA", 0,
            new List<OrderLineCommand> { new(1, 1, 0f) });

        _service.Setup(s => s.UpdateAsync(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _sut.Update(1, cmd, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    // ---- Ship ----

    [Fact]
    public async Task Ship_WithValidRequest_ShouldReturnOk()
    {
        var dto = new OrderDto { Id = 1, IsShipped = true };
        _service.Setup(s => s.ShipAsync(
            It.Is<ShipOrderCommand>(c => c.OrderId == 1 && c.ShipperId == 2),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _sut.Ship(
            1,
            new OrdersController.ShipOrderRequest(ShipperId: 2),
            CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Ship_WhenOrderAlreadyShipped_ShouldReturnConflict()
    {
        _service.Setup(s => s.ShipAsync(It.IsAny<ShipOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Conflict("Order.AlreadyShipped", "Already shipped."));

        var result = await _sut.Ship(
            1,
            new OrdersController.ShipOrderRequest(ShipperId: 2),
            CancellationToken.None);

        result.Should().BeOfType<ObjectResult>();
        ((ObjectResult)result).StatusCode.Should().Be(409);
    }

    // ---- Delete ----

    [Fact]
    public async Task Delete_WhenOrderExists_ShouldReturnNoContent()
    {
        _service.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _sut.Delete(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WhenOrderShipped_ShouldReturnConflict()
    {
        _service.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Conflict("Order.AlreadyShipped", "Cannot delete."));

        var result = await _sut.Delete(1, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>();
        ((ObjectResult)result).StatusCode.Should().Be(409);
    }

    // ---- GetArchived ----

    [Fact]
    public async Task GetArchived_ShouldReturnOk_WithArchivedOrders()
    {
        var archived = new List<OrderDto> { new() { Id = 5, CustomerId = "ALFKI" } }
            .AsReadOnly();

        _service.Setup(s => s.GetArchivedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(archived);

        var result = await _sut.GetArchived(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        ((OkObjectResult)result).Value.Should().Be(archived);
    }

    // ---- Restore ----

    [Fact]
    public async Task Restore_WhenSuccess_ShouldReturnNoContent()
    {
        _service.Setup(s => s.RestoreAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _sut.Restore(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Restore_WhenNotFound_ShouldReturn404()
    {
        _service.Setup(s => s.RestoreAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.NotFound("Order.NotFound", "Not found."));

        var result = await _sut.Restore(999, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>();
        ((ObjectResult)result).StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Restore_WhenOrderNotDeleted_ShouldReturn409()
    {
        _service.Setup(s => s.RestoreAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Conflict("Order.NotDeleted", "Order is not currently deleted."));

        var result = await _sut.Restore(1, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>();
        ((ObjectResult)result).StatusCode.Should().Be(409);
    }
}