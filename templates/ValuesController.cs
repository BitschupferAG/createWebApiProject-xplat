namespace {{PROJECT}}.Controllers;

[ApiController]
[Route("[controller]")]
public class ValuesController : ControllerBase
{
  [HttpGet("Dummy")]
  public ActionResult<IEnumerable<string>> GetDummy()
  {
    this.Log();
    return new[] { "Hello", "from", "{{PROJECT}}" };
  }
}
