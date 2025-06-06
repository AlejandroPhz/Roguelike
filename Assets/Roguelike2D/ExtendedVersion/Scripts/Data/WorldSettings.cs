using UnityEngine;

namespace Roguelike2D
{
    /// <summary>
    /// Collection of settings for the game. This will contain info on the board side, the theme (which have the tile to use
    /// and the player prefab), items and enemies settings
    /// </summary>
    [CreateAssetMenu(fileName = "WorldSettings", menuName = "Scriptable Objects/World Settings")]
    public class WorldSettings : ScriptableObject
    {
        [System.Serializable]
        public class LevelData
        {
            public int Width;
            public int Height;
            public int LowestWallCount;
            public int HighestWallCount;
        
            public int Level;
        }

        public string WorldName;
        public WorldTheme Theme;
        public ItemSettings Items;
        public EnemySettings Enemies;
    
        public LevelData[] LevelDatas;

        public LevelData GetLevelDataForLevel(int level)
        {
            if (LevelDatas.Length == 0 || LevelDatas[0].Level > level)
                return new LevelData()
                {
                    Height = 8,
                    Width = 8,
                    Level = 0
                };

            int entry = 0;
            while (entry < LevelDatas.Length - 1 && LevelDatas[entry+1].Level <= level)
                entry++;

            return LevelDatas[entry];
        }

        /// <summary>
        /// Will register every item, enemies and theme data (tiles) into the Reference Database used for saving
        /// </summary>
        public void RegisterObjectLookup()
        {
            Theme.RegisterCellObjectLookup();
            Items.RegisterCellObjectsLookup();
            Enemies.RegisterCellObjectLookup();
        }
    }
}