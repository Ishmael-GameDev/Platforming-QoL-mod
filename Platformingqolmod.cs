using GlobalEnums;
using PlatformingQoL.Hitbox;
using Modding;
using Modding.Menu;
using System.Collections;
using UnityEngine;

namespace PlatformingQoL
{
    public class PlatformingQoL : Mod, ICustomMenuMod, IGlobalSettings<PlatformingSettings>
    {
        private bool forceFreezeActive = false;

        private float originalTimeScale = 1f;

        private float lastKnownTimeScale = 1f;
        private bool wasPaused = false;

        private const float MinSafeTimeScale = 0.0001f;

        public static bool modEnabled = true;
        public static bool skipperEnabled = true;
        public static bool debugModTimescaleFix = true;
        public static float freezeDuration = 0.5f;
        public static float speedMultiplier = 3f;
        public static int hitboxDisplayMode = 1;

        public static int permanentHitboxLevel = 0;

        public static bool hazardRespawn = false;
        public static float skipTime = 1.8f;
        public static int skipIndex = 0;
        public static float rotateAngle = 0f;

        private float appliedAngle = 0f;
        private Transform camTransform;
        private float currentAngle = 0f;
        private bool cameraReady = false;
        public static bool hitboxSmoothingEnabled = false;
        public static float hitboxSmoothingStrength = 15f;
        public static PlatformingSettings GS { get; set; } = new();

        private float timer = 0f;
        private float interval = 1f;

        public void OnLoadGlobal(PlatformingSettings gs)
        {
            if (gs == null) return;
            GS = gs;
            modEnabled = GS.ModEnabled;
            skipperEnabled = GS.SkipperEnabled;
            debugModTimescaleFix = GS.DebugModTimescaleFix;
            hitboxDisplayMode = GS.HitboxDisplayMode;
            hazardRespawn = GS.HazardRespawn;
            freezeDuration = GS.FreezeDuration;
            speedMultiplier = GS.SpeedMultiplier;
            skipIndex = GS.SkipIndex;
            skipTime = GS.SkipTime;
            rotateAngle = GS.RotateAngle;
            hitboxSmoothingEnabled = GS.HitboxSmoothingEnabled;
            hitboxSmoothingStrength = GS.HitboxSmoothingStrength;
        }

        public PlatformingSettings OnSaveGlobal()
        {
            GS.ModEnabled = modEnabled;
            GS.SkipperEnabled = skipperEnabled;
            GS.DebugModTimescaleFix = debugModTimescaleFix;
            GS.HitboxDisplayMode = hitboxDisplayMode;
            GS.HazardRespawn = hazardRespawn;
            GS.FreezeDuration = freezeDuration;
            GS.SpeedMultiplier = speedMultiplier;
            GS.SkipIndex = skipIndex;
            GS.SkipTime = skipTime;
            GS.RotateAngle = rotateAngle;
            GS.HitboxSmoothingEnabled = hitboxSmoothingEnabled;
            GS.HitboxSmoothingStrength = hitboxSmoothingStrength;
            return GS;
        }

        private Collider2D heroCollider;
        private Collider2D heroBox;

        public void OnDisable()
        {
            On.GameManager.SetTimeScale_float -= GameManager_SetTimeScale;
            if (forceFreezeActive || isCoroutineRunning)
            {
                CancelTimeEffect();
            }
        }

        private bool IsPaused()
        {
            return UIManager.instance != null &&
                   UIManager.instance.uiState.ToString() == "PAUSED";
        }

        private float SafeTimeScale(float value)
        {
            return value > MinSafeTimeScale ? value : 1f;
        }

        private void CancelTimeEffect()
        {
            forceFreezeActive = false;

            if (permanentHitboxLevel == 0 && hitboxDisplayMode != 0)
            {
                hitboxViewer.Unload();
            }

            Time.timeScale = originalTimeScale;
            isCoroutineRunning = false;
        }

        private void GameManager_SetTimeScale(
            On.GameManager.orig_SetTimeScale_float orig,
            GameManager self,
            float newTimeScale)
        {
            if (forceFreezeActive && !IsPaused())
            {
                Time.timeScale = 0f;
                return;
            }
            orig(self, newTimeScale);
        }

