using GlobalEnums;
using Hollow_Knight_Platforming_Mod.Hitbox;
using Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hollow_Knight_Platforming_Mod
{
    public class Hollow_Knight_Platforming_Mod : Mod, IMenuMod
    {
        // Параметры, настраиваемые через меню
        private float freezeDuration = 0.5f;       // 0..1 с, шаг 0.1
        private float speedMultiplier = 3f;        // 1..5, шаг 0.5
        public static bool showHitboxes = true;

        public bool ToggleButtonInsideMenu => false; // или true, если нужна галочка в списке модов
        private bool isCoroutineRunning = false;

        public Hollow_Knight_Platforming_Mod() : base("Platforming QoL") { }

        public override string GetVersion() => "1.0.0.0";
        private HitboxViewer hitboxViewer;
        public override void Initialize()
        {
            On.HeroController.TakeDamage += OnTakeDamage;
            hitboxViewer = new HitboxViewer();
            // hitboxViewer.Load();
            // hitboxViewer.Unload();
            Log("Platforming mod initialized.");
        }

        // Меню настроек (IMenuMod)
        public List<IMenuMod.MenuEntry> GetMenuData(IMenuMod.MenuEntry? toggleButtonEntry)
        {
            string[] freezeOptions = CreateOptions(0f, 1f, 0.1f);
            string[] speedOptions = CreateOptions(1f, 5f, 0.5f);

            return new List<IMenuMod.MenuEntry>
            {
                new IMenuMod.MenuEntry
                {
                    Name = "Freeze Duration (seconds)",
                    Description = "Timescale = 0 duration after taking damage",
                    Values = freezeOptions,
                    Saver = opt => freezeDuration = float.Parse(freezeOptions[opt]),
                    Loader = () => FindClosestIndex(freezeOptions, freezeDuration)
                },

                new IMenuMod.MenuEntry
                {
                    Name = "Speed Multiplier (x)",
                    Description = "Time scale multiplier after freeze",
                    Values = speedOptions,
                    Saver = opt => speedMultiplier = float.Parse(speedOptions[opt]),
                    Loader = () => FindClosestIndex(speedOptions, speedMultiplier)
                },

                new IMenuMod.MenuEntry
                {
                    Name = "Show Hitboxes",
                    Description = "Display hitboxes during slowmo",
                    Values = new[] { "Off", "On" },
                    Saver = opt => showHitboxes = opt == 1,
                    Loader = () => showHitboxes ? 1 : 0
                }
            };
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

        private int FindClosestIndex(string[] options, float current)
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

        // Обработчик урона героя — запускает корутину с эффектом времени
        private void OnTakeDamage(
            On.HeroController.orig_TakeDamage orig,
            HeroController self,
            GameObject go,
            CollisionSide damageSide,
            int damageAmount,
            int hazardType)
        {
            // Вызываем оригинальный метод
            orig(self, go, damageSide, damageAmount, hazardType);

            // Запускаем корутину
            if (!isCoroutineRunning)
            {
                self.StartCoroutine(TimeEffectRoutine());
            }
        }

        // Корутина времени с фризом и ускорением
        private IEnumerator TimeEffectRoutine()
        {
            isCoroutineRunning = true;

            // Ждем 0.13 секунд, чтобы не фризить в момент урона
            yield return new WaitForSecondsRealtime(0.12f);
            if(showHitboxes)
            {
                hitboxViewer.Load();
            }
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(freezeDuration);

            Time.timeScale = speedMultiplier;

            float realDuration = 1.8f / speedMultiplier;
            yield return new WaitForSecondsRealtime(realDuration);
            if (showHitboxes)
            {
                hitboxViewer.Unload();
            }
            Time.timeScale = 1f;

            isCoroutineRunning = false;
        }
    }
}
