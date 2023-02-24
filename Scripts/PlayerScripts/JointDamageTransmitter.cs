using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;


public class JointDamageTransmitter : MonoBehaviour, IDamageable
{
    public PlayerDamageManager toTransmit;
    public float damageMultiplier = 1f;

    public UnityEvent m_OnHit;
    void Start()
    {
        if (m_OnHit == null)
            m_OnHit = new UnityEvent();
    }

    public void TakeDamage(float damage, Vector3 direction, ItemData.DamageType dType, Vector3 point)
    {
        m_OnHit.Invoke();

        float toDamage = damage * damageMultiplier;
        toTransmit.TakeDamage(toDamage, direction, dType, point);
    }

}
