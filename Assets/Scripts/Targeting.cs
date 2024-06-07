using UnityEngine;

public static class Targeting
{
    public static Vector2Int CurrentMouseGridPosition;

    public const float SNAP_GRID_SIZE = 1f;

    public static Vector2Int GetMouseGridPosition(bool usePixelation, Camera targetCamera, LayerMask mask)
    {
        Ray ray;
        RaycastHit hit;

        if (usePixelation)
        {
            ray = GetScaledRayFromMousePosition(targetCamera);
        }
        else
        {
            ray = targetCamera.ScreenPointToRay(Input.mousePosition);
        }

        if (Physics.Raycast(ray, out hit, 200f, mask))
        {
            Vector3 hitPoint = hit.point;
            Vector2 newPosition = new Vector2(hitPoint.x, hitPoint.y);

            if (SNAP_GRID_SIZE > 0)
            {
                newPosition.x = Mathf.Round(newPosition.x / SNAP_GRID_SIZE) * SNAP_GRID_SIZE;
                newPosition.y = Mathf.Round(newPosition.y / SNAP_GRID_SIZE) * SNAP_GRID_SIZE;
            }

            return Vector2Int.RoundToInt(newPosition);
        }

        return Vector2Int.zero;
    }

    public static Vector2Int GetLocalMouseGridPosition(bool usePixelation, Camera targetCamera, LayerMask mask)
    {
        Ray ray;
        RaycastHit hit;

        if (usePixelation)
        {
            ray = GetScaledRayFromMousePosition(targetCamera);
        }
        else
        {
            ray = targetCamera.ScreenPointToRay(Input.mousePosition);
        }

        if (Physics.Raycast(ray, out hit, 200f, mask))
        {
            Vector3 localHitPoint = hit.collider.transform.InverseTransformPoint(hit.point);
            Vector3 scale = hit.collider.transform.localScale;

            Vector2 newPosition = new Vector2(localHitPoint.x * scale.x, localHitPoint.z * scale.z);

            if (SNAP_GRID_SIZE > 0)
            {
                newPosition.x = Mathf.Round(newPosition.x / SNAP_GRID_SIZE) * SNAP_GRID_SIZE;
                newPosition.y = Mathf.Round(newPosition.y / SNAP_GRID_SIZE) * SNAP_GRID_SIZE;
            }

            return Vector2Int.RoundToInt(newPosition);
        }

        return Vector2Int.zero;
    }

    public static GameObject RaycastObject(bool usePixelation, Camera targetCamera, LayerMask maskToInvert = default)
    {
        Ray ray;
        RaycastHit hit;

        if (usePixelation)
        {
            ray = GetScaledRayFromMousePosition(targetCamera);
        }
        else
        {
            ray = targetCamera.ScreenPointToRay(Input.mousePosition);
        }

        if (maskToInvert != default)
        {
            int layerMask = maskToInvert;
            layerMask = ~layerMask;

            if (Physics.Raycast(ray, out hit, 200f, layerMask))
            {
                return hit.collider.gameObject;
            }

            return null;
        }

        if (Physics.Raycast(ray, out hit))
        {
            return hit.collider.gameObject;
        }

        return null;
    }

    private static Ray GetScaledRayFromMousePosition(Camera targetCamera)
    {
        Vector2 mouseScreenPosition = Input.mousePosition;

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        float renderTextureWidth = GameStats.PixelationResolution.x;
        float renderTextureHeight = GameStats.PixelationResolution.y;

        float widthRatio = screenWidth / renderTextureWidth;
        float heightRatio = screenHeight / renderTextureHeight;

        Vector3 scaledMousePosition = new Vector3(mouseScreenPosition.x / widthRatio, mouseScreenPosition.y / heightRatio, 0);
       
        Ray ray = targetCamera.ScreenPointToRay(scaledMousePosition);

        return ray;
    }
}
