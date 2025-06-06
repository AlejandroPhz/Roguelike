using UnityEngine;
using UnityEngine.Tilemaps;

namespace TutorialVersion
{
    public class WallObject : CellObject
    {
        public Tile ObstacleTile;
        public Tile DamagedTile;
        public int MaxHealth = 3;

        private int m_HealthPoint;
        private Tile m_OriginalTile;
  
        public override void Init(Vector2Int cell)
        {
            base.Init(cell);

            m_HealthPoint = MaxHealth;
      
            m_OriginalTile = GameManager.Instance.BoardManager.GetCellTile(cell);
            GameManager.Instance.BoardManager.SetCellTile(cell, ObstacleTile);
        }

        public override bool PlayerWantsToEnter()
        {
            m_HealthPoint -= 1;

            if (m_HealthPoint > 0)
            {
                if (m_HealthPoint == 1)
                {
                    GameManager.Instance.BoardManager.SetCellTile(m_Cell, DamagedTile);
                }
             
                GameManager.Instance.PlayerController.Attack();
                return false;
            }

            GameManager.Instance.BoardManager.SetCellTile(m_Cell, m_OriginalTile);
            Destroy(gameObject);
            return true;
        }
    }
}

