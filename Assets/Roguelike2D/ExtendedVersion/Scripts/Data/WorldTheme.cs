using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

namespace Roguelike2D
{
    /// <summary>
    /// Store all the data needed for visual settings of the game. Store reference to the player prefab and all the tiles
    /// used for the board.
    /// </summary>
    [CreateAssetMenu(fileName = "WorldTheme", menuName = "Scriptable Objects/World Theme")]
    public class WorldTheme : ScriptableObject
    {
        public PlayerController PlayerPrefab;
    
        public Tile[] GroundTiles;
        public Tile[] WallTiles;
        public ExitCellObject ExitCellPrefab;
        public WallObject[] WallPrefabs;

        public GameObject AmbiencePrefab;
        public GameObject MusicPrefab;


        public Tile GetRandomGround()
        {
            return GroundTiles[Random.Range(0, GroundTiles.Length)];
        }

        public Tile GetRandomBlocking()
        {
            return WallTiles[Random.Range(0, WallTiles.Length)];
        }

        public WallObject GetRandomWall()
        {
            return WallPrefabs[Random.Range(0, WallPrefabs.Length)];
        }

        public void RegisterCellObjectLookup()
        {

            Object.Instantiate(AmbiencePrefab, Vector3.zero, Quaternion.identity);
            Object.Instantiate(MusicPrefab, Vector3.zero, Quaternion.identity);

            GameManager.Instance.ReferenceDatabase.RegisterCellObject(ExitCellPrefab);
            foreach (var wall in WallPrefabs)
            {
                GameManager.Instance.ReferenceDatabase.RegisterCellObject(wall);
            }
        
            //also register tiles
            var tilesCollections = new []
            {
                GroundTiles,
                WallTiles
            };

            foreach (var tiles in tilesCollections)
            {
                foreach (var tile in tiles)
                {
                    GameManager.Instance.ReferenceDatabase.RegisterTile(tile);
                }
            }
        }
    }
}