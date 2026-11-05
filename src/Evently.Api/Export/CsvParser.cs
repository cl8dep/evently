using System.Text;

namespace Evently.Api;

public static class CsvParser
{
    // RFC 4180: quoted cells may hold commas, quotes ("") and line breaks.
    public static List<string[]> Parse(string text)
    {
        var rows = new List<string[]>();
        var row = new List<string>();
        var cell = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (quoted)
            {
                if (ch != '"') cell.Append(ch);
                else if (i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                else quoted = false;
            }
            else if (ch == '"') quoted = true;
            else if (ch == ',') { row.Add(cell.ToString()); cell.Clear(); }
            else if (ch is '\r' or '\n')
            {
                if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                row.Add(cell.ToString()); cell.Clear();
                rows.Add([.. row]); row.Clear();
            }
            else cell.Append(ch);
        }
        if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); rows.Add([.. row]); }
        return rows;
    }
}
