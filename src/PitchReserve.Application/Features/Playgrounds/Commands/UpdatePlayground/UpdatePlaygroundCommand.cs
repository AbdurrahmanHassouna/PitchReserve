using MediatR;
using PitchReserve.Domain.Enums;

namespace PitchReserve.Application.Features.Playgrounds.Commands.UpdatePlayground;

public sealed record UpdatePlaygroundCommand(
    Guid Id,
    string Name,
    PitchSize Size,
    SurfaceType SurfaceType,
    decimal HourlyRate,
    string LocationCity,
    string Address,
    decimal? Latitude,
    decimal? Longitude,
    TimeOnly OpenHour,
    TimeOnly CloseHour,
    PlaygroundStatus Status) : IRequest;
