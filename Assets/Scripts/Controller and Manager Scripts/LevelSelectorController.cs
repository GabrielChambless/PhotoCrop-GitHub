using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelSelectorController : MonoBehaviour
{
    public static LevelSelectorController Instance { get; private set; }

    [SerializeField] private GameObject worldControllerPrefab;
    [SerializeField] private GameObject levelControllerPrefab;

    public LevelData SelectedLevelData { get; private set; }
    public HoleData SelectedHoleData { get; private set; }
    public List<ShapeData> SelectedShapeData { get; private set; }

    private GameObject spawnedWorldControllerObject;
    private GameObject spawnedLevelControllerObject;

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

        //TODO; change this
        //SetHoleData(LevelDataLibrary.Instance.AllLevelData[0].HoleData);
        //SetShapeData(LevelDataLibrary.Instance.AllLevelData[0].ShapeDataList);

        //InstantiateLevelController();
    }

    public void InstantiateWorldController()
    {
        if (spawnedWorldControllerObject != null)
        {
            return;
        }

        spawnedWorldControllerObject = Instantiate(worldControllerPrefab);
    }

    public void InstantiateLevelController()
    {
        if (spawnedLevelControllerObject != null)
        {
            return;
        }

        spawnedLevelControllerObject = Instantiate(levelControllerPrefab);
    }

    public void SetLevelData(LevelData levelData)
    {
        SelectedLevelData = levelData;
    }

    public void SetHoleData(HoleData holeData)
    {
        SelectedHoleData = holeData;
    }

    public void SetShapeData(List<ShapeData> shapeData)
    {
        SelectedShapeData = shapeData;
    }
}
