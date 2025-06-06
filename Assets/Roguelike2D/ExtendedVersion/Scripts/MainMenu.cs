using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

namespace Roguelike2D
{
    public class MainMenu : MonoBehaviour
    {
        private UIDocument m_UIDocument;

        public WorldSettings[] WorldSettings;
        public Tilemap BackgroundTilemap;
    
        [DllImport("__Internal")]
        public static extern void JS_FileSystem_Sync();

        private DropdownField m_WorldSettingsThemeDropdown;
        private WorldSettings m_SelectedSettings;
    
        private void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();

            var continueButton = m_UIDocument.rootVisualElement.Q<Button>("Continue");
            var newButton = m_UIDocument.rootVisualElement.Q<Button>("New");
            var quitButton = m_UIDocument.rootVisualElement.Q<Button>("Quit");

            m_WorldSettingsThemeDropdown = m_UIDocument.rootVisualElement.Q<DropdownField>("ThemeDropdown");

            continueButton.clicked += LoadGame;
            newButton.clicked += StartNewGame;
            quitButton.clicked += Application.Quit;
        
            continueButton.SetEnabled(File.Exists(Application.persistentDataPath+"/savefile.save"));

            m_SelectedSettings = WorldSettings[0];

            var worldNames = WorldSettings.Select(settings => settings.WorldName).ToList();

            m_WorldSettingsThemeDropdown.choices = worldNames;
        
            m_WorldSettingsThemeDropdown.RegisterValueChangedCallback(evt =>
            {
                m_SelectedSettings = WorldSettings[m_WorldSettingsThemeDropdown.index];
                FillBackgroundFromTheme();
            });
        
            m_WorldSettingsThemeDropdown.index = 0;
        }

        void FillBackgroundFromTheme()
        {
            for (int y = -50; y < 50; ++y)
            {
                for (int x = -50; x < 50; ++x)
                {
                    BackgroundTilemap.SetTile(new Vector3Int(x,y,0), m_SelectedSettings.Theme.GetRandomGround());
                }
            }
        }

        void StartNewGame()
        {
            DeleteSaveFile();

            CrossSceneDataStore.CreateNewInstance();
            CrossSceneDataStore.Instance.SelectedWordSettings = m_SelectedSettings;
            SceneManager.LoadScene(1, LoadSceneMode.Single);
        }

        void LoadGame()
        {
            WorldSettings foundSettings = null;
        
            //retrieve the world settings
            using (BinaryReader reader = new BinaryReader(new FileStream(Application.persistentDataPath + "/savefile.save", FileMode.Open)))
            {
                //jump over save file version
                reader.ReadInt32();
                string worldName = reader.ReadString();

            
                foreach (var setting in WorldSettings)
                {
                    if (setting.WorldName == worldName)
                    {
                        foundSettings = setting;
                        break;
                    }
                }

                if (foundSettings == null)
                {
                    //something went wrong, the world setting from the save file cannot be found, so we just delete the save
                    //file and go with the currently selected one
                    reader.Close();
                    DeleteSaveFile();
                    foundSettings = m_SelectedSettings;
                }
            }

            CrossSceneDataStore.CreateNewInstance();
            CrossSceneDataStore.Instance.SelectedWordSettings = foundSettings;
            SceneManager.LoadScene(1, LoadSceneMode.Single);
        }

        void DeleteSaveFile()
        {
            string savefile = Application.persistentDataPath + "/savefile.save";
            if (File.Exists(savefile))
            {
                File.Delete(savefile);
#if UNITY_WEBGL && !UNITY_EDITOR
            JS_FileSystem_Sync();
#endif
            }
        }
    }
}