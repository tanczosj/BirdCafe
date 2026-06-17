using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    [Header("Debug")]
    [SerializeField] private bool debugPrintDamage = false;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0f;

    public event Action<float, float> OnHealthChanged;
    public event Action<float> OnDamaged;
    public event Action OnDied;

    private void Awake()
    {
        // Force a clean starting value so the bar is correct at runtime.
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, transform.position, Vector3.zero, null);
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection, GameObject source)
    {
        if (IsDead)
            return;

        amount = Mathf.Max(0f, amount);
        if (amount <= 0f)
            return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        if (debugPrintDamage)
            Debug.Log("damage");

        OnDamaged?.Invoke(amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (IsDead)
            return;

        amount = Mathf.Max(0f, amount);
        if (amount <= 0f)
            return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void SetHealth(float amount)
    {
        currentHealth = Mathf.Clamp(amount, 0f, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f && !IsDead)
            Die();
    }

    private void Die()
    {
        OnDied?.Invoke();
        Debug.Log(gameObject.name + " died.");
    }
}