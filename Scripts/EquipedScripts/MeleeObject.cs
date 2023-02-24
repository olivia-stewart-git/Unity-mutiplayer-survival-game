using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeObject : MonoBehaviour, I_EquipedItem, I_ExtraInput
// Start is called before the first frame update
{
    private PlayerMovementMangaer p_Movement;
    private CrosshairManager crosshairManager;
    private PlayerMouseLook p_Mouselook;
    private PlayerResourcesScript p_Resources;
    private InventoryManager p_Inventory;
    private EquipManager p_Equip;
    private PlayerProjectileManager p_Projectile;
    private PlayerAnimationManager p_Animation;
    private HarvestingManager harvestManager;

    private PlayerAudioManager p_audio;

    private PlayerScript p_Script;

    [SerializeField] private PlayerMoveSync pSync;
    [SerializeField] private ItemData itemData;
    [SerializeField] private Animator thisAnimator;

    [Space]
    [SerializeField] private Transform projectilePoint;

    private Camera mCam;

    private bool isAttacking = false;
    private bool drawn = false;

    private float lastAttack;

    private Coroutine attackingCoroutine;

    public void DeEquip()
    {
        StopAllCoroutines();
        p_Movement.ForceWalk(false);
    }

    public void Drawn()
    {
        drawn = true;
    }

    public void Intialise(GameObject player, ItemInstance instance)
    {
        Debug.Log("Initialised held item " + gameObject.name);
        p_Movement = player.GetComponent<PlayerMovementMangaer>();
        crosshairManager = player.GetComponent<CrosshairManager>();
        p_Mouselook = player.GetComponent<PlayerMouseLook>();
        p_Resources = player.GetComponent<PlayerResourcesScript>();
        p_Inventory = player.GetComponent<InventoryManager>();
        p_Equip = player.GetComponent<EquipManager>();
        p_Projectile = player.GetComponent<PlayerProjectileManager>();
        p_Animation = player.GetComponent<PlayerAnimationManager>();
        p_Script = player.GetComponent<PlayerScript>();
        p_audio = player.GetComponent<PlayerAudioManager>();
        harvestManager = player.GetComponent<HarvestingManager>();

        mCam = p_Mouselook.GetCamera();


        if(pSync != null)
        {
            pSync.Initialise(p_Movement);
        }
    }

    public void LeftButtonDown()
    {
        if (!drawn) return;
       //we try attempt light attack
        if(isAttacking == false)
        {
            if(Time.time > lastAttack)
            {
                curIndex = 0;
            }

            if (attackingCoroutine != null)
            {
                StopCoroutine(attackingCoroutine);
            }
            attackingCoroutine = StartCoroutine(AttackCoroutine(itemData.mData.lightTime));
        }
    }

    public void RegisterAttack()
    {
        crosshairManager.ExpandCrosshair(2.5f, 1f);
        p_Mouselook.RecoilCamera(new Vector2(Random.Range(-itemData.mData.recoilVector.x, itemData.mData.recoilVector.x), Random.Range(-itemData.mData.recoilVector.y, itemData.mData.recoilVector.y)), itemData.mData.recoilSpeed);
        MakeAttackHitbox(itemData.mData.lightAttackRadius, itemData.mData.lightDamage, itemData.mData.lightAttackDistance);
    }
    public void RegisterHeavyAttack()
    {
        crosshairManager.ExpandCrosshair(4f, 1f);
        p_Mouselook.RecoilCamera(new Vector2(Random.Range(-itemData.mData.heavyrecoilVector.x, itemData.mData.heavyrecoilVector.x), Random.Range(-itemData.mData.heavyrecoilVector.y, itemData.mData.heavyrecoilVector.y)), itemData.mData.heavyrecoilSpeed);
        p_Resources.AddValue((int)-itemData.mData.heavyStaminaUse, PlayerResourcesScript.ResourceType.stamina);
        MakeAttackHitbox(itemData.mData.heavyAttackRadius, itemData.mData.heavyDamage, itemData.mData.heavyAttackDistance);
    }

    RaycastHit[] hitColls = new RaycastHit[10];
    public void MakeAttackHitbox(float radius, float damage, float distance)
    {
        if (mCam == null) return;


        GameObject closest = null;
        GameObject harvestObject = null;

        float lastDistance = 100f;
        float lastResourceDistance = 100f;

        Vector3 pt = Vector3.one;
        Vector3 harvestpt = Vector3.one;
        Vector3 normal = Vector3.one;
        Vector3 harvestNormal = Vector3.one;

        Vector3 faceForward = mCam.transform.position + (mCam.transform.forward * (distance / 2));


        int amount = Physics.SphereCastNonAlloc(mCam.transform.position, radius, mCam.transform.forward, hitColls, distance, itemData.mData.collisionMask, QueryTriggerInteraction.Ignore);

        if (amount == 0) return;

       // Debug.Log("hits " +hitColls.Length + " amount " + amount);
  //      if (hitColls[0].transform.gameObject.GetComponent<IDamageable>() != null)
 //       {

     //       closest = hitColls[0].transform.gameObject;
 //           pt = hitColls[0].point;
   //         normal = hitColls[0].normal;
     //   }
    //    if (amount > 1)
  //      {
            for (int i = 0; i < amount; i++)
            {
                Debug.DrawLine(hitColls[i].point, hitColls[i].point + (mCam.transform.position - hitColls[i].point).normalized * 0.5f, Color.red, 10f);
                RaycastHit hit;
                if (Physics.Raycast(mCam.transform.position, (hitColls[i].point - mCam.transform.position).normalized, out hit, Vector3.Distance(mCam.transform.position, hitColls[i].transform.position) + 0.2f, itemData.mData.collisionMask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.transform.gameObject == hitColls[i].transform.gameObject)
                    {
                        //do distance stuff
                        float useDist = Vector3.Distance(faceForward, hit.point);
                        if (itemData.mData.harvestDamage > 0)
                        {
                            if (useDist < lastResourceDistance && hit.transform.gameObject.GetComponent<HarvestNode>() != null)
                            {
                                harvestObject = hit.transform.gameObject;
                                harvestpt = hit.point;
                                harvestNormal = hit.normal;
                                lastResourceDistance = useDist;
                            }
                        }

                        if (useDist < lastDistance && hit.transform.gameObject.GetComponent<IDamageable>() != null)
                        {
                            normal = hit.normal;
                            pt = hit.point;
                            lastDistance = useDist;
                            closest = hit.transform.gameObject;
                        }
                    }
                }
            }
     //   }
     //distance check so if enemy is closer that gets hit
        if(harvestObject != null)
        {
            if (harvestObject.GetComponent<HarvestNode>().harvestType == itemData.mData.harvestType)
            {
                crosshairManager.DoHitmarker();

                harvestManager.HarvestCall(harvestObject, itemData.mData.harvestDamage,mCam.transform.forward, harvestpt, harvestNormal, itemData.mData.harvestType, itemData.itemId, mCam.transform.position);

                if (harvestObject.GetComponent<WorldSurface>() != null)
                {
                    WorldSurface surface = harvestObject.GetComponent<WorldSurface>();
                    p_Projectile.CreateCollsionEffect(surface.surface, itemData.mData.colP, harvestpt, harvestNormal);
                    p_Projectile.SpawnDecal(itemData.mData.colP, harvestpt, harvestNormal, surface.surface);
                }

                return;
            }
        }

        if(closest != null)
        {
            crosshairManager.DoHitmarker();

            if(closest.GetComponent<WorldSurface>() != null)
            {
                WorldSurface surface = closest.GetComponent<WorldSurface>();
                p_Projectile.CreateCollsionEffect(surface.surface, itemData.mData.colP, pt, normal);
                p_Projectile.SpawnDecal(itemData.mData.colP, pt, normal, surface.surface);
            }
                p_Script.RecieveDamageCall(closest, damage, (closest.transform.position - mCam.transform.position).normalized, itemData.mData.damageType, pt);
            EZCameraShake.CameraShaker.Instance.ShakeOnce(0.5f, 1f, 0f, 0.5f);
            // closest.GetComponent<IDamageable>().TakeDamage(damage, (closest.transform.position - mCam.transform.position).normalized, mData.damageType, pt);
            return;
        }

        //create generalised effects
        RaycastHit efffecthit;
        if (Physics.Raycast(mCam.transform.position, mCam.transform.forward, out efffecthit, itemData.mData.heavyAttackDistance + itemData.mData.heavyAttackRadius * 2f, itemData.mData.collisionMask))
        {
            if (efffecthit.transform.gameObject.GetComponent<WorldSurface>() != null)
            {
                WorldSurface surface = efffecthit.transform.gameObject.GetComponent<WorldSurface>();
                p_Projectile.CreateCollsionEffect(surface.surface, itemData.mData.colP, efffecthit.point, efffecthit.normal);
                p_Projectile.SpawnDecal(itemData.mData.colP, efffecthit.point, efffecthit.normal, surface.surface);
            }
        }

    
    }

    private int curIndex = 0;
    //light attack
    IEnumerator AttackCoroutine(float attackTime)
    {
        p_Animation.PlayAnimation("attackLight");

        isAttacking = true;

        thisAnimator.Play(itemData.mData.lightAttackAnimations[curIndex].name, 0, 0f);

        if(curIndex == itemData.mData.lightAttackAnimations.Length - 1)
        {
            curIndex = 1;
        }
        else
        {
            curIndex++;
        }

        lastAttack = Time.time + itemData.mData.endComboTime + attackTime + itemData.mData.betweenTime;

        yield return new WaitForSeconds(attackTime + itemData.mData.betweenTime);
        isAttacking = false;
    }

    IEnumerator HeavyAttack(float attackTime)
    {

        isAttacking = true;
        p_Movement.ForceWalk(true);
        p_Animation.PlayAnimation("attackHeavy");

        thisAnimator.Play(itemData.mData.heavyAttackAnimations[Random.Range(0, itemData.mData.heavyAttackAnimations.Length)].name, 0, 0f);

        lastAttack = Time.time + itemData.mData.endComboTime + attackTime + itemData.mData.betweenTime;
        yield return new WaitForSeconds(attackTime + itemData.mData.betweenTime);
        isAttacking = false;
        p_Movement.ForceWalk(false);
    }

    IEnumerator Throw(float time)
    {
        isAttacking = true;
        p_Movement.ForceWalk(true);
        thisAnimator.Play(itemData.mData.throwAnimation.name, 0, 0f);

        p_Animation.PlayAnimation("throw");
        yield return new WaitForSeconds(time);

        //de-equip shit babiii
        p_Equip.DropHeld();
        p_Movement.ForceWalk(false);
    }

    public void LightSwingSound()
    {

        if (itemData.mData.swingAudioKey != "") p_audio.PlaySound(itemData.mData.swingAudioKey, transform.position);
    }
    public void HeavySwingSound()
    {
        if (itemData.mData.heavySwingAudioKey != "") p_audio.PlaySound(itemData.mData.heavySwingAudioKey, transform.position);
    }


    public void LeftButtonUp()
    {
  
    }
    public void MoveCamera(AnimationEvent a_Event)
    {
        p_Mouselook.RecoilCamera(new Vector2(Random.Range(-(float)a_Event.intParameter, (float)a_Event.intParameter), Random.Range(-(float)a_Event.intParameter, (float)a_Event.intParameter)), a_Event.floatParameter);

        EZCameraShake.CameraShaker.Instance.ShakeOnce(0.4f, 0.3f, 0f, 0.1f);
    }

    public void ShakeCameraLightly()
    {
        EZCameraShake.CameraShaker.Instance.ShakeOnce(0.7f, 1f, 0.5f, 0.5f);
    }
    public void RightButtonDown()
    {
        if (!drawn) return;
        if (!isAttacking && itemData.mData.hasHeavyAttack && (p_Resources.GetStamina() - itemData.mData.heavyStaminaUse) > 0) 
        {

                if (attackingCoroutine != null)
                {
                    StopCoroutine(attackingCoroutine);
                }

                isAttacking = true;
            attackingCoroutine = StartCoroutine(HeavyAttack(itemData.mData.heavyTime));
        }
    }
    
    public void SpawnThrowProjectile()
    {
        //we make projectile
        RaycastHit hit;
        if (Physics.Raycast(mCam.transform.position, mCam.transform.forward, out hit, 0.4f, itemData.mData.collisionMask, QueryTriggerInteraction.Ignore))
        {
            //we collidedat point
            IDamageable damageInterface = hit.transform.gameObject.GetComponent<IDamageable>();

            if(damageInterface != null)
            {
                p_Script.RecieveDamageCall(hit.transform.gameObject, itemData.mData.throwDamage, mCam.transform.forward, itemData.mData.damageType, hit.point);
               // damageInterface.TakeDamage(mData.throwDamage, mCam.transform.forward, mData.damageType, hit.point);
            }

            p_Projectile.InitialiseProjectileObj(itemData.mData.throwProjectile, hit.point, projectilePoint.rotation, true, itemData.mData.throwVelocity, mCam.transform.forward, itemData.mData.throwDamage, p_Equip.GetCurrentItemDurability());

            if(hit.transform.gameObject.GetComponent<WorldSurface>() != null)
            {
                WorldSurface surface = hit.transform.gameObject.GetComponent<WorldSurface>();
                ProjectileObject pObj = p_Projectile.GetProjectileByKey(itemData.mData.throwProjectile).GetComponent<ProjectileObject>();
                p_Projectile.CreateCollsionEffect(surface.surface, pObj.collisionEffectData, hit.point, hit.normal);
                p_Projectile.SpawnDecal(pObj.collisionEffectData, hit.point, hit.normal, surface.surface);
            }
        }
        else
        {
            //we collidedat point
            p_Projectile.InitialiseProjectileObj(itemData.mData.throwProjectile, projectilePoint.position, projectilePoint.rotation, false, itemData.mData.throwVelocity, ((mCam.transform.position + mCam.transform.forward * 100f) - projectilePoint.position).normalized, itemData.mData.throwDamage, p_Equip.GetCurrentItemDurability());
        }
    }
    
    public void RightButtonUp()
    {
    }

    public void ExtraButton1Down()
    {
        if(isAttacking == false && itemData.mData.canThrow)
        {
            StartCoroutine(Throw(itemData.mData.throwAnimation.length));
        }
    }

    public void ExtraButton1Up()
    {

    }

    public void ExtraButton2Down()
    {

    }

    public void ExtraButton2Up()
    {
   
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Vector3 end = mCam.transform.position + (mCam.transform.forward * itemData.mData.lightAttackDistance);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(mCam.transform.position, end);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(end, itemData.mData.lightAttackRadius);
    }

    public void InsectButton()
    {
        
    }
}


