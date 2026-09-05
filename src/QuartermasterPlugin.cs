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
        public const string PluginVersion = "1.4.0";

        private const string ListFileName = "quartermaster.json";

        public static ManualLogSource Log { get; private set; } = null!;

        private static ConfigEntry<bool>? _enabled;
        private static ConfigEntry<bool>? _diagnostics;
        private static ConfigEntry<bool>? _showEditor;
        private static ConfigEntry<KeyboardShortcut>? _toggleKey;
        private static ConfigEntry<float>? _uiScale;
        private static ConfigEntry<float>? _budgetCap;
        private static ConfigEntry<float>? _buyListHeight;

        internal static KeyboardShortcut ToggleKey =>
            _toggleKey != null
                ? _toggleKey.Value
                : new KeyboardShortcut(KeyCode.BackQuote, KeyCode.LeftAlt);

        internal static float UiScale => _uiScale != null ? _uiScale.Value : 0f;

        internal static float BudgetCap => _budgetCap != null ? _budgetCap.Value : 200f;

        internal static float BuyListHeight => _buyListHeight != null ? _buyListHeight.Value : 0f;

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
                "Turn the mod off without removing it. Off adds no convoy options and touches "
                + "no file.");

            _diagnostics = Config.Bind(
                "General", "Diagnostics", false,
                "Extra log lines about what was read from " + ListFileName + ". Turn it on if a "
                + "list is missing, then send the log. Problems are logged either way.");

            _showEditor = Config.Bind(
                "General", "ShowEditor", false,
                "Show the convoy editor: build lists out of the game's ground vehicles and write "
                + "them to " + ListFileName + ". Edit between missions - saving in one reorders "
                + "the buy menu, and online a purchase is sent as that order.");

            _showEditor.Value = false;

            _toggleKey = Config.Bind(
                "Interface", "ToggleKey",
                new KeyboardShortcut(KeyCode.BackQuote, KeyCode.LeftAlt),
                "Shows and hides the editor. Modifiers allowed. Same switch as ShowEditor and "
                + "the window's Close button.");

            _uiScale = Config.Bind(
                "Interface", "UiScale", 0f,
                new ConfigDescription(
                    "How large the editor is drawn. 0 works it out from your screen height. "
                    + "Otherwise 1 is the smallest readable size and 2 suits a 4K panel. No "
                    + "restart needed.",
                    new AcceptableValueRange<float>(0f, 3f)));

            _budgetCap = Config.Bind(
                "Interface", "BudgetWarningAbove", 200f,
                new ConfigDescription(
                    "Warn when a list costs more than this many millions. A warning, not a "
                    + "limit: an expensive list still saves and can still be bought. 0 turns "
                    + "the warning off.",
                    new AcceptableValueRange<float>(0f, 1000f)));

            _buyListHeight = Config.Bind(
                "Interface", "BuyListHeight", 0f,
                new ConfigDescription(
                    "How tall the convoy list in the buy menu may grow, in pixels. 0 measures "
                    + "the room the menu actually has, which is what the log reports.",
                    new AcceptableValueRange<float>(0f, 900f)));

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
