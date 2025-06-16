using EPOOutline;
using UnityEngine;

public interface IInteractable
{
    Outlinable Outlinable { get; }
    InteractableDataSO Data { get; }

    string GetObjectName();
    string GetAnimationName();
    float GetDuration();
    Transform GetInteractPoint();
    void OnInteract();
    void OnStopInteract();
    void OnEnter();
    void OnExit();
}
