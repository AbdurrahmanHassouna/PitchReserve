using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PitchReserve.Application.Common.Models;
using PitchReserve.Application.Features.Users.Commands.UpdateCurrentUserProfile;
using PitchReserve.Application.Features.Users.Commands.UpdateCurrentUserPhoneNumber;

namespace PitchReserve.Api.Controllers;

[Authorize]
public class UsersController : ApiControllerBase
{
    [HttpPatch("me")]
    public async Task<ActionResult<Result>> UpdateProfile(
        [FromBody] UpdateCurrentUserProfileDto dto,
        CancellationToken cancellationToken)
        => Ok(await Mediator.Send(
            new UpdateCurrentUserProfileCommand(dto.FullName, dto.PhoneNumber),
            cancellationToken));

    [HttpPatch("me/phone")]
    public async Task<ActionResult<Result>> UpdatePhone([FromBody] UpdatePhoneNumberDto dto, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new UpdateCurrentUserPhoneNumberCommand(dto.PhoneNumber), cancellationToken));
}

public record UpdateCurrentUserProfileDto(string? FullName, string? PhoneNumber);

public record UpdatePhoneNumberDto(string PhoneNumber);
