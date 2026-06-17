using System;
using UnityEngine;

public class Stamina : MonoBehaviour
{
    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float currentStamina = 100f;

    [Header("Regeneration")]
    [SerializeField] private float regenPerSecond = 18f;
    [SerializeField] private float regenDelayAfterUse = 0.75f;

    [Header("Debug")]
    [SerializeField] private bool debugPrintChanges = false;

    public float Current => currentStamina;
    public float Max => maxStamina;
    public float MaxStamina => maxStamina;
    public bool IsEmpty => currentStamina <= 0f;
    public bool IsFull => currentStamina >= maxStamina;

    public event Action<float, float> OnStaminaChanged;

    private float lastUseTime = -999f;

    private void Awake()
    {
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);

        if (currentStamina <= 0f)
            currentStamina = maxStamina;

        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    private void Update()
    {
        if (Time.time < lastUseTime + regenDelayAfterUse)
            return;

        if (currentStamina >= maxStamina)
            return;

        float oldValue = currentStamina;
        currentStamina += regenPerSecond * Time.deltaTime;
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);

        if (!Mathf.Approximately(oldValue, currentStamina))
        {
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);

            if (debugPrintChanges)
                Debug.Log("stamina: " + currentStamina + " / " + maxStamina);
        }
    }

    public bool Consume(float amount)
    {
        amount = Mathf.Max(0f, amount);

        if (amount <= 0f)
            return true;

        if (currentStamina < amount)
            return false;

        currentStamina -= amount;
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        lastUseTime = Time.time;

        OnStaminaChanged?.Invoke(currentStamina, maxStamina);

        if (debugPrintChanges)
            Debug.Log("stamina used: " + amount + " | now " + currentStamina + " / " + maxStamina);

        return true;
    }

    public void Restore(float amount)
    {
        amount = Mathf.Max(0f, amount);

        if (amount <= 0f)
            return;

        currentStamina += amount;
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);

        OnStaminaChanged?.Invoke(currentStamina, maxStamina);

        if (debugPrintChanges)
            Debug.Log("stamina restored: " + amount + " | now " + currentStamina + " / " + maxStamina);
    }

    public void SetCurrent(float amount)
    {
        currentStamina = Mathf.Clamp(amount, 0f, maxStamina);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);

        if (debugPrintChanges)
            Debug.Log("stamina set: " + currentStamina + " / " + maxStamina);
    }

    public void RefillFull()
    {
        currentStamina = maxStamina;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);

        if (debugPrintChanges)
            Debug.Log("stamina full");
    }
}