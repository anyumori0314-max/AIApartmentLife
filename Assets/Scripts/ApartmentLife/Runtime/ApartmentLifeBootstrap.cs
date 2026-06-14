using UnityEngine;

namespace ApartmentLife.Runtime
{
    // Sceneに何も置いていなくても、Play開始時にMVPを自動で立ち上げます。
    public static class ApartmentLifeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateGame()
        {
            if (Object.FindFirstObjectByType<ApartmentLifeGame>() != null)
            {
                return;
            }

            GameObject gameObject = new GameObject("Apartment Life Game");
            gameObject.AddComponent<ApartmentLifeGame>();
        }
    }
}
