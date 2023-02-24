using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

public class ShoottargetScript : NetworkBehaviour, IDamageable
{
    public float health = 100;
    public ParticleSystem deathparticles;

    private bool dead = false;

    public void PlayDeathParticles() => deathparticles.Play();

    public void DestroyTarget() => Destroy(gameObject);

    [ObserversRpc]
    public void TakeDamage(float damage, Vector3 direction, ItemData.DamageType dType, Vector3 point)
    {
        if (dead) return;

        health -= damage;
        if (health < 0)
        {
            GetComponent<Animator>().Play("TargetProp_die", 0, 0f);
            dead = true;
            return;
        }

        GetComponent<Animator>().Play("TargetProp_hit", 0, 0f);
    }
}
