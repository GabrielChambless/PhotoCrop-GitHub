using System.Collections;
using UnityEngine;

public static class AnimationModels
{
    private static float bounceOffset = 0.25f;

    private static float moveSpeed = 25f;
    private static float snapThreshold = 0.2f;
    private static float bounceIntensity = 0.3f;

    public static IEnumerator DropHoleCells(GameObject holeObject, float duration, float bounceDuration, MonoBehaviour monoBehaviourInstance)
    {
        Vector3 offset = Vector3.forward * -15f;

        for (int i = 0; i < holeObject.transform.childCount; i++)
        {
            Transform child = holeObject.transform.GetChild(i);
            child.position += offset;
        }

        for (int i = 0; i < holeObject.transform.childCount; i++)
        {
            Transform child = holeObject.transform.GetChild(i);
            Vector3 startPosition = child.position;
            Vector3 endPosition = child.position - offset;

            float elapsedTime = 0f;
            float durationPerChild = duration / holeObject.transform.childCount;

            while (elapsedTime < durationPerChild)
            {
                child.position = Vector3.Lerp(startPosition, endPosition, elapsedTime / durationPerChild);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            child.position = endPosition;

            if (AudioController.Instance != null)
            {
                AudioController.Instance.PlaySFX(AudioClipLibrary.AudioClipNames.PlaceShape);
            }

            monoBehaviourInstance.StartCoroutine(BounceEffect(child, endPosition, bounceDuration));
        }
    }

    public static IEnumerator RestartLevel(GameObject holeObject, float duration)
    {
        Vector3 offset = Vector3.forward * 15f;

        Vector3 holeObjectStartPosition = holeObject.transform.position;
        Vector3 holeObjectEndPosition = holeObject.transform.position + offset;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            holeObject.transform.position = Vector3.Lerp(holeObjectStartPosition, holeObjectEndPosition, elapsedTime / duration);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        holeObject.transform.position = holeObjectEndPosition;

        SceneStateManager.Instance.GoToLevel(() => LevelSelectorController.Instance.InstantiateLevelController());
    }

    public static IEnumerator LevelGridTransparency(GameObject levelGrid, Material levelGridMaterial, Color colorToLerpTo, float duration)
    {
        float startTransparency = 0f;
        float endTransparency = levelGridMaterial.GetFloat("_Transparency");

        Color startColor = levelGridMaterial.color;
        Color peakColor = colorToLerpTo;

        float peakTime = duration * 0.75f;
        float elapsedTime = 0f;

        while (elapsedTime < peakTime)
        {
            float transparency = Mathf.Lerp(startTransparency, 1f, elapsedTime / peakTime);
            levelGridMaterial.SetFloat("_Transparency", transparency);

            Color lerpedColor = Color.Lerp(startColor, peakColor, elapsedTime / peakTime);
            levelGridMaterial.color = lerpedColor;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        elapsedTime = 0f;

        // Lerp back to end transparency and original color
        while (elapsedTime < duration - peakTime)
        {
            float transparency = Mathf.Lerp(1f, endTransparency, elapsedTime / (duration - peakTime));
            levelGridMaterial.SetFloat("_Transparency", transparency);

            Color lerpedColor = Color.Lerp(peakColor, startColor, elapsedTime / (duration - peakTime));
            levelGridMaterial.color = lerpedColor;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        levelGridMaterial.SetFloat("_Transparency", endTransparency);
        levelGridMaterial.color = startColor;
    }

    public static IEnumerator MoveSnap(Transform objectTransform, Vector3 targetPos)
    {
        // Continue moving towards the target position until close enough
        while (Vector3.Distance(objectTransform.position, targetPos) > snapThreshold)
        {
            objectTransform.position = Vector3.Lerp(objectTransform.position, targetPos, (moveSpeed + 5) * Time.deltaTime);
            yield return null; // Wait for the next frame
        }

        // Calculate the overshoot position
        Vector3 overshootPos = targetPos + (targetPos - objectTransform.position).normalized * bounceIntensity;
        float bounceTime = 0;

        while (bounceTime < 1f)
        {
            objectTransform.position = Vector3.Lerp(objectTransform.position, overshootPos, bounceTime);
            bounceTime += (moveSpeed + 5) * Time.deltaTime;
            yield return null;
        }

        // Settle back to the target position
        bounceTime = 0;
        while (bounceTime < 1f)
        {
            objectTransform.position = Vector3.Lerp(objectTransform.position, targetPos, bounceTime);
            bounceTime += (moveSpeed + 5) * Time.deltaTime;
            yield return null;
        }

        objectTransform.position = targetPos;
    }

    public static IEnumerator SetChessBoard(GameObject chessBoard, float duration, float bounceDuration, MonoBehaviour monoBehaviourInstance)
    {
        Vector3 offset = Vector3.forward * 10f;

        for (int i = 0; i < chessBoard.transform.childCount; i++)
        {
            Transform child = chessBoard.transform.GetChild(i);
            child.position += offset;
        }

        for (int i = 0; i < chessBoard.transform.childCount; i++)
        {
            Transform child = chessBoard.transform.GetChild(i);
            Vector3 startPosition = child.position;
            Vector3 endPosition = child.position - offset;

            float elapsedTime = 0f;
            float durationPerChild = duration / chessBoard.transform.childCount;

            while (elapsedTime < durationPerChild)
            {
                child.position = Vector3.Lerp(startPosition, endPosition, elapsedTime / durationPerChild);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            child.position = endPosition;

            if (AudioController.Instance != null)
            {
                AudioController.Instance.PlaySFX(AudioClipLibrary.AudioClipNames.PlaceShape);
            }

            monoBehaviourInstance.StartCoroutine(BounceEffect(child, endPosition, bounceDuration));
        }
    }

    private static IEnumerator BounceEffect(Transform child, Vector3 endPosition, float bounceDuration)
    {
        Vector3 bouncePosition = endPosition + (Vector3.forward * bounceOffset);
        float elapsedTime = 0f;

        // Lerp to bounce position
        while (elapsedTime < bounceDuration / 2)
        {
            child.position = Vector3.Lerp(endPosition, bouncePosition, elapsedTime / (bounceDuration / 2));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        elapsedTime = 0f;

        // Lerp back to end position
        while (elapsedTime < bounceDuration / 2)
        {
            child.position = Vector3.Lerp(bouncePosition, endPosition, elapsedTime / (bounceDuration / 2));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        child.position = endPosition;
    }
}