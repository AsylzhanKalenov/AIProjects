using Microsoft.AspNetCore.Mvc;

namespace InstagramBot.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    
    [HttpGet]
    public ActionResult<string> Get() => "Hello World! Test completed";
}