using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

namespace Roguelike2D
{
    /// <summary>
    /// Main class that handle the Board, the surface on which the game happens. It will keep data for each cell, and take
    /// care of generating the level randomly.
    /// </summary>
    public class BoardManager : MonoBehaviour
    {
        /// <summary>
        /// Class that store data for a single cell of the Board
        /// </summary>
        public class CellData
        {
            public bool Passable;
            public List<CellObject> ContainedObjects = new();
            public CellObject UniqueCellObject = null;

            public bool PlayerWantToEnter()
            {
                foreach (var cellObject in ContainedObjects)
                {
                    if (!cellObject.PlayerWantToEnter())
                        return false;
                }

                return true;
            }

            public void PlayerEntered()
            {
                foreach (var cellObject in ContainedObjects)
                {
                    cellObject.PlayerEntered();
                }
            }

            public bool HasObjects() => ContainedObjects.Count > 0;

            public void AddObject(CellObject obj)
            {
                if (obj.IsUnique())
                {
                    if (UniqueCellObject != null)
                    {
                        //we tried to add a unique cell object to a cell already containing one, this shouldn't happen! Error out
                        Debug.LogError($"Tried to the unique cell object {obj.name} to a cell already containing one {UniqueCellObject.name}");
                        return;
                    }

                    UniqueCellObject = obj;
                }
            
                ContainedObjects.Add(obj);
            }

            public bool HaveAttackable(out AttackableCellObject attackable)
            {
                if (UniqueCellObject != null && UniqueCellObject is AttackableCellObject obj)
                {
                    attackable = obj;
                    return true;
                }

                attackable = null;
                return false;
            }

            public void RemoveObject(CellObject obj)
            {
                if (obj == UniqueCellObject)
                    UniqueCellObject = null;
            
                ContainedObjects.Remove(obj);
            }

            public void ClearObjects()
            {
                foreach (var cellObject in ContainedObjects)
                {
                    Destroy(cellObject.gameObject);
                }
                ContainedObjects.Clear();
            }
        }
    
        public BoxCollider2D CameraConfinerBound;

        private Tilemap m_Tilemap;
        private Grid m_Grid;
        private CellData[,] m_BoardData;
        private WorldSettings.LevelData m_CurrentLevelData;
        private List<Vector2Int> m_EmptyCellsList;
        private List<CellObject> m_CurrentAvailableObjectList;
        private List<Enemy> m_CurrentAvailableEnemyList;

        private int m_Width;
        private int m_Height;

        public void Awake()
        {
            m_Tilemap = GetComponentInChildren<Tilemap>();
            m_Grid = GetComponent<Grid>();
        }

    
        /// <summary>
        /// Init the board, generating a new board randomly based on the level in CurrentLevel
        /// </summary>
        public void Init()
        {
            m_EmptyCellsList = new List<Vector2Int>();
            m_CurrentAvailableObjectList = new List<CellObject>();
            m_CurrentAvailableEnemyList = new List<Enemy>();

            m_CurrentLevelData = GameManager.Instance.WorldSettings.GetLevelDataForLevel(GameManager.Instance.CurrentLevel);
            var theme = GameManager.Instance.WorldSettings.Theme;

            m_Width = m_CurrentLevelData.Width;
            m_Height = m_CurrentLevelData.Height;
        
            m_BoardData = new CellData[m_Width, m_Height];

            //Generate the board starting to -20 in x and y and going up to +20 in both direction. This allows to generate
            //some ground to extend outside the screen for visuals.
            for (int y = -20; y < m_Height + 20; ++y)
            {
                for(int x = -20; x < m_Width + 20; ++x)
                {
                    //if we are outside of the play area, we just set some random ground tile to fill the empty space
                    if (x < 0 || y < 0 || x >= m_Width || y >= m_Height)
                    {
                        m_Tilemap.SetTile(new Vector3Int(x, y, 0), theme.GetRandomGround());
                        continue;
                    }
                
                    Tile tile;
                    m_BoardData[x, y] = new CellData();

                    //outside border is blocking impassable walls
                    if (x == 0 || y == 0 || x == m_Width - 1 || y == m_Height - 1)
                    {
                        tile = theme.GetRandomBlocking();
                        m_BoardData[x, y].Passable = false;
                    }
                    else
                    {
                        //for now set it as normal ground and add that tile to the empty list
                        tile = theme.GetRandomGround();
                        m_BoardData[x, y].Passable = true;
                    
                        m_EmptyCellsList.Add(new Vector2Int(x,y));
                    }

                    m_Tilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
        
            //remove the spawn tile from the empty list, as this is occupied by the player
            m_EmptyCellsList.Remove(new Vector2Int(1, 1));
        
            //add the exit cell (removing it from empty cell too)
            var inst = Instantiate(theme.ExitCellPrefab);
            Vector2Int endCoord = new Vector2Int(m_Width - 2, m_Height - 2);
            AddObject(inst, endCoord);
            m_EmptyCellsList.Remove(endCoord);
        
            GenerateWall();
            GenerateObject();
            GenerateEnemies();
        
            UpdateCameraConfiner();
        }

        public void Clean()
        {
            //no board data to cleanup
            if (m_BoardData == null) return;
        
            int width = m_CurrentLevelData.Width;
            int height = m_CurrentLevelData.Height;
        
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    var cellData = m_BoardData[x, y];
                    cellData.ClearObjects();
                }
            }
        }

