using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using FMOD.Studio;
using FMODUnity;

namespace Roguelike2D
{
    public class PlayerController : MonoBehaviour, TurnManager.ITurnEntity
    {
        public float MoveSpeed = 5.0f;
        public InputActionAsset InputActionAsset;

        public int StartAttack = 1;
        public int StartDefense = 0;
        public int StartSpeed = 1;    
    
        public int PlayerAttack => m_CurrentAttack;
        public int PlayerDefense => m_CurrentDefense;
        public int PlayerSpeed => m_CurrentSpeed;
    
        public Vector2Int Cell => m_CellPosition;

        [Header("Audio")] 
        public AudioClip[] WalkingSFX;
        public AudioClip[] AttackSFX;
        public AudioClip[] DamageSFX;

        public EventReference PlayerWalking;
        public EventReference PlayerAttacking;
        public EventReference PlayerDamaged;
        public EventReference PlayerDeath;
        public EventReference GameOverSound;
    
        private BoardManager m_Board;
        private Vector2Int m_CellPosition;

        private bool m_IsPaused;
        private bool m_IsGameOver;

        private bool m_ControlLocked;

        private int m_CurrentAttack;
        private int m_CurrentDefense;
        private int m_CurrentSpeed;

        private SpriteRenderer m_SpriteRenderer;
        private Animator m_Animator;

        // Movement input actions
        private InputAction m_MoveUpAction;
        private InputAction m_MoveRightAction;
        private InputAction m_MoveDownAction;
        private InputAction m_MoveLeftAction;
        private InputAction m_WaitAction;

        // Movement cooldown
        private float m_MoveCooldown = 0.36f;
        private float m_NextMoveTime = 0f;

