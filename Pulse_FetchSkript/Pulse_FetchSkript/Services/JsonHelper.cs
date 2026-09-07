using System.Text.Json;

namespace Pulse_FetchSkript.Services;

/// <summary>
/// Hilfsfunktionen zum robusten Parsen von JSON-Strings aus Datenbankspalten.
/// Unterstützt u.a.:
///  - mehrere hintereinander stehende JSON-Objekte (nimmt das erste vollständige)
///  - Whitespace / BOM vor oder nach dem JSON
///  - zusätzlichen Klartext vor oder nach dem JSON
/// </summary>
public static class JsonHelper
{
    /// <summary>
    /// Versucht, den Roh-String als JSON zu deserialisieren.
    /// Bei Fehlern wird versucht, das erste vollständige {…}-Objekt zu extrahieren.
    /// </summary>
    public static bool TryDeserialize<T>(string? raw, JsonSerializerOptions? options, out T? value, out string? error)
    {
        value = default;
        error = null;

        if (string.IsNullOrWhiteSpace(raw))
        {
            error = "Eingabe ist leer.";
            return false;
        }

        var trimmed = raw.Trim();

        // 1. Versuch: direkt parsen
        try
        {
            value = JsonSerializer.Deserialize<T>(trimmed, options);
            if (value is not null) return true;
        }
        catch (JsonException)
        {
            // weiter mit Extraktor
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }

        // 2. Versuch: erstes vollständiges {…}-Objekt extrahieren
        var extracted = ExtractFirstJsonObject(trimmed);
        if (extracted is null)
        {
            error ??= "Kein gültiges JSON-Objekt gefunden.";
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<T>(extracted, options);
            if (value is null)
            {
                error = "Deserialisierung lieferte null.";
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            error = $"Auch nach Extraktion fehlgeschlagen: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Liefert den Inhalt des ersten ausgewogenen {…}-Objekts (inkl. der Klammern),
    /// oder null wenn keines gefunden wurde.
    /// </summary>
    public static string? ExtractFirstJsonObject(string input)
    {
        if (string.IsNullOrEmpty(input)) return null;

        var start = input.IndexOf('{');
        if (start < 0) return null;

        var depth = 0;
        var inString = false;
        var escape = false;

        for (int i = start; i < input.Length; i++)
        {
            var c = input[i];

            if (inString)
            {
                if (escape) { escape = false; continue; }
                if (c == '\\') { escape = true; continue; }
                if (c == '"') inString = false;
                continue;
            }

            if (c == '"') { inString = true; continue; }

            if (c == '{') depth++;
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return input.Substring(start, i - start + 1);
                }
            }
        }
        return null;
    }
}