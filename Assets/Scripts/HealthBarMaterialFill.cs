using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBarMaterialFill : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health targetHealth;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text healthText;

    [Header("Shader Settings")]
    [SerializeField] private string fillPropertyName = "_FillAmount";
    [SerializeField] private float refillSmoothSpeed = 10f;
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

        if (targetHealth == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                targetHealth = player.GetComponent<Health>();

            if (targetHealth == null)
                targetHealth = FindFirstObjectByType<Health>();
        }
    }

    private void OnEnable()
    {
        if (targetHealth != null)
            targetHealth.OnHealthChanged += HandleHealthChanged;
    }

    private void Start()
    {
        RefreshInstant();
    }

    private void OnDisable()
    {
        if (targetHealth != null)
            targetHealth.OnHealthChanged -= HandleHealthChanged;
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

    private void HandleHealthChanged(float current, float max)
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
            Debug.Log("Health bar updated: " + current + " / " + max);
    }

    private void RefreshInstant()
    {
        if (targetHealth == null || runtimeMaterial == null)
            return;

        targetFill = targetHealth.MaxHealth > 0f
            ? targetHealth.CurrentHealth / targetHealth.MaxHealth
            : 0f;

        displayedFill = targetFill;
        runtimeMaterial.SetFloat(fillPropertyName, displayedFill);
        RefreshText();
    }

    private void RefreshText()
    {
        if (healthText != null && targetHealth != null)
        {
            healthText.text = Mathf.CeilToInt(targetHealth.CurrentHealth) + " / " + Mathf.CeilToInt(targetHealth.MaxHealth);
        }
    }
}