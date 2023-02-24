using UnityEngine;

public interface IDamageable 
{
    void TakeDamage(float damage, Vector3 direction, ItemData.DamageType dType, Vector3 point);
}
