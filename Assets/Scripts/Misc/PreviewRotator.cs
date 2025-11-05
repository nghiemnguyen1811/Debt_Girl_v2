using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// Allows the user to rotate a preview model by dragging and reset it smoothly to the initial rotation.
/// </summary>
public class PreviewRotator : MonoBehaviour, IDragHandler
{
    // ══════════════════════════════════════════════════════
    // 🔧 INSPECTOR FIELDS
    // ══════════════════════════════════════════════════════
    [Header("Target Settings")]
    [SerializeField] private Transform targetModel;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float resetDuration = 0.6f;

    // ══════════════════════════════════════════════════════
    // 🧠 RUNTIME DATA
    // ══════════════════════════════════════════════════════
    private Quaternion initialRotation;
    private Tween rotationTween;

    // ══════════════════════════════════════════════════════
    // 🏁 UNITY EVENTS
    // ══════════════════════════════════════════════════════
    private void Start()
    {
        if (targetModel != null)
            initialRotation = targetModel.rotation;
    }

    private void OnDisable()
    {
        // Clean up any active tween to avoid memory leaks
        rotationTween?.Kill();
    }

    // ══════════════════════════════════════════════════════
    // 🧩 INTERACTIONS
    // ══════════════════════════════════════════════════════
    /// <summary>
    /// Rotates the model horizontally when dragging.
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (targetModel == null) return;

        // Cancel tween if user starts rotating manually
        rotationTween?.Kill();

        float deltaX = eventData.delta.x;
        targetModel.Rotate(Vector3.up, -deltaX * rotationSpeed * Time.deltaTime, Space.World);
    }

    // ══════════════════════════════════════════════════════
    // 🎬 PUBLIC METHODS
    // ══════════════════════════════════════════════════════
    /// <summary>
    /// Smoothly resets the model to its initial rotation using DOTween.
    /// </summary>
    public void ResetRotation()
    {
        if (targetModel == null) return;

        // Stop any existing tween
        rotationTween?.Kill();

        // Smoothly rotate back to the original rotation
        rotationTween = targetModel
            .DORotateQuaternion(initialRotation, resetDuration)
            .SetEase(Ease.OutQuad);
    }
}
