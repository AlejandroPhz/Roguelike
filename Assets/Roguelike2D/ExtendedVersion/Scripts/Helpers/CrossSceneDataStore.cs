using UnityEngine;

namespace Roguelike2D
{
    public class CrossSceneDataStore : MonoBehaviour
    {
        public static CrossSceneDataStore Instance { get; private set; }

        public WorldSettings SelectedWordSettings = null;
    
        public static void CreateNewInstance()
        {
            var gameObject = new GameObject("CrossSceneDataStore");
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(gameObject);
        
            Instance = gameObject.AddComponent<CrossSceneDataStore>();
        }
    }
}