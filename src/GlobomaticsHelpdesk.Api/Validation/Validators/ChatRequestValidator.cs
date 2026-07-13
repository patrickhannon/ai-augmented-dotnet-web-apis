using FluentValidation;
using GlobomaticsHelpdesk.Api.Contracts;

namespace GlobomaticsHelpdesk.Api.Validation.Validators;

public class ChatRequestValidator : AbstractValidator<ChatRequest>
{
    public ChatRequestValidator()
    {
        RuleFor(x => x.Prompt)
            .NotEmpty();
    }
}
