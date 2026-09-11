using System;
using System.Collections.Generic;
using Modding.Menu;
using Satchel.BetterMenus;

namespace PlatformingQoL
{
    internal static class ConfigScreen
    {
        private static Menu menuRef;

        public static MenuScreen GetMenuScreen(MenuScreen modListMenu)
        {
            string[] freezeOptions = CreateOptions(0f, 1f, 0.1f);
            string[] speedOptions = CreateOptions(1f, 5f, 0.5f);
            string[] rotateOptions = CreateOptions(0f, 270f, 90f);

            var elements = new List<Element>
            {
                new TextPanel("General Settings"),

                new HorizontalOption(
                    name: "Mod Enabled",
                    description: "Completely enable or disable the entire mod",
                    values: new[] { "Off", "On" },
                    applySetting: index => PlatformingQoL.modEnabled = index == 1,
                    loadSetting: () => PlatformingQoL.modEnabled ? 1 : 0),

                new HorizontalOption(
                    name: "Smooth Hitboxes",
                    description: "Smoother hitboxes (decreases FPS)",
                    values: new[] { "Off", "On" },
                    applySetting: index => PlatformingQoL.hitboxSmoothingEnabled = index == 1,
                    loadSetting: () => PlatformingQoL.hitboxSmoothingEnabled ? 1 : 0),

                new HorizontalOption(
                    name: "DebugMod Timescale Fix",
                    description: "Restore DebugMod's timescale after closing the pause menu",
                    values: new[] { "Off", "On" },
                    applySetting: index => PlatformingQoL.debugModTimescaleFix = index == 1,
                    loadSetting: () => PlatformingQoL.debugModTimescaleFix ? 1 : 0),

                new KeyBind(
                    name: "Show Hitboxes Key",
                    playerAction: PlatformingQoL.GS.KeyBinds.ToggleHitboxes),

                new TextPanel("Skipper & Freeze Settings"),

                new HorizontalOption(
                    name: "Skipper Enabled",
                    description: "Enable hitboxes, freeze and speedup on hit",
                    values: new[] { "Off", "On" },
                    applySetting: index => PlatformingQoL.skipperEnabled = index == 1,
                    loadSetting: () => PlatformingQoL.skipperEnabled ? 1 : 0),

                new HorizontalOption(
                    name: "Freeze Duration (seconds)",
                    description: "Timescale = 0 duration after taking damage",
                    values: freezeOptions,
                    applySetting: index => PlatformingQoL.freezeDuration = float.Parse(freezeOptions[index]),
                    loadSetting: () => FindClosestIndex(freezeOptions, PlatformingQoL.freezeDuration)),

                new HorizontalOption(
                    name: "Skip Time Amount",
                    description: "Depends on the purpose",
                    values: new[] { "Respawn", "Death" },
                    applySetting: index =>
                    {
                        PlatformingQoL.skipIndex = index;
                        PlatformingQoL.skipTime = index == 0 ? 1.8f : 5f;
                    },
                    loadSetting: () => PlatformingQoL.skipIndex),

                new HorizontalOption(
                    name: "Speed Multiplier (x)",
                    description: "Time scale multiplier after freeze (multiplies current game speed)",
                    values: speedOptions,
                    applySetting: index => PlatformingQoL.speedMultiplier = float.Parse(speedOptions[index]),
                    loadSetting: () => FindClosestIndex(speedOptions, PlatformingQoL.speedMultiplier)),

                new HorizontalOption(
                    name: "Show Freezed Hitboxes",
                    description: "Skipper Hitboxes: Off / Main / All",
                    values: new[] { "Off", "Main Layers", "All" },
                    applySetting: index => PlatformingQoL.hitboxDisplayMode = index,
                    loadSetting: () => PlatformingQoL.hitboxDisplayMode),

                new TextPanel("Camera Settings"),

                new HorizontalOption(
                    name: "Camera Rotation",
                    description: "Degree Angle",
                    values: rotateOptions,
                    applySetting: index => PlatformingQoL.rotateAngle = float.Parse(rotateOptions[index]),
                    loadSetting: () => FindClosestIndex(rotateOptions, PlatformingQoL.rotateAngle)),
            };

            menuRef ??= new Menu(
                name: "Platforming QoL",
                elements: elements.ToArray()
            );

            return menuRef.GetMenuScreen(modListMenu);
        }

        private static string[] CreateOptions(float min, float max, float step)
        {
            List<string> options = new();
            for (float v = min; v <= max + step / 2f; v += step)
            {
                options.Add(v.ToString("0.0"));
            }
            return options.ToArray();
        }

        private static int FindClosestIndex(string[] options, float current)
        {
            int bestIndex = 0;
            float bestDiff = float.MaxValue;
            for (int i = 0; i < options.Length; i++)
            {
                if (float.TryParse(options[i], out float val))
                {
                    float diff = Math.Abs(val - current);
                    if (diff < bestDiff)
                    {
                        bestDiff = diff;
                        bestIndex = i;
                    }
                }
            }
            return bestIndex;
        }
    }
}
