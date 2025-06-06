using System.IO;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Roguelike2D
{
    public class Enemy : AttackableCellObject, TurnManager.ITurnEntity
    {
        public int Health = 3;
        public int Damage = 1;
        public int Speed = 1;

        public AudioClip[] WalkSFX;
        public AudioClip[] DamagedSFX;
        public AudioClip[] AttackSFX;
    
        private int m_CurrentHealth;
        private Animator m_Animator;

        private void Awake()
        {
            GameManager.Instance.TurnManager.AddTrackEntity(this, TurnHappened);
            m_Animator = GetComponent<Animator>();
        }

        private void OnDestroy()
        {
            RemoveFromBoard();
            GameManager.Instance.TurnManager.RemovedTrackedEntity(this);
        }

        public override void Init(Vector2Int coord)
        {
            base.Init(coord);
            m_CurrentHealth = Health;
        }

        public override bool PlayerWantToEnter()
        {
            return false;
        }
    
        bool MoveTo(Vector2Int coord)
        {
            var targetCell = GameManager.Instance.Board.GetCellData(coord);

            if (targetCell == null 
                || !targetCell.Passable
                || targetCell.UniqueCellObject != null)
            {
                return false;
            }
        
            //remove enemy from current cell
            var currentCell = GameManager.Instance.Board.GetCellData(m_Cell);
            currentCell.RemoveObject(this);
        
            if(WalkSFX.Length > 0)
                GameManager.Instance.PlayAudioSFX(WalkSFX[Random.Range(0, WalkSFX.Length)], transform.position);
        
            //added right away to the next cell, as other thing will test if that cell is free right now. The movement is
            //only visual, internally the enemy is already on the new cell
            m_Cell = coord;
            targetCell.AddObject(this);

            GameManager.Instance.MovingObjectSystem.AddMoveRequest(transform,  GameManager.Instance.Board.CellToWorld(coord),
                4.0f, false, 1, isMidway =>
                {
                    m_Animator.SetBool("Moving", false);
                }, () =>
                {
                    m_Animator.SetBool("Moving", true);
                });

            return true;
        }

        int TurnHappened()
        {
            //We added a public property that return the player current cell! 
            var playerCell = GameManager.Instance.Player.Cell;

            int xDist = playerCell.x - m_Cell.x;
            int yDist = playerCell.y - m_Cell.y;

            int absXDist = Mathf.Abs(xDist);
            int absYDist = Mathf.Abs(yDist);

            if ((xDist == 0 && absYDist == 1)
                || (yDist == 0 && absXDist == 1))
            {
                //we are adjacent to the player, attack!
                GameManager.Instance.MovingObjectSystem.AddMoveRequest(transform, 
                    GameManager.Instance.Board.CellToWorld(playerCell), 5.0f, true, 1, isMidway =>
                    {
                        if(isMidway)
                            GameManager.Instance.Player.Damage(Damage);
                    },
                    () =>
                    {
                        m_Animator.SetTrigger("Attack");
                        if(AttackSFX.Length > 0)
                            GameManager.Instance.PlayAudioSFX(AttackSFX[Random.Range(0, AttackSFX.Length)], transform.position);
                    });

                return 10;
            }
        
            if (absXDist > absYDist)
            {
                if (!TryMoveInX(xDist, 1))
                {
                    //if our move was not successful (so no move and not attack)
                    //we try to move along Y
                    TryMoveInY(yDist,0);
                }
            }
            else
            {
                if (!TryMoveInY(yDist,1))
                {
                    TryMoveInX(xDist,0);
                }
            }

            return 10;
        }

        bool TryMoveInX(int xDist, int desiredDistance)
        {
            //try to get closer in x
            if (xDist > 0)
            {
                //player to our right
                return MoveTo(m_Cell + Vector2Int.right);
            }
        
            //player to our left
            return MoveTo(m_Cell + Vector2Int.left);
        }

        bool TryMoveInY(int yDist, int desiredDistance)
        {
            //try to get closer in y
            if (yDist > 0)
            {
                //player on top
                return MoveTo(m_Cell + Vector2Int.up);
            }

            //player below
            return MoveTo(m_Cell + Vector2Int.down);
        }

        public int GetTurnEnergy()
        {
            return Speed * 10;
        }

        public override void Damaged(int amount)
        {
            m_CurrentHealth -= amount;
        
            m_Animator.SetTrigger("Hit");
            if(DamagedSFX.Length > 0)
                GameManager.Instance.PlayAudioSFX(DamagedSFX[Random.Range(0, DamagedSFX.Length)], transform.position);

            if (m_CurrentHealth <= 0)
            {
                Destroy(gameObject);
            }
        }

        public override void Save(BinaryWriter writer)
        {
            writer.Write(m_CurrentHealth);
        }

        public override void Load(BinaryReader reader)
        {
            m_CurrentHealth = reader.ReadInt32();
        }
    }
}