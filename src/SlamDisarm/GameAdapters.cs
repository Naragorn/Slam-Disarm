using SlamDisarm.Logic;
using ThunderRoad;

namespace SlamDisarm
{
    /// <summary>What one player hand holds, as plain facts plus the game objects behind them.</summary>
    internal struct GrabReading
    {
        public GrabInput Input;

        /// <summary>The NPC the grab belongs to (null when none).</summary>
        public Creature Npc;

        /// <summary>The NPC hand whose grip a slam loosens (null when none).</summary>
        public RagdollHand NpcHand;
    }

    /// <summary>
    /// Turns ThunderRoad objects into the plain values the logic works on. No decisions here; those live
    /// in <see cref="GrabRules"/> and <see cref="SlamRules"/>. Every member used is read in the decompiled
    /// ThunderRoad.dll 1.3.2 (see DEVELOPMENT.md, "API check").
    /// </summary>
    internal static class GameAdapters
    {
        public static GrabReading ReadGrab(RagdollHand playerHand)
        {
            var reading = new GrabReading { Input = BaseInput() };
            var handle = playerHand != null ? playerHand.grabbedHandle : null;
            if (handle == null)
            {
                return reading;
            }
            if (handle is HandleRagdoll ragdollHandle)
            {
                ReadBodyPart(ragdollHandle.ragdollPart, ref reading);
            }
            else if (handle.item != null)
            {
                ReadItem(handle.item, ref reading);
            }
            if (reading.NpcHand != null)
            {
                var held = HeldItem(reading.NpcHand);
                reading.Input.NpcHandHoldsItem = held != null;
                reading.Input.HeldItemIsShield = IsShield(held);
            }
            return reading;
        }

        /// <summary>Classifies what a contact of the slammed hand, forearm or weapon hit.</summary>
        public static SlamTarget Target(CollisionInstance hit)
        {
            var group = hit.targetColliderGroup;
            var handler = group != null ? group.collisionHandler : null;
            var isPart = handler != null && handler.isRagdollPart;
            var item = handler != null ? handler.item : null;
            return SlamRules.Classify(group != null, isPart, item != null, item != null && IsHeld(item));
        }

        /// <summary>For the debug log: which collider met which, as "source -> target" (the target "world" when it has no ColliderGroup).</summary>
        public static string Contact(CollisionInstance hit)
        {
            return ContactSide(hit.sourceCollider, hit.sourceColliderGroup) + " -> " + ContactSide(hit.targetCollider, hit.targetColliderGroup);
        }

        private static string ContactSide(UnityEngine.Collider collider, ColliderGroup group)
        {
            var name = collider != null ? collider.name : "?";
            var handler = group != null ? group.collisionHandler : null;
            if (handler == null)
            {
                return (group == null ? "world " : "") + "'" + name + "'";
            }
            if (handler.isRagdollPart)
            {
                return Label(handler.ragdollPart.ragdoll.creature) + " " + handler.ragdollPart.type + " '" + name + "'";
            }
            return handler.item != null ? "item " + Label(handler.item) + " '" + name + "'" : "'" + name + "'";
        }

        /// <summary>The item an NPC hand holds, or null.</summary>
        public static Item HeldItem(RagdollHand hand)
        {
            if (hand == null || hand.grabbedHandle == null)
            {
                return null;
            }
            return hand.grabbedHandle.item;
        }

        /// <summary>As the game's own hand-hit disarm tells a shield (BrainModuleEquipment.OnDamage).</summary>
        public static bool IsShield(Item item)
        {
            return item != null && item.data != null && item.data.moduleAI != null
                && item.data.moduleAI.primaryClass == ItemModuleAI.WeaponClass.Shield;
        }

        public static string Label(Creature creature)
        {
            if (creature == null)
            {
                return "none";
            }
            var id = string.IsNullOrEmpty(creature.creatureId) ? creature.name : creature.creatureId;
            return id + " #" + creature.GetInstanceID();
        }

        public static string Label(Item item)
        {
            if (item == null)
            {
                return "nothing";
            }
            return item.data != null ? item.data.id : item.name;
        }

        private static GrabInput BaseInput()
        {
            return new GrabInput
            {
                ModEnabled = ModOptions.Enabled,
                Thing = GrabbedThing.Nothing,
                ForearmGrabCounts = ModOptions.ForearmGrabCounts,
                WeaponGrabCounts = ModOptions.WeaponGrabCounts,
                ShieldsCanBeSlammed = ModOptions.ShieldsCanBeSlammed,
            };
        }

        private static void ReadBodyPart(RagdollPart part, ref GrabReading reading)
        {
            var creature = part != null && part.ragdoll != null ? part.ragdoll.creature : null;
            reading.Input.Thing = GrabbedThing.BodyPart;
            reading.Input.OwnerIsNpc = creature != null && !creature.isPlayer;
            reading.Input.OwnerAlive = creature != null && !creature.isKilled;
            if (creature == null)
            {
                return;
            }
            reading.Npc = creature;
            reading.NpcHand = HandOf(creature, part, out var kind);
            reading.Input.Part = kind;
        }

        /// <summary>The NPC hand a grabbed hand or forearm belongs to; a forearm is its hand's lowerArmPart.</summary>
        private static RagdollHand HandOf(Creature creature, RagdollPart part, out GrabbedPart kind)
        {
            kind = GrabbedPart.Other;
            if (part is RagdollHand hand)
            {
                kind = GrabbedPart.Hand;
                return hand;
            }
            var side = part.type == RagdollPart.Type.LeftArm ? Side.Left
                : part.type == RagdollPart.Type.RightArm ? Side.Right
                : (Side?)null;
            if (side == null)
            {
                return null;
            }
            var npcHand = creature.GetHand(side.Value);
            if (npcHand == null || npcHand.lowerArmPart != part)
            {
                return null;
            }
            kind = GrabbedPart.Forearm;
            return npcHand;
        }

        private static void ReadItem(Item item, ref GrabReading reading)
        {
            reading.Input.Thing = GrabbedThing.Item;
            var handlers = item.handlers;
            for (var i = 0; i < handlers.Count; i++)
            {
                var hand = handlers[i];
                var creature = hand != null ? hand.creature : null;
                if (creature == null || creature.isPlayer)
                {
                    continue;
                }
                reading.Npc = creature;
                reading.NpcHand = hand;
                reading.Input.OwnerIsNpc = true;
                reading.Input.OwnerAlive = !creature.isKilled;
                return;
            }
        }

        /// <summary>Held by a hand, by telekinesis, or sitting in a holder (a sheath on someone's body).</summary>
        private static bool IsHeld(Item item)
        {
            return item.handlers.Count > 0 || item.isTelekinesisGrabbed || item.holder != null;
        }
    }
}
