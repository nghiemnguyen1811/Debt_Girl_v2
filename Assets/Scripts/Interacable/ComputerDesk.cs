using UnityEngine;

public class ComputerDesk : MonoBehaviour, IInteractable
{
    [Header(" Elements ")]
    [SerializeField] private Transform interactPoint;

    [Header(" Settings ")]
    [SerializeField] private string objectName = "Đồ vật không tên";
    [SerializeField] private string animationName;

    public string GetObjectName()
    {
        return objectName;
    }

    public Transform GetInteractPoint()
    {
        return interactPoint;
    }

    public void OnInteract()
    {
        Debug.Log($"Đã nhấn vào: {objectName}");
    }
}