using System.Collections.Generic;

namespace Quartermaster
{

    internal sealed class ConvoyFile
    {
        internal List<ConvoyEntry> Convoys = new List<ConvoyEntry>();
    }

    internal sealed class ConvoyEntry
    {

        internal string Name = "";

        internal float Cooldown = 60f;

        internal List<string> Factions = new List<string>();

        internal string Icon = "";

        internal string Section = "";

        internal List<ConvoyUnitEntry> Units = new List<ConvoyUnitEntry>();

        internal int Line;
    }

    internal sealed class ConvoyUnitEntry
    {

        internal string Id = "";

        internal int Count = 1;

        internal int Line;
    }
}
