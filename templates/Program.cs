// Erzeugt mit createWebApiProject-xplat {{VERSION}}
using GrueneisR.RestClientGenerator;
using Microsoft.OpenApi;

const string CorsPolicy = "AllowAll";
const string ApiVersion = "v1";
const string ApiTitle = "{{PROJECT}}";
const string RequestsFile = "_requests.http";

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// ------------------------------------------------------------ Services
{{SERVICES}}
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(o => o.SwaggerDoc(ApiVersion, new OpenApiInfo { Title = ApiTitle, Version = ApiVersion }));
services.AddAuthorization();
services.AddCors(o => o.AddPolicy(CorsPolicy, policy => policy
  .SetIsOriginAllowed(_ => true)
  .AllowAnyHeader()
  .AllowAnyMethod()
  .AllowCredentials()));
services.AddRestClientGenerator(o => o
  .SetFolder(Environment.CurrentDirectory)
  .SetFilename(RequestsFile)
  .SetAction($"swagger/{ApiVersion}/swagger.json"));
builder.Logging.AddCustomFormatter();

var app = builder.Build();
{{STARTUP}}

// ------------------------------------------------------------ Pipeline
if (app.Environment.IsDevelopment())
{
  app.UseDeveloperExceptionPage();
  app.UseSwagger();
  app.UseSwaggerUI(o => o.SwaggerEndpoint($"/swagger/{ApiVersion}/swagger.json", ApiTitle));
  app.UseRestClientGenerator();
  Console.ForegroundColor = ConsoleColor.Green;
  Console.WriteLine("Swagger: http://localhost:{{HTTP_PORT}}/swagger");
  Console.WriteLine($"REST-Client-Datei (nach dem ersten Request): {Path.Combine(Environment.CurrentDirectory, RequestsFile)}");
  Console.ResetColor();
}

app.UseCors(CorsPolicy);
app.UseAuthorization();

// ------------------------------------------------------------ Endpoints
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
{{ENDPOINTS}}

Console.WriteLine($"Bereit für Clients um {DateTime.Now:HH:mm:ss}");
app.Run();