        void GenerateObject()
        {
            int itemCount = GameManager.Instance.WorldSettings.Items.GetRandomItemCount(GameManager.Instance.CurrentLevel);
            GameManager.Instance.WorldSettings.Items.GetRandomItemList(ref m_CurrentAvailableObjectList, GameManager.Instance.CurrentLevel);
        
            for (int i = 0; i < itemCount; ++i)
            {
                int randomIndex = Random.Range(0, m_EmptyCellsList.Count);
                Vector2Int coord = m_EmptyCellsList[randomIndex];
            
                m_EmptyCellsList.RemoveAt(randomIndex);
            
                var newObject = Instantiate(m_CurrentAvailableObjectList[Random.Range(0, m_CurrentAvailableObjectList.Count)]);
                AddObject(newObject, coord);
            }
        }

        void GenerateWall()
        {
            int wallCount = Random.Range(m_CurrentLevelData.LowestWallCount, m_CurrentLevelData.HighestWallCount+1);

            for (int i = 0; i < wallCount; ++i)
            {
                int randomIndex = Random.Range(0, m_EmptyCellsList.Count);
                Vector2Int coord = m_EmptyCellsList[randomIndex];
            
                m_EmptyCellsList.RemoveAt(randomIndex);
            
                WallObject newWall = Instantiate(GameManager.Instance.WorldSettings.Theme.GetRandomWall());
                AddObject(newWall, coord);
            }
        }

        void GenerateEnemies()
        {
            int enemyCount = GameManager.Instance.WorldSettings.Enemies.GetRandomEnemyCount(GameManager.Instance.CurrentLevel);
            GameManager.Instance.WorldSettings.Enemies.GetRandomEnemies(ref m_CurrentAvailableEnemyList, GameManager.Instance.CurrentLevel);
        
            for (int i = 0; i < enemyCount; ++i)
            {
                int randomIndex = Random.Range(0, m_EmptyCellsList.Count);
                Vector2Int coord = m_EmptyCellsList[randomIndex];
            
                m_EmptyCellsList.RemoveAt(randomIndex);
            
                Enemy newEnemy = Instantiate(m_CurrentAvailableEnemyList[Random.Range(0, m_CurrentAvailableEnemyList.Count)]);
                AddObject(newEnemy, coord);
            }
        }

        void UpdateCameraConfiner()
        {
            Vector3 centerPos = CellToWorld(new Vector2Int(m_CurrentLevelData.Width / 2, m_CurrentLevelData.Height / 2));

            if (m_CurrentLevelData.Width % 2 == 0)
            {
                //even width mean the center is between cell not in a cell, so offset
                centerPos.x -= m_Tilemap.cellSize.x * 0.5f;
            }

            if (m_CurrentLevelData.Height % 2 == 0)
            {
                centerPos.y -= m_Tilemap.cellSize.y * 0.5f;
            }

            CameraConfinerBound.transform.position = centerPos;
            CameraConfinerBound.size = new Vector2((m_CurrentLevelData.Width) * m_Tilemap.cellSize.x,
                (m_CurrentLevelData.Height) * m_Tilemap.cellSize.y);
        
            GameManager.Instance.CameraConfiner.InvalidateBoundingShapeCache();
        }

