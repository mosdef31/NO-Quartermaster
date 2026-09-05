using System;
using System.Collections.Generic;
using System.Text;

namespace Quartermaster
{
    internal static class ConvoyClipboard
    {

        private const string Prefix = "QM1:";

        internal static string Encode(ConvoyEntry entry)
        {

            var lone = new ConvoyEntry
            {
                Name = entry.Name,
                Cooldown = entry.Cooldown,
                Icon = entry.Icon,
                Enabled = entry.Enabled,
                Section = "",
                Factions = new List<string>(entry.Factions),
                Units = new List<ConvoyUnitEntry>(),
            };

            foreach (ConvoyUnitEntry unit in entry.Units)
                lone.Units.Add(new ConvoyUnitEntry { Id = unit.Id, Count = unit.Count });

            string json = ConvoyWriter.ToJson(new List<ConvoyEntry> { lone });
            return Prefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }

        internal static ConvoyEntry Decode(string text)
        {
            string trimmed = (text ?? "").Trim();

            if (trimmed.Length == 0)
                throw new JsonError("there is nothing on the clipboard to paste", 0);

            if (!trimmed.StartsWith(Prefix, StringComparison.Ordinal))
            {

                if (trimmed.StartsWith("QM", StringComparison.Ordinal))
                    throw new JsonError(
                        "that list was shared by a newer version of Quartermaster than this one", 0);

                throw new JsonError(
                    "that does not look like a shared convoy list - they start with \"QM1:\"", 0);
            }

            string payload = trimmed.Substring(Prefix.Length).Trim();

            payload = payload.Replace("\n", "").Replace("\r", "").Replace(" ", "").Replace("\t", "");

            string json;
            try
            {
                json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            }
            catch (FormatException)
            {
                throw new JsonError(
                    "that shared list is damaged - it looks like only part of it was copied", 0);
            }

            ConvoyFile file = ConvoyInjector.Read(json);

            if (file.Convoys.Count == 0)
                throw new JsonError("that shared list has no convoy in it", 0);

            ConvoyEntry pasted = file.Convoys[0];
            pasted.Section = "";
            return pasted;
        }

        internal static string FreeName(string wanted, List<ConvoyEntry> existing)
        {
            string basis = wanted.Trim().Length == 0 ? "Shared convoy" : wanted.Trim();

            if (!Taken(basis, existing)) return basis;

            for (int n = 2; n < 1000; n++)
            {
                string candidate = basis + " (" + n + ")";
                if (!Taken(candidate, existing)) return candidate;
            }

            return basis + " (" + DateTime.Now.Ticks + ")";
        }

        private static bool Taken(string name, List<ConvoyEntry> existing)
        {
            foreach (ConvoyEntry entry in existing)
                if (string.Equals(entry.Name, name, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }
    }
}
