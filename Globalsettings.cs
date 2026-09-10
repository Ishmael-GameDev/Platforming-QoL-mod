using System;
using InControl;
using Modding.Converters;
using Newtonsoft.Json;

namespace PlatformingQoL
{
    public class PlatformingKeyBinds : PlayerActionSet
    {
        public PlayerAction ToggleHitboxes;

        public PlatformingKeyBinds()
        {
            ToggleHitboxes = CreatePlayerAction("ToggleHitboxes");
            ToggleHitboxes.AddDefaultBinding(Key.H);
        }
    }

    [Serializable]
    public class PlatformingSettings
    {
        public bool ModEnabled = true;
        public bool SkipperEnabled = true;
        public bool DebugModTimescaleFix = true;
        public float FreezeDuration = 0.5f;
        public float SpeedMultiplier = 3f;
        public bool HitboxSmoothingEnabled = false;
        public float HitboxSmoothingStrength = 15f;
        public int HitboxDisplayMode = 1;

        public bool FreezeMode = true;
        public bool HazardRespawn = false;
        public int SkipIndex = 0;
        public float SkipTime = 1.8f;
        public float RotateAngle = 0f;

        [JsonConverter(typeof(PlayerActionSetConverter))]
        public PlatformingKeyBinds KeyBinds = new PlatformingKeyBinds();
    }
}