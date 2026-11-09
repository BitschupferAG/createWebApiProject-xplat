#!/usr/bin/env dotnet
// createWebApiProject-xplat
// Erzeugt eine ASP.NET Core Web-API-Solution – auf Windows, macOS und Linux.
// Inspiriert von https://gitlab.com/rgrueneis/createwebapiproject (nur Windows), von Grund auf neu geschrieben.
// Aufruf über die Startskripte createWebApi-xplat / createMinApi-xplat / createAllApi-xplat.

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

try
{
    return Generator.Run(args);
}
catch (ToolError e)
{
    Generator.Say(ConsoleColor.Red, "Fehler: " + e.Message);
    return 1;
}

sealed class ToolError(string message) : Exception(message);

static class Generator
{
    const string ToolVersion = "1.0.0";
    const int HttpPort = 5000;
    const int HttpsPort = 7001;
    const string SignalRAnalyzerVersion = "1.7.4";
    const string RestClientGeneratorVersion = "1.3.1";
    const string PkcsVersion = "10.0.13";

    static readonly string TemplateDir = Path.Combine(
        AppContext.GetData("EntryPointFileDirectoryPath") as string ?? AppContext.BaseDirectory, "templates");

    public static int Run(string[] args)
    {
        string? Arg(int i) => args.Length > i && !string.IsNullOrWhiteSpace(args[i]) ? args[i] : null;

        var type = Arg(0) ?? "";
        if (type is not ("Web" or "Min" or "All"))
            throw new ToolError("Typ muss Web, Min oder All sein.");
        if (Arg(1) is not { } solutionName)
        {
            PrintUsage(type);
            return 1;
        }
        var targetFolder = Arg(2) ?? ".";
        var dbFile = Arg(3);
        var tableName = Arg(4);
        var addWpf = string.Equals(Arg(5), "true", StringComparison.OrdinalIgnoreCase);
        var useWeb = type != "Min";
        var useMin = type != "Web";
        var project = solutionName;

        Say(ConsoleColor.Magenta, $"createWebApiProject-xplat {ToolVersion} – {type} API");

        // ---------------------------------------------------------------- Versionen
        var sdkVersion = Capture("dotnet", "--version")
            ?? throw new ToolError("dotnet wurde nicht gefunden. Installiere das .NET 10 SDK: https://dot.net");
        var sdkMajor = Major(sdkVersion);
        if (sdkMajor < 10)
            throw new ToolError($".NET SDK {sdkVersion} ist zu alt – mindestens 10 erforderlich.");

        var efVersion = Capture("dotnet-ef", "--version");
        if (efVersion is null || Major(efVersion) < 10)
        {
            var toolsPath = OperatingSystem.IsWindows() ? @"%USERPROFILE%\.dotnet\tools" : "~/.dotnet/tools";
            throw new ToolError(
                $"dotnet-ef 10 fehlt (gefunden: {efVersion ?? "nichts"}).\n" +
                "  Installieren:   dotnet tool install --global dotnet-ef --version 10.*\n" +
                "  Aktualisieren:  dotnet tool update  --global dotnet-ef --version 10.*\n" +
                $"  Danach {toolsPath} im PATH haben und ein neues Terminal öffnen.");
        }
        if (efVersion.Split('.').Length == 2) efVersion += ".0";
        Say(ConsoleColor.Cyan, $".NET SDK {sdkVersion}, dotnet-ef {efVersion}");

        // ---------------------------------------------------------------- Datenbank
        var withDb = dbFile is not null;
        var codeFirst = false;
        var isSqlite = false;
        string dbName = "", dbProject = "", dbSource = "";
        if (withDb)
        {
            if (tableName is null)
                throw new ToolError("tableName fehlt (Tabellenname oder null für Code First).");
            codeFirst = tableName.Equals("null", StringComparison.OrdinalIgnoreCase);
            if (codeFirst) tableName = "Dummies";

            isSqlite = Path.GetExtension(dbFile!).ToLowerInvariant() is ".sqlite" or ".sqlite3" or ".db";
            if (!isSqlite && !OperatingSystem.IsWindows())
                throw new ToolError(".mdf-Datenbanken (SQL Server LocalDB) gibt es nur unter Windows. Verwende eine .sqlite- oder .db-Datei.");

            dbSource = Path.GetFullPath(dbFile!);
            if (!codeFirst && !File.Exists(dbSource))
                throw new ToolError($"Datenbankdatei {dbSource} existiert nicht.");

            dbName = Regex.Replace(Path.GetFileNameWithoutExtension(dbFile!), "[^A-Za-z0-9_]", "");
            if (dbName.Length == 0 || char.IsDigit(dbName[0])) dbName = "App" + dbName;
            if (dbName == "Northwnd") dbName = "Northwind";
            dbProject = dbName + "Db";
        }

        var root = Path.GetFullPath(Path.Combine(targetFolder, solutionName));
        if (Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any())
            throw new ToolError($"Ordner {root} existiert bereits und ist nicht leer.");

        var route = useMin
            ? (withDb ? $"dbTest/{tableName}" : "dummy")
            : (withDb ? $"Values/{tableName}" : "Values/Dummy");

        Say(ConsoleColor.Cyan, $"Solution  {solutionName}\nZiel      {root}\nDatenbank {(withDb ? dbSource : "keine")}");

        // ---------------------------------------------------------------- Solution + Web-Projekt
        Directory.CreateDirectory(root);
        var sln = $"{solutionName}.slnx";
        var web = Path.Combine(root, project);
        var webCsproj = Path.Combine(web, project + ".csproj");
        Run(root, "dotnet", "new", "sln", "--format", "slnx", "-n", solutionName);
        Run(root, "dotnet", "new", "web", "-n", project, "-o", project);
        Run(root, "dotnet", "sln", sln, "add", webCsproj);

        if (useWeb) AddPackage(web, "GrueneisR.SignalRAnalyzer", SignalRAnalyzerVersion);
        AddPackage(web, "Swashbuckle.AspNetCore", $"{sdkMajor}.2.3");
        AddPackage(web, "GrueneisR.RestClientGenerator", RestClientGeneratorVersion);

        Write(web, "Dtos/ProgramYourDtosInThisFolder.cs", $"namespace {project}.Dtos;\n\npublic class DummyDtoClass {{ }}\n");
        Write(web, "Services/ProgramYourServicesInThisFolder.cs", $"namespace {project}.Services;\n\npublic class DummyServiceClass {{ }}\n");

        // ---------------------------------------------------------------- Datenbank-Projekt
        var connectionString = "";
        if (withDb)
        {
            var dbDir = Path.Combine(root, dbProject);
            Run(root, "dotnet", "new", "classlib", "-n", dbProject, "-o", dbProject);
            File.Delete(Path.Combine(dbDir, "Class1.cs"));

            var provider = isSqlite ? "Microsoft.EntityFrameworkCore.Sqlite" : "Microsoft.EntityFrameworkCore.SqlServer";
            AddPackage(dbDir, "Microsoft.EntityFrameworkCore", efVersion);
            AddPackage(dbDir, "Microsoft.EntityFrameworkCore.Design", efVersion);
            AddPackage(dbDir, provider, efVersion);
            AddPackage(dbDir, "System.Security.Cryptography.Pkcs", PkcsVersion);

            // Relativer Pfad: DB-Datei ins DB-Projekt kopieren. Absoluter Pfad: Datei bleibt, wo sie ist.
            var dbPath = Path.IsPathRooted(dbFile!) ? dbSource : Path.Combine(dbDir, Path.GetFileName(dbFile!));
            if (dbPath != dbSource && File.Exists(dbSource)) File.Copy(dbSource, dbPath);

            var databasePart = codeFirst ? $"Database={dbName};" : "";
            connectionString = isSqlite
                ? $"Data Source={dbPath}"
                : $@"Server=(LocalDB)\MSSQLLocalDB;AttachDbFilename={dbPath};{databasePart}Integrated Security=True;MultipleActiveResultSets=True;TrustServerCertificate=True";

            var values = new Dictionary<string, string> { ["DBPROJECT"] = dbProject, ["DBNAME"] = dbName };
            if (codeFirst)
            {
                Fill("CodeFirst/Dummy.cs", Path.Combine(dbDir, "Dummy.cs"), values);
                Fill("CodeFirst/DbContext.cs", Path.Combine(dbDir, $"{dbName}Context.cs"), values);
            }
            else
            {
                Run(dbDir, "dotnet", "ef", "dbcontext", "scaffold", connectionString, provider,
                    "-c", $"{dbName}Context", "--no-onconfiguring", "-f");
                if (isSqlite)
                {
                    // SQLite-Scaffolding erkennt Autoincrement nicht → sonst schlagen Inserts fehl.
                    var ctx = Path.Combine(dbDir, $"{dbName}Context.cs");
                    File.WriteAllText(ctx, File.ReadAllText(ctx).Replace("ValueGeneratedNever", "ValueGeneratedOnAdd"));
                }
            }

            Run(root, "dotnet", "sln", sln, "add", Path.Combine(dbDir, dbProject + ".csproj"));
            Run(web, "dotnet", "add", webCsproj, "reference", Path.Combine(dbDir, dbProject + ".csproj"));
        }

        // ---------------------------------------------------------------- WPF-Client (nur Windows)
        if (addWpf && !OperatingSystem.IsWindows())
        {
            Say(ConsoleColor.Yellow, "Hinweis: WPF gibt es nur unter Windows – der Client wird übersprungen.");
        }
        else if (addWpf)
        {
            var wpf = solutionName + "Wpf";
            var wpfDir = Path.Combine(root, wpf);
            Run(root, "dotnet", "new", "wpf", "-n", wpf, "-o", wpf);
            var values = new Dictionary<string, string>
            {
                ["NAMESPACE"] = wpf,
                ["BASEURL"] = $"http://localhost:{HttpPort}/",
                ["ROUTE"] = route,
            };
            Fill("Wpf/MainWindow.xaml", Path.Combine(wpfDir, "MainWindow.xaml"), values);
            Fill("Wpf/MainWindow.xaml.cs", Path.Combine(wpfDir, "MainWindow.xaml.cs"), values);
            Run(root, "dotnet", "sln", sln, "add", Path.Combine(wpfDir, wpf + ".csproj"));
        }

        // ---------------------------------------------------------------- Dateien im Web-Projekt
        var context = $"{dbName}Context";
        var services = new List<string>();
        var startup = new List<string>();
        var endpoints = new List<string>();
        if (useWeb) services.Add("services.AddControllers();");
        if (withDb)
        {
            services.Add($"var connectionString = builder.Configuration.GetConnectionString(\"{dbName}\");");
            services.Add($"services.AddDbContext<{context}>(o => o.{(isSqlite ? "UseSqlite" : "UseSqlServer")}(connectionString));");
        }
        if (codeFirst)
        {
            startup.Add("using (var scope = app.Services.CreateScope())");
            startup.Add($"  scope.ServiceProvider.GetRequiredService<{context}>().Database.EnsureCreated();");
        }
        if (useWeb) endpoints.Add("app.MapControllers();");
        if (useMin) endpoints.Add(withDb ? "app.MapDbTest();" : "app.MapDummy();");

        var common = new Dictionary<string, string>
        {
            ["PROJECT"] = project,
            ["VERSION"] = ToolVersion,
            ["HTTP_PORT"] = HttpPort.ToString(),
            ["SERVICES"] = string.Join("\n", services),
            ["STARTUP"] = string.Join("\n", startup),
            ["ENDPOINTS"] = string.Join("\n", endpoints),
            ["DBNAME"] = dbName,
            ["TABLE"] = tableName ?? "",
        };
        Fill("Program.cs", Path.Combine(web, "Program.cs"), common);
        Fill("ExtensionMethods.cs", Path.Combine(web, "ExtensionMethods.cs"), common);
        Fill("ConsoleLoggerExtensions.cs", Path.Combine(web, "Logging", "ConsoleLoggerExtensions.cs"), common);
        if (useWeb)
            Fill(withDb ? "ValuesControllerDb.cs" : "ValuesController.cs", Path.Combine(web, "Controllers", "ValuesController.cs"), common);
        if (useMin)
            Fill(withDb ? "DbTestMaps.cs" : "DummyMaps.cs", Path.Combine(web, "Maps", withDb ? "DbTestMaps.cs" : "DummyMaps.cs"), common);

        var usings = new List<string> { "Microsoft.AspNetCore.Mvc", $"{project}.Dtos", $"{project}.Logging", $"{project}.Services" };
        if (useMin) usings.Add($"{project}.Maps");
        if (withDb) usings.AddRange(["Microsoft.EntityFrameworkCore", dbProject]);
        Write(web, "GlobalUsings.cs", string.Concat(usings.Select(u => $"global using {u};\n")));

        Write(web, "_requests.http", $"@host = http://localhost:{HttpPort}\n\nGET {{{{host}}}}/{route}\nAccept: application/json\n");

        EditJson(Path.Combine(web, "appsettings.json"), json =>
        {
            json["Logging"]!["LogLevel"]!["Microsoft.EntityFrameworkCore.Database.Command"] = "Information";
            if (withDb) json["ConnectionStrings"] = new JsonObject { [dbName] = connectionString };
        });
        EditJson(Path.Combine(web, "Properties", "launchSettings.json"), json =>
        {
            var profiles = json["profiles"]!.AsObject();
            foreach (var (name, profile) in profiles)
            {
                profile!["launchBrowser"] = false;
                profile["launchUrl"] = route;
                profile["applicationUrl"] = name == "https"
                    ? $"https://localhost:{HttpsPort};http://localhost:{HttpPort}"
                    : $"http://localhost:{HttpPort}";
            }
        });

        // ---------------------------------------------------------------- Bauen + Starten
        Run(root, "dotnet", "build", sln);

        Say(ConsoleColor.Green, "\nFertig!");
        Say(ConsoleColor.Cyan, $"Swagger:  http://localhost:{HttpPort}/swagger\nTest-URL: http://localhost:{HttpPort}/{route}");
        if (IsPortInUse(HttpPort))
        {
            Say(ConsoleColor.Yellow, $"Achtung: Port {HttpPort} ist schon belegt – das Backend kann so nicht starten.");
            if (OperatingSystem.IsMacOS())
                Say(ConsoleColor.Yellow, "Auf macOS ist das meist der AirPlay-Empfänger: Systemeinstellungen → Allgemein → AirDrop & Handoff → AirPlay-Empfänger ausschalten.");
        }
        if (Console.IsInputRedirected) return 0;

        Console.WriteLine("Enter drücken, um das Backend zu starten (Strg+C beendet es) ...");
        Console.ReadLine();
        Run(web, "dotnet", "run");
        return 0;
    }

