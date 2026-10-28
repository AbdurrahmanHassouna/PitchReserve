namespace PitchReserve.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PitchReserve.Application.Common.Models;
using PitchReserve.Application.Features.Payments.Commands.ConfirmCashPaymentCollection;
using PitchReserve.Application.Features.Payments.Queries.GetOwnerPayments;

[Authorize(Roles = "Owner")]
public class PaymentsController : ApiControllerBase
{
    [HttpPost("{paymentId:guid}/confirm-cash")]
    public async Task<ActionResult<Result>> ConfirmCash(
        [FromRoute] Guid paymentId,
        [FromBody] DateTime paidAt)
    {
        var command = new ConfirmCashPaymentCollectionCommand(paymentId, paidAt);
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [HttpGet("owner-summary")]
    public async Task<ActionResult<OwnerPaymentsSummaryDto>> GetOwnerSummary([FromQuery] GetOwnerPaymentsQuery query)
    {
        var result = await Mediator.Send(query);
        return Ok(result);
    }
}
