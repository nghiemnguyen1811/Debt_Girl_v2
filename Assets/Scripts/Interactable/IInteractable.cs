using UnityEngine;

public interface IInteractable
{
    // === Interaction Lifecycle ===
    void OnEnter();
    void OnExit();
    void OnInteract();
    void OnStopInteract();

    // === Data and Properties ===
    InteractableDataSO Data { get; }
    Vector3 MoodIconOffset { get; }
    GameObject InteractParticle { get; }

    // === Interaction Behavior ===
    Transform GetInteractPoint();
    string GetAnimationName();
    float GetDuration();
}