    static void PrintUsage(string type)
    {
        Say(ConsoleColor.Magenta, $"Aufruf: create{type}Api-xplat solutionName [targetFolder] [pathToDbFile] [tableName] [addWPF]");
        Console.WriteLine("""
              solutionName  Name der Solution, des Projekts und des Ordners
              targetFolder  Ordner, in dem die Solution angelegt wird (Standard: .)
              pathToDbFile  .sqlite/.db (überall) oder .mdf (nur Windows) – erzeugt ein EF-Core-Projekt
              tableName     Tabelle für den Test-Endpoint; null = Code First mit Tabelle Dummies
              addWPF        true = zusätzlich WPF-Client (nur Windows)
            """);
    }

    static void AddPackage(string dir, string package, string version) =>
        Run(dir, "dotnet", "add", "package", package, "-v", version, "-n");

    static void Fill(string template, string target, Dictionary<string, string> values)
    {
        var text = File.ReadAllText(Path.Combine(TemplateDir, template));
        foreach (var (key, value) in values) text = text.Replace("{{" + key + "}}", value);
        if (Regex.Match(text, @"\{\{[A-Z_]+\}\}") is { Success: true } left)
            throw new ToolError($"Platzhalter {left.Value} in Vorlage {template} wurde nicht ersetzt.");
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.WriteAllText(target, text);
    }

