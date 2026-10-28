using FluentValidation;

namespace PitchReserve.Application.Features.Playgrounds.Commands.DeletePlayground;

public sealed class DeletePlaygroundCommandValidator : AbstractValidator<DeletePlaygroundCommand>
{
    public DeletePlaygroundCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
