namespace {{NAMESPACE}}.Controllers;

[ApiController]
[Route("[controller]")]
public class ValuesController({{DBNAME}}Context db) : ControllerBase
{
  [HttpGet("{{TABLE}}")]
  public IActionResult Get{{DBSET}}()
  {
    this.Log();
    return Ok(db.{{DBSET}}.ToList());
  }
}
