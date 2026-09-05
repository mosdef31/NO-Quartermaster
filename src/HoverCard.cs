using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Quartermaster
{
    internal static class HoverCard
    {

        private static UnitDefinition? _over;

        private const float Gap = 12f;

        internal static void Offer(UnitDefinition? definition)
        {
            if (definition == null) return;
            if (Event.current.type != EventType.Repaint) return;

            Rect row = GUILayoutUtility.GetLastRect();
            if (!row.Contains(Event.current.mousePosition)) return;

            _over = definition;
        }

        internal static void Begin()
        {
            if (Event.current.type == EventType.Repaint) _over = null;
        }

        internal static void Draw(Rect window)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (_over == null) return;

            string text = Text(_over);

            var content = new GUIContent(text);
            GUIStyle style = EditorSkin.CardStyle;

            float wide = Mathf.Min(EditorSkin.S(230f), window.width - EditorSkin.S(20f));
            float tall = style.CalcHeight(content, wide);

            Vector2 at = Event.current.mousePosition;

            float x = at.x - EditorSkin.S(8f);
            float y = at.y + Gap;

            if (x + wide > window.width - EditorSkin.S(6f)) x = at.x - wide + EditorSkin.S(8f);
            if (y + tall > window.height - EditorSkin.S(6f)) y = at.y - Gap - tall;

            x = Mathf.Clamp(x, EditorSkin.S(6f), Mathf.Max(EditorSkin.S(6f), window.width - wide - EditorSkin.S(6f)));
            y = Mathf.Clamp(y, EditorSkin.S(6f), Mathf.Max(EditorSkin.S(6f), window.height - tall - EditorSkin.S(6f)));

            GUI.Label(new Rect(x, y, wide, tall), content, style);
        }

        private static string Text(UnitDefinition definition)
        {
            var sb = new StringBuilder();

            sb.Append(definition.unitName ?? definition.jsonKey ?? "").Append('\n');
            sb.Append(definition.jsonKey ?? "").Append('\n');
            sb.Append('\n');

            List<ArmamentLine> lines = UnitArmament.Of(definition);

            if (lines.Count == 0)
            {
                sb.Append("No weapons listed.");
                return sb.ToString();
            }

            foreach (ArmamentLine line in lines)
            {
                sb.Append("  ").Append(line.Name);

                if (line.Stations > 1) sb.Append("  x").Append(line.Stations);

                if (line.AmmoEach > 0)
                {
                    sb.Append("  ").Append(line.AmmoEach);
                    sb.Append(line.AmmoVaries ? " rds max" : " rds");
                }

                sb.Append('\n');
            }

            float ammo = UnitArmament.AmmoValue(definition);

            sb.Append('\n');
            sb.Append("hull ").Append(UnitConverter.ValueReading(definition.value));

            if (ammo > 0f)
                sb.Append("   ammo ").Append(UnitConverter.ValueReading(ammo))
                  .Append("   loaded ").Append(UnitConverter.ValueReading(definition.value + ammo));

            return sb.ToString();
        }
    }
}
