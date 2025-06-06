using UnityEngine;
using UnityEngine.InputSystem;


namespace TutorialVersion
{
    public class PlayerController : MonoBehaviour
    {
        public float MoveSpeed = 5.0f;

        public Vector2Int Cell => m_CellPosition;

        private bool m_IsMoving;
        private Vector3 m_MoveTarget;
        
        private bool m_IsGameOver;

        private BoardManager m_Board;
        private Vector2Int m_CellPosition;
        
        private Animator m_Animator;

        private void Awake()
        {
            m_Animator = GetComponent<Animator>();
        }
        
        public void Spawn(BoardManager boardManager, Vector2Int cell)
        {
            m_Board = boardManager;
            MoveTo(cell, true);
        }
        
        public void Init()
        {
            m_IsGameOver = false;
        }

        public void MoveTo(Vector2Int cell, bool immediate)
        {
            m_CellPosition = cell;

            if (immediate)
            {
                m_IsMoving = false;
                transform.position = m_Board.CellToWorld(m_CellPosition);
            }
            else
            {
                m_IsMoving = true;
                m_MoveTarget = m_Board.CellToWorld(m_CellPosition);
            }
            
            m_Animator.SetBool("Moving", m_IsMoving);
        }

        public void Attack()
        {
            m_Animator.SetTrigger("Attack");
        }

        public void GameOver()
        {
            m_IsGameOver = true;
        }

        private void Update()
        {
            if (m_IsGameOver)
            {
                if (Keyboard.current.enterKey.wasPressedThisFrame)
                {
                    GameManager.Instance.StartNewGame();
                }

                return;
            }
            
            if (m_IsMoving)
            {
                transform.position = Vector3.MoveTowards(transform.position, m_MoveTarget, MoveSpeed * Time.deltaTime);
  
                if (transform.position == m_MoveTarget)
                {
                    m_IsMoving = false;
                    m_Animator.SetBool("Moving", m_IsMoving);
                    var cellData = m_Board.GetCellData(m_CellPosition);
                    if(cellData.ContainedObject != null)
                        cellData.ContainedObject.PlayerEntered();
                }

                return;
            }


            Vector2Int newCellTarget = m_CellPosition;
            bool hasMoved = false;

            if(Keyboard.current.upArrowKey.wasPressedThisFrame)
            {
                newCellTarget.y += 1;
                hasMoved = true;
            }
            else if(Keyboard.current.downArrowKey.wasPressedThisFrame)
            {
                newCellTarget.y -= 1;
                hasMoved = true;
            }
            else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
            {
                newCellTarget.x += 1;
                hasMoved = true;
            }
            else if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
            {
                newCellTarget.x -= 1;
                hasMoved = true;
            }

            if(hasMoved)
            {
                //check if the new position is passable, then move there if it is.
                BoardManager.CellData cellData = m_Board.GetCellData(newCellTarget);

                if(cellData != null && cellData.Passable)
                {
                    GameManager.Instance.TurnManager.Tick();
                    if (cellData.ContainedObject == null)
                    {
                        MoveTo(newCellTarget, false);
                    }
                    else if(cellData.ContainedObject.PlayerWantsToEnter())
                    {
                        MoveTo(newCellTarget, false);
                        //Call PlayerEntered AFTER moving the player! Otherwise not in cell yet
                        cellData.ContainedObject.PlayerEntered();
                    }
                }
            }
        }
    }
}