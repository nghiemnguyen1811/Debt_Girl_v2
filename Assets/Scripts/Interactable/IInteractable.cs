using UnityEngine;

public interface IInteractable
{
    void OnEnter();
    void OnExit();
    void OnInteract();
    void OnStopInteract();
    Transform GetInteractPoint();
    string GetAnimationName();
    float GetDuration();

    InteractableDataSO Data { get; }
    Vector3 MoodIconOffset { get; }
}
