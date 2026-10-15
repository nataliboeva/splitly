using Microsoft.AspNetCore.Http.HttpResults;

namespace Splitly.Api.Common;

/// <summary>ProblemDetails responses shared by several endpoints.</summary>
public static class ApiProblems
{
    /// <summary>The request was well-formed but broke a business rule, e.g. percentages not adding up to 100.</summary>
    public static ProblemHttpResult BusinessRule(string detail) =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "The request could not be completed.",
            detail: detail);

    public static ProblemHttpResult NotFound(string title) =>
        TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: title);

    public static ProblemHttpResult Forbidden(string title) =>
        TypedResults.Problem(statusCode: StatusCodes.Status403Forbidden, title: title);
}
