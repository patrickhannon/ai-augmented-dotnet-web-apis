using FluentValidation;
using GlobomaticsHelpdesk.Api.Contracts;

namespace GlobomaticsHelpdesk.Api.Validation.Validators;

public class CreateTicketRequestValidator : AbstractValidator<CreateTicketRequest>
{
    public CreateTicketRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(8000);

        RuleFor(x => x.Priority)
            .IsInEnum();
    }
}
