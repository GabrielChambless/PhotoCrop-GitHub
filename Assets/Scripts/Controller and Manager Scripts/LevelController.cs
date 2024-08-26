using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    public static LevelController Instance { get; private set; }

    [SerializeField] private GameObject cropControllerPrefab;
    [SerializeField] private GameObject tickManagerPrefab;
    [SerializeField] private GameObject levelGridPrefab;
    [SerializeField] private GameObject fogPlanePrefab;

    [SerializeField] private float moveSpeed = 0.5f;
    [SerializeField] private float snapThreshold = 0.2f;
    [SerializeField] private float bounceIntensity = 0.3f;
    [SerializeField] private float placeSpeed = 0.7f;
    [SerializeField] private float placeBounceIntensity = 0.4f;
    [SerializeField] private float rotateBounceIntensity = 0.1f;

    public bool IsSettingUpLevel;
    public bool LevelIsCompleted;

    public Hole CurrentHole { get; private set; }
    public List<CellEntity> CellEntities { get; private set; } = new List<CellEntity>();
    private Shape currentShape;
    private List<Shape> availableShapes = new List<Shape>();

    private GameObject levelBoardObject;
    private GameObject fogPlaneObject;
    private GameObject levelGridObject;

    private GameObject currentHoleObject;
    private GameObject currentShapeObject;
    private List<GameObject> availableShapeObjects = new List<GameObject>();
    private int currentShapeIndex;

    //List<List<HoleCell>> groupsOfCellType = new List<List<HoleCell>>();

    private CameraController.CameraAngles currentAnglePreference;
    private Vector3 targetPosition;

    private bool isMovingCurrentShape;
    private bool isBouncing;
    private bool inCoroutine;

    private int numberOfShapesPlaced;
    private int numberOfShapesCropped;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        IsSettingUpLevel = true;
        GameStateManager.Instance.CurrentGameState = GameStateManager.GameStates.Level;
        StartCoroutine(SetLevel());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) && !IsSettingUpLevel)
        {
            currentAnglePreference = CameraController.CameraAngles.Angle1;
            CameraController.Instance.MoveToCameraAngle(CameraController.CameraAngles.Angle1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) && !IsSettingUpLevel)
        {
            currentAnglePreference = CameraController.CameraAngles.Angle2;
            CameraController.Instance.MoveToCameraAngle(CameraController.CameraAngles.Angle2);
        }

        if (GameStateManager.Instance.CurrentGameState != GameStateManager.GameStates.Level)
        {
            return;
        }

        Targeting.CurrentMouseGridPosition = Targeting.GetMouseGridPosition(GameStats.UsePixelation, Camera.main, LayerMask.GetMask("LevelPlane"));

        if (!CropController.CropIsActive && isMovingCurrentShape && currentShapeObject != null && !inCoroutine)
        {
            float clampedX = Mathf.Clamp(Targeting.CurrentMouseGridPosition.x, 0, CurrentHole.GridSize.x - 1);
            float clampedY = Mathf.Clamp(Targeting.CurrentMouseGridPosition.y, 0, CurrentHole.GridSize.y - 1);

            Vector3 targetPosition = new Vector3(clampedX, clampedY, -1f);

            MoveAndSnap(currentShapeObject.transform, targetPosition);
        }

        if (Input.GetKeyDown(KeyCode.C) && currentShapeObject != null && !inCoroutine && !CropController.InCoroutine)
        {
            CropController.Instance.SetCropDimensions(currentShape.Dimensions);

            isMovingCurrentShape = !isMovingCurrentShape;

            float clampedX = Mathf.Clamp(Targeting.CurrentMouseGridPosition.x, 0, CurrentHole.GridSize.x - 1);
            float clampedY = Mathf.Clamp(Targeting.CurrentMouseGridPosition.y, 0, CurrentHole.GridSize.y - 1);

            currentShapeObject.transform.position = new Vector3(clampedX, clampedY, -1f);

            CropController.CropIsActive = !CropController.CropIsActive;

            CropController.Instance.ToggleCropController();
            CropController.Instance.transform.position = currentShapeObject.transform.position - (Vector3.forward * 0.51f);

            if (CropController.CropIsActive)
            {
                CameraController.Instance.MoveToCameraAngle(CameraController.CameraAngles.Angle2);
            }
            else //if (currentAnglePreference != CameraController.CameraAngles.Angle2)
            {
                //CameraController.Instance.MoveToCameraAngle(CameraController.CameraAngles.Angle1);
                CameraController.Instance.MoveToCameraAngle(currentAnglePreference);
            }
        }
        else if (Input.GetKeyDown(KeyCode.X) && CropController.CropIsActive && !isMovingCurrentShape && currentShapeObject != null && !inCoroutine && !CropController.InCoroutine)
        {
            numberOfShapesCropped++;

            CropController.Instance.UpdateCropLayout();

            CropController.Instance.CropShape(currentShape, CurrentHole);

            bool completelyEmpty = true;

            foreach (ShapeCell cell in currentShape.ShapeLayout)
            {
                if (cell.CellContentType != GameStats.CellContentTypes.Empty)
                {
                    completelyEmpty = false;
                }
            }

            // TODO; probably need to not allow shapes to be completely empty
            if (completelyEmpty)
            {
                Debug.Log("Cropped shape is empty");
                currentShapeObject.transform.SetParent(currentHoleObject.transform);

                ShapeSelectionController.Instance.RemoveShape(currentShape);

                availableShapeObjects.RemoveAt(currentShapeIndex);
                availableShapes.RemoveAt(currentShapeIndex);

                currentShape = null;
                currentShapeObject = null;

                if (availableShapes.Count > 0)
                {
                    if (currentShapeIndex >= availableShapeObjects.Count)
                    {
                        currentShapeIndex = 0;
                    }

                    currentShapeObject = availableShapeObjects[currentShapeIndex];
                    currentShape = availableShapes[currentShapeIndex];

                    ShapeSelectionController.Instance.StartLerp(currentShape, false);
                }
            }

            isMovingCurrentShape = !isMovingCurrentShape;
            CropController.CropIsActive = !CropController.CropIsActive;

            CropController.Instance.ToggleCropController();
            CropController.Instance.transform.position = currentShapeObject.transform.position - (Vector3.forward * 0.51f);

            //if (currentAnglePreference != CameraController.CameraAngles.Angle2)
            //{
                StartCoroutine(WaitToMoveCameraForCroppingPlaceShape());
            //}

            if (availableShapeObjects.Count == 0 || Hole.GridIsCompletelyFilled(CurrentHole))
            {
                if (currentShapeObject != null)
                {
                    currentShapeObject.SetActive(false);
                }

                StartCoroutine(PerformTickActionsBeforeFinishLevel());
            }
        }

        if (Input.GetKeyDown(KeyCode.R) && (isMovingCurrentShape || CropController.CropIsActive) && currentShapeObject != null && !inCoroutine)
        {
            Shape.RotateClockwise(currentShape);

            AlignRotation(currentShapeObject.transform);

            StartCoroutine(RotateSmoothly(currentShapeObject.transform, -90f, 0.15f));
        }


        CycleThroughAvailableShapes();


        if (Input.GetMouseButtonDown(0) && isMovingCurrentShape && !CropController.CropIsActive && currentShapeObject != null && !inCoroutine)
        {
            if (Shape.CanPlaceShape(CurrentHole, currentShape, Targeting.CurrentMouseGridPosition))
            {
                isMovingCurrentShape = false;

                numberOfShapesPlaced++;

                ShapeSelectionController.Instance.RemoveShape(currentShape);

                StartCoroutine(PlaceShapeCoroutine(currentShapeObject.transform, new Vector3(Targeting.CurrentMouseGridPosition.x, Targeting.CurrentMouseGridPosition.y, 0f)));

                //groupsOfCellType = Hole.GroupCells(currentHole);
            }
            else
            {
                Debug.Log("Can NOT place shape!");
            }
        }
    }

    public void AddToCellEntities(CellEntity cellEntity)
    {
        if (CellEntities.Contains(cellEntity))
        {
            Debug.LogWarning("The cell entities list already contains this entity; returned early.");
            return;
        }

        CellEntities.Add(cellEntity);
    }

    public void RestartLevel()
    {
        if (IsSettingUpLevel)
        {
            return;
        }

        if (currentShapeObject != null)
        {
            currentShapeObject.SetActive(false);
        }

        levelGridObject.SetActive(false);
        ShapeSelectionController.Instance.TogglePhotoObjects();

        if (levelBoardObject != null)
        {
            levelBoardObject.transform.SetParent(currentHoleObject.transform);
        }

        foreach (CellEntity entity in CellEntities)
        {
            if (entity.EntityObject != null)
            {
                entity.EntityObject.transform.SetParent(currentHoleObject.transform);
            }
        }

        GameStateManager.Instance.CurrentGameState = GameStateManager.GameStates.LevelRestarting;

        currentAnglePreference = CameraController.CameraAngles.Angle1;
        CameraController.Instance.MoveToCameraAngle(CameraController.CameraAngles.Angle1);

        StartCoroutine(AnimationModels.RestartLevel(currentHoleObject, 1f));
    }

    private IEnumerator PerformTickActionsBeforeFinishLevel()
    {
        if (TickManager.Instance.TickCoroutine != null)
        {
            Debug.Log("TickRoutine has already started; returned early.");
            yield break;
        }

        foreach (CellEntity entity in CellEntities)
        {
            TickActionModels.AssignEntityTickAction(entity);
            TickManager.Instance.Subscribe(entity, entity.TickAction, entity.TickOrder, entity.TickType);
        }

        TickManager.Instance.StartTicking();

        while (TickManager.Instance.TickCoroutine != null)
        {
            yield return null;
        }

        FinishLevel();
    }

    private void FinishLevel()
    {
        bool atLeastOneGoalWasCompleted = false;
        bool levelIsCompleted = false;

        foreach (LevelGoal goal in LevelSelectorController.Instance.SelectedLevelData.LevelGoals)
        {
            if (goal.IsCompleted)
            {
                Debug.Log("This level goal was already completed.");
                levelIsCompleted = true;
                continue;
            }

            switch (goal.GoalType)
            {
                case LevelGoal.GoalTypes.FillEntireGrid:
                    goal.IsCompleted = Hole.GridIsCompletelyFilled(CurrentHole);
                    atLeastOneGoalWasCompleted = goal.IsCompleted;
                    break;
                case LevelGoal.GoalTypes.WithinShapeLimit:
                    goal.IsCompleted = goal.CheckIfWithinShapeLimit(numberOfShapesPlaced);
                    atLeastOneGoalWasCompleted = goal.IsCompleted;
                    break;
                case LevelGoal.GoalTypes.WithinCropLimit:
                    goal.IsCompleted = goal.CheckIfWithinCropLimit(numberOfShapesCropped);
                    atLeastOneGoalWasCompleted = goal.IsCompleted;
                    break;
            }

            Debug.Log($"{goal.GoalType} completed = {goal.IsCompleted}");
        }

        if (atLeastOneGoalWasCompleted)
        {
            GameStats.LevelsFinishedThisSession.Add(LevelSelectorController.Instance.SelectedLevelData);
        }

        if (levelIsCompleted || atLeastOneGoalWasCompleted)
        {
            GameStateManager.Instance.CurrentGameState = GameStateManager.GameStates.LevelFinish;
            LevelIsCompleted = true;
        }
    }

    private IEnumerator SetLevel()
    {
        SetFogPlane(LevelSelectorController.Instance.SelectedLevelData.World);

        yield return SetLevelBoard();

        yield return SetHole();

        SetShapes();

        yield return SetLevelGrid();

        if (CropController.Instance == null)
        {
            Instantiate(cropControllerPrefab);
        }

        if (TickManager.Instance == null)
        {
            Instantiate(tickManagerPrefab);
        }

        if (ShapeSelectionController.Instance != null)
        {
            ShapeSelectionController.Instance.SetDictionary(availableShapes);
            ShapeSelectionController.Instance.StartLerp(currentShape, false);
        }

        isMovingCurrentShape = true;
        IsSettingUpLevel = false;
    }

    private void SetFogPlane(LevelData.WorldType worldType)
    {
        float zPosition = 0f;

        switch (worldType)
        {
            case LevelData.WorldType.Bricks:
                zPosition = 0f;
                break;
            case LevelData.WorldType.Chess:
                zPosition = 0.85f;
                break;
        }

        fogPlaneObject = Instantiate(fogPlanePrefab);

        fogPlaneObject.transform.position = new Vector3(fogPlaneObject.transform.position.x, fogPlaneObject.transform.position.y, zPosition);
    }

    private IEnumerator SetLevelBoard()
    {
        if (LevelSelectorController.Instance.SelectedLevelData.LevelBoardObject == null)
        {
            Debug.LogWarning("SelectedLevelData.LevelBoardObject is null.");
            yield break;
        }

        levelBoardObject = Instantiate(LevelSelectorController.Instance.SelectedLevelData.LevelBoardObject);

        switch (LevelSelectorController.Instance.SelectedLevelData.World)
        {
            case LevelData.WorldType.Bricks:
                levelBoardObject.transform.GetChild(0).localScale = new Vector3(LevelSelectorController.Instance.SelectedHoleData.GridSize.x, LevelSelectorController.Instance.SelectedHoleData.GridSize.y, levelBoardObject.transform.GetChild(0).transform.localScale.z);
                float xOffset = LevelSelectorController.Instance.SelectedHoleData.GridSize.x % 2 == 0 ? 0.5f : 0f;
                float yOffset = LevelSelectorController.Instance.SelectedHoleData.GridSize.y % 2 == 0 ? 0.5f : 0f;
                levelBoardObject.transform.GetChild(0).position = new Vector3((LevelSelectorController.Instance.SelectedHoleData.GridSize.x / 2) - xOffset, (LevelSelectorController.Instance.SelectedHoleData.GridSize.y / 2) - yOffset, levelBoardObject.transform.GetChild(0).position.z);
                yield return StartCoroutine(AnimationModels.SetBricksBoard(levelBoardObject, 1f, 2.5f, 0.1f, 15f, this));
                break;
            case LevelData.WorldType.Chess:
                yield return StartCoroutine(AnimationModels.SetBoardAndChildren(levelBoardObject, 2.5f, 0.1f, -15f, this));
                break;
        }
    }

    private IEnumerator SetHole()
    {
        (Hole newHole, GameObject newHoleObject) = Hole.CreateHoleAndObjectFromData(LevelSelectorController.Instance.SelectedHoleData);

        newHole.HoleObject = newHoleObject;

        CurrentHole = newHole;
        currentHoleObject = newHoleObject;

        Vector3 offset = Vector3.forward * -15f;

        for (int i = 0; i < CellEntities.Count; i++)
        {
            CellEntities[i].EntityObject.transform.position += offset;
        }

        yield return AnimationModels.DropHoleCells(currentHoleObject, 2.5f, 0.1f, this);

        yield return AnimationModels.DropCellEntities(1f, 0.1f, this);
    }

    private void SetShapes()
    {
        foreach (ShapeData data in LevelSelectorController.Instance.SelectedShapeData)
        {
            (Shape newShape, GameObject newShapeObject) = Shape.CreateShapeAndObjectFromData(data);

            newShape.ShapeObject = newShapeObject;

            if (currentShape == null && currentShapeObject == null)
            {
                currentShape = newShape;
                currentShapeObject = newShapeObject;

                float xOffset = CurrentHole.GridSize.x % 2 == 0 ? 0f : 0.5f;
                float yOffset = CurrentHole.GridSize.y % 2 == 0 ? 0f : 0.5f;
                currentShapeObject.transform.position = new Vector3(CurrentHole.GridSize.x / 2f - xOffset, CurrentHole.GridSize.y / 2f - yOffset, -10f);
            }
            else
            {
                newShapeObject.transform.position = Vector3.up * 100;
            }

            availableShapes.Add(newShape);
            availableShapeObjects.Add(newShapeObject);
        }
    }

    private IEnumerator SetLevelGrid()
    {
        levelGridObject = Instantiate(levelGridPrefab);
        levelGridObject.transform.localScale = new Vector3(CurrentHole.GridSize.x, CurrentHole.GridSize.y, 1f);
        Material levelGridMaterial = levelGridObject.GetComponent<Renderer>().material;
        levelGridMaterial.SetVector("_MainTex_ST", new Vector4(CurrentHole.GridSize.x, CurrentHole.GridSize.y, 0, 0));

        float xOffset = CurrentHole.GridSize.x % 2 == 0 ? 0.5f : 0f;
        float yOffset = CurrentHole.GridSize.y % 2 == 0 ? 0.5f : 0f;
        levelGridObject.transform.position = new Vector3((CurrentHole.GridSize.x / 2) - xOffset, (CurrentHole.GridSize.y / 2) - yOffset, 0.495f);

        yield return AnimationModels.LevelGridTransparency(levelGridObject, levelGridMaterial, new Color(0.764151f, 0.6191803f, 0.4145158f), 1f);
    }

    private IEnumerator WaitToMoveCameraForCroppingPlaceShape()
    {
        inCoroutine = true;

        yield return new WaitForSeconds(0.5f);

        if (currentAnglePreference == CameraController.CameraAngles.Angle1)
        {
            CameraController.Instance.MoveToCameraAngle(CameraController.CameraAngles.Angle1);
        }
        else
        {
            CameraController.Instance.MoveToCameraAngle(CameraController.CameraAngles.Angle2);
        }

        inCoroutine = false;
    }

    private void MoveAndSnap(Transform objectTransform, Vector3 targetPos)
    {
        if (Vector3.Distance(objectTransform.position, targetPos) > snapThreshold)
        {
            objectTransform.position = Vector3.Lerp(objectTransform.position, targetPos, moveSpeed);
            isBouncing = true;
        }
        else if (isBouncing)
        {
            objectTransform.position = Vector3.Lerp(objectTransform.position, targetPos + (targetPos - objectTransform.position).normalized * bounceIntensity, moveSpeed);
            isBouncing = false;
        }
        else
        {
            objectTransform.position = targetPos;
        }
    }

    private IEnumerator PlaceShapeCoroutine(Transform objectTransform, Vector3 targetPos)
    {
        inCoroutine = true;

        while (Vector3.Distance(objectTransform.position, targetPos) > snapThreshold)
        {
            objectTransform.position = Vector3.Lerp(objectTransform.position, targetPos, placeSpeed);
            yield return null;
        }

        AudioController.Instance.PlaySFX(AudioClipLibrary.AudioClipNames.PlaceShape);

        Vector3 overshootPos = targetPos + Vector3.forward * placeBounceIntensity;  // Modify this to control depth of overshoot

        float bounceTime = 0;

        while (bounceTime < 1f)
        {
            objectTransform.position = Vector3.Lerp(objectTransform.position, overshootPos, bounceTime);
            bounceTime += placeSpeed;
            yield return null;
        }

        // Snap back to the target position
        bounceTime = 0;

        while (bounceTime < 1f)
        {
            objectTransform.position = Vector3.Lerp(objectTransform.position, targetPos, bounceTime);
            bounceTime += placeSpeed;
            yield return null;
        }

        objectTransform.position = targetPos;

        currentShapeObject.transform.SetParent(currentHoleObject.transform);

        availableShapeObjects.RemoveAt(currentShapeIndex);
        availableShapes.RemoveAt(currentShapeIndex);

        currentShape = null;
        currentShapeObject = null;

        if (availableShapes.Count > 0)
        {
            if (currentShapeIndex >= availableShapeObjects.Count)
            {
                currentShapeIndex = 0;
            }

            currentShapeObject = availableShapeObjects[currentShapeIndex];
            currentShape = availableShapes[currentShapeIndex];
            isMovingCurrentShape = true;

            ShapeSelectionController.Instance.StartLerp(currentShape, false);
        }

        inCoroutine = false;

        if (availableShapeObjects.Count == 0 || Hole.GridIsCompletelyFilled(CurrentHole))
        {
            StartCoroutine(PerformTickActionsBeforeFinishLevel());
        }
    }


    private void AlignRotation(Transform transform)
    {
        Vector3 currentRotation = transform.eulerAngles;
        currentRotation.z = Mathf.Round(currentRotation.z / 90f) * 90f;
        transform.eulerAngles = currentRotation;
    }

    private IEnumerator RotateSmoothly(Transform target, float angle, float duration)
    {
        inCoroutine = true;

        Quaternion startRotation = target.rotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(0, 0, angle);
        float time = 0.0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            target.rotation = Quaternion.Slerp(startRotation, endRotation, time / duration);
            yield return null;
        }

        // Slightly overshoot the final rotation
        Quaternion overshootRotation = endRotation * Quaternion.Euler(0, 0, rotateBounceIntensity * angle);
        float overshootDuration = duration * 0.2f;  // Shorter duration for the bounce effect
        time = 0.0f;
        while (time < overshootDuration)
        {
            time += Time.deltaTime;
            target.rotation = Quaternion.Slerp(endRotation, overshootRotation, time / overshootDuration);
            yield return null;
        }

        // Bounce back to the end rotation
        time = 0.0f;
        while (time < overshootDuration)
        {
            time += Time.deltaTime;
            target.rotation = Quaternion.Slerp(overshootRotation, endRotation, time / overshootDuration);
            yield return null;
        }

        target.rotation = endRotation;
        inCoroutine = false;
    }

    private void CycleThroughAvailableShapes()
    {
        if (inCoroutine)
        {
            return;
        }

        // Cycle right through available shapes
        if (Input.GetKeyDown(KeyCode.D) && availableShapes.Count > 1)
        {
            Vector3 currentShapePosition = currentShapeObject.transform.position;

            currentShapeIndex++;

            if (currentShapeIndex > availableShapeObjects.Count - 1)
            {
                currentShapeIndex = 0;
            }


            ShapeSelectionController.Instance.StartLerp(currentShape, true);

            currentShapeObject.transform.position = availableShapeObjects[currentShapeIndex].transform.position;

            currentShapeObject = availableShapeObjects[currentShapeIndex];
            currentShape = availableShapes[currentShapeIndex];

            currentShapeObject.transform.position = currentShapePosition;

            ShapeSelectionController.Instance.StartLerp(currentShape, false);

            CropController.Instance.SetCropDimensions(currentShape.Dimensions);
        }

        // Cycle left through available shapes
        else if (Input.GetKeyDown(KeyCode.A) && availableShapes.Count > 1)
        {
            Vector3 currentShapePosition = currentShapeObject.transform.position;

            currentShapeIndex--;

            if (currentShapeIndex < 0)
            {
                currentShapeIndex = availableShapes.Count - 1;
            }

            ShapeSelectionController.Instance.StartLerp(currentShape, true);

            currentShapeObject.transform.position = availableShapeObjects[currentShapeIndex].transform.position;

            currentShapeObject = availableShapeObjects[currentShapeIndex];
            currentShape = availableShapes[currentShapeIndex];

            currentShapeObject.transform.position = currentShapePosition;

            ShapeSelectionController.Instance.StartLerp(currentShape, false);

            CropController.Instance.SetCropDimensions(currentShape.Dimensions);
        }
    }
}
