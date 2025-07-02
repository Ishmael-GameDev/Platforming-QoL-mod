using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Hollow_Knight_Platforming_Mod.Hitbox
{
    public class HitboxViewer
    {
        public static int State { get; private set; }
        private HitboxRender hitboxRender;

        public void Load()
        {
            State = Hollow_Knight_Platforming_Mod.showHitboxes ? 1 : 0; ;
            Unload();
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += CreateHitboxRender;


            ModHooks.ColliderCreateHook += UpdateHitboxRender;

            CreateHitboxRender();
        }

        public void Unload()
        {
            State = Hollow_Knight_Platforming_Mod.showHitboxes ? 1 : 0; ;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= CreateHitboxRender;

            ModHooks.ColliderCreateHook -= UpdateHitboxRender;
            DestroyHitboxRender();
        }

        private void CreateHitboxRender(Scene current, Scene next) => CreateHitboxRender();

        private void CreateHitboxRender()
        {
            DestroyHitboxRender();
            if (GameManager.instance.IsGameplayScene())
            {
                hitboxRender = new GameObject().AddComponent<HitboxRender>();
            }
        }

        private void DestroyHitboxRender()
        {
            if (hitboxRender != null)
            {
                Object.Destroy(hitboxRender);
                hitboxRender = null;
            }
        }

        private void UpdateHitboxRender(GameObject go)
        {
            if (hitboxRender != null)
            {
                hitboxRender.UpdateHitbox(go);
            }
        }
    }
}
