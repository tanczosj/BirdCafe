using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health targetHealth;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text healthText;

    [Header("Settings")]
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private bool debugHealthBar = false;

    private float displayedFill = 1f;
    private float targetFill = 1f;

    private void OnEnable()
    {
        TryBindHealth();
    }

    private void Start()
    {
        TryBindHealth();
        RefreshInstant();
    }

    private void OnDisable()
    {
        if (targetHealth != null)
            targetHealth.OnHealthChanged -= HandleHealthChanged;
    }

    private void Update()
    {
        if (fillImage == null)
            return;

        displayedFill = Mathf.Lerp(displayedFill, targetFill, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
        fillImage.fillAmount = displayedFill;

        if (healthText != null && targetHealth != null)
        {
            healthText.text = Mathf.CeilToInt(targetHealth.CurrentHealth) + " / " + Mathf.CeilToInt(targetHealth.MaxHealth);
        }
    }

    private void TryBindHealth()
    {
        if (targetHealth == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                targetHealth = player.GetComponent<Health>();

            if (targetHealth == null)
                targetHealth = FindFirstObjectByType<Health>();
        }

        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged -= HandleHealthChanged;
            targetHealth.OnHealthChanged += HandleHealthChanged;

            targetFill = targetHealth.MaxHealth > 0f
                ? targetHealth.CurrentHealth / targetHealth.MaxHealth
                : 0f;

            if (debugHealthBar)
                Debug.Log("HealthBar bound to: " + targetHealth.gameObject.name);
        }
        else if (debugHealthBar)
        {
            Debug.LogWarning("HealthBarUI could not find a Health component.");
        }
    }

    private void HandleHealthChanged(float current, float max)
    {
        targetFill = max > 0f ? current / max : 0f;

        if (debugHealthBar)
            Debug.Log("HealthBar updated: " + current + " / " + max);
    }

    private void RefreshInstant()
    {
        if (targetHealth == null || fillImage == null)
            return;

        targetFill = targetHealth.MaxHealth > 0f
            ? targetHealth.CurrentHealth / targetHealth.MaxHealth
            : 0f;

        displayedFill = targetFill;
        fillImage.fillAmount = targetFill;

        if (healthText != null)
        {
            healthText.text = Mathf.CeilToInt(targetHealth.CurrentHealth) + " / " + Mathf.CeilToInt(targetHealth.MaxHealth);
        }
    }
}