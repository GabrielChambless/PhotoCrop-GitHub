using UnityEngine;
using System.Collections.Generic;

public class ShapeSelectionController : MonoBehaviour
{
    public struct LerpData
    {
        public Shape ShapeReference;
        public GameObject ShapeDoubleObject;
        public GameObject ObjectReference;
        public Vector3 originalPoint;
        public Vector3 startPoint;
        public Vector3 endPoint;
        public float duration;
        public bool isReversing;
        public bool isComplete;
        public float elapsedTime;
    }

    public static ShapeSelectionController Instance { get; private set; }

    [SerializeField] private GameObject shapePhotoObject;
    [SerializeField] private float distanceToLerp = 2f;
    [SerializeField] private float lerpDuration = 0.4f;
    [SerializeField] private float originalOffset = 0.2f;
    private Vector3 originalShapePosition;

    public readonly Dictionary<GameObject, LerpData> ShapesToLerp = new Dictionary<GameObject, LerpData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Instance = null;
            Destroy(gameObject);
            return;
        }
    }

    private void Update()
    {
        List<GameObject> keysToUpdate = new List<GameObject>(ShapesToLerp.Keys);

        foreach (GameObject shape in keysToUpdate)
        {
            //GameObject hitObject = null;

            //if (GameStats.UsePixelation)
            //{
            //    hitObject = Targeting.PixelatedRaycastObject(Camera.main, LayerMask.GetMask("CroppingPlane"));
            //}
            //else
            //{
            //    hitObject = Targeting.RaycastObject(Camera.main, LayerMask.GetMask("CroppingPlane"));
            //}

            //if (!ShapesToLerp.ContainsKey(hitObject))
            //{
            //    return;
            //}

            if (!ShapesToLerp.TryGetValue(shape, out LerpData data))
            {
                continue;
            }

            if (data.isComplete)
            {
                continue;
            }

            data.elapsedTime += Time.deltaTime;
            float progress = data.elapsedTime / data.duration;

            Vector3 from = data.isReversing ? data.endPoint : data.startPoint;
            Vector3 to = data.isReversing ? data.startPoint : data.endPoint;

            Vector3 newPosition = Vector3.Lerp(from, to, progress);
            shape.transform.localPosition = new Vector3(shape.transform.localPosition.x, newPosition.y, shape.transform.localPosition.z);

            if (progress >= 1f)
            {
                data.isComplete = true;
            }

            ShapesToLerp[shape] = data;
        }
    }

    public void StartLerp(Shape shape, bool reverse)
    {
        foreach (KeyValuePair<GameObject, LerpData> kvp in ShapesToLerp)
        {
            if (kvp.Value.ShapeReference == shape)
            {
                LerpData data = ShapesToLerp[kvp.Key];
                data.isReversing = reverse;
                data.isComplete = false;
                data.elapsedTime = 0f;
                ShapesToLerp[kvp.Key] = data;

                break;
            }
        }
    }

    public void SetDictionary(List<Shape> shapes)
    {
        foreach (var kvp in ShapesToLerp)
        {
            Destroy(kvp.Value.ObjectReference);
        }

        ShapesToLerp.Clear();

        int shapeCount = shapes.Count;
        float halfCount = shapeCount / 2f;

        for (int i = 0; i < shapeCount; i++)
        {
            Shape shape = shapes[i];
            float xOffset = (i - halfCount + 0.5f) * 2;
            float yOffset = (i % 2 == 0) ? originalOffset : 0;
            float zOffset = (i % 2 == 0) ? 0.04f : 0;

            GameObject photo = InstantiateShapePhotoObject(xOffset, yOffset, zOffset);

            GameObject shapeDouble = Instantiate(shape.ShapeObject);

            shapeDouble.transform.SetParent(photo.transform);
            shapeDouble.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            shapeDouble.transform.localEulerAngles = new Vector3(0f, 0f, shape.ShapeObject.transform.eulerAngles.z);
            shapeDouble.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);

            LerpData data = new LerpData
            {
                ShapeReference = shape,
                ShapeDoubleObject = shapeDouble,
                ObjectReference = photo,
                originalPoint = photo.transform.localPosition,
                startPoint = photo.transform.localPosition,
                endPoint = photo.transform.localPosition + (Vector3.up * distanceToLerp),
                duration = lerpDuration,
                isReversing = false,
                isComplete = true,
                elapsedTime = 0f
            };

            ShapesToLerp[photo] = data;
        }
    }

    public void RemoveShape(Shape shape)
    {
        foreach (KeyValuePair<GameObject, LerpData> kvp in ShapesToLerp)
        {
            if (kvp.Value.ShapeReference == shape)
            {
                Destroy(kvp.Value.ObjectReference);
                ShapesToLerp.Remove(kvp.Key);
                break;
            }
        }

        RecenterShapes();
    }

    public void UpdateShapeInPhoto(Shape shape)
    {
        foreach (KeyValuePair<GameObject, LerpData> kvp in ShapesToLerp)
        {
            if (kvp.Value.ShapeReference == shape)
            {
                //Destroy(kvp.Value.ShapeDoubleObject); //TODO; is setting inactive to prevent larget FPS drop better than just destroying to save memory?
                kvp.Value.ShapeDoubleObject.SetActive(false);

                GameObject shapeDouble = Instantiate(shape.ShapeObject);
                shapeDouble.transform.SetParent(kvp.Value.ObjectReference.transform);
                shapeDouble.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                shapeDouble.transform.localEulerAngles = new Vector3(0f, 0f, shape.ShapeObject.transform.eulerAngles.z);
                shapeDouble.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);

                LerpData data = kvp.Value;
                data.ShapeDoubleObject = shapeDouble;
                ShapesToLerp[kvp.Key] = data;

                break;
            }
        }
    }

    public void TogglePhotoObjects()
    {
        foreach (KeyValuePair<GameObject, LerpData> kvp in ShapesToLerp)
        {
            kvp.Value.ObjectReference.SetActive(!kvp.Value.ObjectReference.activeSelf);
        }
    }

    private GameObject InstantiateShapePhotoObject(float xOffset, float yOffset, float zOffset)
    {
        GameObject photo = Instantiate(shapePhotoObject);
        photo.transform.eulerAngles = CameraController.Instance.CameraRotation1;

        Vector3 localOffset = photo.transform.TransformDirection(new Vector3(xOffset, yOffset, zOffset));
        photo.transform.localPosition += localOffset;

        photo.transform.SetParent(Camera.main.transform);
        photo.transform.localPosition = new Vector3(photo.transform.localPosition.x, photo.transform.localPosition.y - 2f, photo.transform.localPosition.z - 7);
        originalShapePosition = photo.transform.localPosition;

        return photo;
    }

    private void RecenterShapes()
    {
        int shapeCount = ShapesToLerp.Count;
        float halfCount = shapeCount / 2f;
        int index = 0;

        List<GameObject> keys = new List<GameObject>(ShapesToLerp.Keys);

        foreach (GameObject photo in keys)
        {
            LerpData data = ShapesToLerp[photo];
            float xOffset = (index - halfCount + 0.5f) * 2;
            float yOffset = (index % 2 == 0) ? originalOffset : 0;
            float zOffset = (index % 2 == 0) ? 0.04f : 0;

            data.originalPoint = new Vector3(xOffset, originalShapePosition.y + yOffset, originalShapePosition.z + zOffset);
            data.startPoint = data.originalPoint;
            data.endPoint = data.originalPoint + (Vector3.up * distanceToLerp);
            data.isComplete = true;
            data.elapsedTime = 0f;

            photo.transform.localPosition = data.originalPoint;

            ShapesToLerp[photo] = data;
            index++;
        }
    }
}
