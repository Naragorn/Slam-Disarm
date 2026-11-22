using System;
using ThunderRoad;

namespace SlamDisarm
{
    /// <summary>
    /// Entry point. The game's ModManager creates one instance of every ThunderScript subclass in a mod DLL and
    /// calls ScriptLoaded, then Enable (ScriptEnable); ScriptUpdate runs every frame (ThunderRoad 1.3.2).
    /// </summary>
    public sealed class SlamDisarmScript : ThunderScript
    {
        private readonly SlamController controller = new SlamController();
        private bool wasEnabled = true;

        public override void ScriptLoaded(ModManager.ModData modData)
        {
            base.ScriptLoaded(modData);
            ModLog.Info("Loaded " + modData.Name + " " + modData.ModVersion + " (folder " + modData.folderName + ")");
        }

        public override void ScriptEnable()
        {
            EventManager.onCreatureDespawn += OnCreatureDespawn;
            EventManager.onCreatureKill += OnCreatureKill;
            EventManager.onLevelUnload += OnLevelUnload;
            ModLog.Info("Enabled, watching the player's grabs");
        }

        public override void ScriptDisable()
        {
            Unhook();
            ModLog.Info("Disabled");
        }

        public override void ScriptUnload()
        {
            Unhook();
        }

        public override void ScriptUpdate()
        {
            try
            {
                if (wasEnabled && !ModOptions.Enabled)
                {
                    controller.Clear();
                    ModLog.Info("Turned off in the Mod Menu: slams ignored, disarmed enemies may re-arm");
                }
                wasEnabled = ModOptions.Enabled;
                controller.Update(UnityEngine.Time.time);
            }
            catch (Exception ex)
            {
                ModLog.Error("Update failed: " + ex);
                controller.Clear();
            }
        }

        private void Unhook()
        {
            EventManager.onCreatureDespawn -= OnCreatureDespawn;
            EventManager.onCreatureKill -= OnCreatureKill;
            EventManager.onLevelUnload -= OnLevelUnload;
            controller.Clear();
        }

        // The game pools creatures and reuses a dead one's object for a new spawn, so its state is dropped here.
        private void OnCreatureDespawn(Creature creature, EventTime eventTime)
        {
            if (eventTime == EventTime.OnStart)
            {
                controller.Forget(creature);
            }
        }

        private void OnCreatureKill(Creature creature, Player player, CollisionInstance hit, EventTime eventTime)
        {
            if (eventTime == EventTime.OnEnd)
            {
                controller.Forget(creature);
            }
        }

        private void OnLevelUnload(LevelData levelData, LevelData.Mode mode, EventTime eventTime)
        {
            if (eventTime == EventTime.OnStart)
            {
                controller.Clear();
            }
        }
    }
}
