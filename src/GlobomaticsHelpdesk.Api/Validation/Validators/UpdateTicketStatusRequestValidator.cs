using FluentValidation;
using GlobomaticsHelpdesk.Api.Contracts;

namespace GlobomaticsHelpdesk.Api.Validation.Validators;

public class UpdateTicketStatusRequestValidator : AbstractValidator<UpdateTicketStatusRequest>
{
    public UpdateTicketStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}
