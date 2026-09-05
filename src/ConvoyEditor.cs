using System;
using System.Collections.Generic;
using System.IO;
using NuclearOption.Networking;
using UnityEngine;

namespace Quartermaster
{
    internal sealed class ConvoyEditor : MonoBehaviour
    {
        private const int WindowId = 0x51554152;

        private const float CatalogueShare = 0.36f;
        private const float ListsShare = 0.30f;

        private const float BaseWidth = 1240f;
        private const float BaseHeight = 780f;

        private const float MinWidth = 720f;
        private const float MinHeight = 460f;

        private Rect _window;
        private bool _placed;
        private bool _resizing;

        private float _slack;

        private string _catalogueTrouble = "";

        private bool _saidCatalogueTrouble;

        private Vector2 _catalogueScroll;
        private Vector2 _listScroll;
        private Vector2 _convoysScroll;

        private string _search = "";
        private string _status = "";

        private Color _statusColour = EditorSkin.Accent;

        private VehicleType? _role;

        private ConvoyEntry? _editing;

        private List<VehicleDefinition>? _catalogue;

        private List<string>? _factionNames;

        private void Update()
        {
            if (QuartermasterPlugin.ToggleKey.IsDown())
                QuartermasterPlugin.EditorVisible = !QuartermasterPlugin.EditorVisible;
        }

        private void OnGUI()
        {
            if (!QuartermasterPlugin.EditorVisible) return;

            EditorSkin.Ensure(QuartermasterPlugin.UiScale);
            Place();

            GUISkin previous = GUI.skin;
            GUI.skin = EditorSkin.Skin;

            try
            {
                _window = GUI.Window(WindowId, _window, Draw, "QUARTERMASTER",
                                     EditorSkin.WindowStyle);
            }
            finally
            {
                GUI.skin = previous;
            }
        }

        private void Place()
        {
            float wide = Mathf.Min(EditorSkin.S(BaseWidth), Screen.width - EditorSkin.S(20f));
            float tall = Mathf.Min(EditorSkin.S(BaseHeight), Screen.height - EditorSkin.S(20f));

            if (!_placed)
            {
                _window = new Rect(
                    Mathf.Max(10f, (Screen.width - wide) * 0.5f),
                    Mathf.Max(10f, (Screen.height - tall) * 0.35f),
                    wide, tall);
                _placed = true;
                return;
            }

            _window.width = Mathf.Clamp(_window.width, Mathf.Min(EditorSkin.S(MinWidth), wide), wide);
            _window.height = Mathf.Clamp(_window.height, Mathf.Min(EditorSkin.S(MinHeight), tall), tall);
            _window.x = Mathf.Clamp(_window.x, -_window.width * 0.5f, Screen.width - EditorSkin.S(60f));
            _window.y = Mathf.Clamp(_window.y, 0f, Screen.height - EditorSkin.S(40f));
        }

        private void Draw(int id)
        {

            HoverCard.Begin();

            DrawStatusStrip();

            float columnsHeight = _window.height - EditorSkin.S(196f) + _slack;
            columnsHeight = Mathf.Max(columnsHeight, EditorSkin.S(120f));

            float inner = _window.width - EditorSkin.S(28f);
            float catalogue = inner * CatalogueShare;
            float lists = inner * ListsShare;

            GUILayout.BeginHorizontal(GUILayout.Height(columnsHeight));

            DrawCatalogue(catalogue, columnsHeight);
            DrawEditingPanel(columnsHeight);
            DrawConvoyList(lists, columnsHeight);

            GUILayout.EndHorizontal();

            DrawFooter();
            MeasureSlack();
            DrawResizeGrip();

            HoverCard.Draw(_window);

            GUI.DragWindow(new Rect(0f, 0f, _window.width, EditorSkin.S(22f)));
        }

        private void DrawStatusStrip()
        {
            GUILayout.BeginHorizontal(EditorSkin.StripStyle);

            List<ConvoyEntry> entries = ConvoyInjector.Entries;
            string print = ConvoySync.Fingerprint(entries);

            if (ConvoySync.Blocked)
            {
                EditorSkin.Coloured(EditorSkin.Bad, "MULTIPLAYER: your lists differ from the host's",
                                    EditorSkin.LabelStyle);
            }
            else if (ConvoySync.Status.Length > 0)
            {
                EditorSkin.Coloured(EditorSkin.Good, "MULTIPLAYER: matched", EditorSkin.LabelStyle);
            }

            GUILayout.Label("fingerprint " + print, EditorSkin.DimStyle,
                            GUILayout.Width(EditorSkin.S(180f)));

            GUILayout.Label(entries.Count + " list(s)", EditorSkin.DimStyle,
                            GUILayout.Width(EditorSkin.S(70f)));

            GUILayout.FlexibleSpace();

            int unknown = ConvoyInjector.Unresolved.Count;
            if (unknown > 0)
            {
                EditorSkin.Coloured(EditorSkin.Bad,
                    unknown + " unit id" + (unknown == 1 ? "" : "s") + " resolve to nothing",
                    EditorSkin.LabelStyle);
            }

            GUILayout.EndHorizontal();
        }

