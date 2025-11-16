using UnityEngine;
using TMPro;

[ExecuteAlways]
[DisallowMultipleComponent]
public class TMPOutline : MonoBehaviour
{
    public float outlineWidth = 0.3f;
    public Color outlineColor = Color.black;

    TMP_Text text;
    Material runtimeMat;

    void OnEnable()
    {
        Apply();
    }

    void OnValidate()
    {
        Apply();
    }

    void Apply()
    {
        if (text == null)
            text = GetComponent<TMP_Text>();
        if (text == null)
            return;

        // Luôn dùng sharedMaterial nếu material gốc bị destroy
        Material baseMat = text.fontSharedMaterial;

        if (baseMat == null)
        {
            if (text.font != null && text.font.material != null)
                baseMat = text.font.material;     // fallback cuối
            else
                return;
        }

        // Nếu chưa có instance hoặc instance bị mất -> tạo mới 1 lần
        if (runtimeMat == null || runtimeMat.Equals(null))
        {
            runtimeMat = new Material(baseMat);
            runtimeMat.name = baseMat.name + " (Instance Outline)";
            text.fontMaterial = runtimeMat;
        }

        // Apply outline
        runtimeMat.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);
        runtimeMat.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);
    }

    void OnDisable()
    {
        if (!Application.isPlaying && runtimeMat != null && !runtimeMat.Equals(null))
        {
            DestroyImmediate(runtimeMat);
        }
    }
}
