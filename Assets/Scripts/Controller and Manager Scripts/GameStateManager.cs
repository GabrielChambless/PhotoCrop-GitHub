using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    public enum GameStates
    {
        Loading,
        MainMenu,
        Settings,
        WorldSelector,
        WorldLevelSelector,
        Level,
        LevelPauseMenu,
        LevelRestarting,
        LevelFinish,
        LevelFinishMenu
    }

    public GameStates CurrentGameState = GameStates.MainMenu;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

}
