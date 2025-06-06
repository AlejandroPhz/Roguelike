using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Roguelike2D
{
    public class WallObject : AttackableCellObject
    {
        public Tile WallTile;
        public Tile WallTileDamaged;
        public int MaxHealth = 3;

        private Tile m_OriginalTile;
        private int m_CurrentHealth;
    
        public override void Init(Vector2Int coord)
        {
            base.Init(coord);

            m_OriginalTile = GameManager.Instance.Board.GetCellTile(coord);
            GameManager.Instance.Board.SetCellTile(coord, WallTile);

            m_CurrentHealth = MaxHealth;
        }

        public override bool PlayerWantToEnter()
        {
            return false;
        }

        private void OnDestroy()
        {
            RemoveFromBoard();
        }

        public override void Damaged(int amount)
        {
            m_CurrentHealth -= amount;

            if (m_CurrentHealth == 1)
            {
                GameManager.Instance.Board.SetCellTile(m_Cell, WallTileDamaged);
            }
            else if (m_CurrentHealth == 0)
            {
                GameManager.Instance.Board.SetCellTile(m_Cell, m_OriginalTile);
                Destroy(gameObject);
            }
        }

        public override void Save(BinaryWriter writer)
        {
            writer.Write(m_OriginalTile.name);
            writer.Write(m_CurrentHealth);
        }

        public override void Load(BinaryReader reader)
        {
            string tileId = reader.ReadString();
            m_OriginalTile = GameManager.Instance.ReferenceDatabase.GetTileFromInstanceID(tileId);
            m_CurrentHealth = reader.ReadInt32();
        
            if (m_CurrentHealth == 1)
            {
                GameManager.Instance.Board.SetCellTile(m_Cell, WallTileDamaged);
            }
        }
    }
}