        private static void TriggerRespawn()
        {
            if (GameManager.instance == null ||
                HeroController.instance == null ||
                !GameManager.instance.IsGameplayScene() ||
                HeroController.instance.cState.dead ||
                PlayerData.instance.health <= 0)
                return;

            if (UIManager.instance.uiState.ToString() == "PAUSED")
            {
                InputHandler.Instance.StartCoroutine(GameManager.instance.PauseGameToggle());
                GameManager.instance.HazardRespawn();
                return;
            }

            if (UIManager.instance.uiState.ToString() == "PLAYING")
            {
                HeroController.instance.RelinquishControl();
                GameManager.instance.HazardRespawn();
                HeroController.instance.RegainControl();
                return;
            }
        }

        private void CacheHeroColliders()
        {
            var hc = HeroController.instance;
            if (hc == null) return;
            heroCollider = hc.GetComponent<Collider2D>();
            heroBox = hc.transform.Find("HeroBox")?.GetComponent<Collider2D>();
        }

        private void SetHeroCollider(bool enabled)
        {
            if (heroCollider != null) heroCollider.enabled = enabled;
            if (heroBox != null) heroBox.enabled = enabled;
        }

        public bool ToggleButtonInsideMenu => false;
        private bool isCoroutineRunning = false;

        public PlatformingQoL() : base("Platforming QoL") { }
        public override string GetVersion() => "1.1.6.0";

        private HitboxViewer hitboxViewer;

        public override void Initialize()
        {
            On.GameManager.SetTimeScale_float += GameManager_SetTimeScale;
            ModHooks.AfterTakeDamageHook += OnAfterTakeDamage;
            //ModHooks.TakeDamageHook += OnTakeDamageHook;
            On.GameManager.Update += GameManager_Update;
            hitboxViewer = new HitboxViewer();
            Log("Platforming mod initialized.");
        }

        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates)
        {
            return ConfigScreen.GetMenuScreen(modListMenu);
        }

        private void GameManager_Update(On.GameManager.orig_Update orig, GameManager self)
        {
            orig(self);

            HandlePauseTimeScaleRestore();

            if (InputHandler.Instance != null && GS.KeyBinds != null && GS.KeyBinds.ToggleHitboxes.WasPressed)
            {
                permanentHitboxLevel = (permanentHitboxLevel + 1) % 3;

                if (permanentHitboxLevel > 0)
                {
                    hitboxViewer.Load(permanentHitboxLevel);
                }
                else if (!isCoroutineRunning)
                {
                    hitboxViewer.Unload();
                }
            }

            if (!modEnabled) return;

            if (camTransform == null && Camera.main != null)
            {
                camTransform = Camera.main.transform;
            }

            if (camTransform != null)
            {
                timer += Time.unscaledDeltaTime;
                if (timer >= interval)
                {
                    timer = 0f;
                    if (!Mathf.Approximately(appliedAngle, rotateAngle))
                    {
                        camTransform.localRotation = Quaternion.Euler(0f, 0f, rotateAngle);
                        appliedAngle = rotateAngle;
                    }
                }
            }
        }

        private void HandlePauseTimeScaleRestore()
        {
            if (!modEnabled || !debugModTimescaleFix) return;

            bool paused = IsPaused();
            bool effectActive = forceFreezeActive || isCoroutineRunning;

            if (!paused && !effectActive && Time.timeScale > MinSafeTimeScale)
            {
                lastKnownTimeScale = Time.timeScale;
            }

            if (wasPaused && !paused && !effectActive)
            {
                if (!Mathf.Approximately(Time.timeScale, lastKnownTimeScale))
                {
                    Log($"[Platforming QoL] Game reset timescale to {Time.timeScale} on unpause, restoring {lastKnownTimeScale}");
                    Time.timeScale = lastKnownTimeScale;
                }
            }

            wasPaused = paused;
        }

        private int OnAfterTakeDamage(int hazardType, int damageAmount)
        {
            if (!modEnabled || !skipperEnabled) return damageAmount;

            // 1. Проверка на реальный хит (урон больше 0)
            if (damageAmount <= 0) return damageAmount;

            // 2. Проверка на хазард-объект (перенесено из удаленного хука)
            if (hazardType <= (int)GlobalEnums.HazardType.SPIKES) return damageAmount;

            if (!isCoroutineRunning && HeroController.instance != null)
            {
                HeroController.instance.StartCoroutine(HazardTimeEffectRoutine());
            }
            return damageAmount;
        }