        void AddObject(CellObject obj, Vector2Int coord)
        {
            CellData data = m_BoardData[coord.x, coord.y];
            obj.transform.position = CellToWorld(coord);
            data.AddObject(obj);
            obj.Init(coord);
        }

        public Vector3 CellToWorld(Vector2Int cellIndex)
        {
            return m_Grid.GetCellCenterWorld((Vector3Int)cellIndex);
        }

        public CellData GetCellData(Vector2Int cellIndex)
        {
        
            if (cellIndex.x < 0 || cellIndex.x >= m_CurrentLevelData.Width
                                || cellIndex.y < 0 || cellIndex.y >= m_CurrentLevelData.Height)
            {
                return null;
            }

            return m_BoardData[cellIndex.x, cellIndex.y];
        }
        public Tile GetCellTile(Vector2Int cellIndex)
        {
            return m_Tilemap.GetTile<Tile>((Vector3Int)cellIndex);
        }

        public void SetCellTile(Vector2Int cellIndex, Tile tile)
        {
            m_Tilemap.SetTile((Vector3Int)cellIndex, tile);
        }
    
        public void Save(BinaryWriter writer)
        {
            //save the board size
            writer.Write(m_Width);
            writer.Write(m_Height);

            for (int y = 0; y < m_Height; ++y)
            {
                for (int x = 0; x < m_Width; ++x)
                {
                    var cellData = m_BoardData[x, y];
                
                    //save the tile used there
                    writer.Write(m_Tilemap.GetTile<Tile>(new Vector3Int(x,y,0)).name);
                
                    //save the cell data
                    writer.Write(cellData.Passable);
                
                    //save all object
                    writer.Write(cellData.ContainedObjects.Count);
                    for (int i = 0; i < cellData.ContainedObjects.Count; ++i)
                    {
                        var obj = cellData.ContainedObjects[i];
                        //save ID used to lookup on database on reload
                        writer.Write(obj.ID);
                        //then call save on the object itself so it can save some internal data if it wants (e.g. enemy save health)
                        obj.Save(writer);
                    }
                }
            }
        }

        public void Load(BinaryReader reader)
        {
            Clean();
        
            //load size
            m_Width = reader.ReadInt32();
            m_Height = reader.ReadInt32();

            m_BoardData = new CellData[m_Width, m_Height];
        
            //like generation we overshoot on both direction to place some visual ground outside the playable area
            for (int y = -20; y < m_Height + 20; ++y)
            {
                for (int x = -20; x < m_Width + 20; ++x)
                {
                    //if we are outside of the play area, we just set some random ground tile to fill the empty space
                    if (x < 0 || y < 0 || x >= m_Width || y >= m_Height)
                    {
                        m_Tilemap.SetTile(new Vector3Int(x, y, 0), GameManager.Instance.WorldSettings.Theme.GetRandomGround());
                        continue;
                    }
                
                    //setting back the tile by reading its id and asking the database for the right tile 
                    string tileId = reader.ReadString();
                    var tile = GameManager.Instance.ReferenceDatabase.GetTileFromInstanceID(tileId);
                    m_Tilemap.SetTile(new Vector3Int(x,y,0), tile);
                
                    var passable = reader.ReadBoolean();
                
                    //initializing the data
                    m_BoardData[x, y] = new CellData()
                    {
                        Passable = passable
                    };

                    //Adding back objects and loading it (things like walls or enemy need to put back their health etc.)
                    int objectCount = reader.ReadInt32();
                    for (int i = 0; i < objectCount; ++i)
                    {
                        var objId = reader.ReadString();
                        var obj = Instantiate(GameManager.Instance.ReferenceDatabase.GetCellObjectFromID(objId));
                        AddObject(obj, new Vector2Int(x, y));
                        obj.Load(reader);
                    }
                }
            }
        
            //retrieve the world data, the current level will have been set by the GameManager load function
            m_CurrentLevelData = GameManager.Instance.WorldSettings.GetLevelDataForLevel(GameManager.Instance.CurrentLevel);
            UpdateCameraConfiner();
        }
    }
}