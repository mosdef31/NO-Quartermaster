using System.Collections.Generic;
using System.Globalization;
using System.Text;
using HarmonyLib;
using Mirage;
using NuclearOption.Chat;
using NuclearOption.Networking;

namespace Quartermaster
{
    internal static class ConvoySync
    {

        internal const string Prefix = "~~QM1|";

        internal static bool Blocked { get; private set; }

        internal static string Status { get; private set; } = "";

        internal static bool PlaceholderIcons => IsNetworked();

        private const char Unit = '\u001f';
        private const char Group = '\u001d';
        private const char Record = '\u001e';

        internal static string Fingerprint(List<ConvoyEntry> entries)
        {
            var sb = new StringBuilder();

            foreach (ConvoyEntry entry in entries)
            {

                if (!entry.Enabled) continue;

                sb.Append(entry.Name).Append(Unit)
                  .Append(entry.Cooldown.ToString("0.###", CultureInfo.InvariantCulture)).Append(Unit);

                foreach (string faction in entry.Factions)
                    sb.Append(faction).Append(Group);

                sb.Append(Unit);

                foreach (ConvoyUnitEntry unit in entry.Units)
                    sb.Append(unit.Id).Append('=')
                      .Append(unit.Count.ToString(CultureInfo.InvariantCulture)).Append(Group);

                sb.Append(Record);
            }

            return Hash(sb.ToString());
        }

        private static string Hash(string text)
        {
            uint hash = 2166136261u;

            foreach (char c in text)
            {
                hash ^= c;
                hash *= 16777619u;
            }

            return hash.ToString("x8", CultureInfo.InvariantCulture);
        }

        internal static void Announce()
        {
            if (!IsNetworked()) return;

            List<ConvoyEntry> entries = ConvoyInjector.Entries;
            string line = Prefix + Fingerprint(entries) + "|" + entries.Count;

            if (line.Length > 128)
            {

                QuartermasterPlugin.Log.LogError(
                    "The sync line does not fit in a chat frame, so it was not sent.");
                return;
            }

            ChatManager.SendChatMessage(line, allChat: true);
            QuartermasterPlugin.Diag($"Announced the convoy list as {line}.");
        }

        internal static void Receive(string body)
        {
            string[] parts = body.Split('|');
            if (parts.Length < 2) return;

            string theirs = parts[0];
            List<ConvoyEntry> entries = ConvoyInjector.Entries;
            string ours = Fingerprint(entries);

            if (theirs == ours)
            {
                Status = "Your convoy lists match the host's.";
                QuartermasterPlugin.Diag(Status);
                return;
            }

            if (Blocked) return;

            Blocked = true;
            Status =
                "Your quartermaster.json is not the host's, so your custom convoys are off for "
                + "this mission. Use the same file as the host to buy them.";

            QuartermasterPlugin.Log.LogWarning(
                $"The host's convoy list ({theirs}, {parts[1]} list(s)) is not the same as yours "
                + $"({ours}, {entries.Count} list(s)). Your custom options have been removed for "
                + "this mission so that a purchase cannot buy the wrong bundle - the index is what "
                + "crosses the network, and mismatched lists mean mismatched indices. Copy the "
                + "host's quartermaster.json to match.");

            ConvoyInjector.RemoveAll();
        }

        private static bool IsNetworked()
        {
            try
            {

                NetworkServer? server = NetworkSceneSingleton<ChatManager>.i != null
                    ? NetworkSceneSingleton<ChatManager>.i.Server
                    : null;

                if (server != null && server.Active)
                    return server.AuthenticatedPlayers != null
                        && server.AuthenticatedPlayers.Count > 1;

                return NetworkSceneSingleton<ChatManager>.i != null
                    && NetworkSceneSingleton<ChatManager>.i.Client != null
                    && NetworkSceneSingleton<ChatManager>.i.Client.Active;
            }
            catch
            {
                return false;
            }
        }
    }

    [HarmonyPatch(typeof(ChatManager), "UserCode_CmdSendChatMessage_-456754112")]
    internal static class QuartermasterChatServerPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(string message)
        {
            if (string.IsNullOrEmpty(message) || !message.StartsWith(ConvoySync.Prefix))
                return true;

            return false;
        }
    }

    [HarmonyPatch(typeof(ChatManager), "UserCode_TargetReceiveMessage_1307761090")]
    internal static class QuartermasterChatClientPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(string message)
        {
            if (string.IsNullOrEmpty(message) || !message.StartsWith(ConvoySync.Prefix))
                return true;

            try
            {
                ConvoySync.Receive(message.Substring(ConvoySync.Prefix.Length));
            }
            catch (System.Exception e)
            {
                QuartermasterPlugin.Log.LogError($"The convoy sync line could not be read: {e.Message}");
            }

            return false;
        }
    }
}
