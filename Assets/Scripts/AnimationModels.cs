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

    public static IEnumerator DropCellEntities(float duration, float bounceDuration, MonoBehaviour monoBehaviourInstance)
    {
        Vector3 offset = Vector3.forward * -15f;

        for (int i = 0; i < LevelController.Instance.CellEntities.Count; i++)
        {
            Vector3 startPosition = LevelController.Instance.CellEntities[i].EntityObject.transform.position;
            Vector3 endPosition = LevelController.Instance.CellEntities[i].EntityObject.transform.position - offset;

            float elapsedTime = 0f;
            float durationPerChild = duration / LevelController.Instance.CellEntities.Count;

            while (elapsedTime < durationPerChild)
            {
                LevelController.Instance.CellEntities[i].EntityObject.transform.position = Vector3.Lerp(startPosition, endPosition, elapsedTime / durationPerChild);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            LevelController.Instance.CellEntities[i].EntityObject.transform.position = endPosition;

            if (AudioController.Instance != null)
            {
                AudioController.Instance.PlaySFX(AudioClipLibrary.AudioClipNames.PlaceShape);
            }

            monoBehaviourInstance.StartCoroutine(BounceEffect(LevelController.Instance.CellEntities[i].EntityObject.transform, endPosition, bounceDuration));
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

    public static IEnumerator TossInDirection(GameObject obj, Vector2 direction, float duration = 0.25f, float flyDistance = 1f)
    {
        Vector3 startPosition = obj.transform.position;
        Quaternion startRotation = obj.transform.rotation;

        float elapsedTime = 0f;
        Vector3 peakPosition = startPosition + new Vector3(direction.x, direction.y, -1f).normalized * flyDistance;
        Vector3 endPosition = new Vector3(peakPosition.x, peakPosition.y, startPosition.z);

        // Determine the target rotation based on the direction
        Quaternion targetRotation;

        if (direction == Vector2.right)
        {
            targetRotation = startRotation * Quaternion.Euler(0, 0, -90);
        }
        else if (direction == -Vector2.right)
        {
            targetRotation = startRotation * Quaternion.Euler(0, 0, 90);
        }
        else if (direction == Vector2.up)
        {
            targetRotation = Quaternion.Euler(0, startRotation.eulerAngles.y, startRotation.eulerAngles.z);
        }
        else if (direction == Vector2.down)
        {
            targetRotation = Quaternion.Euler(-180, startRotation.eulerAngles.y, startRotation.eulerAngles.z);
        }
        else if (direction == new Vector2(1, 1)) // Up-Right
        {
            targetRotation = Quaternion.Euler(0, startRotation.eulerAngles.y, -45);
        }
        else if (direction == new Vector2(-1, 1)) // Up-Left
        {
            targetRotation = Quaternion.Euler(0, startRotation.eulerAngles.y, 45);
        }
        else if (direction == new Vector2(1, -1)) // Down-Right
        {
            targetRotation = Quaternion.Euler(0, startRotation.eulerAngles.y, -135);
        }
        else //(direction == new Vector2(-1, -1)) // Down-Left
        {
            targetRotation = Quaternion.Euler(0, startRotation.eulerAngles.y, 135);
        }

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;

            if (t < 0.5f)
            {
                // First half: Fly towards peak position
                float tHalf = t * 2;
                obj.transform.position = Vector3.Lerp(startPosition, peakPosition, tHalf);
            }
            else
            {
                // Second half: Fly back to original Z position
                float tHalf = (t - 0.5f) * 2;
                obj.transform.position = Vector3.Lerp(peakPosition, endPosition, tHalf);
            }

            // Apply rotation towards the target rotation
            obj.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the final position and rotation are set
        obj.transform.position = endPosition;
        obj.transform.rotation = targetRotation; // Final rotation should be 90 degrees in the specified direction
    }

    public static IEnumerator RandomRotateSnap(GameObject obj)
    {
        float rotateSpeed = 50f;
        float tossUpHeight = 2f;
        float startHeight = -0.75f;

        float[] angleIncrements = { 0f, 90f, 180f, 270f };

        bool isRotating = true;

        Vector3 startPosition = obj.transform.position;
        Vector3 endPosition = new Vector3(startPosition.x, startPosition.y, startPosition.z - tossUpHeight);

        Vector3 startEulerAngles = obj.transform.eulerAngles;

        Vector3 endEulerAngles = new Vector3(startEulerAngles.x + angleIncrements[Random.Range(0, angleIncrements.Length)] + 180f,
            startEulerAngles.y + angleIncrements[Random.Range(0, angleIncrements.Length)] + 180f, startEulerAngles.z);

        float distanceBetweenAnglesX = endEulerAngles.x - startEulerAngles.x;
        float distanceBetweenAnglesY = endEulerAngles.y - startEulerAngles.y;

        while (isRotating)
        {
            if (startPosition.z > endPosition.z)
            {
                startPosition = new Vector3(startPosition.x, startPosition.y, Mathf.MoveTowards(startPosition.z, endPosition.z, tossUpHeight * 2f / rotateSpeed * 100 * Time.deltaTime));

                obj.transform.position = startPosition;
            }
            else
            {
                endPosition = new Vector3(endPosition.x, endPosition.y, endPosition.z + tossUpHeight);

                startPosition = new Vector3(startPosition.x, startPosition.y, Mathf.MoveTowards(startPosition.z, endPosition.z, tossUpHeight * 2f / rotateSpeed * 100 * Time.deltaTime));

                obj.transform.position = startPosition;
            }


            startEulerAngles = new Vector3(Mathf.MoveTowards(startEulerAngles.x, endEulerAngles.x, distanceBetweenAnglesX / rotateSpeed * 100 * Time.deltaTime),
                 Mathf.MoveTowards(startEulerAngles.y, endEulerAngles.y, distanceBetweenAnglesY / rotateSpeed * 100 * Time.deltaTime), startEulerAngles.z);

            obj.transform.eulerAngles = startEulerAngles;

            if (startEulerAngles.x >= endEulerAngles.x - 0.5f && startEulerAngles.y >= endEulerAngles.y - 0.5f)
            {
                obj.transform.eulerAngles = new Vector3(Mathf.Round(startEulerAngles.x), Mathf.Round(startEulerAngles.y), startEulerAngles.z);

                obj.transform.position = new Vector3(startPosition.x, startPosition.y, startHeight);

                isRotating = false;
            }

            yield return null;
        }
    }

    public static IEnumerator SetFundamentalShapesBoard(GameObject board, float boardDuration, float childrenDuration, float bounceDuration, MonoBehaviour monoBehaviourInstance)
    {
        Vector3 offset = Vector3.forward * 15f;

        for (int i = 0; i < board.transform.childCount; i++)
        {
            Transform child = board.transform.GetChild(i);
            child.position += offset;
        }

        Vector3 startPosition = board.transform.GetChild(0).position;
        Vector3 endPosition = board.transform.GetChild(0).position - offset;

        float elapsedTime = 0f;

        while (elapsedTime < 1f)
        {
            board.transform.GetChild(0).position = Vector3.Lerp(startPosition, endPosition, elapsedTime / boardDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        board.transform.GetChild(0).position = endPosition;

        if (AudioController.Instance != null)
        {
            AudioController.Instance.PlaySFX(AudioClipLibrary.AudioClipNames.PlaceShape);
        }

        for (int k = 0; k < board.transform.GetChild(0).childCount; k++)
        {
            if (board.transform.GetChild(0).transform.GetChild(k).TryGetComponent(out ObjectAnimator objectAnimator))
            {
                objectAnimator.StartAnimating = true;
            }
        }

        monoBehaviourInstance.StartCoroutine(BounceEffect(board.transform.GetChild(0), endPosition, bounceDuration));
        monoBehaviourInstance.StartCoroutine(SetBoardAndChildren(board, childrenDuration, bounceDuration, monoBehaviourInstance, true));
    }

    public static IEnumerator SetBoardAndChildren(GameObject boardWithChildren, float duration, float bounceDuration, MonoBehaviour monoBehaviourInstance, bool excludeFirstChild = false)
    {
        Vector3 offset = Vector3.forward * 15f;

        if (!excludeFirstChild)
        {
            for (int i = 0; i < boardWithChildren.transform.childCount; i++)
            {
                Transform child = boardWithChildren.transform.GetChild(i);
                child.position += offset;
            }
        }

        for (int i = 0; i < boardWithChildren.transform.childCount; i++)
        {
            if (excludeFirstChild && i == 0)
            {
                continue;
            }

            Transform child = boardWithChildren.transform.GetChild(i);
            Vector3 startPosition = child.position;
            Vector3 endPosition = child.position - offset;

            float elapsedTime = 0f;
            float durationPerChild = duration / boardWithChildren.transform.childCount;

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

            for (int k = 0; k < child.transform.childCount; k++)
            {
                if (child.transform.GetChild(k).TryGetComponent(out ObjectAnimator objectAnimator))
                {
                    objectAnimator.StartAnimating = true;
                }
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