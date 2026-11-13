using Microsoft.AspNetCore.Mvc;
using MyApp.Web.Models;

namespace MyApp.Web.Controllers;

[ApiController]
[Route("ui")]
public sealed class UiController : Controller
{
    [HttpPost("render-block")]
    public IActionResult RenderBlock([FromBody] UiBlockRequest block)
    {
        try
        {
            var mapped = UiBlockMapper.Map(block.Type, block.Payload);
            return PartialView(mapped.ViewName, mapped.Model);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
