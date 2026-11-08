using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace {{PROJECT}}.Logging;

public static class ConsoleLoggerExtensions
{
  /// <summary>Kompakte, einzeilige Konsolenausgabe: Zeit, Level, kurze Kategorie, Nachricht.</summary>
  public static ILoggingBuilder AddCustomFormatter(this ILoggingBuilder builder) => builder
    .AddConsole(o => o.FormatterName = CompactConsoleFormatter.FormatterName)
    .AddConsoleFormatter<CompactConsoleFormatter, ConsoleFormatterOptions>();
}

public sealed class CompactConsoleFormatter() : ConsoleFormatter(FormatterName)
{
  public const string FormatterName = "compact";

  public override void Write<TState>(in LogEntry<TState> entry, IExternalScopeProvider? scopeProvider, TextWriter writer)
  {
    var message = entry.Formatter?.Invoke(entry.State, entry.Exception);
    if (message is null) return;
    var level = entry.LogLevel switch
    {
      LogLevel.Trace => "trce",
      LogLevel.Debug => "dbug",
      LogLevel.Information => "info",
      LogLevel.Warning => "warn",
      LogLevel.Error => "fail",
      LogLevel.Critical => "crit",
      _ => "    ",
    };
    var category = entry.Category[(entry.Category.LastIndexOf('.') + 1)..];
    writer.WriteLine($"{DateTime.Now:HH:mm:ss.ff} {level} {category}: {message}");
    if (entry.Exception is not null) writer.WriteLine(entry.Exception);
  }
}
