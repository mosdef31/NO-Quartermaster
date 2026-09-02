using System.Collections.Generic;
using UnityEngine;

namespace Quartermaster
{
    internal sealed class ConvoyEditor : MonoBehaviour
    {
        private const int WindowId = 0x51554152;

        private Rect _window = new Rect(80f, 80f, 780f, 560f);

        private Vector2 _catalogueScroll;
        private Vector2 _listScroll;
        private Vector2 _convoysScroll;

        private string _search = "";
        private string _status = "";

        private ConvoyEntry? _editing;

        private List<VehicleDefinition>? _catalogue;

        private void OnGUI()
        {
            if (!QuartermasterPlugin.EditorVisible) return;

            GUI.skin.label.wordWrap = false;

            _window = GUI.Window(WindowId, _window, Draw, "Quartermaster - convoy lists");
        }

        private void Draw(int id)
        {
            GUILayout.BeginHorizontal();

            DrawCatalogue();
            DrawEditingPanel();
            DrawConvoyList();

            GUILayout.EndHorizontal();

            DrawFooter();

            GUI.DragWindow(new Rect(0f, 0f, _window.width, 20f));
        }

        private void DrawCatalogue()
        {
            GUILayout.BeginVertical(GUILayout.Width(260f));
            GUILayout.Label("Ground vehicles");

            _search = GUILayout.TextField(_search);

            _catalogueScroll = GUILayout.BeginScrollView(_catalogueScroll);

            foreach (VehicleDefinition vehicle in Catalogue())
            {
                if (vehicle == null) continue;
                if (!Matches(vehicle)) continue;

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("+", GUILayout.Width(24f)))
                    Add(vehicle.jsonKey, 1);

                GUILayout.Label(vehicle.unitName, GUILayout.Width(150f));
                GUILayout.Label($"${vehicle.value / 1000f:0}k");

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private List<VehicleDefinition> Catalogue()
        {
            if (_catalogue != null) return _catalogue;

            _catalogue = new List<VehicleDefinition>();

            Encyclopedia encyclopedia = Encyclopedia.i;
            if (encyclopedia == null || encyclopedia.vehicles == null) return _catalogue;

            _catalogue.AddRange(encyclopedia.vehicles);
            _catalogue.Sort((a, b) =>
                string.Compare(a != null ? a.unitName : "", b != null ? b.unitName : "",
                               System.StringComparison.OrdinalIgnoreCase));

            return _catalogue;
        }

        private bool Matches(VehicleDefinition vehicle)
        {
            if (_search.Length == 0) return true;

            return (vehicle.unitName ?? "").IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) >= 0
                || (vehicle.jsonKey ?? "").IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void DrawEditingPanel()
        {
            GUILayout.BeginVertical(GUILayout.Width(300f));

            if (_editing == null)
            {
                GUILayout.Label("No list selected.");
                GUILayout.Label("Pick one on the right, or press New.");
                GUILayout.EndVertical();
                return;
            }

            GUILayout.Label("Name");
            _editing.Name = GUILayout.TextField(_editing.Name);

            GUILayout.Label("Section (optional)");
            _editing.Section = GUILayout.TextField(_editing.Section);

            GUILayout.Label("Icon file (optional, beside the mod)");
            _editing.Icon = GUILayout.TextField(_editing.Icon);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Cooldown", GUILayout.Width(70f));
            string cooldown = GUILayout.TextField(_editing.Cooldown.ToString("0"));
            if (float.TryParse(cooldown, out float parsed)) _editing.Cooldown = Mathf.Max(0f, parsed);
            GUILayout.Label("s");
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            GUILayout.Label("Units");

            _listScroll = GUILayout.BeginScrollView(_listScroll);

            for (int i = _editing.Units.Count - 1; i >= 0; i--)
            {
                ConvoyUnitEntry unit = _editing.Units[i];

                GUILayout.BeginHorizontal();

                GUILayout.Label(DisplayName(unit.Id), GUILayout.Width(120f));

                if (GUILayout.Button("-5", GUILayout.Width(28f))) unit.Count -= 5;
                if (GUILayout.Button("-1", GUILayout.Width(28f))) unit.Count -= 1;

                GUILayout.Label(unit.Count.ToString(), GUILayout.Width(28f));

                if (GUILayout.Button("+1", GUILayout.Width(28f))) unit.Count += 1;
                if (GUILayout.Button("+5", GUILayout.Width(28f))) unit.Count += 5;

                if (GUILayout.Button("x", GUILayout.Width(20f)))
                {
                    _editing.Units.RemoveAt(i);
                    GUILayout.EndHorizontal();
                    continue;
                }

                GUILayout.EndHorizontal();

                if (unit.Count < 1) _editing.Units.RemoveAt(i);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private static string DisplayName(string id)
        {
            if (Encyclopedia.Lookup != null
                && Encyclopedia.Lookup.TryGetValue(id, out UnitDefinition definition)
                && definition != null)
                return definition.unitName;

            return id + " (?)";
        }

        private void Add(string id, int count)
        {
            if (_editing == null)
            {
                _status = "Pick a list first, or press New.";
                return;
            }

            foreach (ConvoyUnitEntry unit in _editing.Units)
            {
                if (unit.Id != id) continue;
                unit.Count += count;
                return;
            }

            _editing.Units.Add(new ConvoyUnitEntry { Id = id, Count = Mathf.Max(1, count) });
        }

        private void DrawConvoyList()
        {
            GUILayout.BeginVertical(GUILayout.Width(190f));
            GUILayout.Label("Your lists");

            _convoysScroll = GUILayout.BeginScrollView(_convoysScroll);

            List<ConvoyEntry> entries = ConvoyInjector.Entries;

            foreach (ConvoyEntry entry in entries)
            {
                GUILayout.BeginHorizontal();

                bool selected = ReferenceEquals(entry, _editing);
                if (GUILayout.Button(selected ? "> " + entry.Name : entry.Name))
                    _editing = entry;

                GUILayout.EndHorizontal();

                if (entry.Section.Length > 0)
                    GUILayout.Label("   in " + entry.Section);
            }

            GUILayout.EndScrollView();

            if (GUILayout.Button("New list"))
            {
                var fresh = new ConvoyEntry { Name = "New convoy", Cooldown = 60f };
                entries.Add(fresh);
                _editing = fresh;
                _status = "New list added. It is not saved yet.";
            }

            GUI.enabled = _editing != null;
            if (GUILayout.Button("Delete selected"))
            {
                entries.Remove(_editing!);
                _editing = null;
                _status = "Deleted. Press Save to write it out.";
            }
            GUI.enabled = true;

            GUILayout.EndVertical();
        }

        private void DrawFooter()
        {
            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Save and apply", GUILayout.Width(140f)))
                SaveAndApply();

            if (GUILayout.Button("Reload from file", GUILayout.Width(140f)))
            {
                _editing = null;
                ConvoyInjector.Reload();
                _status = "Reloaded from quartermaster.json; unsaved changes are gone.";
            }

            if (GUILayout.Button("Close", GUILayout.Width(80f)))
                QuartermasterPlugin.EditorVisible = false;

            GUILayout.Label(_status);
            GUILayout.EndHorizontal();

            GUILayout.Label(
                "Saving during a mission changes the order of the buy menu. In multiplayer that "
                + "order is what a purchase is sent as, so edit between missions.");

            if (ConvoySync.Status.Length > 0)
                GUILayout.Label(ConvoySync.Status);
        }

        private void SaveAndApply()
        {
            List<ConvoyEntry> entries = ConvoyInjector.Entries;

            foreach (ConvoyEntry entry in entries)
            {
                if (entry.Name.Trim().Length == 0)
                {
                    _status = "A list has no name. Give it one before saving.";
                    return;
                }

                if (entry.Units.Count == 0)
                {
                    _status = $"\"{entry.Name}\" has no units in it.";
                    return;
                }
            }

            try
            {
                ConvoyWriter.Save(ConvoyInjector.Path_, entries);
                ConvoyInjector.Reload();
                _editing = null;
                _status = "Saved and applied.";
            }
            catch (System.Exception e)
            {
                _status = "Could not save: " + e.Message;
                QuartermasterPlugin.Log.LogError($"quartermaster.json could not be written: {e}");
            }
        }
    }
}
