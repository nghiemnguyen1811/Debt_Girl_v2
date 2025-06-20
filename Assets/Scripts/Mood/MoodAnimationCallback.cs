using UnityEngine;

public class MoodAnimationCallback : MonoBehaviour
{
    [Header(" Elements ")]
    private PlayerControl playerControl;

    void Start()
    {
        playerControl = GetComponentInParent<PlayerControl>();

        if (playerControl == null)
        {
            Debug.LogWarning("[MoodAnimationCallback] Không tìm thấy PlayerControl trong cha.");
        }
    }

    // Gọi hàm này từ Animation Event và truyền layerIndex vào
    public void EndMoodAnimation()
    {
        if (playerControl?.visualizer == null) return;

        playerControl.animationHandler.ResetMoodLayerWeight();
    }
}
