using FluentValidation;
using GlobomaticsHelpdesk.Api.Contracts;

namespace GlobomaticsHelpdesk.Api.Validation.Validators;

public class TriageRequestValidator : AbstractValidator<TriageRequest>
{
    public TriageRequestValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty()
            .MaximumLength(4000);
    }
}
