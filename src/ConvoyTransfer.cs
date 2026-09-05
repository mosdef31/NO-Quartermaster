using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Quartermaster
{

    internal sealed class ImportResult
    {
        internal List<ConvoyEntry> Entries = new List<ConvoyEntry>();
        internal string Message = "";
    }

    internal static class ConvoyTransfer
    {

        private const string FilePrefix = "QMALL1:";

        internal static string Export(List<ConvoyEntry> entries)
        {
            string json = ConvoyWriter.ToJson(entries);

            string folder = Path.GetDirectoryName(ConvoyInjector.Path_) ?? ".";
            string name = "quartermaster-export-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json";
            string path = Path.Combine(folder, name);

            File.WriteAllText(path, json);

            try
            {
                UnityEngine.GUIUtility.systemCopyBuffer =
                    FilePrefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            }
            catch (Exception e)
            {
                QuartermasterPlugin.Diag("The export could not be put on the clipboard: " + e.Message);
            }

            QuartermasterPlugin.Log.LogInfo($"{entries.Count} list(s) exported to {name}.");
            return path;
        }

        internal static ImportResult Import(string clipboard)
        {
            string trimmed = (clipboard ?? "").Trim();

            if (trimmed.Length == 0)
                throw new JsonError("there is nothing on the clipboard to import", 0);

            var result = new ImportResult();

            if (trimmed.StartsWith(FilePrefix, StringComparison.Ordinal))
            {
                string payload = Strip(trimmed.Substring(FilePrefix.Length));

                string json;
                try
                {
                    json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                }
                catch (FormatException)
                {
                    throw new JsonError(
                        "that export is damaged - it looks like only part of it was copied", 0);
                }

                result.Entries = ConvoyInjector.Read(json).Convoys;
                result.Message = "an exported Quartermaster file";
            }
            else if (trimmed.StartsWith("QM1:", StringComparison.Ordinal))
            {

                result.Entries.Add(ConvoyClipboard.Decode(trimmed));
                result.Message = "one shared list";
            }
            else if (trimmed.StartsWith("{", StringComparison.Ordinal))
            {

                result.Entries = ConvoyInjector.Read(trimmed).Convoys;
                result.Message = "a pasted quartermaster.json";
            }
            else
            {
                throw new JsonError(
                    "that is not a Quartermaster export - they start with \"QMALL1:\", and the "
                    + "contents of a quartermaster.json work too", 0);
            }

            if (result.Entries.Count == 0)
                throw new JsonError("there are no convoy lists in what was imported", 0);

            return result;
        }

        internal static int Merge(List<ConvoyEntry> into, List<ConvoyEntry> imported)
        {
            int added = 0;

            foreach (ConvoyEntry entry in imported)
            {
                entry.Name = ConvoyClipboard.FreeName(entry.Name, into);
                into.Add(entry);
                added++;
            }

            return added;
        }

        private static string Strip(string payload)
        {
            return payload.Trim()
                          .Replace("\n", "").Replace("\r", "")
                          .Replace(" ", "").Replace("\t", "");
        }
    }
}
