using System.Security.Claims;
using Ecommerce.Application.Common;
using Ecommerce.Application.Orders;
using Ecommerce.Application.Orders.Dtos;
using Ecommerce.Application.Shipping;
using Ecommerce.Application.Shipping.Dtos;
using Ecommerce.Domain.Identity;
using Ecommerce.Domain.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(
    IOrderService orderService,
    IOrderCallAttemptService callAttemptService,
    IShippingService shippingService) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<OrderDetailDto>> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await orderService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Track), new { orderNumber = order.OrderNumber, phone = order.Phone }, order);
    }

    [HttpGet("track")]
    [AllowAnonymous]
    public async Task<ActionResult<OrderDetailDto>> Track(
        [FromQuery] string orderNumber,
        [FromQuery] string phone,
        CancellationToken cancellationToken)
    {
        var order = await orderService.TrackAsync(orderNumber, phone, cancellationToken);
        return Ok(order);
    }

    [HttpGet]
    [Authorize(Roles = Roles.OrderManagers)]
    public async Task<ActionResult<PagedResult<OrderSummaryDto>>> GetPaged(
        [FromQuery] OrderStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await orderService.GetPagedAsync(status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = Roles.OrderManagers)]
    public async Task<ActionResult<OrderDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await orderService.GetByIdAsync(id, cancellationToken);
        return Ok(order);
    }

    [HttpPost("{id:guid}/status")]
    [Authorize(Roles = Roles.OrderManagers)]
    public async Task<ActionResult<OrderDetailDto>> ChangeStatus(
        Guid id,
        ChangeOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var changedByUserId = userIdClaim is not null ? Guid.Parse(userIdClaim) : (Guid?)null;

        var order = await orderService.ChangeStatusAsync(id, request, changedByUserId, cancellationToken);
        return Ok(order);
    }

    [HttpPost("{id:guid}/call-attempts")]
    [Authorize(Roles = Roles.OrderManagers)]
    public async Task<ActionResult<OrderDetailDto>> RecordCallAttempt(
        Guid id,
        RecordCallAttemptRequest request,
        CancellationToken cancellationToken)
    {
        var agentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var order = await callAttemptService.RecordAsync(id, request, agentUserId, cancellationToken);
        return Ok(order);
    }

    [HttpPost("{id:guid}/shipment")]
    [Authorize(Roles = Roles.OrderManagers)]
    public async Task<ActionResult<ShipmentDto>> CreateShipment(
        Guid id,
        CreateShipmentRequest request,
        CancellationToken cancellationToken)
    {
        var shipment = await shippingService.CreateShipmentAsync(id, request, cancellationToken);
        return Ok(shipment);
    }
}
