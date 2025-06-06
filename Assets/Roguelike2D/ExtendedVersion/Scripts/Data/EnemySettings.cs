using System.Collections.Generic;
using UnityEngine;

namespace Roguelike2D
{
    /// <summary>
    /// A class that store settings for enemy spawn. This is made of 2 arrays :
    /// - EnemyCount which is for a given level the minimum and maximum number of enemy to spawn. If a level number is not
    ///   defined, closest lower level below is used (e.g. if only level 0 and 4 is defined, level 2 will use level 0 settings)
    /// - Enemies the array of enemies, with their probabilities per level. Like enemy count, the probability of the lowest
    ///   closest level is used. Probabilities can be any integer number as they will be relative to each other (e.g an
    ///   enemy with probability 100 will have twice the chance of being picked as one with 50) 
    /// </summary>
    [CreateAssetMenu(fileName = "EnemySettings", menuName = "Scriptable Objects/EnemySettings")]
    public class EnemySettings : ScriptableObject
    {
        [System.Serializable]
        public class EnemyCountEntry
        {
            public int Level;
            public int Minimum;
            public int Maximum;
        }
    
        [System.Serializable]
        public class EnemySettingEntry
        {
            [System.Serializable]
            public class EnemyProbability
            {
                public int Level;
                public int Weight;
            }

            public Enemy Enemy;
            public EnemyProbability[] Probabilities;
        }

        public EnemyCountEntry[] EnemyCount;
        public EnemySettingEntry[] Enemies;

        public int GetRandomEnemyCount(int level)
        {
            if (EnemyCount.Length == 0 || EnemyCount[0].Level > level)
                return 0;
        
            //getting the lowest entry the closest from the required level (or the exact level if exist).
            int entry = 0;
            while (entry < EnemyCount.Length - 1 && EnemyCount[entry+1].Level < level)
            {
                entry++;
            }

            return Random.Range(EnemyCount[entry].Minimum, EnemyCount[entry].Maximum + 1);
        }

        /// <summary>
        /// This will fill the given array with enemies based on their probabilities. e.g if there is an enemy with a
        /// probabilities of 10 and another of 5, there will be 15 enemies in the list, that way picking a random entries in
        /// that list will respect probabilities.
        /// </summary>
        /// <param name="enemiesList">Will be filled with enemies after call</param>
        /// <param name="level">The level for which return the enemy list</param>
        public void GetRandomEnemies(ref List<Enemy> enemiesList, int level)
        {
            enemiesList.Clear();
            foreach (var enemy in Enemies)
            {
                if(enemy.Probabilities.Length == 0 || enemy.Probabilities[0].Level > level)
                    continue;

                int entry = 0;
                while (entry < enemy.Probabilities.Length - 1 && enemy.Probabilities[entry+1].Level <= level)
                    entry++;
            
                for (int i = 0; i < enemy.Probabilities[entry].Weight; ++i)
                {
                    enemiesList.Add(enemy.Enemy);
                }
            }
        }

        /// <summary>
        /// Call to register all enemies in that settings in the Databse used 
        /// </summary>
        public void RegisterCellObjectLookup()
        {
            foreach (var enemy in Enemies)
            {
                GameManager.Instance.ReferenceDatabase.RegisterCellObject(enemy.Enemy);
            }
        }
    }
}