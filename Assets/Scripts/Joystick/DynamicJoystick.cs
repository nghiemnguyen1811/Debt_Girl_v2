using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class DynamicJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header(" Settings ")]
    [SerializeField] private float moveRadius = 100f;
    [SerializeField] private float knobSmoothSpeed = 10f;

    [Header(" UI ")]
    [SerializeField] private RectTransform joystickRoot;
    [SerializeField] private RectTransform knob;

    [HideInInspector] public Vector2 direction;
    private Vector2 startTouchPosition;
    private Vector2 targetKnobPosition;
    private bool isDragging = false;

    void Start()
    {
        joystickRoot.gameObject.SetActive(false);
        knob.anchoredPosition = Vector2.zero;
        targetKnobPosition = Vector2.zero;
    }

    void Update()
    {
        if (joystickRoot.gameObject.activeSelf)
            knob.anchoredPosition = Vector2.Lerp(knob.anchoredPosition, targetKnobPosition, Time.deltaTime * knobSmoothSpeed);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsPointerOverRealUI(eventData))
            return;

        joystickRoot.gameObject.SetActive(true);
        joystickRoot.position = eventData.position;
        startTouchPosition = eventData.position;
        knob.anchoredPosition = Vector2.zero;
        targetKnobPosition = Vector2.zero;
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        Vector2 delta = eventData.position - startTouchPosition;
        delta = Vector2.ClampMagnitude(delta, moveRadius);
        targetKnobPosition = delta;
        direction = delta / moveRadius;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        joystickRoot.gameObject.SetActive(false);
        direction = Vector2.zero;
        knob.anchoredPosition = Vector2.zero;
        targetKnobPosition = Vector2.zero;
        isDragging = false;
    }

    /// <summary>
    /// Kiểm tra xem pointer có đang nhấn vào UI có tag "UIBlock" không
    /// </summary>
    private bool IsPointerOverRealUI(PointerEventData eventData)
    {
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            if (result.gameObject.layer == LayerMask.NameToLayer("UI"))
                return true;
        }

        return false;
    }
}
