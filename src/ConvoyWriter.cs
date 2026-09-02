using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Quartermaster
{
    internal static class ConvoyWriter
    {

        internal static string ToJson(List<ConvoyEntry> entries)
        {

            var sections = new List<string>();
            foreach (ConvoyEntry entry in entries)
            {
                if (entry.Section.Length == 0) continue;
                if (!sections.Contains(entry.Section)) sections.Add(entry.Section);
            }

            var sb = new StringBuilder();
            sb.Append("{\n");

            sb.Append("  \"convoys\": [\n");
            WriteEntries(sb, entries, "", "    ");
            sb.Append("  ]");

            if (sections.Count > 0)
            {
                sb.Append(",\n  \"sections\": [\n");

                for (int i = 0; i < sections.Count; i++)
                {
                    sb.Append("    {\n      \"name\": ").Append(Quote(sections[i]))
                      .Append(",\n      \"convoys\": [\n");
                    WriteEntries(sb, entries, sections[i], "        ");
                    sb.Append("      ]\n    }");
                    if (i < sections.Count - 1) sb.Append(',');
                    sb.Append('\n');
                }

                sb.Append("  ]");
            }

            sb.Append("\n}\n");
            return sb.ToString();
        }

        internal static void Save(string path, List<ConvoyEntry> entries)
        {
            string text = ToJson(entries);
            string temporary = path + ".writing";

            File.WriteAllText(temporary, text);

            if (File.Exists(path)) File.Delete(path);
            File.Move(temporary, path);

            QuartermasterPlugin.Log.LogInfo(
                $"{Path.GetFileName(path)} written: {entries.Count} list(s).");
        }

        private static void WriteEntries(StringBuilder sb, List<ConvoyEntry> entries,
                                         string section, string indent)
        {
            var mine = new List<ConvoyEntry>();
            foreach (ConvoyEntry entry in entries)
                if (entry.Section == section) mine.Add(entry);

            for (int i = 0; i < mine.Count; i++)
            {
                ConvoyEntry entry = mine[i];

                sb.Append(indent).Append("{\n");
                sb.Append(indent).Append("  \"name\": ").Append(Quote(entry.Name)).Append(",\n");
                sb.Append(indent).Append("  \"cooldown\": ").Append(Number(entry.Cooldown)).Append(",\n");

                if (entry.Icon.Length > 0)
                    sb.Append(indent).Append("  \"icon\": ").Append(Quote(entry.Icon)).Append(",\n");

                sb.Append(indent).Append("  \"factions\": [");
                for (int f = 0; f < entry.Factions.Count; f++)
                {
                    if (f > 0) sb.Append(", ");
                    sb.Append(Quote(entry.Factions[f]));
                }
                sb.Append("],\n");

                sb.Append(indent).Append("  \"units\": [\n");
                for (int u = 0; u < entry.Units.Count; u++)
                {
                    ConvoyUnitEntry unit = entry.Units[u];
                    sb.Append(indent).Append("    { \"id\": ").Append(Quote(unit.Id))
                      .Append(", \"count\": ").Append(unit.Count.ToString(CultureInfo.InvariantCulture))
                      .Append(" }");
                    if (u < entry.Units.Count - 1) sb.Append(',');
                    sb.Append('\n');
                }
                sb.Append(indent).Append("  ]\n");

                sb.Append(indent).Append('}');
                if (i < mine.Count - 1) sb.Append(',');
                sb.Append('\n');
            }
        }

        private static string Quote(string value)
        {
            var sb = new StringBuilder(value.Length + 2);
            sb.Append('"');

            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }

            sb.Append('"');
            return sb.ToString();
        }

        private static string Number(float value)
        {
            if (value == (int)value) return ((int)value).ToString(CultureInfo.InvariantCulture);
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
