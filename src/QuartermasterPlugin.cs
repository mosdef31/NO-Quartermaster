using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Quartermaster
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("NuclearOption.exe")]
    public sealed class QuartermasterPlugin : BaseUnityPlugin
    {
        public const string PluginGuid    = "com.quartermaster";
        public const string PluginName    = "Quartermaster";
        public const string PluginVersion = "1.2.1";

        private const string ListFileName = "quartermaster.json";

        public static ManualLogSource Log { get; private set; } = null!;

        private static ConfigEntry<bool>? _enabled;
        private static ConfigEntry<bool>? _diagnostics;
        private static ConfigEntry<bool>? _showEditor;

        internal static bool EditorVisible
        {
            get => _showEditor != null && _showEditor.Value;
            set { if (_showEditor != null) _showEditor.Value = value; }
        }

        internal static void Diag(string message)
        {
            if (_diagnostics != null && _diagnostics.Value)
                Log.LogInfo(message);
        }

        private void Awake()
        {
            Log = Logger;

            _enabled = Config.Bind(
                "General", "Enabled", true,
                "Turn the mod off without removing it. Off means no options are added to the "
                + "convoy menu and no file is read or written.");

            _diagnostics = Config.Bind(
                "General", "Diagnostics", false,
                "Write extra lines to the BepInEx log describing what was read from "
                + ListFileName + ", what each list parsed to and what each icon loaded as. Off by "
                + "default. Turn it on if a list of yours is not appearing, then send the log. "
                + "Problems are logged either way; this only adds the commentary around them.");

            _showEditor = Config.Bind(
                "General", "ShowEditor", false,
                "Show the in-game convoy editor. Tick this to open a window that lists every "
                + "ground vehicle, lets you build a convoy out of them and writes the result to "
                + ListFileName + ". Untick it, or press Close in the window, to hide it again. "
                + "Editing during a mission changes the order of the buy menu, which is what a "
                + "purchase is sent as in multiplayer, so edit between missions.");

            if (!_enabled.Value)
            {
                Logger.LogInfo($"{PluginName} {PluginVersion} is turned off in its settings.");
                return;
            }

            string here = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            IconLoader.ModFolder = here;
            ConvoyInjector.Load(Path.Combine(here, ListFileName));

            var harmony = new Harmony(PluginGuid);

            try
            {
                harmony.PatchAll(typeof(QuartermasterPlugin).Assembly);
            }
            catch (Exception e)
            {
                Log.LogError(
                    "The multiplayer convoy-list check could not be installed: " + e.Message
                    + ". The custom options still work, but a client whose quartermaster.json "
                    + "differs from the host's will not be warned.");
            }

            MethodInfo? afterLoad = AccessTools.Method(typeof(Encyclopedia), "AfterLoad", Type.EmptyTypes);
            if (afterLoad == null)
            {
                Log.LogError("This game version has no Encyclopedia.AfterLoad; nothing was added.");
                return;
            }

            harmony.Patch(afterLoad,
                postfix: new HarmonyMethod(typeof(QuartermasterPlugin), nameof(AfterEncyclopediaLoad)));

            harmony.Patch(AccessTools.Method(typeof(ContributeToFaction), nameof(ContributeToFaction.RefreshVehicleList)),
                prefix: new HarmonyMethod(typeof(QuartermasterPlugin), nameof(BeforeVehicleList)),
                postfix: new HarmonyMethod(typeof(QuartermasterPlugin), nameof(AfterVehicleList)));

            var host = new GameObject("Quartermaster_Editor");
            DontDestroyOnLoad(host);
            host.hideFlags = HideFlags.HideAndDontSave;
            host.AddComponent<ConvoyEditor>();

            Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
        }

        private static void AfterEncyclopediaLoad()
        {
            Safely();

            try
            {
                ConvoySync.Announce();
            }
            catch (Exception e)
            {
                Log.LogError($"The convoy list could not be announced: {e.Message}");
            }
        }

        private static void BeforeVehicleList(ContributeToFaction __instance)
        {
            Safely();

            try
            {
                ConvoyListPanel.ClearOldButtons(__instance);
            }
            catch (Exception e)
            {
                Log.LogError($"The old convoy buttons could not be cleared: {e.Message}");
            }
        }

        private static void AfterVehicleList(ContributeToFaction __instance)
        {
            try
            {
                ConvoyListPanel.MakeScrollable(__instance);
            }
            catch (Exception e)
            {
                Log.LogError($"The convoy list could not be made scrollable: {e.Message}");
            }
        }

        private static void Safely()
        {
            try
            {
                ConvoyInjector.EnsureAllFactions();
            }
            catch (Exception e)
            {
                Log.LogError($"The custom options could not be added: {e.Message}");
            }
        }
    }
}
