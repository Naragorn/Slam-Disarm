using System;
using System.Collections.Generic;
using ThunderRoad;

namespace SlamDisarm
{
    /// <summary>
    /// One player hand's watch over an NPC's grip: while the grab counts, it listens to the collision starts of
    /// the NPC's hand, forearm and held weapon (CollisionHandler.OnCollisionStartEvent) and hands each contact on.
    /// The handler delegate and the handler list are made once, so a grab costs no allocation per frame.
    /// </summary>
    internal sealed class SlamWatch
    {
        private readonly List<CollisionHandler> subscribed = new List<CollisionHandler>();
        private readonly CollisionHandler.CollisionEvent onContact;
        private readonly Action<SlamWatch, CollisionInstance> contact;

        public SlamWatch(Side side, Action<SlamWatch, CollisionInstance> contact)
        {
            Side = side;
            this.contact = contact;
            onContact = OnContact;
        }

        public Side Side { get; }

        /// <summary>The player's hand that holds the NPC's hand, forearm or weapon.</summary>
        public RagdollHand PlayerHand { get; private set; }

        public Creature Npc { get; private set; }

        public RagdollHand NpcHand { get; private set; }

        /// <summary>The weapon the NPC hand held when the watch started.</summary>
        public Item Weapon { get; private set; }

        public bool Active => NpcHand != null;

        public bool Watches(RagdollHand npcHand, Item weapon)
        {
            return Active && NpcHand == npcHand && Weapon == weapon;
        }

        public void Start(RagdollHand playerHand, Creature npc, RagdollHand npcHand, Item weapon)
        {
            Stop();
            PlayerHand = playerHand;
            Npc = npc;
            NpcHand = npcHand;
            Weapon = weapon;
            Subscribe(npcHand.collisionHandler);
            if (npcHand.lowerArmPart != null)
            {
                Subscribe(npcHand.lowerArmPart.collisionHandler);
            }
            if (weapon != null)
            {
                var handlers = weapon.collisionHandlers;
                for (var i = 0; i < handlers.Count; i++)
                {
                    Subscribe(handlers[i]);
                }
            }
            if (ModOptions.DebugLogging)
            {
                ModLog.Info("GRAB | " + GameAdapters.Label(npc) + " | watching " + npcHand.side + " hand with '" + GameAdapters.Label(weapon)
                    + "' (" + subscribed.Count + " bodies), your " + Side + " hand");
            }
        }

        public void Stop()
        {
            for (var i = 0; i < subscribed.Count; i++)
            {
                if (subscribed[i] != null)
                {
                    subscribed[i].OnCollisionStartEvent -= onContact;
                }
            }
            if (Active)
            {
                ModLog.Verbose("GRAB | " + GameAdapters.Label(Npc) + " | stopped watching");
            }
            subscribed.Clear();
            PlayerHand = null;
            Npc = null;
            NpcHand = null;
            Weapon = null;
        }

        private void Subscribe(CollisionHandler handler)
        {
            if (handler == null || subscribed.Contains(handler))
            {
                return;
            }
            handler.OnCollisionStartEvent += onContact;
            subscribed.Add(handler);
        }

        private void OnContact(CollisionInstance hit)
        {
            if (!Active || hit == null)
            {
                return;
            }
            try
            {
                contact(this, hit);
            }
            catch (Exception ex)
            {
                ModLog.Error("Slam handling failed: " + ex);
            }
        }
    }
}