        private void DrawCatalogue(float width, float height)
        {
            GUILayout.BeginVertical(EditorSkin.PanelStyle,
                                    GUILayout.Width(width), GUILayout.Height(height));

            GUILayout.Label("GROUND VEHICLES", EditorSkin.HeadingStyle);

            _search = GUILayout.TextField(_search, EditorSkin.FieldStyle);

            DrawRoleFilter(width);

            GUILayout.Space(EditorSkin.S(6f));

            _catalogueScroll = GUILayout.BeginScrollView(_catalogueScroll, GUILayout.ExpandHeight(true));

            float plus = EditorSkin.S(26f);
            float role = EditorSkin.S(56f);
            float price = EditorSkin.S(66f);

            float furniture = EditorSkin.S(34f) + plus + role + price;
            float nameWidth = Mathf.Max(EditorSkin.S(60f), width - furniture);

            int index = 0;
            foreach (VehicleDefinition vehicle in Catalogue())
            {
                if (vehicle == null) continue;
                if (!Matches(vehicle)) continue;

                GUILayout.BeginHorizontal(index++ % 2 == 0 ? EditorSkin.RowStyle : EditorSkin.RowAltStyle,
                                          GUILayout.Height(EditorSkin.S(27f)));

                bool add = false;
                Sprite? icon = vehicle.friendlyIcon;

                if (icon != null)
                {
                    if (GUILayout.Button(GUIContent.none, EditorSkin.SmallButtonStyle,
                                         GUILayout.Width(plus)))
                        add = true;

                    if (Event.current.type == EventType.Repaint)
                        DrawSprite(GUILayoutUtility.GetLastRect(), icon);
                }
                else if (GUILayout.Button("+", EditorSkin.SmallButtonStyle, GUILayout.Width(plus)))
                {
                    add = true;
                }

                GUILayout.Label(vehicle.unitName ?? "", EditorSkin.LabelStyle,
                                GUILayout.Width(nameWidth));

                GUILayout.Label(RoleTag(vehicle), EditorSkin.DimStyle, GUILayout.Width(role));

                GUILayout.Label(Price(vehicle), EditorSkin.PriceStyle, GUILayout.Width(price));

                GUILayout.EndHorizontal();

                Rect rowRect = GUILayoutUtility.GetLastRect();
                Event click = Event.current;

                if (click.type == EventType.MouseDown
                    && click.button == 0
                    && rowRect.Contains(click.mousePosition))
                {
                    add = true;
                    click.Use();
                }

                if (add) Add(vehicle.jsonKey, 1);

                HoverCard.Offer(vehicle);
            }

            if (index == 0)
                GUILayout.Label(
                    Catalogue().Count == 0
                        ? "No vehicles yet - " + _catalogueTrouble
                          + ". This clears itself; leave the window open."
                        : "No vehicle matches that search or filter.",
                    EditorSkin.WrapStyle);

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void MeasureSlack()
        {
            if (Event.current.type != EventType.Repaint) return;

            float bottom = GUILayoutUtility.GetLastRect().yMax;
            float gap = _window.height - EditorSkin.S(12f) - bottom;

            if (Mathf.Abs(gap) < 2f) return;

            _slack = Mathf.Clamp(_slack + gap, -EditorSkin.S(200f), EditorSkin.S(400f));
        }

        private void DrawRoleFilter(float width)
        {
            var options = new List<VehicleType?> { null };
            foreach (VehicleType type in Enum.GetValues(typeof(VehicleType)))
                options.Add(type);

            var labels = new List<string>();
            foreach (VehicleType? option in options)
                labels.Add(option == null ? "ALL" : ShortRole(option.Value));

            GUIStyle style = EditorSkin.ToggleStyle;

            float cell = EditorSkin.WidestOf(style, labels)
                         + style.padding.left + style.padding.right;

            float step = cell + style.margin.left + style.margin.right;
            float room = width - EditorSkin.S(18f);

            int perRow = Mathf.Max(1, Mathf.FloorToInt(room / step));
            int rows = Mathf.CeilToInt(options.Count / (float)perRow);

            for (int i = 0; i < rows * perRow; i++)
            {
                if (i % perRow == 0)
                {
                    if (i > 0) GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }

                if (i >= options.Count)
                {

                    GUILayout.Space(step);
                    continue;
                }

                VehicleType? option = options[i];
                bool on = Nullable.Equals(_role, option);

                if (GUILayout.Button(labels[i],
                                     on ? EditorSkin.ToggleOnStyle : EditorSkin.ToggleStyle,
                                     GUILayout.Width(cell)))
                    _role = on ? null : option;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private static string ShortRole(VehicleType type)
        {
            switch (type)
            {
                case VehicleType.IR_SAM: return "IR SAM";
                case VehicleType.R_SAM: return "R SAM";
                default: return type.ToString();
            }
        }

        private static void DrawSprite(Rect rect, Sprite sprite)
        {
            Texture texture = sprite.texture;
            if (texture == null || texture.width <= 0 || texture.height <= 0) return;

            Rect source;
            try
            {
                source = sprite.textureRect;
            }
            catch (Exception)
            {
                source = sprite.rect;
            }

            if (source.width <= 0f || source.height <= 0f) return;

            var coords = new Rect(source.x / texture.width,
                                  source.y / texture.height,
                                  source.width / texture.width,
                                  source.height / texture.height);

            float inset = EditorSkin.S(3f);
            Rect box = new Rect(rect.x + inset, rect.y + inset,
                                Mathf.Max(1f, rect.width - inset * 2f),
                                Mathf.Max(1f, rect.height - inset * 2f));

            float fit = Mathf.Min(box.width / source.width, box.height / source.height);
            float drawnWidth = source.width * fit;
            float drawnHeight = source.height * fit;

            var target = new Rect(box.x + (box.width - drawnWidth) * 0.5f,
                                  box.y + (box.height - drawnHeight) * 0.5f,
                                  drawnWidth, drawnHeight);

            GUI.DrawTextureWithTexCoords(target, texture, coords, alphaBlend: true);
        }

        private static string RoleTag(VehicleDefinition vehicle)
        {
            return ShortRole(vehicle.vehicleType);
        }

        private static string Price(VehicleDefinition vehicle)
        {
            return UnitConverter.ValueReading(vehicle.value);
        }

        private List<VehicleDefinition> Catalogue()
        {
            if (_catalogue != null && _catalogue.Count > 0) return _catalogue;

            var built = new List<VehicleDefinition>();
            Encyclopedia encyclopedia = Encyclopedia.i;

            if (encyclopedia == null)
                _catalogueTrouble = "the game has not finished loading its unit list";
            else if (encyclopedia.vehicles == null || encyclopedia.vehicles.Count == 0)
                _catalogueTrouble = "the game's own vehicle list is empty";
            else
                _catalogueTrouble = "";

            if (_catalogueTrouble.Length > 0)
            {

                if (!_saidCatalogueTrouble)
                {
                    _saidCatalogueTrouble = true;
                    QuartermasterPlugin.Log.LogWarning(
                        "The editor has no vehicles to show because " + _catalogueTrouble
                        + ". It will keep asking, so this should clear itself once the game "
                        + "has loaded.");
                }

                _catalogue = built;
                return built;
            }

            _saidCatalogueTrouble = false;

            built.AddRange(encyclopedia!.vehicles);
            built.Sort((a, b) =>
                string.Compare(a != null ? a.unitName : "", b != null ? b.unitName : "",
                               StringComparison.OrdinalIgnoreCase));

            _catalogue = built;
            return built;
        }

        private bool Matches(VehicleDefinition vehicle)
        {
            if (_role.HasValue && vehicle.vehicleType != _role.Value) return false;
            if (_search.Length == 0) return true;

            return (vehicle.unitName ?? "").IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0
                || (vehicle.jsonKey ?? "").IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void DrawEditingPanel(float height)
        {
            GUILayout.BeginVertical(EditorSkin.PanelStyle,
                                    GUILayout.ExpandWidth(true), GUILayout.Height(height));

            if (_editing == null)
            {
                GUILayout.Label("NOTHING SELECTED", EditorSkin.HeadingStyle);
                GUILayout.Label(
                    "Pick a list on the right, or press New. A list is a bundle of vehicles "
                    + "that becomes one button in the convoy purchase menu.",
                    EditorSkin.WrapStyle);
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
                return;
            }

            GUILayout.Label("EDITING", EditorSkin.HeadingStyle);

            DrawField("Name", ref _editing.Name);

            GUILayout.BeginHorizontal();
            DrawField("Section (optional)", ref _editing.Section);
            GUILayout.Space(EditorSkin.S(6f));

            DrawField("Icon file (optional)", ref _editing.Icon);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Cooldown", EditorSkin.LabelStyle, GUILayout.Width(EditorSkin.S(70f)));
            string cooldown = GUILayout.TextField(_editing.Cooldown.ToString("0"), EditorSkin.FieldStyle,
                                                  GUILayout.Width(EditorSkin.S(70f)));
            if (float.TryParse(cooldown, out float parsed)) _editing.Cooldown = Mathf.Max(0f, parsed);
            GUILayout.Label("seconds between purchases", EditorSkin.DimStyle);
            GUILayout.EndHorizontal();

            DrawFactionTargeting();

            GUILayout.Label("UNITS", EditorSkin.HeadingStyle);

            _listScroll = GUILayout.BeginScrollView(_listScroll, GUILayout.ExpandHeight(true));
            DrawUnitRows();
            GUILayout.EndScrollView();

            DrawCostStrip();

            GUILayout.EndVertical();
        }

        private static void DrawField(string label, ref string value)
        {
            GUILayout.BeginVertical();
            GUILayout.Label(label, EditorSkin.DimStyle);
            value = GUILayout.TextField(value ?? "", EditorSkin.FieldStyle);
            GUILayout.EndVertical();
        }

        private void DrawFactionTargeting()
        {
            GUILayout.Label("OFFERED TO", EditorSkin.HeadingStyle);

            List<string> names = FactionNames();

            if (names.Count == 0)
            {
                GUILayout.Label("No factions loaded yet. Open this in a mission to choose.",
                                EditorSkin.WrapStyle);
                return;
            }

            var labels = new List<string> { "Every faction" };
            labels.AddRange(names);

            GUIStyle plain = EditorSkin.ToggleStyle;
            float cell = EditorSkin.WidestOf(plain, labels)
                         + plain.padding.left + plain.padding.right;

            GUILayout.BeginHorizontal();

            bool all = _editing!.Factions.Count == 0;
            if (GUILayout.Button("Every faction",
                                 all ? EditorSkin.ToggleOnStyle : EditorSkin.ToggleStyle,
                                 GUILayout.Width(cell)))
                _editing.Factions.Clear();

            foreach (string name in names)
            {
                bool on = !all && Has(name);

                if (GUILayout.Button(name, on ? EditorSkin.ToggleOnStyle : EditorSkin.ToggleStyle,
                                     GUILayout.Width(cell)))
                {
                    if (on) Remove(name);
                    else _editing.Factions.Add(name);
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (!all)
                EditorSkin.Coloured(EditorSkin.Warn,
                    "A list only some factions get sits at a different position in each "
                    + "faction's menu. Safe alone; online the fingerprint check catches it.",
                    EditorSkin.WrapStyle);
        }

        private bool Has(string name)
        {
            foreach (string held in _editing!.Factions)
                if (string.Equals(held, name, StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }

        private void Remove(string name)
        {
            for (int i = _editing!.Factions.Count - 1; i >= 0; i--)
                if (string.Equals(_editing.Factions[i], name, StringComparison.OrdinalIgnoreCase))
                    _editing.Factions.RemoveAt(i);
        }

        private List<string> FactionNames()
        {
            if (_factionNames != null) return _factionNames;

            var names = new List<string>();
            Encyclopedia encyclopedia = Encyclopedia.i;

            if (encyclopedia == null || encyclopedia.factions == null)
                return names;

            foreach (Faction faction in encyclopedia.factions)
            {
                if (faction == null || string.IsNullOrEmpty(faction.factionName)) continue;
                if (!names.Contains(faction.factionName)) names.Add(faction.factionName);
            }

            _factionNames = names;
            return names;
        }

        private void DrawUnitRows()
        {
            if (_editing!.Units.Count == 0)
            {
                GUILayout.Label("No units yet. Press + on a vehicle to the left.",
                                EditorSkin.WrapStyle);
                return;
            }

            for (int i = _editing.Units.Count - 1; i >= 0; i--)
            {
                ConvoyUnitEntry unit = _editing.Units[i];
                string? why = ConvoyInjector.WhyUnresolved(unit.Id);

                GUILayout.BeginHorizontal(i % 2 == 0 ? EditorSkin.RowStyle : EditorSkin.RowAltStyle,
                                          GUILayout.Height(EditorSkin.S(26f)));

                if (why != null)
                    EditorSkin.Coloured(EditorSkin.Bad, unit.Id, EditorSkin.LabelStyle,
                                        GUILayout.ExpandWidth(true));
                else
                    GUILayout.Label(DisplayName(unit.Id), EditorSkin.LabelStyle,
                                    GUILayout.ExpandWidth(true));

                float small = EditorSkin.S(30f);

                if (GUILayout.Button("-5", EditorSkin.SmallButtonStyle, GUILayout.Width(small)))
                    unit.Count -= 5;
                if (GUILayout.Button("-1", EditorSkin.SmallButtonStyle, GUILayout.Width(small)))
                    unit.Count -= 1;

                GUILayout.Label(unit.Count.ToString(), EditorSkin.CountStyle,
                                GUILayout.Width(EditorSkin.S(44f)));

                if (GUILayout.Button("+1", EditorSkin.SmallButtonStyle, GUILayout.Width(small)))
                    unit.Count += 1;
                if (GUILayout.Button("+5", EditorSkin.SmallButtonStyle, GUILayout.Width(small)))
                    unit.Count += 5;

                if (GUILayout.Button("x", EditorSkin.SmallButtonStyle, GUILayout.Width(EditorSkin.S(24f))))
                {
                    _editing.Units.RemoveAt(i);
                    GUILayout.EndHorizontal();
                    continue;
                }

                GUILayout.EndHorizontal();

                HoverCard.Offer(Definition(unit.Id));

                if (why != null)
                    EditorSkin.Coloured(EditorSkin.Bad, "     " + why + " - it will not be bought",
                                        EditorSkin.WrapStyle);

                if (unit.Count < 1) _editing.Units.RemoveAt(i);
            }
        }

        private void DrawCostStrip()
        {
            float cost = Cost(_editing!);

            GUILayout.BeginHorizontal(EditorSkin.StripStyle);

            GUILayout.Label("BUNDLE COST", EditorSkin.DimStyle, GUILayout.Width(EditorSkin.S(90f)));
            EditorSkin.Coloured(EditorSkin.Accent, UnitConverter.ValueReading(cost),
                                EditorSkin.CountStyle, GUILayout.Width(EditorSkin.S(100f)));

            GUILayout.Label(Vehicles(_editing!) + " vehicle(s)", EditorSkin.DimStyle);

            GUILayout.FlexibleSpace();

            if (TryAllocation(out float allocation))
            {
                if (cost > allocation)
                    EditorSkin.Coloured(EditorSkin.Bad,
                        "Too dear - you have " + UnitConverter.ValueReading(allocation),
                        EditorSkin.LabelStyle);
                else
                    EditorSkin.Coloured(EditorSkin.Good,
                        "You have " + UnitConverter.ValueReading(allocation),
                        EditorSkin.LabelStyle);
            }

            GUILayout.EndHorizontal();

            DrawBudgetWarning(cost);
        }

        private void DrawBudgetWarning(float cost)
        {
            float budget = QuartermasterPlugin.BudgetCap;
            if (budget <= 0f || cost <= budget) return;

            EditorSkin.Coloured(EditorSkin.Warn,
                "Over " + UnitConverter.ValueReading(budget) + " - this price may be too high.",
                EditorSkin.WrapStyle);
        }

        private static float Cost(ConvoyEntry entry)
        {
            float total = 0f;

            foreach (ConvoyUnitEntry unit in entry.Units)
            {
                UnitDefinition? definition = Definition(unit.Id);
                if (definition == null) continue;

                total += (definition.value + UnitArmament.AmmoValue(definition))
                         * Mathf.Max(1, unit.Count);
            }

            return total;
        }

        private static int Vehicles(ConvoyEntry entry)
        {
            int count = 0;
            foreach (ConvoyUnitEntry unit in entry.Units) count += Mathf.Max(1, unit.Count);
            return count;
        }

        private static bool TryAllocation(out float allocation)
        {
            allocation = 0f;

            try
            {
                if (!GameManager.GetLocalPlayer(out Player player) || player == null) return false;
                allocation = player.Allocation;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static UnitDefinition? Definition(string id)
        {
            if (string.IsNullOrEmpty(id) || Encyclopedia.Lookup == null) return null;

            if (Encyclopedia.Lookup.TryGetValue(id, out UnitDefinition exact) && exact != null)
                return exact;

            foreach (KeyValuePair<string, UnitDefinition> pair in Encyclopedia.Lookup)
                if (string.Equals(pair.Key, id, StringComparison.OrdinalIgnoreCase))
                    return pair.Value;

            return null;
        }

        private static Sprite? ListIcon(ConvoyEntry entry)
        {
            if (entry.Icon.Length > 0)
            {
                Sprite? named = IconLoader.Load(entry.Icon, entry.Name);
                if (named != null) return named;
            }

            foreach (ConvoyUnitEntry unit in entry.Units)
            {
                UnitDefinition? definition = Definition(unit.Id);
                if (definition != null && definition.friendlyIcon != null)
                    return definition.friendlyIcon;
            }

            return null;
        }

        private static string DisplayName(string id)
        {
            UnitDefinition? definition = Definition(id);
            return definition != null ? definition.unitName : id;
        }

        private void Add(string id, int count)
        {
            if (_editing == null)
            {
                Say("Pick a list first, or press New.", EditorSkin.Warn);
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

        private void DrawConvoyList(float width, float height)
        {
            GUILayout.BeginVertical(EditorSkin.PanelStyle,
                                    GUILayout.Width(width), GUILayout.Height(height));

            GUILayout.Label("YOUR LISTS", EditorSkin.HeadingStyle);

            List<ConvoyEntry> entries = ConvoyInjector.Entries;

            float arrow = EditorSkin.S(22f);
            float onOff = EditorSkin.S(32f);

            float badge = EditorSkin.S(22f);

            float rowFurniture = EditorSkin.S(38f) + arrow + arrow + onOff + badge;
            float listNameWidth = Mathf.Max(EditorSkin.S(70f), width - rowFurniture);

            _convoysScroll = GUILayout.BeginScrollView(_convoysScroll, GUILayout.ExpandHeight(true));

            string section = "";
            bool first = true;

            for (int i = 0; i < entries.Count; i++)
            {
                ConvoyEntry entry = entries[i];
                bool selected = ReferenceEquals(entry, _editing);

                if (first || entry.Section != section)
                {
                    section = entry.Section;
                    first = false;

                    if (i > 0) GUILayout.Space(EditorSkin.S(6f));

                    GUILayout.Label(section.Length > 0 ? section.ToUpperInvariant() : "UNSECTIONED",
                                    EditorSkin.SectionStyle);
                }

                GUILayout.BeginHorizontal(i % 2 == 0 ? EditorSkin.RowStyle : EditorSkin.RowAltStyle,
                                          GUILayout.Height(EditorSkin.S(26f)));

                if (GUILayout.Button("^", EditorSkin.SmallButtonStyle, GUILayout.Width(arrow)))
                    Move(entries, i, -1);
                if (GUILayout.Button("v", EditorSkin.SmallButtonStyle, GUILayout.Width(arrow)))
                    Move(entries, i, 1);

                if (GUILayout.Button(entry.Enabled ? "on" : "off",
                                     entry.Enabled ? EditorSkin.ToggleOnStyle : EditorSkin.ToggleStyle,
                                     GUILayout.Width(onOff)))
                {
                    entry.Enabled = !entry.Enabled;

                    Say(entry.Enabled
                            ? entry.Name + " is on. Press Save to apply."
                            : entry.Name + " is off - it stays in your file and leaves the buy "
                              + "menu. Press Save to apply.",
                        EditorSkin.Accent);
                }

                GUIStyle style = selected
                    ? EditorSkin.ListButtonSelectedStyle
                    : entry.Enabled
                        ? EditorSkin.ListButtonStyle
                        : EditorSkin.ListButtonOffStyle;

                Rect badgeRect = GUILayoutUtility.GetRect(badge, badge,
                                                          GUILayout.Width(badge),
                                                          GUILayout.Height(badge));

                if (Event.current.type == EventType.Repaint)
                {
                    Sprite? listIcon = ListIcon(entry);
                    if (listIcon != null) DrawSprite(badgeRect, listIcon);
                }

                if (GUILayout.Button(entry.Name, style, GUILayout.Width(listNameWidth)))
                    _editing = entry;

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("New", EditorSkin.ButtonStyle, GUILayout.ExpandWidth(true)))
                NewList(entries);

            GUI.enabled = _editing != null;
            if (GUILayout.Button("Clone", EditorSkin.ButtonStyle, GUILayout.ExpandWidth(true)))
                Clone(entries);
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUI.enabled = _editing != null;
            if (GUILayout.Button("Copy", EditorSkin.ButtonStyle, GUILayout.ExpandWidth(true)))
                Copy();
            GUI.enabled = true;

            if (GUILayout.Button("Paste", EditorSkin.ButtonStyle, GUILayout.ExpandWidth(true)))
                Paste(entries);
            GUILayout.EndHorizontal();

            GUILayout.Space(EditorSkin.S(6f));
            GUILayout.Label("WHOLE FILE", EditorSkin.SectionStyle);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Export", EditorSkin.ButtonStyle, GUILayout.ExpandWidth(true)))
                ExportAll(entries);

            if (GUILayout.Button("Import +", EditorSkin.ButtonStyle, GUILayout.ExpandWidth(true)))
                ImportAll(entries, replace: false);

            if (GUILayout.Button("Replace", EditorSkin.ButtonStyle, GUILayout.ExpandWidth(true)))
                ImportAll(entries, replace: true);
            GUILayout.EndHorizontal();

            GUILayout.Space(EditorSkin.S(6f));

            GUI.enabled = _editing != null;
            if (GUILayout.Button("Delete selected", EditorSkin.ButtonStyle))
                DeleteSelected(entries);
            GUI.enabled = true;

            GUILayout.EndVertical();
        }

        private void ExportAll(List<ConvoyEntry> entries)
        {
            try
            {
                string path = ConvoyTransfer.Export(entries);

                Say("Exported " + entries.Count + " list(s) to " + Path.GetFileName(path)
                    + " and to your clipboard.", EditorSkin.Good);
            }
            catch (Exception e)
            {
                Say("Could not export: " + e.Message, EditorSkin.Bad);
                QuartermasterPlugin.Log.LogError($"The convoy file could not be exported: {e}");
            }
        }

        private void ImportAll(List<ConvoyEntry> entries, bool replace)
        {
            ImportResult result;

            try
            {
                result = ConvoyTransfer.Import(GUIUtility.systemCopyBuffer);
            }
            catch (JsonError bad)
            {
                Say("Nothing imported - " + bad.Message + ".", EditorSkin.Bad);
                return;
            }
            catch (Exception e)
            {
                Say("Nothing imported - " + e.Message, EditorSkin.Bad);
                return;
            }

            string backup = "";

            if (replace)
            {
                try
                {
                    backup = Path.GetFileName(ConvoyTransfer.Export(entries));
                }
                catch (Exception e)
                {
                    Say("Nothing replaced - the backup could not be written: " + e.Message,
                        EditorSkin.Bad);
                    return;
                }

                entries.Clear();
                _editing = null;
            }

            int added = ConvoyTransfer.Merge(entries, result.Entries);

            Say(replace
                    ? "Replaced everything with " + added + " list(s) from " + result.Message
                      + ". Your old lists are in " + backup + ". Press Save to apply."
                    : "Added " + added + " list(s) from " + result.Message + ". Press Save to apply.",
                EditorSkin.Good);
        }

        private void NewList(List<ConvoyEntry> entries)
        {
            var fresh = new ConvoyEntry
            {
                Name = ConvoyClipboard.FreeName("New convoy", entries),
                Cooldown = 60f,
            };

            entries.Add(fresh);
            _editing = fresh;
            Say("New list added. It is not saved yet.", EditorSkin.Accent);
        }

        private void Clone(List<ConvoyEntry> entries)
        {
            ConvoyEntry? source = _editing;
            if (source == null) return;

            var copy = new ConvoyEntry
            {
                Name = ConvoyClipboard.FreeName(source.Name + " copy", entries),
                Cooldown = source.Cooldown,
                Icon = source.Icon,
                Enabled = source.Enabled,
                Section = source.Section,
                Factions = new List<string>(source.Factions),
                Units = new List<ConvoyUnitEntry>(),
            };

            foreach (ConvoyUnitEntry unit in source.Units)
                copy.Units.Add(new ConvoyUnitEntry { Id = unit.Id, Count = unit.Count });

            int at = entries.IndexOf(source);
            entries.Insert(at < 0 ? entries.Count : at + 1, copy);
            _editing = copy;

            Say($"Cloned as \"{copy.Name}\". Press Save to write it out.", EditorSkin.Accent);
        }

        private void Move(List<ConvoyEntry> entries, int index, int direction)
        {
            if (index < 0 || index >= entries.Count) return;

            ConvoyEntry moving = entries[index];

            int target = -1;
            for (int i = index + direction; i >= 0 && i < entries.Count; i += direction)
            {
                if (entries[i].Section != moving.Section) continue;
                target = i;
                break;
            }

            if (target < 0)
            {
                Say(moving.Section.Length > 0
                        ? $"\"{moving.Name}\" is already at the end of section \"{moving.Section}\"."
                        : $"\"{moving.Name}\" is already at the end of the list.",
                    EditorSkin.Warn);
                return;
            }

            entries[index] = entries[target];
            entries[target] = moving;

            Say("Reordered. The buy menu follows this order, so press Save to keep it.",
                EditorSkin.Accent);
        }

        private void Copy()
        {
            if (_editing == null) return;

            try
            {
                GUIUtility.systemCopyBuffer = ConvoyClipboard.Encode(_editing);
                Say($"\"{_editing.Name}\" copied. Paste it to anyone running Quartermaster.",
                    EditorSkin.Good);
            }
            catch (Exception e)
            {
                Say("Could not copy: " + e.Message, EditorSkin.Bad);
            }
        }

        private void Paste(List<ConvoyEntry> entries)
        {
            try
            {
                ConvoyEntry pasted = ConvoyClipboard.Decode(GUIUtility.systemCopyBuffer);
                pasted.Name = ConvoyClipboard.FreeName(pasted.Name, entries);

                entries.Add(pasted);
                _editing = pasted;

                Say($"Pasted \"{pasted.Name}\". Press Save to write it out.", EditorSkin.Good);
            }
            catch (JsonError bad)
            {
                Say("Nothing pasted - " + bad.Message + ".", EditorSkin.Bad);
            }
            catch (Exception e)
            {
                Say("Nothing pasted - " + e.Message, EditorSkin.Bad);
            }
        }

        private void DeleteSelected(List<ConvoyEntry> entries)
        {
            ConvoyEntry? victim = _editing;
            if (victim == null) return;

            int at = entries.IndexOf(victim);
            if (at < 0)
            {

                _editing = null;
                Say("That list is no longer the one on file. Reloaded selection; pick it again.",
                    EditorSkin.Warn);
                return;
            }

            entries.RemoveAt(at);
            _editing = null;

            string name = victim.Name;
            if (SaveAndApply())
            {
                Say($"Deleted \"{name}\" and wrote it out.", EditorSkin.Good);
                return;
            }

            entries.Insert(at, victim);
            _editing = victim;
            Say("Not deleted - " + _status, EditorSkin.Bad);
        }

        private void DrawFooter()
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Save and apply", EditorSkin.ButtonStyle,
                                 GUILayout.Width(EditorSkin.S(150f))))
                SaveAndApply();

            if (GUILayout.Button("Reload from file", EditorSkin.ButtonStyle,
                                 GUILayout.Width(EditorSkin.S(150f))))
            {
                _editing = null;
                ConvoyInjector.Reload();
                Say("Reloaded from quartermaster.json. Unsaved changes are gone.", EditorSkin.Accent);
            }

            if (GUILayout.Button("Close", EditorSkin.ButtonStyle, GUILayout.Width(EditorSkin.S(90f))))
                QuartermasterPlugin.EditorVisible = false;

            EditorSkin.Coloured(_statusColour, _status, EditorSkin.LabelStyle,
                                GUILayout.ExpandWidth(true));

            GUILayout.EndHorizontal();

            GUILayout.Label(
                "Saving in a mission reorders the buy menu, and online a purchase is sent as "
                + "that order - so edit between missions. " + QuartermasterPlugin.ToggleKey
                + " hides this window.",
                EditorSkin.WrapStyle);
        }

        private void DrawResizeGrip()
        {
            float size = EditorSkin.S(16f);
            var grip = new Rect(_window.width - size, _window.height - size, size, size);

            GUI.Label(grip, "//", EditorSkin.DimStyle);

            Event now = Event.current;

            if (now.type == EventType.MouseDown && grip.Contains(now.mousePosition))
            {
                _resizing = true;
                now.Use();
            }
            else if (now.type == EventType.MouseUp)
            {
                _resizing = false;
            }
            else if (_resizing && now.type == EventType.MouseDrag)
            {
                _window.width += now.delta.x;
                _window.height += now.delta.y;
                now.Use();
            }
        }

        private void Say(string message, Color colour)
        {
            _status = message;
            _statusColour = colour;
        }

        private bool SaveAndApply()
        {
            List<ConvoyEntry> entries = ConvoyInjector.Entries;

            foreach (ConvoyEntry entry in entries)
            {
                if (entry.Name.Trim().Length == 0)
                {
                    Say("A list has no name. Give it one before saving.", EditorSkin.Bad);
                    return false;
                }

                if (entry.Units.Count == 0)
                {
                    Say($"\"{entry.Name}\" has no units in it.", EditorSkin.Bad);
                    return false;
                }
            }

            try
            {
                ConvoyWriter.Save(ConvoyInjector.Path_, entries);
                ConvoyInjector.Reload();
                _editing = null;
                Say("Saved and applied.", EditorSkin.Good);
                return true;
            }
            catch (Exception e)
            {
                Say("Could not save: " + e.Message, EditorSkin.Bad);
                QuartermasterPlugin.Log.LogError($"quartermaster.json could not be written: {e}");
                return false;
            }
        }
    }
}
