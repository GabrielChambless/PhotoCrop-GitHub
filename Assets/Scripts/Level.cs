using UnityEngine;

public class Level : MonoBehaviour
{
    [SerializeField] private LevelData levelData;

    public LevelData LevelData => levelData;
}
