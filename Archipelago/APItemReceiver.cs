/*
 * ArchDandara documentation
 * Purpose: Queues and applies received AP items when the game is ready for grants.
 * Why: AP packets can arrive while menus or managers are not initialized, so delayed grants prevent lost items.
 * Notes: Items stay queued until GameAccess reports that save, player, powerup manager, and story manager are all ready.
 */

using System.Collections.Generic;
using System.Threading;
using ArchDandara.Game;

namespace ArchDandara.Archipelago
{
    public static class APItemReceiver
    {
        private static readonly Queue<ReceivedItem> PendingItems = new Queue<ReceivedItem>();
        private static readonly object QueueLock = new object();
        private const int MaxItemsPerFrame = 5;
        private const float RetryDelaySeconds = 2.0f;
        private static float NextProcessTime;

        public static void Enqueue(int index, string itemName)
        {
            EnqueueInternal(index, itemName, false);
        }

        public static void EnqueueReplay(int index, string itemName)
        {
            EnqueueInternal(index, itemName, true);
        }

        private static void EnqueueInternal(int index, string itemName, bool replayOnly)
        {
            if (string.IsNullOrEmpty(itemName))
                return;

            Monitor.Enter(QueueLock);
            try
            {
                PendingItems.Enqueue(new ReceivedItem(index, itemName, replayOnly));
            }
            finally
            {
                Monitor.Exit(QueueLock);
            }
        }

        public static void Clear()
        {
            Monitor.Enter(QueueLock);
            try
            {
                PendingItems.Clear();
            }
            finally
            {
                Monitor.Exit(QueueLock);
            }
        }

        public static void ProcessQueue()
        {
            if (!GameAccess.IsReadyForApItemGrants)
                return;

            if (UnityEngine.Time.time < NextProcessTime)
                return;

            int processedThisFrame = 0;
            while (processedThisFrame < MaxItemsPerFrame)
            {
                ReceivedItem item;

                Monitor.Enter(QueueLock);
                try
                {
                    if (PendingItems.Count == 0)
                        return;

                    item = PendingItems.Dequeue();
                }
                finally
                {
                    Monitor.Exit(QueueLock);
                }

                try
                {
                    if (!ItemGrantService.Grant(item.Name))
                    {
                        EnqueueInternal(item.Index, item.Name, item.ReplayOnly);
                        NextProcessTime = UnityEngine.Time.time + RetryDelaySeconds;
                        return;
                    }

                    if (item.ReplayOnly)
                    {
                        SaveSync.MarkReceivedItemReplayed(item.Index);
                        if (ItemIds.IsMoneyItem(item.Name))
                            ShopSaltBalanceService.ScheduleApply();
                    }
                    else
                    {
                        SaveSync.MarkReceivedItemProcessed(item.Index, item.Name);
                    }

                    SaveSync.Save();
                    processedThisFrame++;
                }
                catch (System.Exception ex)
                {
                    MLLog.Error("[APItemReceiver] Failed to grant " + item.Name + ": " + ex);
                    EnqueueInternal(item.Index, item.Name, item.ReplayOnly);
                    NextProcessTime = UnityEngine.Time.time + RetryDelaySeconds;
                    return;
                }
            }
        }

        private class ReceivedItem
        {
            public readonly int Index;
            public readonly string Name;
            public readonly bool ReplayOnly;

            public ReceivedItem(int index, string name, bool replayOnly)
            {
                Index = index;
                Name = name;
                ReplayOnly = replayOnly;
            }
        }
    }
}
