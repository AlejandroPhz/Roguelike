using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Roguelike2D
{
    public class GameManager : MonoBehaviour
    {
        private const int SaveFileVersion = 1;
    
        public static GameManager Instance { get; private set; }
    
        public BoardManager Board;
        public CinemachineCamera Camera;

        public UIDocument UIDoc;
    
        public WorldSettings WorldSettings;

        public PlayerController Player => m_Player;
        public CinemachineConfiner2D CameraConfiner => m_CameraConfiner;
        public TurnManager TurnManager { get; private set;}
        public MovingObjectSystem MovingObjectSystem { get; private set; }
        public ReferenceDatabase ReferenceDatabase { get; private set; }

        public int CurrentLevel => m_CurrentLevel;

        private int m_CurrentLevel = 1;
        private int m_FoodAmount = 100;

        public AudioMixer AudioMixer;
        public AudioSource SFXSourcePrefab;

        private PlayerController m_Player;

        private Label m_DayLabel;
        private Label m_FoodLabel;
        private Label m_PlayerDamageLabel;
        private Label m_PlayerDefenseLabel;
        private Label m_PlayerSpeedLabel;
    
        private VisualElement m_GameOverPanel;
        private Label m_GameOverMessage;
        private CinemachineConfiner2D m_CameraConfiner;

        private VisualElement m_PauseMenuBackground;
        private Button m_PauseButton;
        private Button m_SaveExitButton;
        private Slider m_MainAudioSlider;
        private Slider m_MusicAudioSlider;
        private Slider m_SFXAudioSlider;
    
        private Queue<AudioSource> m_AudioSourcePool = new Queue<AudioSource>();
    
        private bool m_Pause = false;
        private bool m_NewLevelRequest = false;

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
        
            Instance = this;

            if (CrossSceneDataStore.Instance != null)
            {
                WorldSettings = CrossSceneDataStore.Instance.SelectedWordSettings;
            }
        }
    
        void Start()
        {
            TurnManager = new TurnManager();
            TurnManager.OnTick += OnTurnHappen;

            MovingObjectSystem = new MovingObjectSystem();
            ReferenceDatabase = new ReferenceDatabase();
        
            m_FoodAmount = 30;
            m_CurrentLevel = 1;

            m_DayLabel = UIDoc.rootVisualElement.Q<Label>("DayLabel");
            m_DayLabel.text = "Day 0";
        
            m_FoodLabel = UIDoc.rootVisualElement.Q<Label>("FoodLabel");
            m_FoodLabel.text = m_FoodAmount.ToString();
        
            m_PlayerDamageLabel = UIDoc.rootVisualElement.Q<Label>("DamageLabel");
            m_PlayerDefenseLabel = UIDoc.rootVisualElement.Q<Label>("DefenseLabel");
            m_PlayerSpeedLabel = UIDoc.rootVisualElement.Q<Label>("SpeedLabel");

            m_GameOverPanel = UIDoc.rootVisualElement.Q<VisualElement>("GameOverPanel");
            m_GameOverMessage = m_GameOverPanel.Q<Label>("GameOverMessage");

            m_CameraConfiner = Camera.GetComponent<CinemachineConfiner2D>();

            m_PauseMenuBackground = UIDoc.rootVisualElement.Q<VisualElement>("MenuOverlay");
            m_SaveExitButton = m_PauseMenuBackground.Q<Button>("SaveQuit");
            m_SaveExitButton.clicked += SaveAndExit;
        
            m_PauseButton = UIDoc.rootVisualElement.Q<Button>("MenuButton");
            m_PauseButton.clicked += () => { Pause(!m_Pause); };

            int audioSourcePoolSize = 16;
            for (int i = 0; i < audioSourcePoolSize; ++i)
            {
                m_AudioSourcePool.Enqueue(Instantiate(SFXSourcePrefab));
            }
        
            m_MainAudioSlider = UIDoc.rootVisualElement.Q<Slider>("MainAudioSlider");
            m_MusicAudioSlider = UIDoc.rootVisualElement.Q<Slider>("MusicAudioSlider");
            m_SFXAudioSlider = UIDoc.rootVisualElement.Q<Slider>("SFXAudioSlider");

            m_MainAudioSlider.RegisterValueChangedCallback(evt => { AudioMixer.SetFloat("MainVolume", NormalizedToDB(evt.newValue/100.0f)); });
            m_MusicAudioSlider.RegisterValueChangedCallback(evt => { AudioMixer.SetFloat("MusicVolume", NormalizedToDB(evt.newValue/100.0f)); });
            m_SFXAudioSlider.RegisterValueChangedCallback(evt => { AudioMixer.SetFloat("SFXVolume", NormalizedToDB(evt.newValue/100.0f)); });

            UpdateAudioSliders();

            StartOrLoadNewGame();
        }

        private void Update()
        {
            MovingObjectSystem.Update();
        
            if (m_NewLevelRequest)
            {
                m_NewLevelRequest = false;
                StartNextLevel();
            }
        }

        [DllImport("__Internal")]
        public static extern void JS_FileSystem_Sync();

        public void StartOrLoadNewGame()
        {
            //when reloading a game after a game over, we need to make sure the old player was destroyed
            if (m_Player != null)
                Destroy(m_Player.gameObject);
        
            m_GameOverPanel.style.visibility = Visibility.Hidden;
            m_PauseMenuBackground.style.visibility = Visibility.Hidden;
        
            m_CurrentLevel = 0;
            m_FoodAmount = 20;

            Random.InitState((int)DateTime.Now.Ticks);
            var state = Random.state;

            // if there is a save file, load and delete it (this is a roguelike, save file are only used to suspend a game)
            string savefile = Application.persistentDataPath + "/savefile.save";
            if (File.Exists(savefile))
            {
                using (BinaryReader reader = new BinaryReader(new FileStream(savefile, FileMode.Open)))
                {
                    int saveVersion = reader.ReadInt32();

                    string worldName = reader.ReadString();
                    if (WorldSettings.WorldName != worldName)
                    {
                        //In normal game you have to go through main menu which will set the right world from the save file
                        //In editor you could start the scene which will load a save directly, then use editor code to find
                        //the right world settings from assets
                        // If this return false, worldName wasn't found so start a new game
                        if (!HandleMismatchedWorldName(worldName))
                        {
                        
                            reader.Close();
                            File.Delete(savefile);
#if UNITY_WEBGL && !UNITY_EDITOR
                        //On WebGl, file system need to explicitly be synced so the database is updated
                        JS_FileSystem_Sync();
#endif
                            InitNewGame();
                            return;
                        }
                    }
                
                    WorldSettings.RegisterObjectLookup();
                    CreatePlayer();
                
                    m_CurrentLevel = reader.ReadInt32();
                    m_FoodAmount = reader.ReadInt32();

                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    state = (Random.State)binaryFormatter.Deserialize(reader.BaseStream);
                    Random.state = state;
                
                    Board.Load(reader);
                    Player.Load(reader);
                
                    Player.Spawn(Board, Player.Cell);
                    m_FoodLabel.text = "Food : " + m_FoodAmount;
                }
            
                File.Delete(savefile);
#if UNITY_WEBGL && !UNITY_EDITOR
            //On WebGl, file system need to explicitly be synced so the database is updated
            JS_FileSystem_Sync();
#endif
            }
            else
            {
                InitNewGame();
            }
        }

        void InitNewGame()
        {
            WorldSettings.RegisterObjectLookup();
            CreatePlayer();
            StartNextLevel();
            m_FoodLabel.text = "Food : " + m_FoodAmount;
        }

        private void OnDestroy()
        {
            TurnManager.OnTick -= OnTurnHappen;
        }

        void CreatePlayer()
        {
            m_Player = Instantiate(WorldSettings.Theme.PlayerPrefab); 
            Camera.Follow = m_Player.transform;
        
            Player.Init();
            UpdatePlayerStats();
        }

        void OnTurnHappen()
        {
            //we now call the internal change food function instead of changing directly
            ChangeFood(-1);
        }

        //create a private function that take care of changing food
        public void ChangeFood(int amount)
        {
            m_FoodAmount += amount;
            m_FoodLabel.text = m_FoodAmount.ToString();
        
            if (m_FoodAmount <= 0)
            {
                Player.GameOver();
            
                m_GameOverMessage.text = "Game Over\n\n" 
                                         + "You have survived " + m_CurrentLevel + " days\n\n"
                                         + "Press Enter to Restart";
                m_GameOverPanel.style.visibility = Visibility.Visible;
            }
        }

        //Called when player press an input, will make sure to fast forward all pending animation and tick a turn
        public void PlayerInput()
        {
            MovingObjectSystem.FlushAll();
        }
    
        public void RequestNextLevel()
        {
            m_NewLevelRequest = true;
        }

        void StartNextLevel()
        {
            Board.Clean();
            Board.Init();
            Player.Spawn(Board, new Vector2Int(1,1));
        
            m_CurrentLevel++;
            m_DayLabel.text = $"Day {m_CurrentLevel}";
        }

        public void UpdatePlayerStats()
        {
            m_PlayerDamageLabel.text = Player.PlayerAttack.ToString();
            m_PlayerDefenseLabel.text = Player.PlayerDefense.ToString();
            m_PlayerSpeedLabel.text = Player.PlayerSpeed.ToString();
        }

        void Pause(bool pause)
        {
            if (pause)
            {
                Player.Pause();
                m_PauseMenuBackground.style.visibility = Visibility.Visible;
            }
            else
            {
                Player.Unpause();
                m_PauseMenuBackground.style.visibility = Visibility.Hidden;
            }
        
            m_Pause = pause;
        }

        void SaveAndExit()
        {
            string savefile = Application.persistentDataPath + "/savefile.save";
            using (BinaryWriter writer = new BinaryWriter(new FileStream(savefile, FileMode.Create)))
            {
                //set a version to save file, used for retro compatibility
                writer.Write(SaveFileVersion);
        
                writer.Write(WorldSettings.WorldName);
                writer.Write(m_CurrentLevel);
                writer.Write(m_FoodAmount);
            
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(writer.BaseStream, Random.state);
            
                Board.Save(writer);
                Player.Save(writer);
            }
        
#if UNITY_WEBGL && !UNITY_EDITOR
            JS_FileSystem_Sync();
#endif

            SceneManager.LoadScene(0, LoadSceneMode.Single);
        }

        // This will look for the world named worldName in the Assets if in the editor, and set the worldsetting to the right
        // one. If the setting isn't found or this is a build, it start a new game and delete the save file.
        // if this return false, caller will exit as the world wasn't found, if it return true, it can go on with loading
        bool HandleMismatchedWorldName(string worldName)
        {
        
            //in editor, look for the settings in assets
#if UNITY_EDITOR
            bool found = false;
            var allWorldSettingsGUID = AssetDatabase.FindAssets("t:WorldSettings");
            foreach (var guid in allWorldSettingsGUID)
            {
                var worldSettings =
                    AssetDatabase.LoadAssetAtPath<WorldSettings>(AssetDatabase.GUIDToAssetPath(guid));
            
                if (worldSettings.WorldName == worldName)
                {
                    WorldSettings = worldSettings;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
#endif
                //in build just output error and return false
                Debug.LogError($"World Setting named {worldName} from save file could not be found. Creating new game");
                return false;
#if UNITY_EDITOR
            }
#endif

            return true;
        }

        public void PlayAudioSFX(AudioClip clip, Vector3 position)
        {
            var source = m_AudioSourcePool.Dequeue();
            m_AudioSourcePool.Enqueue(source);

            source.transform.position = position;
            source.PlayOneShot(clip);
        }

        void UpdateAudioSliders()
        {
            float temp = 0.0f;
            AudioMixer.GetFloat("MainVolume", out temp);
            m_MainAudioSlider.SetValueWithoutNotify(DBToNormalized(temp) * 100);
            AudioMixer.GetFloat("MusicVolume", out temp);
            m_MusicAudioSlider.SetValueWithoutNotify(DBToNormalized(temp) * 100);
            AudioMixer.GetFloat("SFXVolume", out temp);
            m_SFXAudioSlider.SetValueWithoutNotify(DBToNormalized(temp) * 100);
        }

        float DBToNormalized(float DbValue)
        {
            return Mathf.Pow(10, DbValue / 10);
        }

        float NormalizedToDB(float value)
        {
            return 10 * Mathf.Log10(Mathf.Max(0.00001f, value));
        }
    }
}