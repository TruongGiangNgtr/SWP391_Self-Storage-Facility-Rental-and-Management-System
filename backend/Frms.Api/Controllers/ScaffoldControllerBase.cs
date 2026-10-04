using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[ApiController]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status501NotImplemented)]
public abstract class ScaffoldControllerBase : ControllerBase
{
    protected ObjectResult ScaffoldNotImplemented(string apiId)
    {
        return StatusCode(
            StatusCodes.Status501NotImplemented,
            new ApiErrorResponse(
                "ENDPOINT_NOT_IMPLEMENTED",
                $"{apiId} is a contract scaffold and has not been implemented.",
                HttpContext.TraceIdentifier));
    }
}
