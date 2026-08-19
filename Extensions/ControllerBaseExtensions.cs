using Microsoft.AspNetCore.Mvc;

namespace MIC.risk.Extensions;

/// <summary>
/// Error helpers that keep every failure on the wire as RFC 9457 <c>application/problem+json</c>,
/// which is what the OpenAPI document already advertises. Anonymous <c>{ Message }</c> bodies
/// looked nothing like the published contract and broke response validation on the client.
/// </summary>
public static class ControllerBaseExtensions
{
    public static IActionResult NotFoundProblem(this ControllerBase controller, string detail) =>
        controller.Problem(
            detail: detail,
            statusCode: StatusCodes.Status404NotFound,
            title: "Not Found");

    public static IActionResult BadRequestProblem(this ControllerBase controller, string detail) =>
        controller.Problem(
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest,
            title: "Bad Request");

    public static IActionResult UnauthorizedProblem(this ControllerBase controller, string detail) =>
        controller.Problem(
            detail: detail,
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Unauthorized");

    public static IActionResult ForbiddenProblem(this ControllerBase controller, string detail) =>
        controller.Problem(
            detail: detail,
            statusCode: StatusCodes.Status403Forbidden,
            title: "Forbidden");
}
