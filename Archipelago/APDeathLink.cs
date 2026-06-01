/*
 * ArchDandara documentation
 * Purpose: Handles DeathLink tag state, incoming deaths, and outgoing death messages.
 * Why: DeathLink is optional per slot and should stay isolated from normal item and location traffic.
 * Notes: Incoming deaths and outgoing causes both pass through this class so DeathLink tags can be enabled or removed without touching normal AP session code.
 */

using System;
using ArchDandara.Config;
using ArchDandara.Game;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.MessageLog.Messages;

namespace ArchDandara.Archipelago
{
    public static class APDeathLink
    {
        private static DeathLinkService Service;
        private static bool Enabled;
        private static bool ReceivingDeathLink;
        private static float LastSentTime;
        private const float SendDebounceSeconds = 2.0f;

        public static bool IsReceivingDeathLink
        {
            get { return ReceivingDeathLink; }
        }

        public static void Initialize(ArchipelagoSession session, bool enabled)
        {
            Service = null;
            Enabled = false;
            if (object.ReferenceEquals(session, null))
                return;

            try
            {
                Service = DeathLinkProvider.CreateDeathLinkService(session);
                Service.OnDeathLinkReceived -= OnDeathLinkReceived;
                Service.OnDeathLinkReceived += OnDeathLinkReceived;
                SetEnabled(enabled, "slot data");
            }
            catch (Exception ex)
            {
                MLLog.Warning("[DeathLink] Failed to initialize: " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        public static void Disconnect()
        {
            if (!object.ReferenceEquals(Service, null))
                Service.OnDeathLinkReceived -= OnDeathLinkReceived;

            Service = null;
            Enabled = false;
            ReceivingDeathLink = false;
        }

        public static void OnMessage(LogMessage message)
        {
            TagsChangedLogMessage tagsChanged = message as TagsChangedLogMessage;
            if (tagsChanged == null)
                return;

            if (!IsOurPlayer(tagsChanged))
                return;

            bool deathLinkTag = HasTag(tagsChanged.Tags, "DeathLink");
            if (deathLinkTag != Enabled)
                SetEnabled(deathLinkTag, "TextClient tag change");
        }

        public static void SendDeath(string cause)
        {
            if (!Enabled || object.ReferenceEquals(Service, null))
                return;

            if (ReceivingDeathLink)
                return;

            float now = UnityEngine.Time.time;
            if (now - LastSentTime < SendDebounceSeconds)
                return;

            LastSentTime = now;
            if (string.IsNullOrEmpty(cause))
                cause = APConfig.SlotName + " died in Dandara.";

            try
            {
                Service.SendDeathLink(new DeathLink(APConfig.SlotName, cause));
                APClient.SendChat("[DeathLink] " + cause);
                MLLog.Msg("[DeathLink] Sent: " + cause);
            }
            catch (Exception ex)
            {
                MLLog.Warning("[DeathLink] Failed to send: " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static void OnDeathLinkReceived(DeathLink deathLink)
        {
            if (!Enabled || object.ReferenceEquals(deathLink, null))
                return;

            if (deathLink.Source == APConfig.SlotName)
                return;

            PlayerController player = GameAccess.Player;
            if (object.ReferenceEquals(player, null))
                return;

            ReceivingDeathLink = true;
            try
            {
                MLLog.Msg("[DeathLink] Received from " + deathLink.Source + ": " + deathLink.Cause);
                player.ForcePlayerDeath();
            }
            finally
            {
                ReceivingDeathLink = false;
            }
        }

        private static void SetEnabled(bool enabled, string source)
        {
            if (object.ReferenceEquals(Service, null))
                return;

            try
            {
                if (enabled)
                    Service.EnableDeathLink();
                else
                    Service.DisableDeathLink();

                Enabled = enabled;
                MLLog.Msg("[DeathLink] " + (enabled ? "Enabled" : "Disabled") + " from " + source + ".");
            }
            catch (Exception ex)
            {
                MLLog.Warning("[DeathLink] Failed to set enabled=" + enabled + ": " +
                                    ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static bool HasTag(string[] tags, string tag)
        {
            if (tags == null || string.IsNullOrEmpty(tag))
                return false;

            for (int i = 0; i < tags.Length; i++)
            {
                if (tags[i] == tag)
                    return true;
            }

            return false;
        }

        private static bool IsOurPlayer(PlayerSpecificLogMessage message)
        {
            if (object.ReferenceEquals(message, null) || object.ReferenceEquals(message.Player, null))
                return false;

            if (message.IsActivePlayer)
                return true;

            string alias = message.Player.Alias;
            string name = message.Player.Name;
            return alias == APConfig.SlotName || name == APConfig.SlotName;
        }
    }
}
