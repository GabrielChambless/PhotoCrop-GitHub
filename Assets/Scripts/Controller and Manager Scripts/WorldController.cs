using UnityEngine;

public class WorldController : MonoBehaviour
{
    [SerializeField] private GameObject world1Prefab;
    [SerializeField] private GameObject world2Prefab;

    private World world1;
    private World world2;

    private World lastClickedWorld;

    private void Awake()
    {
        GameStateManager.Instance.CurrentGameState = GameStateManager.GameStates.WorldSelector;
        InstantiateWorlds();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && GameStateManager.Instance.CurrentGameState == GameStateManager.GameStates.WorldSelector)
        {
            GameObject hitObject = Targeting.RaycastObject(GameStats.UsePixelation, Camera.main);

            HandleClickedWorld(hitObject);
        }

        if (Input.GetKeyDown(KeyCode.Escape) && GameStateManager.Instance.CurrentGameState == GameStateManager.GameStates.WorldLevelSelector)
        {
            GameStateManager.Instance.CurrentGameState = GameStateManager.GameStates.WorldSelector;
            lastClickedWorld.ToggleWorldLevelObjects();

        }
    }

    private void HandleClickedWorld(GameObject hitObject)
    {
        if (hitObject == null)
        {
            return;
        }

        switch (hitObject)
        {
            case GameObject value when value == world1.gameObject:
                if (GameStateManager.Instance.CurrentGameState != GameStateManager.GameStates.WorldLevelSelector)
                {
                    GameStateManager.Instance.CurrentGameState = GameStateManager.GameStates.WorldLevelSelector;
                    lastClickedWorld = world1;
                    world1.ToggleWorldLevelObjects();
                }
                break;
            case GameObject value when value == world2.gameObject:
                if (GameStateManager.Instance.CurrentGameState != GameStateManager.GameStates.WorldLevelSelector)
                {
                    GameStateManager.Instance.CurrentGameState = GameStateManager.GameStates.WorldLevelSelector;
                    lastClickedWorld = world2;
                    world2.ToggleWorldLevelObjects();
                }
                break;
        }
    }

    private void InstantiateWorlds()
    {
        GameObject worldObject;

        if (GameStats.World1Unlocked)
        {
            worldObject = Instantiate(world1Prefab);
            world1 = worldObject.GetComponent<World>();
        }
        if (GameStats.World2Unlocked)
        {
            worldObject = Instantiate(world2Prefab);
            world2 = worldObject.GetComponent<World>();
        }
    }
}
