using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using MIC.risk.Models;

namespace MIC.risk.Authorization;

public class RiskReportOwnerHandler : AuthorizationHandler<SameOwnerRequirement, RiskReport>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SameOwnerRequirement requirement,
        RiskReport resource)
    {
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null && resource.Employee.IdentityUserId == userId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