        private void Awake()
        {
            m_Animator = GetComponent<Animator>();

            m_MoveUpAction = InputActionAsset.FindAction("Gameplay/MoveUp");
            m_MoveUpAction.Enable();
            m_MoveRightAction = InputActionAsset.FindAction("Gameplay/MoveRight");
            m_MoveRightAction.Enable();
            m_MoveDownAction = InputActionAsset.FindAction("Gameplay/MoveDown");
            m_MoveDownAction.Enable();
            m_MoveLeftAction = InputActionAsset.FindAction("Gameplay/MoveLeft");
            m_MoveLeftAction.Enable();

            m_WaitAction = InputActionAsset.FindAction("Gameplay/Wait");
            m_WaitAction.Enable();

            m_SpriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Attacking(AttackableCellObject target)
        {
            var targetObject = target as MonoBehaviour;

            GameManager.Instance.MovingObjectSystem.AddMoveRequest(transform, targetObject.transform.position,
                MoveSpeed, true, 0, isMidway =>
                {
                    if (isMidway)
                    {
                        target.Damaged(m_CurrentAttack);
                        if (AttackSFX.Length != 0)
                            GameManager.Instance.PlayAudioSFX(AttackSFX[Random.Range(0, AttackSFX.Length)], transform.position);
                    }
                });

            m_Animator.SetTrigger("Attack");
            RuntimeManager.PlayOneShot(PlayerAttacking);
        }

        public void Damage(int damageAmount)
        {
            GameManager.Instance.ChangeFood(-damageAmount);

            if (GameManager.Instance.m_FoodAmount > 0)
                RuntimeManager.PlayOneShot(PlayerDamaged);

            if (GameManager.Instance.m_FoodAmount == 0)
                RuntimeManager.PlayOneShot(PlayerDeath);

            m_Animator.SetTrigger("Hit");

            if (DamageSFX.Length != 0)
                GameManager.Instance.PlayAudioSFX(DamageSFX[Random.Range(0, DamageSFX.Length)], transform.position);
        }

        public void Spawn(BoardManager board, Vector2Int cell)
        {
            m_Board = board;
            MoveTo(cell, true);
        }

        public void MoveTo(Vector2Int cell, bool immediateMove)
        {
            m_CellPosition = cell;

            if (immediateMove)
            {
                transform.position = m_Board.CellToWorld(m_CellPosition);
                m_ControlLocked = false;
            }
            else
            {
                RuntimeManager.PlayOneShot(PlayerWalking);

                GameManager.Instance.MovingObjectSystem.AddMoveRequest(transform, m_Board.CellToWorld(m_CellPosition),
                    MoveSpeed, false, 0, isMidway =>
                    {
                        m_ControlLocked = false;
                        m_Animator.SetBool("Moving", false);

                        var cellData = m_Board.GetCellData(m_CellPosition);
                        cellData.PlayerEntered();
                    });

                if (WalkingSFX.Length != 0)
                    GameManager.Instance.PlayAudioSFX(WalkingSFX[Random.Range(0, WalkingSFX.Length)], transform.position);

                m_ControlLocked = true;
                m_Animator.SetBool("Moving", m_ControlLocked);
            }
        }

        public void Init()
        {
            m_IsGameOver = false;
            m_ControlLocked = false;
            m_IsPaused = false;

            m_CurrentAttack = StartAttack;
            m_CurrentDefense = StartDefense;
            m_CurrentSpeed = StartSpeed;

            GameManager.Instance.UpdatePlayerStats();
            GameManager.Instance.TurnManager.RegisterPlayer(this);
        }

        public void GameOver()
        {
            RuntimeManager.PlayOneShot(GameOverSound);
            m_IsGameOver = true;
        }

        public void Pause()
        {
            m_IsPaused = true;
        }

        public void Unpause()
        {
            m_IsPaused = false;
        }

        private void Update()
        {
            if (m_IsPaused || m_IsGameOver || m_ControlLocked)
                return;

            if (Time.time < m_NextMoveTime)
                return;

            if (m_IsGameOver && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                GameManager.Instance.StartOrLoadNewGame();
                return;
            }

            if (m_WaitAction.WasPerformedThisFrame())
            {
                GameManager.Instance.TurnManager.PlayerAct(10);
                m_NextMoveTime = Time.time + m_MoveCooldown;
                return;
            }

            Vector2Int newCellTarget = m_CellPosition;
            bool hasMove = false;

            if (m_MoveUpAction.WasPressedThisFrame())
            {
                newCellTarget.y += 1;
                hasMove = true;
            }
            else if (m_MoveDownAction.WasPressedThisFrame())
            {
                newCellTarget.y -= 1;
                hasMove = true;
            }

            if (m_MoveRightAction.WasPressedThisFrame())
            {
                newCellTarget.x += 1;
                hasMove = true;
                m_SpriteRenderer.flipX = false;
            }
            else if (m_MoveLeftAction.WasPressedThisFrame())
            {
                newCellTarget.x -= 1;
                hasMove = true;
                m_SpriteRenderer.flipX = true;
            }

            if (!hasMove) return;

            var cellData = m_Board.GetCellData(newCellTarget);
            if (cellData != null && cellData.Passable)
            {
                GameManager.Instance.PlayerInput();

                if (cellData.ContainedObjects.Count == 0)
                {
                    MoveTo(newCellTarget, false);
                    GameManager.Instance.TurnManager.PlayerAct(10);
                    m_NextMoveTime = Time.time + m_MoveCooldown;
                }
                else if (cellData.HaveAttackable(out var attackable))
                {
                    Attacking(attackable);
                    GameManager.Instance.TurnManager.PlayerAct(10);
                    m_NextMoveTime = Time.time + m_MoveCooldown;
                }
                else if (cellData.PlayerWantToEnter())
                {
                    MoveTo(newCellTarget, false);
                    GameManager.Instance.TurnManager.PlayerAct(10);
                    m_NextMoveTime = Time.time + m_MoveCooldown;
                }
            }
        }

        public int GetTurnEnergy()
        {
            return m_CurrentSpeed * 10;
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(m_CellPosition.x);
            writer.Write(m_CellPosition.y);

            writer.Write(m_CurrentAttack);
            writer.Write(m_CurrentDefense);
            writer.Write(m_CurrentSpeed);
        }

        public void Load(BinaryReader reader)
        {
            m_CellPosition.x = reader.ReadInt32();
            m_CellPosition.y = reader.ReadInt32();

            m_CurrentAttack = reader.ReadInt32();
            m_CurrentDefense = reader.ReadInt32();
            m_CurrentSpeed = reader.ReadInt32();
        }
    }
}
