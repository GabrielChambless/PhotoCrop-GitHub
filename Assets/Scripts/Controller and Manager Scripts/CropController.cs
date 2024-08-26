using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Collections;

public class CropController : MonoBehaviour
{
    public static CropController Instance { get; private set; }

    [SerializeField] private Camera croppingCamera;

    [SerializeField] private GameObject innerGrid;
    [SerializeField] private GameObject topRightCorner, topLeftCorner, bottomRightCorner, bottomLeftCorner;
    [SerializeField] private GameObject topBorder, bottomBorder, leftBorder, rightBorder;
    [SerializeField] private Vector2Int cropDimensions = new Vector2Int(3, 3);

    public static bool CropIsActive;
    public static bool InCoroutine;

    private List<Vector2Int> cropLayout = new List<Vector2Int>();

    private Vector2Int croppingMousePosition;

    private Vector2Int topBorderStartPosition;
    private Vector2Int bottomBorderStartPosition;
    private Vector2Int rightBorderStartPosition;
    private Vector2Int leftBorderStartPosition;

    private bool isMovingBorder;
    private bool isMovingCorner;

    private GameObject lastClickedBorder;
    private Vector2Int lastClickedBorderMinPosition;
    private Vector2Int lastClickedBorderMaxPosition;

    private GameObject lastClickedCorner;
    private Vector2Int lastClickedCornerMinPosition;
    private Vector2Int lastClickedCornerMaxPosition;


    [SerializeField] private float moveSpeed = 25f;
    [SerializeField] private float snapThreshold = 0.2f;
    [SerializeField] private float bounceIntensity = 0.3f;

    [SerializeField] private float placeSpeed = 0.7f;
    [SerializeField] private float placeBounceIntensity = 0.4f;

    private Vector3 targetPosition; // Target position to move towards


