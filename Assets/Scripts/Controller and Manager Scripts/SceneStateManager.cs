using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneStateManager : MonoBehaviour
{
    public static SceneStateManager Instance { get; private set; }

    public enum SceneStates
    {
        LevelSelector,
        Level
    }

    public SceneStates CurrentSceneState = SceneStates.LevelSelector;

    private bool canActivateScene = false;

    private void Awake()
    {
        //TODO; don't hard code this, change when making settings
        Application.targetFrameRate = 60;

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

    private void SetupNewGame()
    {
        // TODO; load scene logic

        // load scene with GameStateManager already in scene
    }

    private void SetupLoadGame()
    {
        // TODO; load scene logic
        if (GameDataManager.LoadedGameDataExists)
        {
            Debug.Log("Loaded game data exists.");
        }
        // load scene with GameStateManager already in scene
    }

    public void GoToMainMenu(Action onFinish = null)
    {
        if (CurrentSceneState != SceneStates.LevelSelector)
        {
            CurrentSceneState = SceneStates.LevelSelector;
            StartCoroutine(LoadSceneAsync(nameof(SceneStates.LevelSelector), false, onFinish));
            return;
        }
    }

    public void GoToLevel(Action onFinish = null)
    {
        CurrentSceneState = SceneStates.Level;
        StartCoroutine(LoadSceneAsync(nameof(SceneStates.Level), false, onFinish));
    }

    public void ActivateScene()
    {
        canActivateScene = true;
    }

    public IEnumerator LoadSceneAsync(string sceneName, bool disableSceneUntilLoaded, Action onFinish = null)
    {
        GameStateManager.Instance.CurrentGameState = GameStateManager.GameStates.Loading;

        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);

        if (disableSceneUntilLoaded)
        {
            asyncOperation.allowSceneActivation = false;
        }

        while (!asyncOperation.isDone)
        {
            // Optionally perform actions while loading, e.g., update a loading screen
            // Debug.Log($"Loading progress: {asyncOperation.progress * 100}%");

            if (disableSceneUntilLoaded && asyncOperation.progress >= 0.9f && !asyncOperation.allowSceneActivation)
            {
                if (canActivateScene)
                {
                    asyncOperation.allowSceneActivation = true;
                }
            }

            yield return null;
        }

        onFinish?.Invoke();
    }
}
