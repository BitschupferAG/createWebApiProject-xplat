namespace {{PROJECT}}.Maps;

public record DummyDto(string Name, int Value);

public static class DummyMaps
{
  public static void MapDummy(this WebApplication app) =>
    app.MapGet("/dummy", () => new DummyDto("{{PROJECT}}", 42)).WithTags("Dummy");
}
