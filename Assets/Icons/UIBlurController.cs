using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UIBlurController : MonoBehaviour
{
    [Range(0f, 10f)]
    public float blurSize = 2f;

    [Tooltip("Should the blur start enabled when the scene loads?")]
    public bool blurOnStart = true;

    private Material runtimeMaterial;
    private Image img;

    private static readonly int SizeProp = Shader.PropertyToID("_Size");

    private void Awake()
    {
        img = GetComponent<Image>();
        if (img == null)
        {
            Debug.LogError("UIBlurController requires an Image component.", this);
            enabled = false;
            return;
        }

        // Create a unique material instance so changes don't affect other UI elements
        if (img.material != null)
        {
            runtimeMaterial = new Material(img.material);
            img.material = runtimeMaterial;
        }
        else
        {
            Debug.LogError("UIBlurController: Image has no material. Assign a blur material in the Inspector.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        SetBlur(blurOnStart ? blurSize : 0f);
    }

    /// <summary>
    /// Set the blur amount (0 means no blur).
    /// </summary>
    public void SetBlur(float size)
    {
        if (runtimeMaterial == null) return;
        runtimeMaterial.SetFloat(SizeProp, size);
    }

    public void EnableBlur()
    {
        SetBlur(blurSize);
    }

    public void DisableBlur()
    {
        SetBlur(0f);
    }
}
