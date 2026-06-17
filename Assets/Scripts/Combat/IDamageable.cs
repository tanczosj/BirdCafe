using UnityEngine;

public interface IDamageable
{
    bool IsDead { get; }
    void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection, GameObject instigator = null);
}