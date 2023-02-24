using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class AmmunitionData : ScriptableObject
{
    [Header("Ammunition settings")]
    public ItemData.DamageType damageType;
    public string ammoSize;
    public float muzzleVelocity = 5.6f;
    public float gravity = 0.2f;
    public float damage;

    [Header(" multipliers")]
    public float fleshMultiplier = 1f;
    public float accuracyMultiplier = 1f;

    [Header("Proctile")]
    public bool overrideToProjectile = false;
    public string projectile;

    [Header("Hiteffects")]
    public CollisionParticleData colP;

    [Header("Single shot settings")]
    public GameObject ammoRepresentor;
}
