using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Quartermaster
{
    internal static class ConvoyInjector
    {

        private const int MaxConvoyIndex = 255;

        private static ConvoyFile? _file;

        internal static string Path_ { get; private set; } = "";

        internal static List<ConvoyEntry> Entries =>
            _file != null ? _file.Convoys : new List<ConvoyEntry>();

        private static readonly Dictionary<Faction, List<Faction.ConvoyGroup>> _added =
            new Dictionary<Faction, List<Faction.ConvoyGroup>>();

        private static readonly HashSet<string> _reported = new HashSet<string>();

        internal static readonly Dictionary<string, string> Unresolved =
            new Dictionary<string, string>();

        internal static void Load(string path)
        {
            _file = null;
            Path_ = path;

            try
            {
                if (!File.Exists(path))
                {
                    File.WriteAllText(path, DefaultJson.Text);
                    QuartermasterPlugin.Log.LogInfo(
                        $"No {Path.GetFileName(path)} found, so a starter one was written.");
                }

                string text = File.ReadAllText(path);

                QuartermasterPlugin.Diag(
                    $"Read {text.Length} character(s) from {path}.");

                _file = Read(text);

                QuartermasterPlugin.Log.LogInfo(
                    $"{Path.GetFileName(path)}: {_file.Convoys.Count} list(s) read.");

                foreach (ConvoyEntry entry in _file.Convoys)
                    QuartermasterPlugin.Diag(
                        $"  line {entry.Line}: \"{entry.Name}\", {entry.Units.Count} unit type(s), "
                        + $"cooldown {entry.Cooldown:0}s, "
                        + (entry.Factions.Count == 0 ? "every faction" : $"{entry.Factions.Count} faction(s)")
                        + (entry.Icon.Length > 0 ? $", icon \"{entry.Icon}\"" : ", icon from its first unit")
                        + ".");
            }
            catch (JsonError e)
            {

                QuartermasterPlugin.Log.LogError(
                    $"{Path.GetFileName(path)} could not be understood - {e.Message}. "
                    + "No custom options were added. Fix that line, or delete the file and "
                    + "start the game to get the starter one back.");
            }
            catch (IOException e)
            {
                QuartermasterPlugin.Log.LogError(
                    $"{Path.GetFileName(path)} could not be opened: {e.Message}");
            }
        }

        internal static ConvoyFile Read(string text)
        {
            var file = new ConvoyFile();

            JsonValue root = Json.Parse(text);
            if (!root.IsObject)
                throw new JsonError("the file should start with '{'", root.Line);

            JsonValue? convoys = root.Member("convoys");
            JsonValue? sections = root.Member("sections");

            if (convoys == null && sections == null)
                throw new JsonError(
                    "there is no \"convoys\" list and no \"sections\" list in this file", root.Line);

            if (convoys != null)
            {
                if (!convoys.IsArray)
                    throw new JsonError("\"convoys\" should be a list, written [ ... ]", convoys.Line);
                ReadInto(file, convoys, "");
            }

            if (sections != null)
            {
                if (!sections.IsArray)
                    throw new JsonError("\"sections\" should be a list, written [ ... ]", sections.Line);

                foreach (JsonValue section in sections.Items!)
                {
                    if (!section.IsObject)
                        throw new JsonError(
                            "every entry in \"sections\" should be a { \"name\": ..., "
                            + "\"convoys\": [ ... ] } block", section.Line);

                    JsonValue? sectionName = section.Member("name");
                    if (sectionName == null || sectionName.Type != JsonValue.Kind.String
                        || sectionName.AsString().Length == 0)
                        throw new JsonError("this section needs a \"name\"", section.Line);

                    JsonValue? sectionConvoys = section.Member("convoys");
                    if (sectionConvoys == null || !sectionConvoys.IsArray)
                        throw new JsonError(
                            $"section \"{sectionName.AsString()}\" needs a \"convoys\" list, "
                            + "written [ ... ]", section.Line);

                    ReadInto(file, sectionConvoys, sectionName.AsString());
                }
            }

            return file;
        }

        private static void ReadInto(ConvoyFile file, JsonValue array, string section)
        {
            foreach (JsonValue item in array.Items!)
            {
                try
                {
                    ConvoyEntry parsed = ReadEntry(item);
                    parsed.Section = section;
                    file.Convoys.Add(parsed);
                }
                catch (JsonError bad)
                {
                    QuartermasterPlugin.Log.LogWarning(
                        $"A list was skipped - {bad.Message}. Every other list in the file was "
                        + "still read.");
                }
            }
        }

        private static ConvoyEntry ReadEntry(JsonValue item)
        {
            {
                if (!item.IsObject)
                    throw new JsonError("every entry in \"convoys\" should be a { ... } block", item.Line);

                var entry = new ConvoyEntry { Line = item.Line };

                JsonValue? name = item.Member("name");
                if (name == null || name.Type != JsonValue.Kind.String || name.AsString().Length == 0)
                    throw new JsonError("this entry needs a \"name\", which is the text on the button",
                                        item.Line);
                entry.Name = name.AsString();

                JsonValue? cooldown = item.Member("cooldown");
                if (cooldown != null)
                {
                    if (cooldown.Type != JsonValue.Kind.Number)
                        throw new JsonError("\"cooldown\" should be a number of seconds", cooldown.Line);
                    entry.Cooldown = (float)cooldown.AsNumber(60d);
                }

                JsonValue? enabled = item.Member("enabled");
                if (enabled != null)
                {
                    if (enabled.Type != JsonValue.Kind.Bool)
                        throw new JsonError("\"enabled\" should be true or false", enabled.Line);
                    entry.Enabled = enabled.Bool;
                }

                JsonValue? icon = item.Member("icon");
                if (icon != null)
                {
                    if (icon.Type != JsonValue.Kind.String)
                        throw new JsonError("\"icon\" should be a file name in quotes", icon.Line);
                    entry.Icon = icon.AsString();
                }

                JsonValue? factions = item.Member("factions");
                if (factions != null)
                {
                    if (!factions.IsArray)
                        throw new JsonError("\"factions\" should be a list of names, written [ ... ]",
                                            factions.Line);
                    foreach (JsonValue f in factions.Items!)
                    {
                        if (f.Type != JsonValue.Kind.String)
                            throw new JsonError("every faction should be a name in quotes", f.Line);
                        entry.Factions.Add(f.AsString());
                    }
                }

                JsonValue? units = item.Member("units");
                if (units == null || !units.IsArray)
                    throw new JsonError($"\"{entry.Name}\" needs a \"units\" list, written [ ... ]",
                                        item.Line);

                foreach (JsonValue u in units.Items!)
                {
                    if (!u.IsObject)
                        throw new JsonError("every unit should be a { \"id\": ..., \"count\": ... } block",
                                            u.Line);

                    JsonValue? id = u.Member("id");
                    if (id == null || id.Type != JsonValue.Kind.String || id.AsString().Length == 0)
                        throw new JsonError("this unit needs an \"id\", which is the unit's id and not "
                                            + "its display name", u.Line);

                    var unit = new ConvoyUnitEntry { Id = id.AsString(), Line = u.Line };

                    JsonValue? count = u.Member("count");
                    if (count != null)
                    {
                        if (count.Type != JsonValue.Kind.Number)
                            throw new JsonError("\"count\" should be a whole number", count.Line);
                        unit.Count = (int)count.AsNumber(1d);
                    }

                        entry.Units.Add(unit);
                }

                return entry;
            }
        }

        internal static void EnsureAllFactions()
        {
            if (_file == null) return;

            Encyclopedia encyclopedia = Encyclopedia.i;
            if (encyclopedia == null || encyclopedia.factions == null)
            {
                QuartermasterPlugin.Diag(
                    "The Encyclopedia has no factions yet, so nothing was added on this pass.");
                return;
            }

            foreach (Faction faction in encyclopedia.factions)
                Ensure(faction);
        }

        internal static void Ensure(Faction? faction)
        {
            if (_file == null || faction == null) return;

            List<Faction.ConvoyGroup> groups = faction.GetConvoyGroups();
            int added = 0;

            foreach (ConvoyEntry entry in _file.Convoys)
            {
                if (string.IsNullOrEmpty(entry.Name)) continue;

                if (!entry.Enabled) continue;

                if (!WantsFaction(entry, faction)) continue;
                if (faction.TryGetConvoyGroup(entry.Name, out _)) continue;

                if (groups.Count > MaxConvoyIndex)
                {

                    QuartermasterPlugin.Log.LogWarning(
                        $"{faction.factionName} is full at 256 convoy options; the rest were skipped.");
                    break;
                }

                Faction.ConvoyGroup? group = Build(entry);
                if (group == null) continue;

                groups.Add(group);
                Remember(faction, group);
                added++;
            }

            if (added > 0)
                QuartermasterPlugin.Log.LogInfo(
                    $"{faction.factionName}: {added} option(s) added, {groups.Count} in total.");
        }

        private static void Remember(Faction faction, Faction.ConvoyGroup group)
        {
            if (!_added.TryGetValue(faction, out List<Faction.ConvoyGroup> ours))
                _added[faction] = ours = new List<Faction.ConvoyGroup>();

            ours.Add(group);
        }

        internal static void RemoveAll()
        {
            int removed = 0;

            foreach (KeyValuePair<Faction, List<Faction.ConvoyGroup>> pair in _added)
            {
                if (pair.Key == null) continue;

                List<Faction.ConvoyGroup> groups = pair.Key.GetConvoyGroups();
                foreach (Faction.ConvoyGroup ours in pair.Value)
                    if (groups.Remove(ours)) removed++;
            }

            _added.Clear();

            if (removed > 0)
                QuartermasterPlugin.Log.LogInfo(
                    $"{removed} custom convoy option(s) were taken back out before reloading.");
        }

        internal static void Reload()
        {
            RemoveAll();
            _reported.Clear();

            Unresolved.Clear();

            Load(Path_);
            EnsureAllFactions();
        }

        private static bool WantsFaction(ConvoyEntry entry, Faction faction)
        {

            if (entry.Factions == null || entry.Factions.Count == 0) return true;

            foreach (string name in entry.Factions)
                if (string.Equals(name, faction.factionName, System.StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }

        private static Faction.ConvoyGroup? Build(ConvoyEntry entry)
        {
            var group = new Faction.ConvoyGroup
            {
                Name = entry.Name,
                coolDown = entry.Cooldown,
                Constituents = new List<Faction.ConvoyUnit>(),
            };

            if (entry.Icon.Length > 0 && !ConvoySync.PlaceholderIcons)
                group.icon = IconLoader.Load(entry.Icon, entry.Name);

            foreach (ConvoyUnitEntry unit in entry.Units)
            {
                UnitDefinition? definition = Resolve(unit.Id);
                if (definition == null) continue;

                int count = Mathf.Max(1, unit.Count);

                group.Constituents.Add(new Faction.ConvoyUnit { Type = definition, Count = count });

                if (group.icon == null)
                    group.icon = definition.friendlyIcon;
            }

            if (group.Constituents.Count == 0)
            {
                QuartermasterPlugin.Log.LogWarning(
                    $"\"{entry.Name}\" (line {entry.Line}) has no usable units, so it was skipped.");
                return null;
            }

            return group;
        }

        private static UnitDefinition? Resolve(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            if (Encyclopedia.Lookup == null)
            {
                Report(id, "the encyclopedia is not loaded yet");
                return null;
            }

            if (!Encyclopedia.Lookup.TryGetValue(id, out UnitDefinition definition))
            {

                definition = null!;
                foreach (KeyValuePair<string, UnitDefinition> pair in Encyclopedia.Lookup)
                {
                    if (!string.Equals(pair.Key, id, System.StringComparison.OrdinalIgnoreCase)) continue;
                    definition = pair.Value;
                    break;
                }
            }

            if (definition == null)
            {
                Report(id, "no unit has that id");
                return null;
            }

            if (definition.unitPrefab == null)
            {
                Report(id, "that unit has no prefab");
                return null;
            }

            return definition;
        }

        private static void Report(string id, string why)
        {

            Unresolved[id] = why;

            if (_reported.Add(id))
                QuartermasterPlugin.Log.LogWarning($"Unit \"{id}\" was skipped: {why}.");
        }

        internal static string? WhyUnresolved(string id)
        {
            if (string.IsNullOrEmpty(id)) return "this unit has no id";

            if (Unresolved.TryGetValue(id, out string remembered)) return remembered;

            if (Encyclopedia.Lookup == null) return null;

            foreach (KeyValuePair<string, UnitDefinition> pair in Encyclopedia.Lookup)
            {
                if (!string.Equals(pair.Key, id, System.StringComparison.OrdinalIgnoreCase)) continue;
                return pair.Value != null && pair.Value.unitPrefab != null
                    ? null
                    : "that unit has no prefab";
            }

            return "no unit has that id";
        }
    }
}