    static void Write(string dir, string relativePath, string content)
    {
        var path = Path.Combine(dir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    static void EditJson(string path, Action<JsonNode> edit)
    {
        var json = JsonNode.Parse(File.ReadAllText(path))!;
        edit(json);
        File.WriteAllText(path, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    static void Run(string cwd, string file, params string[] args)
    {
        Say(ConsoleColor.DarkGray, $"> {file} {string.Join(' ', args)}");
        var psi = new ProcessStartInfo(file) { WorkingDirectory = cwd };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi) ?? throw new ToolError($"{file} konnte nicht gestartet werden.");
        p.WaitForExit();
        if (p.ExitCode != 0)
            throw new ToolError($"Befehl fehlgeschlagen (Exit-Code {p.ExitCode}): {file} {string.Join(' ', args)}");
    }

    // Letzte nicht-leere Ausgabezeile, oder null wenn das Programm fehlt / fehlschlägt.
    static string? Capture(string file, params string[] args)
    {
        try
        {
            var psi = new ProcessStartInfo(file) { RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var a in args) psi.ArgumentList.Add(a);
            using var p = Process.Start(psi)!;
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return p.ExitCode == 0
                ? output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault()
                : null;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    static bool IsPortInUse(int port)
    {
        try
        {
            using var client = new System.Net.Sockets.TcpClient();
            return client.ConnectAsync("localhost", port).Wait(500);
        }
        catch (AggregateException)
        {
            return false;
        }
    }

    static int Major(string version) => int.TryParse(version.Split('.')[0], out var m) ? m : 0;

    public static void Say(ConsoleColor color, string text)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }
}
