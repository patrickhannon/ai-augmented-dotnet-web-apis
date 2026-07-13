using FluentValidation;
using GlobomaticsHelpdesk.Api.Contracts;

namespace GlobomaticsHelpdesk.Api.Validation.Validators;

public class ReassignTicketRequestValidator : AbstractValidator<ReassignTicketRequest>
{
    public ReassignTicketRequestValidator()
    {
        // AssignedAgentId may be null (= unassign) or any Guid; further checks happen in the endpoint.
    }
}
