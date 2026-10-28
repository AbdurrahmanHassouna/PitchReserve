using MediatR;
using PitchReserve.Application.Common.Models;

namespace PitchReserve.Application.Features.Users.Commands.UpdateCurrentUserPhoneNumber;

public record UpdateCurrentUserPhoneNumberCommand(string PhoneNumber) : IRequest<Result>;
