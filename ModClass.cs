using GlobalEnums;
using Hollow_Knight_Platforming_Mod.Hitbox;
using Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using UnityEngine;
using static Hollow_Knight_Platforming_Mod.Hollow_Knight_Platforming_Mod;

namespace Hollow_Knight_Platforming_Mod
{
    [System.Serializable]
    public class PlatformingSettings
    {
        public float FreezeDuration = 0.5f;
        public float SpeedMultiplier = 3f;
        public bool ShowHitboxes = true;
        public bool FreezeMode = true;
        public bool HazardRespawn = false;
        public int SkipIndex = 0;
        public float SkipTime = 1.8f;
        public float RotateAngle = 0f;
    }
    public class Hollow_Knight_Platforming_Mod : Mod, IMenuMod, IGlobalSettings<PlatformingSettings>
    {
        private bool forceFreezeActive = false;
        private float originalGenericTimeScale = 1f;
        private float freezeDuration = 0.5f;       // 0..2 с, шаг 0.1
        private float speedMultiplier = 3f;        // 1..5, шаг 0.5
        public static bool showHitboxes = true;
        public static bool freezeMode = true;
        public static bool hazardRespawn = false;
        private float skipTime=1.8f;
        private int skipIndex = 0;
        private float rotateAngle = 0f;
        private float appliedAngle = 0f;
        private Transform camTransform;
        private float currentAngle = 0f;
        private bool cameraReady = false;
        public PlatformingSettings GS = new();
        private float timer = 0f;
        private float interval = 1f; // 1 секунда
        // Загружаем настройки и переписываем переменные мода
        public void OnLoadGlobal(PlatformingSettings gs)
        {
            if (gs == null) return;

            GS = gs;

            showHitboxes = GS.ShowHitboxes;
            freezeMode = GS.FreezeMode;
            hazardRespawn = GS.HazardRespawn;
            freezeDuration = GS.FreezeDuration;
            speedMultiplier = GS.SpeedMultiplier;
            skipIndex = GS.SkipIndex;
            skipTime = GS.SkipTime;
            rotateAngle = GS.RotateAngle;
        }

        // Сохраняем текущее состояние переменных мода в объект настроек
        public PlatformingSettings OnSaveGlobal()
        {
            GS.ShowHitboxes = showHitboxes;
            GS.FreezeMode = freezeMode;
            GS.HazardRespawn = hazardRespawn;
            GS.FreezeDuration = freezeDuration;
            GS.SpeedMultiplier = speedMultiplier;
            GS.SkipIndex = skipIndex;
            GS.SkipTime = skipTime;
            GS.RotateAngle = rotateAngle;

            return GS;
        }
        private Collider2D heroCollider;
        private Collider2D heroBox;
        public void OnDisable()
        {
            On.GameManager.SetTimeScale_float -= GameManager_SetTimeScale;

            TimeController.GenericTimeScale = 1f;
        }

        private void GameManager_SetTimeScale(
            On.GameManager.orig_SetTimeScale_float orig,
            GameManager self,
            float newTimeScale)
        {
            if (forceFreezeActive)
            {
                // Полный фриз, что бы ни делала игра
                TimeController.GenericTimeScale = 0f;
                return;
            }

            // обычное поведение
            orig(self, newTimeScale);
        }
        private static void TriggerRespawn()
        {
            // Базовые проверки — ровно как в DebugMod
            if (GameManager.instance == null ||
                HeroController.instance == null ||
                !GameManager.instance.IsGameplayScene() ||
                HeroController.instance.cState.dead ||
                PlayerData.instance.health <= 0)
                return;

            // Если игра на паузе — корректно закрываем её
            if (UIManager.instance.uiState.ToString() == "PAUSED")
            {
                InputHandler.Instance.StartCoroutine(GameManager.instance.PauseGameToggle());
                GameManager.instance.HazardRespawn();
                return;
            }

            // Нормальный игровой режим
            if (UIManager.instance.uiState.ToString() == "PLAYING")
            {
                HeroController.instance.RelinquishControl();
                GameManager.instance.HazardRespawn();
                HeroController.instance.RegainControl();
                return;
            }

            // Иные состояния UI — ничего не делаем
        }

        private void CacheHeroColliders()
        {
            var hc = HeroController.instance;
            if (hc == null) return;

            heroCollider = hc.GetComponent<Collider2D>();

            heroBox = hc.transform
                .Find("HeroBox")?
                .GetComponent<Collider2D>();
        }

        private void SetHeroCollider(bool enabled)
        {
            if (heroCollider != null)
                heroCollider.enabled = enabled;

            if (heroBox != null)
                heroBox.enabled = enabled;
        }

        public bool ToggleButtonInsideMenu => false;
        private bool isCoroutineRunning = false;

        public Hollow_Knight_Platforming_Mod() : base("Platforming QoL") { }

