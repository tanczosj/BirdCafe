using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StaminaBarMaterialFill : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Stamina targetStamina;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text staminaText;

    [Header("Shader Settings")]
    [SerializeField] private string fillPropertyName = "_FillAmount";
    [SerializeField] private float refillSmoothSpeed = 14f;
    [SerializeField] private bool snapDownImmediately = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private Material runtimeMaterial;
    private float displayedFill = 1f;
    private float targetFill = 1f;

    private void Awake()
    {
        if (fillImage == null)
            fillImage = GetComponent<Image>();

        if (fillImage != null && fillImage.material != null)
        {
            runtimeMaterial = Instantiate(fillImage.material);
            fillImage.material = runtimeMaterial;
        }

        if (targetStamina == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                targetStamina = player.GetComponent<Stamina>();

            if (targetStamina == null)
                targetStamina = FindFirstObjectByType<Stamina>();
        }
    }

    private void OnEnable()
    {
        if (targetStamina != null)
            targetStamina.OnStaminaChanged += HandleStaminaChanged;
    }

    private void Start()
    {
        RefreshInstant();
    }

    private void OnDisable()
    {
        if (targetStamina != null)
            targetStamina.OnStaminaChanged -= HandleStaminaChanged;
    }

    private void Update()
    {
        if (runtimeMaterial == null)
            return;

        if (displayedFill < targetFill)
        {
            displayedFill = Mathf.Lerp(
                displayedFill,
                targetFill,
                1f - Mathf.Exp(-refillSmoothSpeed * Time.deltaTime));
        }
        else
        {
            displayedFill = targetFill;
        }

        runtimeMaterial.SetFloat(fillPropertyName, displayedFill);
        RefreshText();
    }

    private void HandleStaminaChanged(float current, float max)
    {
        float newFill = max > 0f ? current / max : 0f;
        targetFill = newFill;

        if (snapDownImmediately && displayedFill > newFill)
        {
            displayedFill = newFill;

            if (runtimeMaterial != null)
                runtimeMaterial.SetFloat(fillPropertyName, displayedFill);
        }

        RefreshText();

        if (debugLogs)
            Debug.Log("Stamina bar updated: " + current + " / " + max);
    }

    private void RefreshInstant()
    {
        if (targetStamina == null || runtimeMaterial == null)
            return;

        targetFill = targetStamina.Max > 0f
            ? targetStamina.Current / targetStamina.Max
            : 0f;

        displayedFill = targetFill;
        runtimeMaterial.SetFloat(fillPropertyName, displayedFill);
        RefreshText();
    }

    private void RefreshText()
    {
        if (staminaText != null && targetStamina != null)
        {
            staminaText.text = Mathf.CeilToInt(targetStamina.Current) + " / " + Mathf.CeilToInt(targetStamina.Max);
        }
    }
}