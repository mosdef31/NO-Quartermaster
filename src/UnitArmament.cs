using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Quartermaster
{

    internal sealed class ArmamentLine
    {

        internal string Name = "";

        internal int Stations;

        internal int AmmoEach;

        internal bool AmmoVaries;
    }

    internal static class UnitArmament
    {

        private static readonly FieldInfo? TurretStations =
            AccessTools.Field(typeof(Turret), "weaponStations");

        private static readonly Dictionary<UnitDefinition, List<ArmamentLine>> Cache =
            new Dictionary<UnitDefinition, List<ArmamentLine>>();

        private static bool _warned;

        internal static List<ArmamentLine> Of(UnitDefinition? definition)
        {
            if (definition == null) return Empty;

            if (Cache.TryGetValue(definition, out List<ArmamentLine> cached)) return cached;

            List<ArmamentLine> lines = Read(definition);
            Cache[definition] = lines;
            return lines;
        }

        internal static float AmmoValue(UnitDefinition? definition)
        {
            if (definition == null) return 0f;

            float total = 0f;

            foreach (WeaponStation station in Stations(definition))
            {
                WeaponInfo? info = InfoOf(station);
                if (info == null) continue;

                total += FullAmmoOf(station) * info.costPerRound;
            }

            return total;
        }

        private static readonly List<ArmamentLine> Empty = new List<ArmamentLine>();

        private static List<ArmamentLine> Read(UnitDefinition definition)
        {
            var lines = new List<ArmamentLine>();
            var byName = new Dictionary<string, ArmamentLine>(StringComparer.Ordinal);

            foreach (WeaponStation station in Stations(definition))
            {
                WeaponInfo? info = InfoOf(station);
                if (info == null) continue;

                if (info.hideInDisplay) continue;

                string name = Name(info);
                if (name.Length == 0) continue;

                int ammo = FullAmmoOf(station);

                if (byName.TryGetValue(name, out ArmamentLine line))
                {
                    line.Stations++;

                    if (ammo != line.AmmoEach)
                    {
                        line.AmmoVaries = true;
                        line.AmmoEach = Mathf.Max(line.AmmoEach, ammo);
                    }

                    continue;
                }

                line = new ArmamentLine { Name = name, Stations = 1, AmmoEach = ammo };
                byName[name] = line;
                lines.Add(line);
            }

            return lines;
        }

        private static IEnumerable<WeaponStation> Stations(UnitDefinition definition)
        {
            var found = new List<WeaponStation>();

            if (TurretStations == null)
            {
                if (!_warned)
                {
                    _warned = true;
                    QuartermasterPlugin.Log.LogWarning(
                        "Turret has no 'weaponStations' field on this game version, so the "
                        + "catalogue cannot show what a vehicle is armed with. Everything else "
                        + "works as before.");
                }

                return found;
            }

            try
            {
                GameObject prefab = definition.unitPrefab;
                if (prefab == null) return found;

                foreach (Turret turret in prefab.GetComponentsInChildren<Turret>(true))
                {
                    if (turret == null) continue;
                    if (TurretStations.GetValue(turret) is not WeaponStation[] stations) continue;

                    foreach (WeaponStation station in stations)
                        if (station != null) found.Add(station);
                }
            }
            catch (Exception e)
            {
                QuartermasterPlugin.Diag(
                    $"The armament of {definition.unitName} could not be read: {e.Message}");
                found.Clear();
            }

            return found;
        }

        private static WeaponInfo? InfoOf(WeaponStation station)
        {
            if (station.WeaponInfo != null) return station.WeaponInfo;
            if (station.Weapons == null) return null;

            foreach (Weapon weapon in station.Weapons)
                if (weapon != null && weapon.info != null) return weapon.info;

            return null;
        }

        private static int FullAmmoOf(WeaponStation station)
        {
            if (station.FullAmmo > 0) return station.FullAmmo;

            int total = 0;
            if (station.Weapons == null) return 0;

            foreach (Weapon weapon in station.Weapons)
                if (weapon != null) total += Mathf.Max(0, weapon.ammo);

            return total;
        }

        private static string Name(WeaponInfo info)
        {
            if (!string.IsNullOrEmpty(info.weaponName)) return info.weaponName;
            if (!string.IsNullOrEmpty(info.shortName)) return info.shortName;
            return info.name ?? "";
        }
    }
}
