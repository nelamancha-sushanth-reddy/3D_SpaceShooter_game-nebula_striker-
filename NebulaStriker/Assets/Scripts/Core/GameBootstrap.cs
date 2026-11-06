using UnityEngine;

namespace NebulaStriker
{
    /// <summary>
    /// Safety net: if Play is pressed in ANY scene that has no GameManager,
    /// one is created automatically so the game always runs.
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameManager.Instance != null) return;
            new GameObject("GameManager").AddComponent<GameManager>();
        }
    }
}
