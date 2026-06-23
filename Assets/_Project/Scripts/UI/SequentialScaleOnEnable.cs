using System.Collections;
using System;
using UnityEngine;

public sealed class SequentialScaleOnEnable : MonoBehaviour
{
    [Serializable]
    private sealed class ParallelGroup
    {
        public string mainItemName;
        public string[] itemNames;
    }

    [SerializeField] private Transform elementsRoot;
    [SerializeField] private float itemDuration = 0.18f;
    [SerializeField] private float itemDelay = 0.08f;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField]
    private ParallelGroup[] parallelGroups =
    {
        new ParallelGroup { mainItemName = "Art2", itemNames = new[] { "ImgArrow1" } },
        new ParallelGroup { mainItemName = "Art3", itemNames = new[] { "ImgArrow2" } }
    };

    private Coroutine playRoutine;
    private Transform[] items = new Transform[0];
    private Vector3[] targetScales = new Vector3[0];

    private void OnEnable()
    {
        Play();
    }

    private void OnDisable()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        RestoreScales();
    }

    public void Play()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
        }

        CacheItems();
        playRoutine = StartCoroutine(PlaySequence());
    }

    private void CacheItems()
    {
        var root = elementsRoot != null ? elementsRoot : transform;
        var childCount = root.childCount;

        if (items.Length != childCount)
        {
            items = new Transform[childCount];
            targetScales = new Vector3[childCount];
        }

        for (var i = 0; i < childCount; i++)
        {
            var item = root.GetChild(i);
            items[i] = item;
            targetScales[i] = item.localScale;
        }
    }

    private IEnumerator PlaySequence()
    {
        for (var i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].gameObject.activeSelf)
            {
                items[i].localScale = Vector3.zero;
            }
        }

        for (var i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (item == null || !item.gameObject.activeSelf || IsParallelChild(item))
            {
                continue;
            }

            var parallelItems = FindParallelItems(item);
            if (parallelItems.Length == 0)
            {
                yield return ScaleItem(item, targetScales[i]);
            }
            else
            {
                yield return ScaleItemsTogether(item, targetScales[i], parallelItems);
            }

            if (itemDelay > 0f && i < items.Length - 1)
            {
                yield return Wait(itemDelay);
            }
        }

        playRoutine = null;
    }

    private IEnumerator ScaleItem(Transform item, Vector3 targetScale)
    {
        if (itemDuration <= 0f)
        {
            item.localScale = targetScale;
            yield break;
        }

        var elapsed = 0f;
        while (elapsed < itemDuration)
        {
            elapsed += GetDeltaTime();
            var progress = Mathf.Clamp01(elapsed / itemDuration);
            var easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            item.localScale = targetScale * easedProgress;
            yield return null;
        }

        item.localScale = targetScale;
    }

    private IEnumerator ScaleItemsTogether(Transform mainItem, Vector3 mainTargetScale, Transform[] parallelItems)
    {
        if (itemDuration <= 0f)
        {
            mainItem.localScale = mainTargetScale;
            for (var i = 0; i < parallelItems.Length; i++)
            {
                SetTargetScale(parallelItems[i]);
            }

            yield break;
        }

        var elapsed = 0f;
        while (elapsed < itemDuration)
        {
            elapsed += GetDeltaTime();
            var progress = Mathf.Clamp01(elapsed / itemDuration);
            var easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            mainItem.localScale = mainTargetScale * easedProgress;

            for (var i = 0; i < parallelItems.Length; i++)
            {
                var parallelItem = parallelItems[i];
                var parallelIndex = GetItemIndex(parallelItem);
                if (parallelItem != null && parallelIndex >= 0)
                {
                    parallelItem.localScale = targetScales[parallelIndex] * easedProgress;
                }
            }

            yield return null;
        }

        mainItem.localScale = mainTargetScale;
        for (var i = 0; i < parallelItems.Length; i++)
        {
            SetTargetScale(parallelItems[i]);
        }
    }

    private Transform[] FindParallelItems(Transform mainItem)
    {
        if (parallelGroups == null)
        {
            return Array.Empty<Transform>();
        }

        for (var i = 0; i < parallelGroups.Length; i++)
        {
            var group = parallelGroups[i];
            if (group == null || group.mainItemName != mainItem.name || group.itemNames == null)
            {
                continue;
            }

            var result = new Transform[group.itemNames.Length];
            var resultCount = 0;

            for (var j = 0; j < group.itemNames.Length; j++)
            {
                var item = FindItemByName(group.itemNames[j]);
                if (item != null && item.gameObject.activeSelf)
                {
                    result[resultCount] = item;
                    resultCount++;
                }
            }

            if (resultCount == result.Length)
            {
                return result;
            }

            Array.Resize(ref result, resultCount);
            return result;
        }

        return Array.Empty<Transform>();
    }

    private bool IsParallelChild(Transform item)
    {
        if (parallelGroups == null || item == null)
        {
            return false;
        }

        for (var i = 0; i < parallelGroups.Length; i++)
        {
            var group = parallelGroups[i];
            if (group == null || group.itemNames == null)
            {
                continue;
            }

            for (var j = 0; j < group.itemNames.Length; j++)
            {
                if (group.itemNames[j] == item.name)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private Transform FindItemByName(string itemName)
    {
        for (var i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].name == itemName)
            {
                return items[i];
            }
        }

        return null;
    }

    private int GetItemIndex(Transform item)
    {
        for (var i = 0; i < items.Length; i++)
        {
            if (items[i] == item)
            {
                return i;
            }
        }

        return -1;
    }

    private void SetTargetScale(Transform item)
    {
        var index = GetItemIndex(item);
        if (item != null && index >= 0)
        {
            item.localScale = targetScales[index];
        }
    }

    private IEnumerator Wait(float delay)
    {
        if (useUnscaledTime)
        {
            yield return new WaitForSecondsRealtime(delay);
        }
        else
        {
            yield return new WaitForSeconds(delay);
        }
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private void RestoreScales()
    {
        for (var i = 0; i < items.Length; i++)
        {
            if (items[i] != null)
            {
                items[i].localScale = targetScales[i];
            }
        }
    }
}
