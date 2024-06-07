using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public List<GameObject> worldLevelObjects = new List<GameObject>();

    private List<Level> worldLevels = new List<Level>();


    private void Awake()
    {
        foreach (GameObject levelObject in worldLevelObjects)
        {
            worldLevels.Add(levelObject.GetComponent<Level>());
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && GameStateManager.Instance.CurrentGameState == GameStateManager.GameStates.WorldLevelSelector)
        {
            GameObject hitObject = Targeting.RaycastObject(GameStats.UsePixelation, Camera.main);

            HandleClickedLevel(hitObject);
        }
    }

    public void ToggleWorldLevelObjects()
    {
        foreach (GameObject levelObject in worldLevelObjects)
        {
            levelObject.SetActive(!levelObject.activeSelf);
        }
    }

    private void HandleClickedLevel(GameObject hitObject)
    {
        if (hitObject == null)
        {
            return;
        }

        foreach (Level level in worldLevels)
        {
            if (hitObject == level.gameObject)
            {
                LevelSelectorController.Instance.SetLevelData(level.LevelData);
                LevelSelectorController.Instance.SetHoleData(level.LevelData.HoleData);
                LevelSelectorController.Instance.SetShapeData(level.LevelData.ShapeDataList);

                SceneStateManager.Instance.GoToLevel(() => LevelSelectorController.Instance.InstantiateLevelController());
                break;
            }
        }
    }
}