        private IEnumerator AnyHitsTimeEffectRoutine()
        {
            if (!modEnabled || !skipperEnabled) yield break;

            isCoroutineRunning = true;

            bool skipperControlsHitboxes = permanentHitboxLevel == 0 && hitboxDisplayMode != 0;
            bool loadedBySkipper = false;

            if (skipperControlsHitboxes)
            {
                hitboxViewer.Load(hitboxDisplayMode);
                loadedBySkipper = true;
            }

            forceFreezeActive = true;
            originalTimeScale = SafeTimeScale(Time.timeScale);
            //Log($"[Platforming QoL] Capture (AnyHits): Time.timeScale={Time.timeScale}");

            float freezeStart = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - freezeStart < freezeDuration)
            {
                if (IsPaused() || !modEnabled || !skipperEnabled) { CancelTimeEffect(); yield break; }
                if (Time.timeScale != 0f)
                    Time.timeScale = 0f;
                yield return null;
            }

            forceFreezeActive = false;

            if (loadedBySkipper) hitboxViewer.Unload();

            float targetSpeed = SafeTimeScale(originalTimeScale * speedMultiplier);
            Time.timeScale = targetSpeed;

            float realDuration = skipTime / targetSpeed;
            //Log($"[Platforming QoL] AnyHits skip: original={originalTimeScale}, targetSpeed={targetSpeed}, realDuration={realDuration}");
            float speedStart = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - speedStart < realDuration)
            {
                if (IsPaused() || !modEnabled || !skipperEnabled) { CancelTimeEffect(); yield break; }
                if (!Mathf.Approximately(Time.timeScale, targetSpeed))
                    Time.timeScale = targetSpeed;
                yield return null;
            }

            Time.timeScale = originalTimeScale;
            isCoroutineRunning = false;
        }

        /*private int OnTakeDamageHook(ref int hazardType, int damage)
        {
            if (!modEnabled || !skipperEnabled) return damage;
            if (!freezeMode) return damage;
            if (damage <= 0) return damage;

            //Log($"[Platforming QoL] TakeDamageHook: hazardType={hazardType}, damage={damage}");

            if (hazardType <= (int)GlobalEnums.HazardType.SPIKES) return damage;

            if (!isCoroutineRunning && HeroController.instance != null)
            {
                HeroController.instance.StartCoroutine(HazardTimeEffectRoutine());
            }
            return damage;
        }*/

        private IEnumerator HazardTimeEffectRoutine()
        {
            if (!modEnabled || !skipperEnabled) yield break;

            isCoroutineRunning = true;

            bool skipperControlsHitboxes = permanentHitboxLevel == 0 && hitboxDisplayMode != 0;
            bool loadedBySkipper = false;

            if (skipperControlsHitboxes)
            {
                hitboxViewer.Load(hitboxDisplayMode);
                loadedBySkipper = true;
            }

            forceFreezeActive = true;
            originalTimeScale = SafeTimeScale(Time.timeScale);
            //Log($"[Platforming QoL] Capture (Hazard): Time.timeScale={Time.timeScale}");

            float freezeStart = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - freezeStart < freezeDuration)
            {
                if (IsPaused() || !modEnabled || !skipperEnabled) { CancelTimeEffect(); yield break; }
                if (Time.timeScale != 0f)
                    Time.timeScale = 0f;
                yield return null;
            }

            forceFreezeActive = false;

            if (loadedBySkipper) hitboxViewer.Unload();

            float targetSpeed = SafeTimeScale(originalTimeScale * speedMultiplier);
            Time.timeScale = targetSpeed;

            float realDuration = skipTime / targetSpeed;
            //Log($"[Platforming QoL] Hazard skip: original={originalTimeScale}, targetSpeed={targetSpeed}, realDuration={realDuration}");
            float speedStart = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - speedStart < realDuration)
            {
                if (IsPaused() || !modEnabled || !skipperEnabled) { CancelTimeEffect(); yield break; }
                if (!Mathf.Approximately(Time.timeScale, targetSpeed))
                    Time.timeScale = targetSpeed;
                yield return null;
            }

            Time.timeScale = originalTimeScale;
            isCoroutineRunning = false;
        }
    }
}
