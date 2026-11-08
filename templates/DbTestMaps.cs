namespace {{PROJECT}}.Maps;

public static class DbTestMaps
{
  public static void MapDbTest(this WebApplication app)
  {
    var group = app.MapGroup("/dbTest").WithTags("DbTest");
    group.MapGet("/{{TABLE}}", ({{DBNAME}}Context db) => db.{{TABLE}}.ToList());
  }
}
