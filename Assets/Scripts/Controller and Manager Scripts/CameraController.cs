using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CameraController : MonoBehaviour
{
    public enum CameraAngles
    {
        Angle1,
        Angle2
    }

    public static CameraController Instance { get; private set; }

    [SerializeField] private RawImage pixelationRawImage;
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float edgeThreshold = 400f;
    [SerializeField] private Vector3 moveLimits = new Vector3(3f, 3f, 3f);
    [SerializeField] private float rotateSpeed = 7.5f;

    public Vector3 CameraRotation1 = new Vector3(-45f, -30f, 35f);
    public Vector3 CameraRotation2 = new Vector3(-10f, -10f, 1.75f);

    public Vector3 CameraPosition1;
    public Vector3 CameraPosition2;

    private Camera currentCamera;
    private CameraAngles currentCameraAngle = CameraAngles.Angle1;

    private Quaternion targetRotation;
    private Vector3 targetPosition;
    private bool isTransitioning;

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

        currentCamera = GetComponent<Camera>();
        targetRotation = transform.rotation;
       
        if (LevelSelectorController.Instance != null && LevelSelectorController.Instance.SelectedHoleData != null)
        {
            targetPosition = new Vector3((LevelSelectorController.Instance.SelectedHoleData.GridSize.x - 1.5f) / 2f, (LevelSelectorController.Instance.SelectedHoleData.GridSize.y - 1f) / 2f, 0f)
                + transform.forward * -9;

            CameraPosition1 = targetPosition;

            Quaternion rotationQuaternion = Quaternion.Euler(CameraRotation2);
            Vector3 forwardDirection = rotationQuaternion * Vector3.forward;

            CameraPosition2 = new Vector3((LevelSelectorController.Instance.SelectedHoleData.GridSize.x - 1f) / 2f, (LevelSelectorController.Instance.SelectedHoleData.GridSize.y - 1f) / 2f, 0f)
                + forwardDirection * -9f;
        }

        TogglePixelationEffect(GameStats.UsePixelation);
    }

    private void Update()
    {
        if (GameStateManager.Instance.CurrentGameState != GameStateManager.GameStates.Level && GameStateManager.Instance.CurrentGameState != GameStateManager.GameStates.LevelRestarting
            && GameStateManager.Instance.CurrentGameState != GameStateManager.GameStates.LevelFinish)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            currentCamera.orthographic = !currentCamera.orthographic;
        }

        //if (!isTransitioning && !CropController.CropIsActive && LevelController.Instance != null && !LevelController.Instance.IsSettingUpLevel)
        //{
        //    HandleCameraMovement();
        //    return;
        //}

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, rotateSpeed * Time.deltaTime);

        if (Quaternion.Angle(transform.rotation, targetRotation) < 0.1f && Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            transform.rotation = targetRotation;
            transform.position = targetPosition;
            isTransitioning = false;
        }
    }

    public void TogglePixelationEffect(bool usePixelation)
    {
        if (currentCamera.targetTexture != null && !usePixelation)
        {
            pixelationRawImage.transform.parent.gameObject.SetActive(false);
            currentCamera.targetTexture = null;
            return;
        }

        if (MenuController.Instance != null)
        {
            SetPixelationTexture(MenuController.Instance.SettingsData.ResolutionPresets[MenuController.Instance.SettingsData.ResolutionIndex].PixelationRenderTexture);
        }

        pixelationRawImage.transform.parent.gameObject.SetActive(true);
    }

    public void SetPixelationTexture(RenderTexture pixelationTexture)
    {
        pixelationRawImage.texture = pixelationTexture;
        currentCamera.targetTexture = pixelationTexture;
    }

    public void MoveToCameraAngle(CameraAngles cameraAngle)
    {
        //if (currentCameraAngle == cameraAngle && !CropController.CropIsActive)
        //{
        //    return;
        //}

        switch (cameraAngle)
        {
            case CameraAngles.Angle1:
                currentCameraAngle = CameraAngles.Angle1;
                targetRotation = Quaternion.Euler(CameraRotation1);
                targetPosition = CameraPosition1;
                if (CropController.CropIsActive)
                {
                    targetPosition = new Vector3(CropController.Instance.transform.position.x + 1.5f, CropController.Instance.transform.position.y - 1.5f, targetPosition.z);
                }
                isTransitioning = true;
                break;
            case CameraAngles.Angle2:
                currentCameraAngle = CameraAngles.Angle2;
                targetRotation = Quaternion.Euler(CameraRotation2);
                targetPosition = CameraPosition2;
                if (CropController.CropIsActive)
                {
                    targetPosition = new Vector3(CropController.Instance.transform.position.x + 1.5f, CropController.Instance.transform.position.y - 1.5f, targetPosition.z);
                }
                isTransitioning = true;
                break;
        }
    }

    public void ChangeOrthographicCameraSize(Camera targetCamera, float startSize, float endSize, float duration)
    {
        StartCoroutine(LerpOrthographicCameraSize(targetCamera, startSize, endSize, duration));
    }

    public void BillboardObject(GameObject objectToBillboard)
    {
        objectToBillboard.transform.LookAt(objectToBillboard.transform.position + transform.rotation * Vector3.forward, transform.rotation * Vector3.up);
    }

    private void HandleCameraMovement()
    {
        //Vector3 moveDirection = Vector3.zero;

        //if (Input.mousePosition.x >= Screen.width - edgeThreshold)
        //    moveDirection.x = 1;
        //if (Input.mousePosition.x <= edgeThreshold)
        //    moveDirection.x = -1;
        //if (Input.mousePosition.y >= Screen.height - edgeThreshold)
        //    moveDirection.y = 1;
        //if (Input.mousePosition.y <= edgeThreshold)
        //    moveDirection.y = -1;

        //Vector3 targetPosition = transform.position + moveDirection;

        //targetPosition.x = Mathf.Clamp(targetPosition.x, cameraOffset.x - moveLimits.x, cameraOffset.x + moveLimits.x);
        //targetPosition.y = Mathf.Clamp(targetPosition.y, cameraOffset.y - moveLimits.y, cameraOffset.y + moveLimits.y);
        //targetPosition.z = Mathf.Clamp(targetPosition.z, cameraOffset.z - moveLimits.z, cameraOffset.z + moveLimits.z);

        float clampedX = Mathf.Clamp(Targeting.CurrentMouseGridPosition.x, 0, LevelController.Instance.CurrentHole.GridSize.x - 1);
        float clampedY = Mathf.Clamp(Targeting.CurrentMouseGridPosition.y, 0, LevelController.Instance.CurrentHole.GridSize.y - 1);

        Vector3 newPosition = new Vector3(clampedX, clampedY, 0) 
            + new Vector3(targetPosition.x - ((LevelController.Instance.CurrentHole.GridSize.x - 1)/ 2f), targetPosition.y - ((LevelController.Instance.CurrentHole.GridSize.y - 1) / 2f), -10f);

        transform.position = Vector3.Lerp(transform.position, newPosition, smoothSpeed * Time.deltaTime);
    }

    IEnumerator LerpOrthographicCameraSize(Camera camera, float initialSize, float finalSize, float time)
    {
        float elapsedTime = 0;

        camera.orthographicSize = initialSize;

        while (elapsedTime < time)
        {
            camera.orthographicSize = Mathf.Lerp(initialSize, finalSize, (elapsedTime / time));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        camera.orthographicSize = finalSize;
    }
}
