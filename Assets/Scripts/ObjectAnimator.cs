using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectAnimator : MonoBehaviour
{
    public enum AnimationTypes
    {
        None,
        Rotate,
        Hover,
        ColorPulsate,
        MoveBackAndForth
    }

    [SerializeField] private List<AnimationTypes> currentAnimations;

    [SerializeField] private Vector3 rotationAngle = Vector3.zero;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float rotationInterval = 0f;
    [SerializeField] private float rotationDurationIfInterval = 0.5f;

    [SerializeField] private float hoverStartOffset = 1f;
    [SerializeField] private float hoverRange = 0.5f;
    [SerializeField] private float hoverSpeed = 2f;

    [SerializeField] private Color startColor = Color.white;
    [SerializeField] private Color endColor = Color.yellow;
    [SerializeField] private float colorPulsateSpeed = 0.5f;

    [SerializeField] private Vector3 moveBackAndForthPosition;
    [SerializeField] private float moveBackAndForthSpeed = 2f;

    public bool StartAnimating;
    private Vector3 originalLocalPosition;

    private float rotationTimer;

    private Vector3 hoverStartPosition;
    private float hoverTimer;
    private bool hasReachedHoverStartPosition;
    private float lerpProgressToHoverStartPosition;
    private float timeToReachHoverStartPosition = 1f;

    private Renderer objectRenderer;
    private float colorTimer;

    private bool movingToTarget = true;
    private float moveProgress = 0f;


    void Start()
    {
        originalLocalPosition = transform.localPosition;
        hoverStartPosition = originalLocalPosition + (Vector3.up * hoverStartOffset);
        objectRenderer = GetComponent<Renderer>();
    }

    void Update()
    {
        if (!StartAnimating)
        {
            return;
        }

        foreach (AnimationTypes animation in currentAnimations)
        {
            switch (animation)
            {
                case AnimationTypes.Rotate:
                    Rotate();
                    break;
                case AnimationTypes.Hover:
                    Hover();
                    break;
                case AnimationTypes.ColorPulsate:
                    ColorPulsate();
                    break;
                case AnimationTypes.MoveBackAndForth:
                    MoveBackAndForth();
                    break;
            }
        }
    }

    private void Rotate()
    {
        if (rotationInterval > 0f)
        {
            rotationTimer += Time.deltaTime;
            if (rotationTimer >= rotationInterval)
            {
                StartCoroutine(RotateOverTime());
                rotationTimer = 0f;
            }

            return;
        }

        RotateObjectWithoutInterval();
    }

    private void RotateObjectWithoutInterval()
    {
        transform.Rotate(rotationAngle * rotationSpeed * Time.deltaTime);
    }

    private IEnumerator RotateOverTime()
    {
        Quaternion startRotation = transform.rotation;
        Quaternion endRotation = transform.rotation * Quaternion.Euler(rotationAngle);
        float elapsedTime = 0f;

        while (elapsedTime < rotationDurationIfInterval)
        {
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, elapsedTime / rotationDurationIfInterval);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.rotation = endRotation;
    }

    private void Hover()
    {
        if (!hasReachedHoverStartPosition)
        {
            lerpProgressToHoverStartPosition += Time.deltaTime / timeToReachHoverStartPosition;
            transform.localPosition = Vector3.Lerp(originalLocalPosition, hoverStartPosition, lerpProgressToHoverStartPosition);

            if (lerpProgressToHoverStartPosition >= 1f)
            {
                transform.localPosition = hoverStartPosition;
                hasReachedHoverStartPosition = true;
            }

            return;
        }

        hoverTimer += Time.deltaTime * hoverSpeed;
        float newY = hoverStartPosition.y + Mathf.Sin(hoverTimer) * hoverRange;
        transform.localPosition = new Vector3(hoverStartPosition.x, newY, hoverStartPosition.z);
    }

    private void ColorPulsate()
    {
        if (objectRenderer == null)
        {
            return;
        }

        colorTimer += Time.deltaTime * colorPulsateSpeed;
        float lerpFactor = Mathf.PingPong(colorTimer, 1.0f);
        objectRenderer.material.color = Color.Lerp(startColor, endColor, lerpFactor);
    }

    private void MoveBackAndForth()
    {
        moveProgress += Time.deltaTime * moveBackAndForthSpeed;

        if (movingToTarget)
        {
            transform.localPosition = Vector3.Lerp(originalLocalPosition, moveBackAndForthPosition, moveProgress);

            if (moveProgress >= 1f)
            {
                movingToTarget = false;
                moveProgress = 0f;
            }

            return;
        }

        transform.localPosition = Vector3.Lerp(moveBackAndForthPosition, originalLocalPosition, moveProgress);

        if (moveProgress >= 1f)
        {
            movingToTarget = true;
            moveProgress = 0f;
        }
    }
}