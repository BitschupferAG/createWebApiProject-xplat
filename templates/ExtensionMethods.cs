using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.SignalR;

namespace {{PROJECT}};

public static class ExtensionMethods
{
  /// <summary>Kopiert alle gleichnamigen, zuweisbaren Properties von <paramref name="source"/> nach <paramref name="target"/>.</summary>
  public static T CopyFrom<T>(this T target, object source) where T : notnull => target.CopyPropertiesFrom(source);

  /// <inheritdoc cref="CopyFrom{T}"/>
  public static T CopyPropertiesFrom<T>(this T target, object source) where T : notnull
  {
    ArgumentNullException.ThrowIfNull(source);
    var readable = source.GetType()
      .GetProperties(BindingFlags.Public | BindingFlags.Instance)
      .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
      .ToDictionary(p => p.Name);
    foreach (var prop in target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
    {
      if (!prop.CanWrite || !readable.TryGetValue(prop.Name, out var from)) continue;
      if (prop.PropertyType.IsAssignableFrom(from.PropertyType)) prop.SetValue(target, from.GetValue(source));
    }
    return target;
  }

  /// <summary>Erzeugt ein <typeparamref name="T"/> (z.B. einen record) aus den gleichnamigen Properties von <paramref name="source"/>.</summary>
  public static T TransformTo<T>(this object source) where T : notnull
  {
    ArgumentNullException.ThrowIfNull(source);
    var values = source.GetType()
      .GetProperties(BindingFlags.Public | BindingFlags.Instance)
      .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
      .ToDictionary(p => p.Name, p => p.GetValue(source), StringComparer.OrdinalIgnoreCase);
    var ctor = typeof(T).GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
    var ctorArgs = ctor.GetParameters()
      .Select(p => values.TryGetValue(p.Name!, out var v) ? v : p.HasDefaultValue ? p.DefaultValue : null)
      .ToArray();
    return ((T)ctor.Invoke(ctorArgs)).CopyPropertiesFrom(source);
  }

  /// <summary>Schreibt Klasse, Methode und optional eine Nachricht auf die Konsole.</summary>
  public static void Log(this ControllerBase controller, string msg = "", [CallerMemberName] string method = "")
    => Write(controller.GetType().Name, method, msg);

  /// <inheritdoc cref="Log(ControllerBase, string, string)"/>
  public static void Log(this Hub hub, string msg = "", [CallerMemberName] string method = "")
    => Write(hub.GetType().Name, method, msg);

  private static void Write(string type, string method, string msg)
  {
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"{DateTime.Now:HH:mm:ss.ff} {type}.{method} {msg}".TrimEnd());
    Console.ResetColor();
  }
}
