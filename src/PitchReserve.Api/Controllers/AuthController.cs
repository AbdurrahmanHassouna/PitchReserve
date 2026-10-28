namespace PitchReserve.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PitchReserve.Application.Common.Models;
using PitchReserve.Application.Features.Users.Commands.Login;
using PitchReserve.Application.Features.Users.Commands.RegisterUser;
using PitchReserve.Application.Features.Users.Common;
using PitchReserve.Application.Features.Users.Queries.GetCurrentUser;

[Route("api/[controller]")]
public class AuthController : ApiControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<Result<AuthResponseDto>>> Register([FromBody] RegisterUserCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<Result<AuthResponseDto>>> Login([FromBody] LoginCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<Result<CurrentUserDto>>> GetCurrentUser()
    {
        var result = await Mediator.Send(new GetCurrentUserQuery());
        return Ok(result);
    }
}