    // Declare a dictionary to keep track of bouncing states for each transform
    private Dictionary<Transform, bool> bouncingStates = new Dictionary<Transform, bool>();

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
    }

    private void Start()
    {
        SetCropDimensions(cropDimensions);
    }

    private void Update()
    {
        if (!CropIsActive)
        {
            isMovingBorder = false; 
            isMovingCorner = false;
            return;
        }

        croppingMousePosition = Targeting.GetLocalMouseGridPosition(GameStats.UsePixelation, Camera.main, LayerMask.GetMask("CroppingPlane"));

        if (Input.GetMouseButtonDown(0) && !InCoroutine)
        {
            GameObject hitObject = Targeting.RaycastObject(GameStats.UsePixelation, Camera.main, LayerMask.GetMask("CroppingPlane"));

            HandleBorderSelection(hitObject);
            HandleCornerSelection(hitObject);
        }

        if (Input.GetMouseButtonUp(0) && (isMovingBorder || isMovingCorner) && !InCoroutine)
        {
            if (isMovingBorder)
            {
                isMovingBorder = false;

                MoveBorderOnMouseUp();
            }
            else
            {
                isMovingCorner = false;

                MoveCornerOnMouseUp();
            }
        }

        if (isMovingBorder && lastClickedBorder != null)
        {
            MoveBorder();
        }
        else if (isMovingCorner && lastClickedCorner != null)
        {
            MoveCorner();
        }
    }

    public void SetCropDimensions(Vector2Int dimensions)
    {
        cropDimensions = dimensions;

        topBorderStartPosition = new Vector2Int(0, Mathf.CeilToInt(cropDimensions.y / 2f));
        bottomBorderStartPosition = topBorderStartPosition * -1;
        rightBorderStartPosition = new Vector2Int(Mathf.CeilToInt(cropDimensions.x / 2f), 0);
        leftBorderStartPosition = rightBorderStartPosition * -1;

        topBorder.transform.localPosition = new Vector3(topBorderStartPosition.x, topBorderStartPosition.y, topBorder.transform.localPosition.z);
        bottomBorder.transform.localPosition = new Vector3(bottomBorderStartPosition.x, bottomBorderStartPosition.y, bottomBorder.transform.localPosition.z);
        rightBorder.transform.localPosition = new Vector3(rightBorderStartPosition.x, rightBorderStartPosition.y, rightBorder.transform.localPosition.z);
        leftBorder.transform.localPosition = new Vector3(leftBorderStartPosition.x, leftBorderStartPosition.y, leftBorder.transform.localPosition.z);

        topRightCorner.transform.localPosition = new Vector3(rightBorderStartPosition.x, topBorderStartPosition.y, topRightCorner.transform.localPosition.z);
        topLeftCorner.transform.localPosition = new Vector3(leftBorderStartPosition.x, topBorderStartPosition.y, topLeftCorner.transform.localPosition.z);
        bottomRightCorner.transform.localPosition = new Vector3(rightBorderStartPosition.x, bottomBorderStartPosition.y, bottomRightCorner.transform.localPosition.z);
        bottomLeftCorner.transform.localPosition = new Vector3(leftBorderStartPosition.x, bottomBorderStartPosition.y, bottomLeftCorner.transform.localPosition.z);
    }

    public void ToggleCropController()
    {
        ToggleCroppingObjects();

        // TODO; add or remove this?
        if (CropIsActive)
        {
            CameraController.Instance.ChangeOrthographicCameraSize(Camera.main, 6f, 4f, 0.4f);
            return;
        }

        CameraController.Instance.ChangeOrthographicCameraSize(Camera.main, 4f, 6f, 0.4f);
    }

    public void UpdateCropLayout()
    {
        cropLayout.Clear();

        int minX = (int)leftBorder.transform.localPosition.x + 1;
        int maxX = (int)rightBorder.transform.localPosition.x - 1;
        int minY = (int)bottomBorder.transform.localPosition.y + 1;
        int maxY = (int)topBorder.transform.localPosition.y - 1;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int cellPosition = new Vector2Int(x, y);
                cropLayout.Add(cellPosition);
            }
        }
    }

    private void ToggleCroppingObjects()
    {
        innerGrid.SetActive(CropIsActive);
        topBorder.SetActive(CropIsActive);
        bottomBorder.SetActive(CropIsActive);
        rightBorder.SetActive(CropIsActive);
        leftBorder.SetActive(CropIsActive);

        topRightCorner.SetActive(CropIsActive);
        topLeftCorner.SetActive(CropIsActive);
        bottomRightCorner.SetActive(CropIsActive);
        bottomLeftCorner.SetActive(CropIsActive);

        topBorder.transform.localPosition = new Vector3(topBorderStartPosition.x, topBorderStartPosition.y, topBorder.transform.localPosition.z);
        bottomBorder.transform.localPosition = new Vector3(bottomBorderStartPosition.x, bottomBorderStartPosition.y, bottomBorder.transform.localPosition.z);
        rightBorder.transform.localPosition = new Vector3(rightBorderStartPosition.x, rightBorderStartPosition.y, rightBorder.transform.localPosition.z);
        leftBorder.transform.localPosition = new Vector3(leftBorderStartPosition.x, leftBorderStartPosition.y, leftBorder.transform.localPosition.z);

        topRightCorner.transform.localPosition = new Vector3(rightBorderStartPosition.x, topBorderStartPosition.y, topRightCorner.transform.localPosition.z);
        topLeftCorner.transform.localPosition = new Vector3(leftBorderStartPosition.x, topBorderStartPosition.y, topLeftCorner.transform.localPosition.z);
        bottomRightCorner.transform.localPosition = new Vector3(rightBorderStartPosition.x, bottomBorderStartPosition.y, bottomRightCorner.transform.localPosition.z);
        bottomLeftCorner.transform.localPosition = new Vector3(leftBorderStartPosition.x, bottomBorderStartPosition.y, bottomLeftCorner.transform.localPosition.z);

        UpdateCropLayout();
    }

    public void CropShape(Shape shape, Hole currentHole)
    {
        List<ShapeCell> removedCells = new List<ShapeCell>();

        for (int i = shape.ShapeLayout.Count - 1; i >= 0; i--)
        {
            if (!cropLayout.Contains(shape.ShapeLayout[i].Position)
                && shape.ShapeLayout[i].CellContentType != GameStats.CellContentTypes.Empty)
            {
                removedCells.Add(shape.ShapeLayout[i]);
                shape.ShapeLayout.RemoveAt(i);
            }
        }

        // Refill the removed positions with empty cells
        foreach (ShapeCell removedCell in removedCells)
        {
            shape.ShapeLayout.Add(new ShapeCell
            {
                Position = removedCell.Position,
                IsFilled = false,
                CellContentType = GameStats.CellContentTypes.Empty
            });
        }

        List<Shape> croppedShapes = GroupAdjacentCells(removedCells, shape.Dimensions);

        foreach (Shape newShape in croppedShapes)
        {
            FillEmptySpaces(newShape);

            GameObject newShapeObject = new GameObject("CroppedShapeObject");
            newShapeObject.transform.position = shape.ShapeObject.transform.position;

            foreach (ShapeCell cell in newShape.ShapeLayout)
            {
                if (cell.ShapeCellObject != null)
                {
                    cell.ShapeCellObject.transform.SetParent(newShapeObject.transform);
                    cell.ShapeCellObject.transform.localPosition = new Vector3(cell.Position.x, cell.Position.y, 0f);
                }
            }

            if (Shape.CanPlaceShape(currentHole, newShape, new Vector2Int((int)shape.ShapeObject.transform.position.x, (int)shape.ShapeObject.transform.position.y)))
            {
                float worldTypeOffsetZ = 0f;

                switch (LevelSelectorController.Instance.SelectedLevelData.World)
                {
                    case LevelData.WorldType.Bricks:
                        worldTypeOffsetZ = 0.5f;
                        break;
                    case LevelData.WorldType.Chess:
                        worldTypeOffsetZ = 0f;
                        break;
                }

                Vector3 targetPosition = new Vector3(newShapeObject.transform.position.x, newShapeObject.transform.position.y, worldTypeOffsetZ);

                ShapeSelectionController.Instance.UpdateShapeInPhoto(shape);

                StartCoroutine(PlaceShapeCoroutine(newShapeObject.transform, targetPosition, currentHole.HoleObject.transform));
            }
            else
            {
                newShapeObject.transform.position = Vector3.up * 200;
                Debug.Log("Shape cannot be placed!");
            }
        }
    }

    private List<Shape> GroupAdjacentCells(List<ShapeCell> cells, Vector2Int originalDimensions)
    {
        List<Shape> newShapes = new List<Shape>();
        cells.RemoveAll(cell => cell.CellContentType == GameStats.CellContentTypes.Empty); // Remove empty cells from consideration

        while (cells.Count > 0)
        {
            List<ShapeCell> cluster = new List<ShapeCell>();
            ShapeCell start = cells[0];
            cells.RemoveAt(0);
            cluster.Add(start);

            // Check adjacency including diagonals
            for (int i = 0; i < cluster.Count; i++)
            {
                for (int j = cells.Count - 1; j >= 0; j--)
                {
                    if (IsAdjacent(cluster[i], cells[j]))
                    {
                        cluster.Add(cells[j]);
                        cells.RemoveAt(j);
                    }
                }
            }

            Shape newShape = new Shape(new List<ShapeCell>(cluster), originalDimensions);
            newShapes.Add(newShape);
        }

        return newShapes;
    }

    private void FillEmptySpaces(Shape shape)
    {
        HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>(shape.ShapeLayout.Select(c => c.Position));
        int center = shape.Dimensions.x / 2; // Assuming square dimensions

        for (int y = -center; y <= center; y++)
        {
            for (int x = -center; x <= center; x++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (!occupiedPositions.Contains(pos))
                {
                    shape.ShapeLayout.Add(new ShapeCell
                    {
                        Position = pos,
                        IsFilled = false,
                        CellContentType = GameStats.CellContentTypes.Empty
                    });
                }
            }
        }
    }

    private bool IsAdjacent(ShapeCell a, ShapeCell b)
    {
        return Math.Abs(a.Position.x - b.Position.x) <= 1 && Math.Abs(a.Position.y - b.Position.y) <= 1;
    }

    private void HandleBorderSelection(GameObject hitObject)
    {
        if (hitObject == topBorder || hitObject == bottomBorder || hitObject == rightBorder || hitObject == leftBorder)
        {
            isMovingBorder = true;
            ConfigureBorder(hitObject);
        }
    }

    private void HandleCornerSelection(GameObject hitObject)
    {
        if (hitObject == topRightCorner || hitObject == topLeftCorner || hitObject == bottomRightCorner || hitObject == bottomLeftCorner)
        {
            isMovingCorner = true;
            ConfigureCorner(hitObject);
        }
    }

    private void ConfigureBorder(GameObject border)
    {
        if (border == topBorder)
        {
            lastClickedBorder = topBorder;
            lastClickedBorderMinPosition = Vector2Int.up;
            lastClickedBorderMaxPosition = topBorderStartPosition;
        }
        else if (border == bottomBorder)
        {
            lastClickedBorder = bottomBorder;
            lastClickedBorderMinPosition = bottomBorderStartPosition;
            lastClickedBorderMaxPosition = -Vector2Int.up;
        }
        else if (border == rightBorder)
        {
            lastClickedBorder = rightBorder;
            lastClickedBorderMinPosition = Vector2Int.right;
            lastClickedBorderMaxPosition = rightBorderStartPosition;
        }
        else if (border == leftBorder)
        {
            lastClickedBorder = leftBorder;
            lastClickedBorderMinPosition = leftBorderStartPosition;
            lastClickedBorderMaxPosition = -Vector2Int.right;
        }
    }

    private void ConfigureCorner(GameObject corner)
    {
        if (corner == topRightCorner)
        {
            lastClickedCorner = corner;
            lastClickedCornerMinPosition = new Vector2Int(1, 1);
            lastClickedCornerMaxPosition = topBorderStartPosition + (Vector2Int.right * topBorderStartPosition.y);
        }
        else if (corner == topLeftCorner)
        {
            lastClickedCorner = corner;
            lastClickedCornerMinPosition = new Vector2Int(-1, 1);
            lastClickedCornerMaxPosition = topBorderStartPosition - (Vector2Int.right * topBorderStartPosition.y);
        }
        else if (corner == bottomRightCorner)
        {
            lastClickedCorner = corner;
            lastClickedCornerMinPosition = bottomBorderStartPosition - (Vector2Int.right * bottomBorderStartPosition.y);
            lastClickedCornerMaxPosition = new Vector2Int(1, -1);
        }
        else if (corner == bottomLeftCorner)
        {
            lastClickedCorner = corner;
            lastClickedCornerMinPosition = bottomBorderStartPosition + (Vector2Int.right * bottomBorderStartPosition.y);
            lastClickedCornerMaxPosition = new Vector2Int(-1, -1);
        }
    }

    private void MoveBorder()
    {
        //Vector3 localMousePosition = transform.InverseTransformPoint(croppingMousePosition.x, croppingMousePosition.y, transform.position.z);

        float clampedLocalX = Mathf.Clamp(croppingMousePosition.x, lastClickedBorderMinPosition.x, lastClickedBorderMaxPosition.x);
        float clampedLocalY = Mathf.Clamp(croppingMousePosition.y, lastClickedBorderMinPosition.y, lastClickedBorderMaxPosition.y);

        if (lastClickedBorder == topBorder)
        {
            if (clampedLocalY != topRightCorner.transform.localPosition.y)
            {
                MoveAndSnap(topRightCorner.transform, new Vector3(topRightCorner.transform.localPosition.x, clampedLocalY, topRightCorner.transform.localPosition.z));
                MoveAndSnap(topLeftCorner.transform, new Vector3(topLeftCorner.transform.localPosition.x, clampedLocalY, topLeftCorner.transform.localPosition.z));
            }
        }
        else if (lastClickedBorder == bottomBorder)
        {
            if (clampedLocalY != bottomRightCorner.transform.localPosition.y)
            {
                MoveAndSnap(bottomRightCorner.transform, new Vector3(bottomRightCorner.transform.localPosition.x, clampedLocalY, bottomRightCorner.transform.localPosition.z));
                MoveAndSnap(bottomLeftCorner.transform, new Vector3(bottomLeftCorner.transform.localPosition.x, clampedLocalY, bottomLeftCorner.transform.localPosition.z));
            }
        }
        else if (lastClickedBorder == rightBorder)
        {
            if (clampedLocalX != topRightCorner.transform.localPosition.x)
            {
                MoveAndSnap(topRightCorner.transform, new Vector3(clampedLocalX, topRightCorner.transform.localPosition.y, topRightCorner.transform.localPosition.z));
                MoveAndSnap(bottomRightCorner.transform, new Vector3(clampedLocalX, bottomRightCorner.transform.localPosition.y, bottomRightCorner.transform.localPosition.z));
            }
        }
        else if (lastClickedBorder == leftBorder)
        {
            if (clampedLocalX != topLeftCorner.transform.localPosition.x)
            {
                MoveAndSnap(topLeftCorner.transform, new Vector3(clampedLocalX, topLeftCorner.transform.localPosition.y, topLeftCorner.transform.localPosition.z));
                MoveAndSnap(bottomLeftCorner.transform, new Vector3(clampedLocalX, bottomLeftCorner.transform.localPosition.y, bottomLeftCorner.transform.localPosition.z));
            }
        }

        targetPosition = new Vector3(clampedLocalX, clampedLocalY, lastClickedBorder.transform.localPosition.z);
        MoveAndSnap(lastClickedBorder.transform, targetPosition);
    }

    private void MoveBorderOnMouseUp()
    {
        //Vector3 localMousePosition = transform.InverseTransformPoint(croppingMousePosition.x, croppingMousePosition.y, transform.position.z);

        float clampedLocalX = Mathf.Clamp(croppingMousePosition.x, lastClickedBorderMinPosition.x, lastClickedBorderMaxPosition.x);
        float clampedLocalY = Mathf.Clamp(croppingMousePosition.y, lastClickedBorderMinPosition.y, lastClickedBorderMaxPosition.y);

        if (lastClickedBorder == topBorder)
        {
            if (clampedLocalY != topRightCorner.transform.localPosition.y)
            {
                StartCoroutine(MouseUpCoroutine(topRightCorner.transform, new Vector3(topRightCorner.transform.localPosition.x, clampedLocalY, topRightCorner.transform.localPosition.z)));
                StartCoroutine(MouseUpCoroutine(topLeftCorner.transform, new Vector3(topLeftCorner.transform.localPosition.x, clampedLocalY, topLeftCorner.transform.localPosition.z)));
            }
        }
        else if (lastClickedBorder == bottomBorder)
        {
            if (clampedLocalY != bottomRightCorner.transform.localPosition.y)
            {
                StartCoroutine(MouseUpCoroutine(bottomRightCorner.transform, new Vector3(bottomRightCorner.transform.localPosition.x, clampedLocalY, bottomRightCorner.transform.localPosition.z)));
                StartCoroutine(MouseUpCoroutine(bottomLeftCorner.transform, new Vector3(bottomLeftCorner.transform.localPosition.x, clampedLocalY, bottomLeftCorner.transform.localPosition.z)));
            }
        }
        else if (lastClickedBorder == rightBorder)
        {
            if (clampedLocalX != topRightCorner.transform.localPosition.x)
            {
                StartCoroutine(MouseUpCoroutine(topRightCorner.transform, new Vector3(clampedLocalX, topRightCorner.transform.localPosition.y, topRightCorner.transform.localPosition.z)));
                StartCoroutine(MouseUpCoroutine(bottomRightCorner.transform, new Vector3(clampedLocalX, bottomRightCorner.transform.localPosition.y, bottomRightCorner.transform.localPosition.z)));
            }
        }
        else if (lastClickedBorder == leftBorder)
        {
            if (clampedLocalX != topLeftCorner.transform.localPosition.x)
            {
                StartCoroutine(MouseUpCoroutine(topLeftCorner.transform, new Vector3(clampedLocalX, topLeftCorner.transform.localPosition.y, topLeftCorner.transform.localPosition.z)));
                StartCoroutine(MouseUpCoroutine(bottomLeftCorner.transform, new Vector3(clampedLocalX, bottomLeftCorner.transform.localPosition.y, bottomLeftCorner.transform.localPosition.z)));
            }
        }

        targetPosition = new Vector3(clampedLocalX, clampedLocalY, lastClickedBorder.transform.localPosition.z);
        StartCoroutine(MouseUpCoroutine(lastClickedBorder.transform, targetPosition));
    }

    private void MoveCorner()
    {
        //Vector3 localMousePosition = transform.InverseTransformPoint(croppingMousePosition.x, croppingMousePosition.y, transform.position.z);

        float clampedLocalX = 0;

        if (lastClickedCornerMinPosition.x > lastClickedCornerMaxPosition.x)
        {
            clampedLocalX = Mathf.Clamp(croppingMousePosition.x, lastClickedCornerMaxPosition.x, lastClickedCornerMinPosition.x);
        }
        else
        {
            clampedLocalX = Mathf.Clamp(croppingMousePosition.x, lastClickedCornerMinPosition.x, lastClickedCornerMaxPosition.x);
        }

        float clampedLocalY = Mathf.Clamp(croppingMousePosition.y, lastClickedCornerMinPosition.y, lastClickedCornerMaxPosition.y);


        if (lastClickedCorner == topRightCorner)
        {
            if (clampedLocalX != bottomRightCorner.transform.localPosition.x)
            {
                targetPosition = new Vector3(clampedLocalX, bottomRightCorner.transform.localPosition.y, lastClickedCorner.transform.localPosition.z);
                MoveAndSnap(bottomRightCorner.transform, targetPosition);
                MoveAndSnap(rightBorder.transform, new Vector3(clampedLocalX, rightBorder.transform.localPosition.y, rightBorder.transform.localPosition.z));
            }
            if (clampedLocalY != topLeftCorner.transform.localPosition.y)
            {
                targetPosition = new Vector3(topLeftCorner.transform.localPosition.x, clampedLocalY, lastClickedCorner.transform.localPosition.z);
                MoveAndSnap(topLeftCorner.transform, targetPosition);
                MoveAndSnap(topBorder.transform, new Vector3(topBorder.transform.localPosition.x, clampedLocalY, topBorder.transform.localPosition.z));
            }
        }
        else if (lastClickedCorner == topLeftCorner)
        {
            if (clampedLocalX != bottomLeftCorner.transform.localPosition.x)
            {
                targetPosition = new Vector3(clampedLocalX, bottomLeftCorner.transform.localPosition.y, lastClickedCorner.transform.localPosition.z);
                MoveAndSnap(bottomLeftCorner.transform, targetPosition);
                MoveAndSnap(leftBorder.transform, new Vector3(clampedLocalX, leftBorder.transform.localPosition.y, leftBorder.transform.localPosition.z));
            }
            if (clampedLocalY != topRightCorner.transform.localPosition.y)
            {
                targetPosition = new Vector3(topRightCorner.transform.localPosition.x, clampedLocalY, lastClickedCorner.transform.localPosition.z);
                MoveAndSnap(topRightCorner.transform, targetPosition);
                MoveAndSnap(topBorder.transform, new Vector3(topBorder.transform.localPosition.x, clampedLocalY, topBorder.transform.localPosition.z));
            }
        }
        else if (lastClickedCorner == bottomRightCorner)
        {
            if (clampedLocalX != topRightCorner.transform.localPosition.x)
            {
                targetPosition = new Vector3(clampedLocalX, topRightCorner.transform.localPosition.y, lastClickedCorner.transform.localPosition.z);
                MoveAndSnap(topRightCorner.transform, targetPosition);
                MoveAndSnap(rightBorder.transform, new Vector3(clampedLocalX, rightBorder.transform.localPosition.y, rightBorder.transform.localPosition.z));
            }
            if (clampedLocalY != bottomLeftCorner.transform.localPosition.y)
            {
                targetPosition = new Vector3(bottomLeftCorner.transform.localPosition.x, clampedLocalY, lastClickedCorner.transform.localPosition.z);
                MoveAndSnap(bottomLeftCorner.transform, targetPosition);
                MoveAndSnap(bottomBorder.transform, new Vector3(bottomBorder.transform.localPosition.x, clampedLocalY, bottomBorder.transform.localPosition.z));
            }
        }
        else if (lastClickedCorner == bottomLeftCorner)
        {
            if (clampedLocalX != topLeftCorner.transform.localPosition.x)
            {
                targetPosition = new Vector3(clampedLocalX, topLeftCorner.transform.localPosition.y, lastClickedCorner.transform.localPosition.z);
                MoveAndSnap(topLeftCorner.transform, targetPosition);
                MoveAndSnap(leftBorder.transform, new Vector3(clampedLocalX, leftBorder.transform.localPosition.y, leftBorder.transform.localPosition.z));
            }
            if (clampedLocalY != bottomRightCorner.transform.localPosition.y)
            {
                targetPosition = new Vector3(bottomRightCorner.transform.localPosition.x, clampedLocalY, lastClickedCorner.transform.localPosition.z);
                MoveAndSnap(bottomRightCorner.transform, targetPosition);
                MoveAndSnap(bottomBorder.transform, new Vector3(bottomBorder.transform.localPosition.x, clampedLocalY, bottomBorder.transform.localPosition.z));
            }
        }

        targetPosition = new Vector3(clampedLocalX, clampedLocalY, lastClickedCorner.transform.localPosition.z);
        MoveAndSnap(lastClickedCorner.transform, targetPosition);
    }

    private void MoveCornerOnMouseUp()
    {
        //Vector3 localMousePosition = transform.InverseTransformPoint(croppingMousePosition.x, croppingMousePosition.y, transform.position.z);

        float clampedLocalX = 0;

        if (lastClickedCornerMinPosition.x > lastClickedCornerMaxPosition.x)
        {
            clampedLocalX = Mathf.Clamp(croppingMousePosition.x, lastClickedCornerMaxPosition.x, lastClickedCornerMinPosition.x);
        }
        else
        {
            clampedLocalX = Mathf.Clamp(croppingMousePosition.x, lastClickedCornerMinPosition.x, lastClickedCornerMaxPosition.x);
        }

        float clampedLocalY = Mathf.Clamp(croppingMousePosition.y, lastClickedCornerMinPosition.y, lastClickedCornerMaxPosition.y);


        if (lastClickedCorner == topRightCorner)
        {
            if (clampedLocalX != bottomRightCorner.transform.localPosition.x)
            {
                targetPosition = new Vector3(clampedLocalX, bottomRightCorner.transform.localPosition.y, lastClickedCorner.transform.localPosition.z);
                StartCoroutine(MouseUpCoroutine(bottomRightCorner.transform, targetPosition));
                StartCoroutine(MouseUpCoroutine(rightBorder.transform, new Vector3(clampedLocalX, rightBorder.transform.localPosition.y, rightBorder.transform.localPosition.z)));
            }
            if (clampedLocalY != topLeftCorner.transform.localPosition.y)
            {
                targetPosition = new Vector3(topLeftCorner.transform.localPosition.x, clampedLocalY, lastClickedCorner.transform.localPosition.z);
                StartCoroutine(MouseUpCoroutine(topLeftCorner.transform, targetPosition));
                StartCoroutine(MouseUpCoroutine(topBorder.transform, new Vector3(topBorder.transform.localPosition.x, clampedLocalY, topBorder.transform.localPosition.z)));
            }
        }
        else if (lastClickedCorner == topLeftCorner)
        {
            if (clampedLocalX != bottomLeftCorner.transform.localPosition.x)
            {
                targetPosition = new Vector3(clampedLocalX, bottomLeftCorner.transform.localPosition.y, lastClickedCorner.transform.localPosition.z);
                StartCoroutine(MouseUpCoroutine(bottomLeftCorner.transform, targetPosition));
                StartCoroutine(MouseUpCoroutine(leftBorder.transform, new Vector3(clampedLocalX, leftBorder.transform.localPosition.y, leftBorder.transform.localPosition.z)));
            }
            if (clampedLocalY != topRightCorner.transform.localPosition.y)
            {
                targetPosition = new Vector3(topRightCorner.transform.localPosition.x, clampedLocalY, lastClickedCorner.transform.localPosition.z);
                StartCoroutine(MouseUpCoroutine(topRightCorner.transform, targetPosition));
                StartCoroutine(MouseUpCoroutine(topBorder.transform, new Vector3(topBorder.transform.localPosition.x, clampedLocalY, topBorder.transform.localPosition.z)));
            }
        }
        else if (lastClickedCorner == bottomRightCorner)
        {
            if (clampedLocalX != topRightCorner.transform.localPosition.x)
            {
                targetPosition = new Vector3(clampedLocalX, topRightCorner.transform.localPosition.y, lastClickedCorner.transform.localPosition.z);
                StartCoroutine(MouseUpCoroutine(topRightCorner.transform, targetPosition));
                StartCoroutine(MouseUpCoroutine(rightBorder.transform, new Vector3(clampedLocalX, rightBorder.transform.localPosition.y, rightBorder.transform.localPosition.z)));
            }
            if (clampedLocalY != bottomLeftCorner.transform.localPosition.y)
            {
                targetPosition = new Vector3(bottomLeftCorner.transform.localPosition.x, clampedLocalY, lastClickedCorner.transform.localPosition.z);
                StartCoroutine(MouseUpCoroutine(bottomLeftCorner.transform, targetPosition));
                StartCoroutine(MouseUpCoroutine(bottomBorder.transform, new Vector3(bottomBorder.transform.localPosition.x, clampedLocalY, bottomBorder.transform.localPosition.z)));
            }
        }
        else if (lastClickedCorner == bottomLeftCorner)
        {
            if (clampedLocalX != topLeftCorner.transform.localPosition.x)
            {
                targetPosition = new Vector3(clampedLocalX, topLeftCorner.transform.localPosition.y, lastClickedCorner.transform.localPosition.z);
                StartCoroutine(MouseUpCoroutine(topLeftCorner.transform, targetPosition));
                StartCoroutine(MouseUpCoroutine(leftBorder.transform, new Vector3(clampedLocalX, leftBorder.transform.localPosition.y, leftBorder.transform.localPosition.z)));
            }
            if (clampedLocalY != bottomRightCorner.transform.localPosition.y)
            {
                targetPosition = new Vector3(bottomRightCorner.transform.localPosition.x, clampedLocalY, lastClickedCorner.transform.localPosition.z);
                StartCoroutine(MouseUpCoroutine(bottomRightCorner.transform, targetPosition));
                StartCoroutine(MouseUpCoroutine(bottomBorder.transform, new Vector3(bottomBorder.transform.localPosition.x, clampedLocalY, bottomBorder.transform.localPosition.z)));
            }
        }

        targetPosition = new Vector3(clampedLocalX, clampedLocalY, lastClickedCorner.transform.localPosition.z);
        StartCoroutine(MouseUpCoroutine(lastClickedCorner.transform, targetPosition));
    }

    private void MoveAndSnap(Transform objectTransform, Vector3 targetPos)
    {
        if (!bouncingStates.ContainsKey(objectTransform))
        {
            bouncingStates[objectTransform] = false;
        }

        if (Vector3.Distance(objectTransform.localPosition, targetPos) > snapThreshold)
        {
            objectTransform.localPosition = Vector3.Lerp(objectTransform.localPosition, targetPos, moveSpeed * Time.deltaTime);
            bouncingStates[objectTransform] = true;
        }
        else if (bouncingStates[objectTransform])
        {
            objectTransform.localPosition = Vector3.Lerp(objectTransform.localPosition, targetPos + (targetPos - objectTransform.localPosition).normalized * bounceIntensity, 0.7f);    //TODO; perhaps don't hard code this
            bouncingStates[objectTransform] = false;
        }
        else
        {
            objectTransform.localPosition = targetPos;
        }
    }

    IEnumerator MouseUpCoroutine(Transform objectTransform, Vector3 targetPos)
    {
        InCoroutine = true;

        if (!bouncingStates.ContainsKey(objectTransform))
        {
            bouncingStates[objectTransform] = false;
        }

        // Continue moving towards the target localPosition until close enough
        while (Vector3.Distance(objectTransform.localPosition, targetPos) > snapThreshold)
        {
            objectTransform.localPosition = Vector3.Lerp(objectTransform.localPosition, targetPos, (moveSpeed + 5) * Time.deltaTime);
            bouncingStates[objectTransform] = true;
            yield return null; // Wait for the next frame
        }

        // Apply the bounce effect
        if (bouncingStates[objectTransform])
        {
            // Calculate the overshoot localPosition
            Vector3 overshootPos = targetPos + (targetPos - objectTransform.localPosition).normalized * bounceIntensity;
            float bounceTime = 0;
            while (bounceTime < 1f)
            {
                objectTransform.localPosition = Vector3.Lerp(objectTransform.localPosition, overshootPos, bounceTime);
                bounceTime += (moveSpeed + 5) * Time.deltaTime;
                yield return null;
            }

            // Settle back to the target localPosition
            bounceTime = 0;
            while (bounceTime < 1f)
            {
                objectTransform.localPosition = Vector3.Lerp(objectTransform.localPosition, targetPos, bounceTime);
                bounceTime += (moveSpeed + 5) * Time.deltaTime;
                yield return null;
            }

            bouncingStates[objectTransform] = false; // Reset the bounce state
        }

        objectTransform.localPosition = targetPos;

        UpdateCropLayout();
        InCoroutine = false;
    }

    IEnumerator PlaceShapeCoroutine(Transform objectTransform, Vector3 targetPos, Transform parentTransform)
    {
        while (Vector3.Distance(objectTransform.position, targetPos) > snapThreshold)
        {
            objectTransform.position = Vector3.Lerp(objectTransform.position, targetPos, placeSpeed);
            yield return null;
        }

        // Calculate the overshoot position, specifically along the z-axis
        Vector3 overshootPos = targetPos + Vector3.forward * placeBounceIntensity;  // Modify this to control depth of overshoot

        // Move to the overshoot position
        float bounceTime = 0;

        while (bounceTime < 1f)
        {
            objectTransform.position = Vector3.Lerp(objectTransform.position, overshootPos, bounceTime);
            bounceTime += placeSpeed;  // Adjust the rate of the bounce effect
            yield return null;
        }

        switch (LevelSelectorController.Instance.SelectedLevelData.World)
        {
            case LevelData.WorldType.Bricks:
                AudioController.Instance.PlaySFX(AudioController.Instance.RandomBrickClick().Name);
                break;
            case LevelData.WorldType.Chess:
                AudioController.Instance.PlaySFX(AudioClipLibrary.AudioClipNames.DefaultShapePlace);
                break;
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

        objectTransform.SetParent(parentTransform);
    }
}