        public override string GetVersion() => "1.0.0.0";
        private HitboxViewer hitboxViewer;
        public override void Initialize()
        {
            On.GameManager.SetTimeScale_float += GameManager_SetTimeScale;
            ModHooks.AfterTakeDamageHook += OnAfterTakeDamage;
            ModHooks.TakeDamageHook += OnTakeDamageHook;
            On.GameManager.Update += GameManager_Update;
            hitboxViewer = new HitboxViewer();
            // hitboxViewer.Load();
            // hitboxViewer.Unload();
            Log("Platforming mod initialized.");
        }
        private void GameManager_Update(On.GameManager.orig_Update orig, GameManager self)
        {
            orig(self);

            // Инициализация камеры один раз
            if (camTransform == null && Camera.main != null)
            {
                camTransform = Camera.main.transform;
            }

            if (camTransform != null)
            {
                timer += Time.unscaledDeltaTime; // игнорируем timescale

                if (timer >= interval)
                {
                    timer = 0f;

                    // Применяем угол из настроек
                    if (!Mathf.Approximately(appliedAngle, rotateAngle))
                    {
                        camTransform.localRotation = Quaternion.Euler(0f, 0f, rotateAngle);
                        appliedAngle = rotateAngle;
                    }
                }
            }
        }

        private void ApplyCameraRotation()
        {
            if (camTransform == null)
                return;

            camTransform.localRotation = Quaternion.Euler(0f, 0f, currentAngle);
        }
        public List<IMenuMod.MenuEntry> GetMenuData(IMenuMod.MenuEntry? toggleButtonEntry)
        {
            string[] freezeOptions = CreateOptions(0f, 2f, 0.1f);
            string[] speedOptions = CreateOptions(1f, 5f, 0.5f);
            string[] rotateOptions = CreateOptions(0f, 270f, 90f);

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
                    Name = "Skip Type",
                    Description = "Depends on the purpose",
                    Values = new[] { "Respawn", "Death" },
    

                    Saver = opt =>
                    {
                        skipIndex = opt;
                        skipTime = (opt == 0) ? 1.8f : 5f;
                    },
    

                    Loader = () => skipIndex
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
                },

                new IMenuMod.MenuEntry 
                { 
                    Name = "Freeze Mode", 
                    Description = "Launch trigger", 
                    Values = new[] { "Hazard objects only", "Any hits" }, 
                    Saver = opt => freezeMode = opt == 0, 
                    Loader = () => freezeMode ? 0 : 1 
                },

                new IMenuMod.MenuEntry
                {
                    Name = "Camera Rotation",
                    Description = "Degree Angle",
                    Values = rotateOptions,
                    Saver = opt => rotateAngle = float.Parse(rotateOptions[opt]),
                    Loader = () => FindClosestIndex(rotateOptions, rotateAngle)
                }

                /*new IMenuMod.MenuEntry
                {
                    Name = "Hazard Respawn",
                    Description = "Respawn player in hazard objects mode",
                    Values = new[] { "Off", "On" },
                    Saver = opt => hazardRespawn = opt == 1,
                    Loader = () => hazardRespawn ? 1 : 0
                }*/
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

        private int OnAfterTakeDamage(int hazardType, int damageAmount)
        {
            if (freezeMode)
                return damageAmount;
            if (damageAmount <= 0)
                return damageAmount;

            if (!isCoroutineRunning && HeroController.instance != null)
            {
                HeroController.instance.StartCoroutine(AnyHitsTimeEffectRoutine());
            }
            return damageAmount;
        }
        private IEnumerator AnyHitsTimeEffectRoutine()
        {
            isCoroutineRunning = true;

            if (showHitboxes)
            {
                hitboxViewer.Load();
            }

            forceFreezeActive = true;

            originalGenericTimeScale = TimeController.GenericTimeScale;
            TimeController.GenericTimeScale = 0f;

            float freezeStart = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - freezeStart < freezeDuration)
            {
                yield return null;
            }

            forceFreezeActive = false;


            if (showHitboxes)
            {
                hitboxViewer.Unload();
            }

            TimeController.GenericTimeScale = speedMultiplier;
            float realDuration = skipTime / speedMultiplier;
            yield return new WaitForSecondsRealtime(realDuration);
            TimeController.GenericTimeScale = originalGenericTimeScale;

            isCoroutineRunning = false;
        }
        private int OnTakeDamageHook(ref int hazardType, int damage)
        {
            if (!freezeMode)
                return damage;

            if (damage <= 0)
                return damage;

            if (hazardType <= (int)GlobalEnums.HazardType.SPIKES)
                return damage;

            if (!isCoroutineRunning && HeroController.instance != null)
            {
                HeroController.instance.StartCoroutine(HazardTimeEffectRoutine());
            }

            return damage;
        }
        private IEnumerator HazardTimeEffectRoutine()
        {
            isCoroutineRunning = true;

            if (showHitboxes)
            {
                hitboxViewer.Load();
            }

            float freezeStart = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - freezeStart < freezeDuration)
            {
                if (Time.timeScale != 0f)
                    Time.timeScale = 0f;

                yield return null;
            }

            if (showHitboxes)
            {
                hitboxViewer.Unload();
            }
            //if(hazardRespawn)
            //    TriggerRespawn();
            Time.timeScale = speedMultiplier;

            float realDuration = (skipTime) / speedMultiplier;
            float speedStart = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - speedStart < realDuration)
            {
                if (Time.timeScale != speedMultiplier)
                    Time.timeScale = speedMultiplier;

                yield return null;
            }
            Time.timeScale = 1f;
            isCoroutineRunning = false;
        }
    }
}
