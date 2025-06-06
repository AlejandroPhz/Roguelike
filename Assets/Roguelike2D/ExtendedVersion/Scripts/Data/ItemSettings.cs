using System.Collections.Generic;
using UnityEngine;

namespace Roguelike2D
{
    /// <summary>
    /// Settings for items count and probabilities. It contains 2 arrays :
    /// - ItemsCount: random items count per level. If a level isn't defined, it use the lowest closest level
    /// - Items: probabilities per level. Probabilities are relative to each other (e.g. an item with probability of 20
    ///   will be twice as likely to be picked as one with probability 10)
    /// </summary>
    [CreateAssetMenu(fileName = "ItemSettings", menuName = "Scriptable Objects/ItemSettings")]
    public class ItemSettings : ScriptableObject
    {
        [System.Serializable]
        public class ItemCountEntry
        {
            public int Level;
            public int Minimum;
            public int Maximum;
        }
    
        [System.Serializable]
        public class ItemSettingEntry
        {
            [System.Serializable]
            public class ItemProbability
            {
                public int Level;
                public int Weight;
            }

            public CellObject Item;
            public ItemProbability[] Probabilities;
        }

        public ItemCountEntry[] ItemsCount;
        public ItemSettingEntry[] Items;

        /// <summary>
        /// Return a random number of items based on settings for the requested level.
        /// </summary>
        /// <param name="level">The levelor which to get a random number of enemies</param>
        /// <returns></returns>
        public int GetRandomItemCount(int level)
        {
            if (ItemsCount.Length == 0 || ItemsCount[0].Level > level)
                return 0;
        
            int entry = 0;
            while (entry < ItemsCount.Length - 1 && ItemsCount[entry+1].Level < level)
            {
                entry++;
            }

            return Random.Range(ItemsCount[entry].Minimum, ItemsCount[entry].Maximum + 1);
        }

        /// <summary>
        /// Will return in the given list a list of objects with their number linked to their probability. Picking a random
        /// entry in this list will pick a random items which take the probabilities 
        /// </summary>
        /// <param name="objectsList">This List will contain a list of items, duplicated based on their probabailities</param>
        /// <param name="level">The level for which get a random items</param>
        public void GetRandomItemList(ref List<CellObject> objectsList, int level)
        {
            objectsList.Clear();
            foreach (var item in Items)
            {
                if(item.Probabilities.Length == 0 || item.Probabilities[0].Level > level)
                    continue;

                int entry = 0;
                while (entry < item.Probabilities.Length - 1 && item.Probabilities[entry+1].Level <= level)
                    entry++;
            
                for (int i = 0; i < item.Probabilities[entry].Weight; ++i)
                {
                    objectsList.Add(item.Item);
                }
            }
        }

        /// <summary>
        /// Call this to register every items in settings into the Databased used to retrieve item from its ID
        /// </summary>
        public void RegisterCellObjectsLookup()
        {
            foreach (var item in Items)
            {
                GameManager.Instance.ReferenceDatabase.RegisterCellObject(item.Item);
            }
        }
    }
}