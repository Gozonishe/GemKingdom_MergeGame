using UnityEngine;

public sealed class ClockHandRotator : MonoBehaviour
{
    [SerializeField] private RectTransform target;
    [SerializeField] private float moveDuration = 0.18f;
    [SerializeField] private float pauseDuration = 0.5f;
    [SerializeField] private bool clockwise = true;
    [SerializeField] private bool useUnscaledTime = true;

    private float baseAngle;
    private float fromAngle;
    private float toAngle;
    private float moveElapsed;
    private float pauseElapsed;
    private int stepIndex;
    private bool isMoving;

    private void Awake()
    {
        if (target == null)
        {
            target = transform as RectTransform;
        }
    }

    private void OnEnable()
    {
        if (target == null)
        {
            return;
        }

        baseAngle = target.localEulerAngles.z;
        stepIndex = 0;
        isMoving = false;
        moveElapsed = 0f;
        pauseElapsed = 0f;
        SetAngle(baseAngle);
    }

    private void Update()
    {
        if (target == null)
        {
            return;
        }

        var deltaTime = GetDeltaTime();
        if (!isMoving)
        {
            pauseElapsed += deltaTime;
            if (pauseElapsed < pauseDuration)
            {
                return;
            }

            BeginMove();
        }

        Move(deltaTime);
    }

    private void BeginMove()
    {
        isMoving = true;
        moveElapsed = 0f;
        pauseElapsed = 0f;
        fromAngle = GetAngleForStep(stepIndex);
        stepIndex++;
        toAngle = GetAngleForStep(stepIndex);
    }

    private void Move(float deltaTime)
    {
        if (moveDuration <= 0f)
        {
            CompleteMove();
            return;
        }

        moveElapsed += deltaTime;
        var progress = Mathf.Clamp01(moveElapsed / moveDuration);
        var easedProgress = Mathf.SmoothStep(0f, 1f, progress);
        SetAngle(Mathf.Lerp(fromAngle, toAngle, easedProgress));

        if (progress >= 1f)
        {
            CompleteMove();
        }
    }

    private void CompleteMove()
    {
        SetAngle(toAngle);
        if (stepIndex >= 4)
        {
            stepIndex = 0;
        }

        isMoving = false;
        pauseElapsed = 0f;
    }

    private float GetAngleForStep(int index)
    {
        var direction = clockwise ? -1f : 1f;
        return baseAngle + (90f * index * direction);
    }

    private void SetAngle(float angle)
    {
        target.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }
}
