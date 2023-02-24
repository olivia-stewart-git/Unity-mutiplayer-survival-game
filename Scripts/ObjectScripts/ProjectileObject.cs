using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

[RequireComponent(typeof(Rigidbody))]
public class ProjectileObject : NetworkBehaviour
{
    private float damage;
    public bool stickOnCollision;
    public ItemData.DamageType dType;
    public CollisionParticleData collisionEffectData;
    [Space]
    public LayerMask collisionLayerMask;
    public bool raycastCollision;
    public Transform projectileTip;

    Rigidbody rb;

    private bool collided = false;

    public Collider projectileCollider;

    public bool doHitmarker = true;
    private CrosshairManager p_Crosshair;

    private PlayerScript p_Script;

    [Header("Creating pickup")]
    public bool makePickupOnHit;
    public int pickupItemId;//id to creat pickup from


    private int storedDurability;

    public void Awake()
    {
        lastPos = transform.position;
        rb = GetComponent<Rigidbody>();
    }

    bool canDo = false;

    private PlayerProjectileManager p_Projectile;
    public void SetCollisionFrom(PlayerProjectileManager pProj)
    {
        p_Projectile = pProj;
    }

    public void SetDamage(float setdamage, GameObject source)
    {
        canDo = true;
        GameObject player = source.GetComponent<PlayerReferencer>().GetPlayer();

        p_Script = player.GetComponent<PlayerScript>();

        damage = setdamage;

        if (doHitmarker)
        {
            p_Crosshair = source.GetComponent<CrosshairManager>();
        }
    }


    Vector3 lastPos;
    private void FixedUpdate()
    {
        if (!canDo || collided) return;
        if (raycastCollision)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, (projectileTip.position - lastPos).normalized, out hit, Vector3.Distance(lastPos, projectileTip.position) + 0.1f, collisionLayerMask, QueryTriggerInteraction.Ignore))
            {
                IDamageable damageInterface = hit.transform.gameObject.GetComponent<IDamageable>();
                if (damageInterface != null)
                {
                    //    damageInterface.TakeDamage(damage, (point - transform.position).normalized, dType, point);
                    p_Script.RecieveDamageCall(hit.transform.gameObject, damage, (hit.point - transform.position).normalized, dType, hit.point);
                    if (doHitmarker && p_Crosshair != null)
                    {
                        p_Crosshair.DoHitmarker();
                    }
                }
                if (hit.transform.gameObject.GetComponent<WorldSurface>() != null)
                {
                    WorldSurface surface = hit.transform.gameObject.gameObject.GetComponent<WorldSurface>();
                    p_Projectile.CreateCollsionEffect(surface.surface, collisionEffectData, hit.point, (hit.point - transform.position).normalized);
                }
                if (makePickupOnHit)
                {
                    CreatePickupAfterHit(hit.point);
                }
                //we run this on all clients
                DoneCollision(hit.point);
            }

            lastPos = transform.position;
        }
    }

    public void ApplyDurability(int dur)
    {
        storedDurability = dur;
    }

    public void AddVelocity(float velocity, Vector3 direction)
    {
        rb = GetComponent<Rigidbody>();

        rb.AddForce(velocity * direction, ForceMode.Impulse);
    }

    public void CreatePickupAfterHit(Vector3 point)
    {
        if (!collided)
        {
            collided = true;
            p_Projectile.CreateProjectilePickup(point, transform.rotation, pickupItemId, GetComponent<NetworkObject>(), storedDurability);
        }
    }

    public void CollidedAtPoint(Vector3 point)
    {
        if (!canDo) return;
        collided = true;
        if (projectileCollider != null)
        {
            projectileCollider.enabled = false;
        }

        if (stickOnCollision)
        {
           // rb.isKinematic = true;
            rb.velocity = Vector3.zero;
           // rb.useGravity = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collided || raycastCollision) return;
        if (!canDo) return;

        Vector3 point = collision.GetContact(0).point;

        IDamageable damageInterface = collision.gameObject.GetComponent<IDamageable>();
        if(damageInterface != null)
        {
        //    damageInterface.TakeDamage(damage, (point - transform.position).normalized, dType, point);
            p_Script.RecieveDamageCall(collision.gameObject, damage, (point - transform.position).normalized, dType, point);
            if (doHitmarker && p_Crosshair != null)
            {
                p_Crosshair.DoHitmarker();
            }
        }
        if (collision.gameObject.gameObject.GetComponent<WorldSurface>() != null)
        {
            WorldSurface surface = collision.gameObject.gameObject.GetComponent<WorldSurface>();
            p_Projectile.CreateCollsionEffect(surface.surface, collisionEffectData, point, (point - transform.position).normalized);
        }
        if (makePickupOnHit)
        {
            CreatePickupAfterHit(point);
        }
        //we run this on all clients
        DoneCollision(point);
    }

    [ObserversRpc]
    public void DoneCollision(Vector3 point)
    {
        collided = true;
        CollidedAtPoint(point);
    }

}
