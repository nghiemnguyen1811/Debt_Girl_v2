using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MoneyRainUI : MonoBehaviour
{
    [Header("Parent Containers")]
    [SerializeField] private RectTransform moneyParent;
    [SerializeField] private RectTransform coinParent;
    [SerializeField] private RectTransform paperParent;

    [Header("UI Canvas Area")]
    [SerializeField] private RectTransform spawnAreaCanvas;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 0.5f;
    [SerializeField] private float spawnOffsetY = 100f;
    [SerializeField] private float despawnOffsetY = 100f;

    private float halfCanvasWidth;
    private float halfCanvasHeight;

    private enum ObjectType { Money, Coin, Paper }

    private class FallingObject
    {
        public RectTransform rect;
        public float swayOffset;
        public float fallSpeed;
        public float swayAmount;
        public float rotationSpeed;
        public float rotationOffset;
        public bool isActive;
        public ObjectType type;
    }

    private readonly List<FallingObject> allObjects = new();
    private readonly List<Rect> activeRects = new();

    void Start()
    {
        if (spawnAreaCanvas == null)
        {
            Debug.LogError("Spawn Area Canvas is not assigned.");
            enabled = false;
            return;
        }

        Vector2 canvasSize = spawnAreaCanvas.rect.size;
        halfCanvasWidth = canvasSize.x * 0.5f;
        halfCanvasHeight = canvasSize.y * 0.5f;

        InitFromParent(moneyParent, 50, 150, 20, 50, 30, ObjectType.Money);
        InitFromParent(coinParent, 150, 250, 0, 0, 360, ObjectType.Coin);
        InitFromParent(paperParent, 30, 80, 60, 100, 20, ObjectType.Paper);

        StartCoroutine(SpawnRoutine());
    }

    void Update()
    {
        float time = Time.time;
        float bottomY = -halfCanvasHeight - despawnOffsetY;
        activeRects.Clear();

        foreach (var obj in allObjects)
        {
            if (!obj.isActive) continue;

            UpdateFallingObject(obj, time);

            Vector2 size = obj.rect.rect.size;
            Vector2 pos = obj.rect.anchoredPosition;
            activeRects.Add(new Rect(pos.x - size.x * 0.5f, pos.y - size.y * 0.5f, size.x, size.y));

            if (pos.y < bottomY)
                Deactivate(obj);
        }
    }

    void InitFromParent(RectTransform parent, float minSpeed, float maxSpeed, float minSway, float maxSway, float rotationSpeed, ObjectType type)
    {
        if (parent == null) return;

        foreach (Transform child in parent)
        {
            if (child is not RectTransform rect) continue;
            rect.gameObject.SetActive(false);

            allObjects.Add(new FallingObject
            {
                rect = rect,
                isActive = false,
                swayOffset = Random.Range(0f, 2f * Mathf.PI),
                fallSpeed = Random.Range(minSpeed, maxSpeed),
                swayAmount = Random.Range(minSway, maxSway),
                rotationSpeed = rotationSpeed,
                rotationOffset = Random.Range(0f, 360f),
                type = type
            });
        }
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(spawnInterval * 0.8f, spawnInterval * 1.5f));
            var obj = GetInactiveObject();
            if (obj == null || !FindSpawnPosition(obj.rect, out Vector2 pos)) continue;

            Activate(obj, pos);
        }
    }

    void UpdateFallingObject(FallingObject obj, float time)
    {
        float t = time + obj.swayOffset;
        obj.rect.anchoredPosition += Vector2.down * obj.fallSpeed * Time.deltaTime;

        switch (obj.type)
        {
            case ObjectType.Money:
            case ObjectType.Paper:
                obj.rect.anchoredPosition += Vector2.right * Mathf.Sin(t) * obj.swayAmount * Time.deltaTime;
                obj.rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t) * obj.rotationSpeed);
                break;
            case ObjectType.Coin:
                float angle = (t * obj.rotationSpeed + obj.rotationOffset) % 360f;
                obj.rect.localRotation = Quaternion.Euler(0f, 0f, angle);
                break;
        }
    }

    void Activate(FallingObject obj, Vector2 pos)
    {
        obj.rect.anchoredPosition = pos;
        obj.rect.localRotation = Quaternion.identity;
        obj.rect.gameObject.SetActive(true);
        obj.isActive = true;

        Vector2 size = obj.rect.rect.size;
        activeRects.Add(new Rect(pos.x - size.x * 0.5f, pos.y - size.y * 0.5f, size.x, size.y));
    }

    void Deactivate(FallingObject obj)
    {
        obj.rect.gameObject.SetActive(false);
        obj.isActive = false;
    }

    bool FindSpawnPosition(RectTransform rect, out Vector2 result)
    {
        Vector2 size = rect.rect.size;
        float y = halfCanvasHeight + size.y * 0.5f + spawnOffsetY;

        for (int i = 0; i < 10; i++)
        {
            float x = Random.Range(-halfCanvasWidth + size.x * 0.5f, halfCanvasWidth - size.x * 0.5f);
            Rect candidate = new(x - size.x * 0.5f, y - size.y * 0.5f, size.x, size.y);
            if (!activeRects.Exists(r => r.Overlaps(candidate)))
            {
                result = new Vector2(x, y);
                return true;
            }
        }

        result = Vector2.zero;
        return false;
    }

    FallingObject GetInactiveObject()
    {
        for (int i = 0; i < 10; i++)
        {
            int index = Random.Range(0, allObjects.Count);
            if (!allObjects[index].isActive)
                return allObjects[index];
        }

        return allObjects.Find(obj => !obj.isActive);
    }
}
