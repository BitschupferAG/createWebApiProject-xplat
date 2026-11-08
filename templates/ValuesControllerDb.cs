namespace {{PROJECT}}.Controllers;

[ApiController]
[Route("[controller]")]
public class ValuesController({{DBNAME}}Context db) : ControllerBase
{
  [HttpGet("{{TABLE}}")]
  public IActionResult Get{{TABLE}}()
  {
    this.Log();
    return Ok(db.{{TABLE}}.ToList());
  }
}
