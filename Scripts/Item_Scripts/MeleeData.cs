using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class MeleeData : ScriptableObject
{
    public CollisionParticleData colP;
    [Space]
    public ItemData.DamageType damageType;

    public LayerMask collisionMask;

    public AnimationClip[] lightAttackAnimations;
    public AnimationClip[] heavyAttackAnimations;
    [Space]
    public float betweenTime = 0.2f;
    public float lightTime = 0.5f;
    public float heavyTime = 1.5f;
    public float endComboTime = 1f;
    [Space]
    public float heavyDamage;
    public float lightDamage = 10f;

    public HarvestingManager.HarvestType harvestType;
    public float harvestDamage = 0f;

    public int durabilityOnHit = 10;
    [Space]
    public float lightAttackDistance = 0.7f;
    public float heavyAttackDistance = 0.8f;
    public float lightAttackRadius = 0.3f;
    public float heavyAttackRadius = 0.4f;
    [Space]
    public Vector2 recoilVector = new Vector2(5f, 5f);
    public float recoilSpeed = 5f;
    public Vector2 heavyrecoilVector = new Vector2(7.5f, 7.5f);
    public float heavyrecoilSpeed = 3f;
    [Space]
    public float heavyStaminaUse = 20f;
    [Space]
    public bool hasHeavyAttack = true;

    [Header("Throwing")]
    public bool canThrow = false;
    public float throwVelocity = 20f;
    public float throwDamage;
    public AnimationClip throwAnimation;
    public string throwProjectile;

    [Header("Audio")]
    public string swingAudioKey;
    [Space]
    public string heavySwingAudioKey;
}


