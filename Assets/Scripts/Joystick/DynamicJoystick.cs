using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Dynamic joystick that appears on touch/click and controls a 2D direction vector.
/// </summary>
public class DynamicJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    #region === Settings ===

    [Header(" Settings ")]
    [SerializeField] private float moveRadius = 100f;
    [SerializeField] private float knobSmoothSpeed = 10f;

    [HideInInspector] public Vector2 direction; // Normalized direction of movement

    private Vector2 startTouchPosition;
    private Vector2 targetKnobPosition;
    private bool isDragging = false;

    #endregion

    #region === UI References ===

    [Header(" UI ")]
    [SerializeField] private RectTransform joystickRoot;
    [SerializeField] private RectTransform knob;

    #endregion

    #region === Unity Events ===

    private void Start()
    {
        joystickRoot.gameObject.SetActive(false);
        knob.anchoredPosition = Vector2.zero;
        targetKnobPosition = Vector2.zero;
    }

    private void Update()
    {
        if (joystickRoot.gameObject.activeSelf)
            knob.anchoredPosition = Vector2.Lerp(knob.anchoredPosition, targetKnobPosition, Time.deltaTime * knobSmoothSpeed);
    }

    #endregion

    #region === Pointer Event Handlers ===

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

    #endregion

    #region === Helper Methods ===

    /// <summary>
    /// Check if the pointer is over a UI element with "UI" layer.
    /// Prevents joystick from triggering under menus, etc.
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

    #endregion
}
