using System.Collections.Generic;
using UnityEngine.Tilemaps;

namespace Roguelike2D
{
    /// <summary>
    /// This Database match a string ID to Tile and CellObject . This is used by the save system. When saving the ID of
    /// a tile or object is saved in the save file. On load, this will return the correct prefab/tile based on the ID to
    /// recreate the proper object.
    /// </summary>
    public class ReferenceDatabase
    {
        private Dictionary<string, Tile> m_TileLookupTable = new();
        private Dictionary<string, CellObject> m_CellObjectLookup = new();
    
        public void RegisterCellObject(CellObject obj)
        {
            obj.SetID();
            m_CellObjectLookup.TryAdd(obj.ID, obj);
        }

        public CellObject GetCellObjectFromID(string id)
        {
            m_CellObjectLookup.TryGetValue(id, out var cellObj);
            return cellObj;
        }

        public void RegisterTile(Tile tile)
        {
            m_TileLookupTable.TryAdd(tile.name, tile);
        }
    
        public Tile GetTileFromInstanceID(string id)
        {
            m_TileLookupTable.TryGetValue(id, out Tile tile);
            return tile;
        }
    }
}