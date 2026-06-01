/*
 * ArchDandara documentation
 * Purpose: Applies generated ammo and mana cost scaling to weapon components.
 * Why: AP slot options can change resource balance, so cost edits are centralized instead of scattered through patches.
 * Notes: Cost scaling should always derive from vanilla base values so reconnecting or reapplying settings does not multiply twice.
 */

using System.Collections.Generic;
using ArchDandara.Archipelago;
using UnityEngine;

namespace ArchDandara.Game
{
    public static class AmmoCostService
    {
        private static readonly Dictionary<int, float> SecondaryShotBaseCosts = new Dictionary<int, float>();
        private static readonly Dictionary<int, float> ActivateObjectBaseCosts = new Dictionary<int, float>();
        private static float NextUpdateTime;

        public static void ResetCache()
        {
            // Keep the original cost cache. Slot data can be applied again while the
            // same weapon style objects are already scaled, and clearing here would
            // make the scaled value become the new "normal" cost.
            NextUpdateTime = 0.0f;
        }

        public static void Update()
        {
            if (Time.time < NextUpdateTime)
                return;

            NextUpdateTime = Time.time + 1.0f;
            ApplyToSecondaryShotStyles();
            ApplyToActivateObjectSecondaryStyles();
        }

        private static void ApplyToSecondaryShotStyles()
        {
            SecondaryShotStyle[] styles = Resources.FindObjectsOfTypeAll<SecondaryShotStyle>();
            for (int i = 0; i < styles.Length; i++)
            {
                SecondaryShotStyle style = styles[i];
                if (object.ReferenceEquals(style, null))
                    continue;

                int key = GetKey(style);
                float baseCost;
                if (!SecondaryShotBaseCosts.TryGetValue(key, out baseCost))
                {
                    baseCost = style.ammoCost;
                    SecondaryShotBaseCosts[key] = baseCost;
                }

                style.ammoCost = baseCost * APSlotSettings.AmmoCostMultiplier;
            }
        }

        private static void ApplyToActivateObjectSecondaryStyles()
        {
            ActivateObjectSecondaryStyle[] styles = Resources.FindObjectsOfTypeAll<ActivateObjectSecondaryStyle>();
            for (int i = 0; i < styles.Length; i++)
            {
                ActivateObjectSecondaryStyle style = styles[i];
                if (object.ReferenceEquals(style, null))
                    continue;

                int key = GetKey(style);
                float baseCost;
                if (!ActivateObjectBaseCosts.TryGetValue(key, out baseCost))
                {
                    baseCost = style.cost;
                    ActivateObjectBaseCosts[key] = baseCost;
                }

                style.cost = baseCost * APSlotSettings.AmmoCostMultiplier;
            }
        }

        private static int GetKey(Object obj)
        {
            return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
