using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GunObject : MonoBehaviour, I_EquipedItem, I_ExtraInput, I_MenuEvent
{
    [Header("Shoot settings")]
    public string shootSound;

    private string desiredShootSound;

    public string unloadSound;
    public string reloadSound;
    public string reloadSlideSound;
    public string chargeSound;

    [SerializeField] private LayerMask shootCheckMask;
    [SerializeField] private Transform barrelEnd;

    private ItemInstance thisGunInstance;

    [SerializeField] private ItemData thisGun;
     private ItemReference allitems;

    private bool drawn = false;
    private GameObject playerObj;
    private Transform playerbody;

    //player references
    private PlayerMovementMangaer p_Movement;
    private CrosshairManager crosshairManager;
    private PlayerMouseLook p_Mouselook;
    private InventoryManager inventoryManager;
    private WeaponSway wSway;
    private PlayerProjectileManager p_Projectile;
    private PlayerAudioManager p_audio;
    private PlayerAnimationManager p_Animation;
    private PlayerScript pScript;
    private PlayerBuildingManager p_Building;
    
    [Header("ads settings")]
    //ads settings
    private bool inAds;
    private bool canAds = true;
    [Space]
    public Transform adsBase;
    public Transform adsCentre;
    public float adsFovOffsetCompensator = 0.3f;

    //ui stuff
    [SerializeField] private GameObject gunCanvasObj;
    private GameObject gunCanvas;

    private Slider ammoSlider;
    private Slider accuracySlider;
    private Slider chargeSlider;

    private GameObject noAmmoLoadedtext;
    private GameObject magInsantiateObject;
    private GameObject gunReloadPanel;

    //reload settings
    private float lastReloadDown;
    private bool reloadPanelOpen = false;
    private bool reloading = false;

    private GameObject magObject;
    [SerializeField] private Transform magObjectParent;

    //atachment mod settings
    [Header("Attachment settings")]
    public AttachMentLocation[] attachments;
    private bool inAttachmentMenu = false;

    [Header("Animation settings")]
    [SerializeField] private Animator gunAnimator;
    [SerializeField] private AnimationClip shootNormal;
    [SerializeField] private AnimationClip shootAds;
    [Space]
    [SerializeField] private PlayerMoveSync pMoveSync;

    [Header("Particle settings")]
    [SerializeField] private ParticleSystem shootParticles;
    [SerializeField] private ParticleSystem shellParticles;

    [SerializeField] private GameObject attachmentIconUi;
    private List<GameObject> createdattachmentUi = new List<GameObject>();

    [Header("Sounds")]
    [SerializeField] private AudioClip equipAttachmentSound;
    [SerializeField] private AudioClip removeAttachmentSound;

    private Camera gamecam;

    ObjectPooler objPooler;
    public void DeEquip()
    {
        ExitAds();
        leftBDown = false;
        if (inAttachmentMenu)
        {
            CloseAttachmentMenu();
        }
        else
        {
            if (createdattachmentUi.Count > 0)
            {
                foreach (GameObject item in createdattachmentUi)
                {
                    Destroy(item);
                }
            }
        }
        Destroy(gunCanvas);
        StopAllCoroutines();
    }

    public void Drawn()
    {
        drawn = true;
    }

    public void Intialise(GameObject player, ItemInstance instance)
    {
        desiredShootSound = shootSound;

        playerObj = player;
        p_Movement = playerObj.GetComponent<PlayerMovementMangaer>();
        crosshairManager = playerObj.GetComponent<CrosshairManager>();
        p_Mouselook = playerObj.GetComponent<PlayerMouseLook>();
        inventoryManager = playerObj.GetComponent<InventoryManager>();
        p_Projectile = player.GetComponent<PlayerProjectileManager>();
        p_audio = player.GetComponent<PlayerScript>().GetAudioManager();
        p_Animation = player.GetComponent<PlayerAnimationManager>();
        p_Building = player.GetComponent<PlayerBuildingManager>();

        pScript = player.GetComponent<PlayerScript>();

        allitems = pScript.GetItemReference();

        wSway = playerObj.GetComponent<WeaponSway>();
        wSway.InitialiseRecoilValues(thisGun.gunData.rotationSpeed, thisGun.gunData.postionalSpeed, thisGun.gunData.rotationReturnSpeed, thisGun.gunData.postionalReturnSpeed);

        gamecam = p_Mouselook.GetCamera();

        gunCanvas = Instantiate(gunCanvasObj);

        GunUiReference uiRef = gunCanvas.GetComponent<GunUiReference>();
        ammoSlider = uiRef.ammoSlider;
        accuracySlider = uiRef.accuracySlider;
        chargeSlider = uiRef.chargeSlider;
        accuracySlider.maxValue = 1f;
        noAmmoLoadedtext = uiRef.noAmmoLoadedtext;

        gunReloadPanel = uiRef.magazineViewHolder;
        magInsantiateObject = uiRef.magazineIndicatorToInstantiate;

        playerbody = p_Mouselook.GetBody();

        if(pMoveSync != null)
        {
            pMoveSync.Initialise(p_Movement);
        }

        //load magazine info
        thisGunInstance = instance;
        SetMagUi(thisGunInstance);

        //set mag
        if (thisGunInstance.magLoaded)
        {
            ItemData toUse = allitems.allItems[thisGunInstance.magObjectId];
            if (thisGun.gunData.useSingleShots)
            {
                magObject = Instantiate(toUse.ammoData.ammoRepresentor, magObjectParent.position, magObjectParent.rotation, magObjectParent);
            }
            else
            {
                magObject = Instantiate(toUse.magazineData.magObject, magObjectParent.position, magObjectParent.rotation, magObjectParent);
            }
        }

        //set attachments
        if (attachments.Length > 0)
        {
            GenerateAttachmentModels();
            GenerateAttachMentModifiers();
        }

         objPooler = ObjectPooler.Instance;
    }

    public void OnInventoryOpen()
    {
        if (inAttachmentMenu)
        {
            CloseAttachmentMenu();
        }

        if (inAds)
        {
            ExitAds();
        }

        leftBDown = false;
    }


    #region shooting
    private bool leftBDown = false;
    private float lastTime;

    private float currentaccuracy = 0f;

    public void LeftButtonDown()
    {
        leftBDown = true;
        if (reloadPanelOpen)
        {
            MoveMagPanelSelection(false);
        }
    }

    public void LeftButtonUp()
    {
        leftBDown = false;
    }

    private bool ChargeAble()
    {
        if (!thisGun.gunData.chargeToFire) return true;
        //check if over threshold
        if (thisGun.gunData.canShootEarly && chargeValue > thisGun.gunData.earlyChargeThreshold) return true;
        if (chargeValue >= 1f)
        {
            return true;
        }

        return false;
    }
    private float lastleftBDown;
    private bool bDownlastFrame = false;
    private float tDif;
    private float chargeValue;
    private bool charging = false;
    private bool lastCharge;
    public void Update()
    {
        //set attachment pos
        if (inAttachmentMenu && createdattachmentUi.Count > 0)
        {
            for (int i = 0; i < createdattachmentUi.Count; i++)
            {
                createdattachmentUi[i].transform.position = gamecam.WorldToScreenPoint(attachments[i].attachmentLocal.position);
            }
        }

        if(leftBDown)
        {
            if (thisGun.gunData.chargeToFire)
            {
                if (CanShoot() == true && Checkmag() == true && Time.time > lastTime)
                {
                    gunAnimator.SetBool("ChargeWeapon", true);
                    p_Animation.SetBool("Drawn", true);

                    if (bDownlastFrame == false)
                    {
                        bDownlastFrame = true;
                        lastleftBDown = Time.time;
                    }

                    tDif = Time.time - lastleftBDown;
                }
                else
                {
                    tDif = 0;
                    chargeValue = 0;
                }
            }
            else
            {
                ShootInput();
            }
        }
        chargeValue = tDif / thisGun.gunData.timeToCharge; //this value is normalized between 0 and 1

        if (inAds)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetAdsPosition, 8f * Time.deltaTime);
        }
        else
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, Vector3.zero, 8f * Time.deltaTime);
        }

        if (!leftBDown)
        {
            if (bDownlastFrame && thisGun.gunData.chargeToFire)
            {
                ShootInput();
            }
            if (thisGun.gunData.chargeToFire)
            {
                gunAnimator.SetBool("ChargeWeapon", false);
                p_Animation.SetBool("Drawn", false);

                if (thisGun.gunData.allowAdsOnlyOnCharge && inAds)
                {
                    ExitAds();
                }
            }

            bDownlastFrame = false;
            chargeValue = 0f;
        }
        if(chargeValue > 0 && thisGun.gunData.chargeToFire)
        {
            charging = true;
            if (!inAds)
            {
                chargeSlider.gameObject.SetActive(true);
            }
            else
            {
                chargeSlider.gameObject.SetActive(false);
            }
            chargeSlider.value = chargeValue;
        }
        else
        {
            charging = false;
            chargeSlider.gameObject.SetActive(false);
        }

        if (charging)
        {
            if (thisGun.gunData.forceWalkOnCharge)
            {
                p_Movement.ForceWalk(true);
            }
        }
        else
        {
            if (thisGun.gunData.forceWalkOnCharge)
            {
                if (!inAds)
                {
                    p_Movement.ForceWalk(false);
                }
            }
        }
        if(charging && !lastCharge)
        {
            if (chargeSound != "") p_audio.PlaySound(chargeSound, transform.position);
        }
        else
        {
            if(!charging && lastCharge)
            {
                if (chargeSound != "") p_audio.PlaySound(chargeSound, transform.position);
            }
        }

        lastCharge = charging;

        //here we reset accuracy
        if(Time.time > lastTime + thisGun.gunData.returnAccuracyTime && currentaccuracy != 0)
        {
            currentaccuracy = Mathf.Lerp(currentaccuracy, 0f, thisGun.gunData.returnAccuracySpeed * Time.deltaTime);
            accuracySlider.value = currentaccuracy;
        }
    }

    //we try to shoot on a positive input
    public void ShootInput()
    {
        //performshooting
        if (CanShoot() == true && Checkmag() == true && Time.time > lastTime && ChargeAble())
        {
            if(thisGun.gunData.chargeToFire && leftBDown && !thisGun.gunData.shootOnCharge)
            {
                return;
            }

            if (!thisGun.gunData.chargeToFire && !leftBDown) return;

            if (!leftBDown)
            {
                Debug.Log("no keydown yo");
            }

            p_Animation.PlayAnimation("shoot");
            //audio
            if (shootSound != "") p_audio.PlaySound(desiredShootSound, transform.position);

            //for continuing shooting
            lastTime = Time.time + thisGun.gunData.fireRate;
            if (thisGun.gunData.shootType == GunData.ShootType.single || thisGun.gunData.resetChargeOnShot)
            {
                chargeValue = 0;
                charging = false;
                leftBDown = false;
            }

            //ads
            if (thisGun.gunData.exitAdsOnShoot && inAds)
            {
                ExitAds();
            }

            //make shoot visuals
            MakeShootVisuals();

            //get ammo
            AmmunitionData ammoData = allitems.allItems[thisGunInstance.loadedMagazineIds[0]].ammoData; //we get the first ammunitition piece

            //we remove from mag
            if (thisGun.gunData.infiniteAmmo == false)
            {
                thisGunInstance.loadedMagazineIds.RemoveAt(0);
                if (thisGunInstance.loadedMagazineIds.Count == 0 && thisGun.gunData.useSingleShots)
                {
                    thisGunInstance.magLoaded = false;
                }
                SetMagUi(thisGunInstance);
            }

            //do accuracy
            float accuracyAdd = 1f / thisGun.gunData.toMaxAccuracyShots;
            currentaccuracy += accuracyAdd;
            currentaccuracy = Mathf.Clamp(currentaccuracy, 0f, 1f);
            accuracySlider.value = currentaccuracy;

            //get accuracy pattern
            Vector2 camRot = new Vector2(thisGun.gunData.cameraRecoilBaseAngle + (thisGun.gunData.recoilPatternX.Evaluate(currentaccuracy) * (thisGun.gunData.camMultiplier)), thisGun.gunData.cameraRecoilBaseAngle + (thisGun.gunData.recoilPatternX.Evaluate(currentaccuracy) * thisGun.gunData.camMultiplier));
            camRot = new Vector2(camRot.x * thisGun.gunData.recoilPatternNegativeValues.Evaluate(currentaccuracy) * 10f, camRot.y);
            if (inAds)
            {
                camRot *= thisGun.gunData.camAdsMultiplier;
            }
            p_Mouselook.RecoilCamera(camRot, thisGun.gunData.camRecoilSpeed);

            //calculate full accuracy
            float moveAccuracyMultiplier = p_Movement.GetAccuracyMultiplier();
            float attachMultiplier = calculated_attachment_hipfireaccuracy;
            float gunMultiplier = thisGun.gunData.accuracy;
            float ammoAccuracy = ammoData.accuracyMultiplier;
            if (inAds)
            {
                attachMultiplier = calculated_attachment_adsAccuracy;
            }


            float totalCalculatedAccuracy = moveAccuracyMultiplier * attachMultiplier * gunMultiplier * ammoAccuracy * ((currentaccuracy * thisGun.gunData.maxAccuracyMultiplier) + 1);
            if (inAds)
            {
                totalCalculatedAccuracy *= thisGun.gunData.adsAccuracy;
            }
            //change crosshair
            crosshairManager.ExpandCrosshair(totalCalculatedAccuracy, thisGun.gunData.crosshairMultiplier);

            //get forward vector
            Vector3 fVector = ForwardVector(totalCalculatedAccuracy);

            //calculate shoot direction
            Vector3 point = GetShootPoint(fVector, ammoData);

            if (ammoData.overrideToProjectile)
            {
                RaycastHit hit;
                if (Physics.Raycast(gamecam.transform.position, fVector, out hit, 0.4f, shootCheckMask, QueryTriggerInteraction.Ignore))
                {
                    //we collidedat point
                    IDamageable damageInterface = hit.transform.gameObject.GetComponent<IDamageable>();

                    if (damageInterface != null)
                    {
                        pScript.RecieveDamageCall(hit.transform.gameObject, ammoData.damage, fVector, ammoData.damageType, hit.point);
                        // damageInterface.TakeDamage(mData.throwDamage, mCam.transform.forward, mData.damageType, hit.point);
                    }
                    float velValue = ammoData.muzzleVelocity * thisGun.gunData.velocityMultiplier * calculated_attachment_velocityMultiplier;
                    if (thisGun.gunData.chargeToFire && thisGun.gunData.canShootEarly)
                    {
                        velValue *= thisGun.gunData.modCurve.Evaluate(chargeValue);
                    }
                    p_Projectile.InitialiseProjectileObj(ammoData.projectile, hit.point, barrelEnd.rotation, true, velValue, fVector, ammoData.damage, 1000);

                    if (hit.transform.gameObject.GetComponent<WorldSurface>() != null)
                    {
                        WorldSurface surface = hit.transform.gameObject.GetComponent<WorldSurface>();
                        ProjectileObject projObj = p_Projectile.GetProjectileByKey(ammoData.projectile).GetComponent<ProjectileObject>();
                        p_Projectile.CreateCollsionEffect(surface.surface, projObj.collisionEffectData, hit.point, hit.normal);
                        p_Projectile.SpawnDecal(projObj.collisionEffectData, hit.point, hit.normal, surface.surface);
                    }
                }
                else
                {
                    float velValue = ammoData.muzzleVelocity * thisGun.gunData.velocityMultiplier * calculated_attachment_velocityMultiplier;
                    if (thisGun.gunData.chargeToFire && thisGun.gunData.canShootEarly)
                    {
                        velValue *= thisGun.gunData.modCurve.Evaluate(chargeValue);
                    }
                    p_Projectile.InitialiseProjectileObj(ammoData.projectile, barrelEnd.position, barrelEnd.rotation, false, velValue, ((gamecam.transform.position + fVector * 100f) - barrelEnd.position).normalized, ammoData.damage, 100);
                }
            }
            else
            {//raycast shot
                if (Vector3.Distance(gamecam.transform.position, point) > Vector3.Distance(gamecam.transform.position, barrelEnd.position))
                {

                    Vector3 barrelVector = GetBarrelToVector(point);
                  
                    //calculate the raycast
                    Vector3[] linePoints = GetTrajectoryPositions(barrelVector, ammoData, barrelEnd.position, point);

                    p_Projectile.CreateMultiplayerBullet(linePoints, iterationTime);

                    StartCoroutine(PerformTrajRaycasts(linePoints, ammoData));
                }
            }
        }
    }

    private Vector3 ForwardVector(float accuracy)
    {
        //make rotation based on accuracy to determine where shot will go
        Vector3 forwardVector = Vector3.forward;
        float deviation = Random.Range(0f, accuracy);
        float angle = Random.Range(0f, 360f);
        forwardVector = Quaternion.AngleAxis(deviation, Vector3.up) * forwardVector;
        forwardVector = Quaternion.AngleAxis(angle, Vector3.forward) * forwardVector;
        forwardVector = gamecam.transform.rotation * forwardVector;
     
        return forwardVector;
    }

    private Vector3 GetBarrelToVector(Vector3 endPoint)
    {
        Debug.DrawLine(endPoint, barrelEnd.position, Color.green, 10f);
        return (endPoint - barrelEnd.position).normalized;
    }
    private float iterationTime = 0.1f;
    private int iterationCount = 25;

    private Vector3[] GetTrajectoryPositions(Vector3 direction, AmmunitionData ammodata, Vector3 origin, Vector3 target)
    {
        //calculate launch angle
       // float angle = Vector3.Angle(new Vector3(direction.x, 0, direction.z), direction);
        //we get angle
        float length = Vector3.Distance(target, origin);
        float opposite = Mathf.Abs(target.y - origin.y);
        float angle = Mathf.Atan(opposite / length);
        
       if (target.y < origin.y)
       {
            angle *= -1f;
       }

        Debug.DrawRay(barrelEnd.position, direction, Color.grey, 10f);

        List<Vector3> allPoints = new List<Vector3>();
        allPoints.Add(origin);
        for (int i = 3; i < iterationCount; i++)
        {
            //  x = (v0 cosθ0)t
            float t = i * iterationTime;
            float velValue = ammodata.muzzleVelocity * thisGun.gunData.velocityMultiplier * calculated_attachment_velocityMultiplier;
            if(thisGun.gunData.chargeToFire && thisGun.gunData.canShootEarly)
            {
                velValue *= thisGun.gunData.modCurve.Evaluate(chargeValue);
            }
            float xValue = GetXAtTime(t, velValue, angle);
            float yValue = GetYPosition(xValue, ammodata.gravity, angle, velValue);
            Vector3 calculatedLine = direction * xValue;
            Vector3 calculatedPosition = new Vector3(calculatedLine.x, yValue, calculatedLine.z);
            calculatedPosition += origin;
            Vector3 lastpos = allPoints[allPoints.Count - 1];
            allPoints.Add(calculatedPosition);    
        }

        return allPoints.ToArray();
    }

    private IEnumerator PerformTrajRaycasts(Vector3[] points, AmmunitionData ammo)
    {
        //lerp babi
        GameObject bulletInstance = objPooler.SpawnFromPool("Bullet", barrelEnd.position, barrelEnd.rotation);
        BulletRepScript bullRep = bulletInstance.GetComponent<BulletRepScript>();
        bullRep.SpawnObject(points, iterationTime);

        //first check
        RaycastHit hit1;
        if (Physics.Raycast(barrelEnd.position, (points[3] - barrelEnd.position).normalized, out hit1, Vector3.Distance(barrelEnd.position, points[3]) + 0.1f, shootCheckMask, QueryTriggerInteraction.Ignore))
        {
            Debug.DrawRay(hit1.point, hit1.normal, Color.red, 20f);
            Debug.DrawRay(hit1.point, (points[3] - barrelEnd.position).normalized, Color.cyan, 20f);
            bullRep.SetEndPoint(hit1.point);
            BulletHitSomething(hit1, ammo, (points[3] - barrelEnd.position).normalized);
        }
        else
        {
            //go throul points
            Vector3 lastpoint = points[0];
            for (int i = 1; i < points.Length; i++)
            {
                Vector3 curPos = points[i];
                Debug.DrawLine(curPos, lastpoint, Color.blue, 10f);

                Vector3 testDir = (curPos - lastpoint).normalized;

                RaycastHit hit;
                if (Physics.Raycast(curPos, testDir, out hit, Vector3.Distance(curPos, lastpoint) + 0.1f, shootCheckMask))
                {
                    Debug.DrawRay(hit.point, hit.normal, Color.red, 20f);
                    Debug.DrawRay(hit.point, testDir, Color.cyan, 20f);
                    bullRep.SetEndPoint(hit.point);
                    BulletHitSomething(hit, ammo, testDir);
                    break;
                }

                lastpoint = curPos;
                yield return new WaitForSeconds(iterationTime);
            }
        }
    }

    private float GetXAtTime(float time, float velocity, float angle)
    {
        return (velocity * Mathf.Cos(angle)) * time;
    }

    private float GetYPosition(float x, float gravity, float angle, float startVelocity)
    {
        float tanPart = x * Mathf.Tan(angle);
        float cosPart = (gravity * x * x) / (2 * Mathf.Pow(startVelocity * Mathf.Cos(angle), 2));
        float yVal = tanPart - cosPart;
        return yVal;
    }

    public Vector3 GetShootPoint(Vector3 forwardVector, AmmunitionData ammo)
    {
        //raycast out to check what way to shoot
        Vector3 targetPos; //this is for if we are looking at sky or something really far away
        RaycastHit hit;
        if (Physics.Raycast(gamecam.transform.position, forwardVector, out hit, 100f, shootCheckMask, QueryTriggerInteraction.Ignore))
        {
            //assuming we find as position to aim bullet at
            targetPos = hit.point;

            float distance = Vector3.Distance(gamecam.transform.position, targetPos);

            if(Vector3.Distance(gamecam.transform.position, hit.point) < Vector3.Distance(gamecam.transform.position, barrelEnd.position) && !ammo.overrideToProjectile)
            {
                BulletHitSomething(hit, ammo, forwardVector);
                Debug.DrawLine(gamecam.transform.position, hit.point, Color.red, 5f);
                return targetPos;
            }
        }
        else
        {
            targetPos =  gamecam.transform.position + (forwardVector.normalized * 100f);
        }     

      //  Debug.DrawLine(targetPos, gamecam.transform.position, Color.red, 10f);
            //check for barrel obstruction
            RaycastHit barrelHit;
            if (Physics.Raycast(barrelEnd.position, (targetPos - barrelEnd.position).normalized, out barrelHit, Vector3.Distance(barrelEnd.position, targetPos) + 10f, shootCheckMask, QueryTriggerInteraction.Ignore))
            {
                targetPos = barrelHit.point;
            }

            //Debug.DrawLine(targetPos, barrelEnd.position, Color.green, 10f);
        barrelEnd.LookAt(targetPos);
        
        return targetPos;
    }

    private ParticleSystem shootOverrideParticles;
    void MakeShootVisuals()
    {
        //play anim
        if (inAds)
        {
            gunAnimator.Play(shootAds.name, 0, 0f);
        }
        else
        {
            gunAnimator.Play(shootNormal.name, 0, 0f);
        }

        //make particles
        if(shootParticles != null && shootOverrideParticles == null)
        {
            shootParticles.Play();
        }
        else
        {
            if(shootOverrideParticles != null)
            {
                shootOverrideParticles.Play();
            }
        }

        //make recoil 
        float damper = 1f;
        if (inAds)
        {
            damper = thisGun.gunData.visualAimRecoilAimDamper;
        }
        wSway.ApplyRecoil(thisGun.gunData.rotationalRecoil * damper, thisGun.gunData.recoilKickback * damper);
    }

    bool CanShoot()
    {
        if (inAttachmentMenu || reloadPanelOpen)
        {
            return false;
        }
        return true;
    }

    bool Checkmag()
    { 
        if(thisGunInstance.magLoaded == false)
        {
            return false;
        }
        if(thisGunInstance.loadedMagazineIds == null || thisGunInstance.loadedMagazineIds.Count == 0)
        {
            return false;
        }
        return true;
    }

    public void PlayShellAnim()
    {
        if(shellParticles != null)
        {
            shellParticles.Play();
        }
    }

    public void BulletHitSomething(RaycastHit hit, AmmunitionData ammo, Vector3 dir)
    {
        if (hit.transform.CompareTag("Build"))
        {
            float damage = ammo.damage * thisGun.gunData.damageMultiplier;
            p_Building.DamageBuild(hit.transform.gameObject, (int)damage, thisGun.gunData.buildPenetration, dir);
        }

        IDamageable dInterface = hit.transform.gameObject.GetComponent<IDamageable>();
        if (dInterface != null)
        {
            crosshairManager.DoHitmarker();

            float damage = ammo.damage * thisGun.gunData.damageMultiplier;

            //send damage call
            pScript.RecieveDamageCall(hit.transform.gameObject, damage, dir, ammo.damageType, hit.point);
           // dInterface.TakeDamage(damage, dir, ammo.damageType, hit.point);
        }


        //make hit effect
        Vector3 normal = hit.normal;

        if (hit.transform.gameObject.GetComponent<WorldSurface>() != null)
        {
            WorldSurface surface = hit.transform.gameObject.GetComponent<WorldSurface>();
            p_Projectile.CreateCollsionEffect(surface.surface, ammo.colP, hit.point, normal);
            p_Projectile.SpawnDecal(ammo.colP, hit.point, hit.normal, surface.surface);
        }
    }

    #endregion

    #region ads
    public void RightButtonDown()
    {
        //ads 
        if (!drawn) return;
        if (canAds && reloadPanelOpen == false && inAttachmentMenu == false)
        {
            if (thisGun.gunData.allowAdsOnlyOnCharge)
            {
                if (chargeValue >= 1f)
                {
                    EnterAds();
                }
            }
            else
            {
                EnterAds();
            }
        }

        if (reloadPanelOpen)
        {
            MoveMagPanelSelection(true);
        }
    }

    public void RightButtonUp()
    {
        if (!drawn) return;
        if (inAds)
        {
            ExitAds();
        }
    }

    private Vector3 targetAdsPosition = Vector3.zero;

    public void EnterAds()
    {
        Vector3 toAds = new Vector3(transform.localPosition.x, transform.localPosition.y + sightOffset, (transform.localPosition.z + adsFovOffsetCompensator) - backSightOffset);
        targetAdsPosition = toAds;

        inAds = true;
        crosshairManager.SetCrosshairOn(false);

        p_Mouselook.SetAdsSenstivity(true);
        p_Mouselook.SetViewModelFov(15f);

        gunAnimator.SetBool("ads", true);
        p_Movement.ForceWalk(true);

        wSway.SetSwayDampen(true);


        if (fovOverride == 0f)
        {
            p_Mouselook.SetFov(thisGun.gunData.ironSightsFov, true);
        }
        else
        {

            p_Mouselook.SetFov(fovOverride, true);
        }
    }

    public void ExitAds()
    {
        inAds = false;
        crosshairManager.SetCrosshairOn(true);

        p_Mouselook.SetAdsSenstivity(false);
        p_Mouselook.SetViewModelFov(60f);

        gunAnimator.SetBool("ads", false);
        p_Movement.ForceWalk(false);

        wSway.SetSwayDampen(false);

        transform.localPosition = Vector3.zero;


        p_Mouselook.SetFov(0f, false);
        
    }
    #endregion

    #region reloading
    public void SetMagUi(ItemInstance instance)
    {
        if (instance != null)
        {         
            if (instance.magLoaded == false)
            {
                ammoSlider.value = 0;
                noAmmoLoadedtext.SetActive(true);
            }
            else
            {
                ItemData data = allitems.allItems[instance.magObjectId];
                if (thisGun.gunData.useSingleShots)
                {
                    ammoSlider.maxValue = 1;
                }
                else
                {
                    ammoSlider.maxValue = data.stackCapacity;
                }
                ammoSlider.value = instance.loadedMagazineIds.Count;
                noAmmoLoadedtext.SetActive(false);
            }
        }
    }
    public void ExtraButton2Down()
    {
        //handle reloading
        if (reloading == true || drawn == false) return;

        lastReloadDown = Time.time;
    }

    public void ExtraButton2Up()
    {
        if (reloading == true || drawn == false || charging) return;

        if (reloadPanelOpen == false)
        {
            if (Time.time > lastReloadDown + 0.2f)
            {
                OpenMagPanel();
            }
            else
            {
                if (thisGun.gunData.useSingleShots)
                {
                    SingleReload();
                }
                else
                {
                    MagazineReload();
                }
            }
        }
        else
        {
            CloseMagPanel();
        }
    }

    public void SwitchMagObject()
    {
        if (!reloading) return;
        if (magObject != null)
        {
            Destroy(magObject);
        }

        ItemData toUse = allitems.allItems[reloadid];
        if (thisGun.gunData.useSingleShots)
        {
            magObject = Instantiate(toUse.ammoData.ammoRepresentor, magObjectParent.position, magObjectParent.rotation, magObjectParent);
        }
        else
        {
            magObject = Instantiate(toUse.magazineData.magObject, magObjectParent.position, magObjectParent.rotation, magObjectParent);
        }
    }

    public void DeleteMagObject()
    {
        if (magObject != null)
        {
            Destroy(magObject);
        }
    }

    public float reloadCamImpulseAmount = 1f;
    public void MakeRandomImpulse()
    {
        p_Mouselook.RecoilCamera(new Vector2(Random.Range(-reloadCamImpulseAmount, reloadCamImpulseAmount), Random.Range(-reloadCamImpulseAmount, reloadCamImpulseAmount)), 4f);
    }

    public void MagazineReload()
    {
        //we find mag to reload to
        List<InventorySlot> magSlots = inventoryManager.FindMagazinesOfType(thisGun.gunData.ammunitionSize);
        if (magSlots.Count == 0) return;
        int toId = 0;

        ItemInstance useInstance = magSlots[toId].storedItem;
        inventoryManager.RemoveFromSlot(magSlots[toId]);

        //get rid of current mag
        if(thisGunInstance.magLoaded == true)
        {
            ItemInstance toAddInstance = new ItemInstance();
            toAddInstance.id = thisGunInstance.magObjectId;
            toAddInstance.stackedItemIds = thisGunInstance.loadedMagazineIds;
            inventoryManager.AddToInventory(toAddInstance, null);
        }

        if (inAds)
        {
            ExitAds();
        }

        StartCoroutine(QuickReload(useInstance));
    }

    public void SingleReload()
    {
        //we find mag to reload to
        List<InventorySlot> ammoSlots = inventoryManager.FindAmmoOfType(thisGun.gunData.ammunitionSize);
        if (ammoSlots.Count == 0) return;
        int toId = 0;
        ItemInstance useInstance = ammoSlots[toId].storedItem;
        inventoryManager.SubtractOneFromSlot(ammoSlots[toId]);


        //get rid of current mag
        if (thisGunInstance.magLoaded == true)
        {
            ItemInstance toAddInstance = new ItemInstance();
            toAddInstance.id = thisGunInstance.magObjectId;
            toAddInstance.stackedItemIds = thisGunInstance.loadedMagazineIds;
            inventoryManager.AddToInventory(toAddInstance, null);
        }

        if (inAds)
        {
            ExitAds();
        }

        StartCoroutine(QuickReload(useInstance));
    }

    //this handles just reload anim, loading and unloading of mag instances must be handled else ware\
    private int reloadid;
    public IEnumerator QuickReload(ItemInstance reloadMag)
    {
        if(thisGunInstance.magLoaded == true && thisGun.gunData.unloadFirst == true)
        {
            if (unloadSound != "") p_audio.PlaySound(unloadSound, transform.position);
            gunAnimator.SetTrigger("Reload_Unload");
            yield return new WaitForSeconds(thisGun.gunData.unloadTime); //for extra time to unloadold
        }

        //playanim on network
        p_Animation.PlayAnimation("reload");

        reloading = true;
        gunAnimator.SetBool("Reload", true);
        reloadid = reloadMag.id;

        if (shootSound != "") p_audio.PlaySound(reloadSound, transform.position);

        yield return new WaitForSeconds(thisGun.gunData.reloadTime - thisGun.gunData.slideTimeOffset);

        if (thisGunInstance.loadedMagazineIds.Count == 0 && thisGun.gunData.useSlideReload)
        {
            gunAnimator.SetTrigger("Reload_Slide");
            yield return new WaitForSeconds(thisGun.gunData.slideTimeOffset); //for extra time to chamber new round
            if (reloadSlideSound != "") p_audio.PlaySound(reloadSlideSound, transform.position);
            yield return new WaitForSeconds( thisGun.gunData.slideTime - thisGun.gunData.slideTimeOffset); //for extra time to chamber new round
        }
        else
        {
            yield return new WaitForSeconds(thisGun.gunData.slideTimeOffset);
        }
        //we replace the values
        gunAnimator.SetBool("Reload", false);
        thisGunInstance.magObjectId = reloadMag.id;
        thisGunInstance.magLoaded = true;
        if (thisGun.gunData.useSingleShots)
        {
            thisGunInstance.loadedMagazineIds = new List<int>();
            thisGunInstance.loadedMagazineIds.Add(reloadMag.id);
        }
        else
        {
            thisGunInstance.loadedMagazineIds = reloadMag.stackedItemIds;
        }

        SetMagUi(thisGunInstance);

        reloading = false;
    }


    private List<MagMenuScript> magSlotsInPanel;
    private int currentMagPanelNum = 0;

    public void OpenMagPanel()
    {
        if (reloading) return;
        if (inAttachmentMenu)
        {
            CloseAttachmentMenu();
        }

        gunReloadPanel.SetActive(true);
        reloadPanelOpen = true;

        currentMagPanelNum = 0;

        //we find mag to reload to
        bool magCount = false;
        List<InventorySlot> magSlots = new List<InventorySlot>();
        if (thisGun.gunData.useSingleShots)
        {
            magSlots = inventoryManager.FindAmmoOfType(thisGun.gunData.ammunitionSize);
        }
        else
        {
            magSlots = inventoryManager.FindMagazinesOfType(thisGun.gunData.ammunitionSize);
        }
        if (magSlots.Count == 0) { magCount = true; };

        magSlotsInPanel = new List<MagMenuScript>();

        //create for current mag
        if(thisGunInstance.magLoaded == true)
        {
            GameObject createdInstance = Instantiate(magInsantiateObject, gunReloadPanel.transform);
            MagMenuScript mScript = createdInstance.GetComponent<MagMenuScript>();

            mScript.currentLoaded.SetActive(true);

            ItemInstance toAddInstance = new ItemInstance();
            toAddInstance.id = thisGunInstance.magObjectId;
            toAddInstance.stackedItemIds = thisGunInstance.loadedMagazineIds;
            
            mScript.referredInstance = toAddInstance;

            magSlotsInPanel.Add(mScript);
        }
        else
        {
            if(magCount == true)
            {
                CloseMagPanel();
                return;
            }
        }


        //create for mags in inventory
        foreach (InventorySlot item in magSlots)
        {
            GameObject createdInstance = Instantiate(magInsantiateObject, gunReloadPanel.transform);
            MagMenuScript mScript = createdInstance.GetComponent<MagMenuScript>();

            mScript.referredInstance = item.storedItem;
            mScript.referredSlot = item;

            magSlotsInPanel.Add(mScript);
        }

        foreach (MagMenuScript useScript in magSlotsInPanel)
        {
            if (thisGun.gunData.useSingleShots)
            {
                useScript.ammoSlider.gameObject.SetActive(false);
                useScript.ammoText.text = (useScript.referredInstance.stackedItemIds.Count +1).ToString();
                useScript.iconImage.sprite = allitems.allItems[useScript.referredInstance.id].iconSprite;
            }
            else
            {
                useScript.ammoSlider.gameObject.SetActive(true);
                useScript.ammoSlider.maxValue = allitems.allItems[useScript.referredInstance.id].stackCapacity;
                useScript.ammoSlider.value = useScript.referredInstance.stackedItemIds.Count;
                useScript.ammoText.text = useScript.referredInstance.stackedItemIds.Count.ToString();

                useScript.iconImage.sprite = allitems.allItems[useScript.referredInstance.id].iconSprite;
            }
        }
        p_Movement.ForceWalk(true);
        SelectMagPanelInstance(0);
    }

    public void MoveMagPanelSelection(bool toRight)
    {
        if (!reloadPanelOpen) return;

        if (toRight)
        {
            if (currentMagPanelNum == magSlotsInPanel.Count - 1)
            {
                currentMagPanelNum = 0;
            }
            else
            {
                currentMagPanelNum++;
            }
        }
        else
        {
            if(currentMagPanelNum == 0)
            {
                currentMagPanelNum = magSlotsInPanel.Count - 1;
            }
            else
            {
                currentMagPanelNum--;
            }
        }

        SelectMagPanelInstance(currentMagPanelNum);
    }

    public void CloseMagPanel()
    {
        if(magSlotsInPanel != null && magSlotsInPanel.Count > 0)
        {
            foreach (MagMenuScript item in magSlotsInPanel)
            {
                Destroy(item.gameObject);
            }
        }

        gunReloadPanel.SetActive(false);
        reloadPanelOpen = false;

        if (thisGunInstance.magLoaded == true && currentMagPanelNum == 0)
        {
            //unload
        }
        else
        {

            //get rid of current mag
            if (thisGunInstance.magLoaded == true)
            {
                ItemInstance toAddInstance = new ItemInstance();
                toAddInstance.id = thisGunInstance.magObjectId;
                toAddInstance.stackedItemIds = thisGunInstance.loadedMagazineIds;
                inventoryManager.AddToInventory(toAddInstance, null);
            }

            if (magSlotsInPanel != null && magSlotsInPanel.Count > 0)
            {
                MagMenuScript toUse = magSlotsInPanel[currentMagPanelNum];
                if (thisGun.gunData.useSingleShots)
                {
                    inventoryManager.SubtractOneFromSlot(toUse.referredSlot);
                }
                else
                {
                    inventoryManager.RemoveFromSlot(toUse.referredSlot);
                }
                StartCoroutine(QuickReload(toUse.referredInstance));
            }
        }

        magSlotsInPanel = new List<MagMenuScript>();

        p_Movement.ForceWalk(false);
    }

    public void SelectMagPanelInstance(int pos)
    {
        if (!reloadPanelOpen || magSlotsInPanel == null) return;
        foreach (MagMenuScript item in magSlotsInPanel)
        {
            item.backing.color = item.unselected;
        }

        magSlotsInPanel[pos].backing.color = magSlotsInPanel[pos].selected;
    }

    #endregion
    
    #region attachments
    public void ExtraButton1Up()
    {
  
    }
    public void ExtraButton1Down()
    {
        if (reloading || attachments.Length == 0) return;
        if (inAttachmentMenu)
        {
            CloseAttachmentMenu();
        }
        else
        {
            OpenAttachmentMenu();
        }
    }
    public void SlowReloadCamShake()
    {
        EZCameraShake.CameraShaker.Instance.ShakeOnce(1f, 1f, 1f, 3f);
    }

    public void ReloadCamShake()
    {
        EZCameraShake.CameraShaker.Instance.ShakeOnce(0.6f, 1.3f, 0.3f, 1f);
    }
    public void RumbleCamShake()
    {
        EZCameraShake.CameraShaker.Instance.ShakeOnce(1f, 1f, 1f, 2f);
    }
    public void OpenAttachmentMenu()
    {
        if (charging || !drawn) return;

        inAttachmentMenu = true;
        if (reloadPanelOpen)
        {
            CloseMagPanel();
        }

        if (inAds)
        {
            ExitAds();
        }

        gunAnimator.SetBool("attachment", true);
        p_Movement.ForceWalk(true);

        for (int i = 0; i < attachments.Length; i++)
        {
            GameObject instance = Instantiate(attachmentIconUi, gamecam.WorldToScreenPoint(attachments[i].attachmentLocal.position), Quaternion.identity, gunCanvas.transform);
            AttachmentSlotScript asScript = instance.GetComponent<AttachmentSlotScript>();
            asScript.g_Object = this;
            asScript.locationid = i;
            createdattachmentUi.Add(instance);
            GenerateAttachmentSlotImage(asScript);
        }
        SelectAttachment(0);
       
        p_Mouselook.SetCursorLock(false);
        p_Mouselook.SetMouseLookOverride(true);
        playerObj.GetComponent<WeaponSway>().SetSwayDampen(true);
    }

    private int currentAttachment = 0;

    public void SelectAttachment(int id)
    {
        for (int i = 0; i < createdattachmentUi.Count; i++)
        {
            AttachmentSlotScript a_slot = createdattachmentUi[i].GetComponent<AttachmentSlotScript>();
            a_slot.backingImage.color = a_slot.unselectedColor;
        }

        AttachmentSlotScript as_slot = createdattachmentUi[id].GetComponent<AttachmentSlotScript>();
        as_slot.backingImage.color = as_slot.selectedColor;

        GenerateAttachMentOptions(as_slot);

        currentAttachment = id;
    }

    public void AttachmentSlotSelected(AttachmentSlotScript slot)
    {
        if(slot.locationid != currentAttachment)
        {
            SelectAttachment(slot.locationid);
        }
    }

    public void GenerateAttachMentOptions(AttachmentSlotScript slot)
    {
        //clear old 
        for (int i = 0; i < createdattachmentUi.Count; i++)
        {
            if (i != slot.locationid)
            {
                AttachmentSlotScript a_slot = createdattachmentUi[i].GetComponent<AttachmentSlotScript>();
                if(a_slot.createdOptions.Count > 0)
                {
                    foreach (GameObject g in a_slot.createdOptions)
                    {
                        Destroy(g);
                    }
                    a_slot.createdOptions = new List<GameObject>();
                }
                a_slot.selectionHolder.SetActive(false);
            }
        }

        //generate new
        slot.selectionHolder.SetActive(true);

        GenerateAttachmentSelections(slot);
    }

    public void GenerateAttachmentSelections(AttachmentSlotScript slot)
    {
        if(slot.createdOptions != null && slot.createdOptions.Count > 0)
        {
            foreach (GameObject item in slot.createdOptions)
            {
                Destroy(item);
            }
        }
        slot.createdOptions = new List<GameObject>();

        //we create options to select
        List<InventorySlot> inventorySlots = inventoryManager.FindAttachmentsOfType(attachments[slot.locationid].attachType);
        if (inventorySlots.Count == 0)
        {
            slot.arrowImage.sprite = slot.nonSprite;
            return;
        }
        else
        {
            slot.arrowImage.sprite = slot.arrowSprite;
        }

        for (int i = 0; i < inventorySlots.Count; i++)
        {
            GameObject createdInstance = Instantiate(slot.toCreateSelection, slot.createPos);
            AttachOptionScript attachOption = createdInstance.GetComponent<AttachOptionScript>();

            ItemInstance useInstance = inventorySlots[i].storedItem;
            ItemData useData = allitems.allItems[useInstance.id];
            attachOption.iconImage.sprite = useData.iconSprite;

            attachOption.connectedSlot = slot;
            attachOption.connectedInventorySlot = inventorySlots[i];

            slot.createdOptions.Add(createdInstance);
        }
    }

    public void GenerateAttachmentModels()
    {
        shootOverrideParticles = null;

        for (int i = 0; i < attachments.Length; i++)
        {
            AttachmentClass aClass = thisGunInstance.storedAttachments[i];
            AttachMentLocation aLocation = attachments[i];
            if(aLocation.attachObjInstance != null)
            {
                Destroy(aLocation.attachObjInstance);
            }
            if (aClass.occupied)
            {
                ItemData useData = allitems.allItems[aClass.toAttachedId];
                aLocation.attachObjInstance = Instantiate(useData.attachmentData.attachmentObject, aLocation.attachmentLocal.position, aLocation.attachmentLocal.rotation, aLocation.attachmentLocal);
                if(aLocation.defaultObj != null)
                {
                    aLocation.defaultObj.SetActive(false);
                }

                //override particles
                if (useData.attachmentData.overrideShootParticles)
                {
                    shootOverrideParticles = aLocation.attachObjInstance.GetComponent<AttacheMentObjectScript>().overrideParticles;
                }
            }
            else
            {
                if (aLocation.defaultObj != null)
                {
                    aLocation.defaultObj.SetActive(true);
                }
            }
        }
    }

    public void ChangeGunAttachment(InventorySlot inventoryslot, AttachmentSlotScript attachslot)
    {
        ItemInstance useInstance = inventoryslot.storedItem;
        inventoryManager.RemoveFromSlot(inventoryslot);

        AttachmentClass aClass = thisGunInstance.storedAttachments[attachslot.locationid];
        if (aClass.occupied)
        {
            ItemInstance backToInventoryInstance = new ItemInstance();
            backToInventoryInstance.id = aClass.toAttachedId;
            backToInventoryInstance.currentDurability = aClass.attachmentDurability;
            backToInventoryInstance.stackedItemIds = new List<int>();
            inventoryManager.AddToInventory(backToInventoryInstance, null);
        }
        aClass.occupied = true;
        aClass.toAttachedId = useInstance.id;
        aClass.attachmentDurability = useInstance.currentDurability;

        //updateoptions
        GenerateAttachMentOptions(attachslot);
        GenerateAttachmentSlotImage(attachslot);
        GenerateAttachmentModels();
        GenerateAttachMentModifiers();

        p_audio.PlayLocalAudioClip(equipAttachmentSound);
    }

    public void GenerateAttachmentSlotImage(AttachmentSlotScript slot)
    {
        AttachmentClass aClass = thisGunInstance.storedAttachments[slot.locationid];
        if(!aClass.occupied)
        {
            slot.iconImage.enabled = false;
            slot.removeButton.SetActive(false);
        }
        else
        {
            slot.removeButton.SetActive(true);
            slot.iconImage.enabled = true;
            ItemData aData = allitems.allItems[aClass.toAttachedId];
            slot.iconImage.sprite = aData.iconSprite;
        }
    }

    public void DeEquipFromAttachmentSlot(AttachmentSlotScript slot)
    {
        AttachmentClass aClass = thisGunInstance.storedAttachments[slot.locationid];
        if (aClass.occupied)
        {
            ItemInstance backToInventoryInstance = new ItemInstance();
            backToInventoryInstance.id = aClass.toAttachedId;
            backToInventoryInstance.currentDurability = aClass.attachmentDurability;
            backToInventoryInstance.stackedItemIds = new List<int>();
            inventoryManager.AddToInventory(backToInventoryInstance, null);
        }
        aClass.occupied = false;

        //updateoptions
        GenerateAttachMentOptions(slot);
        GenerateAttachmentSlotImage(slot);
        GenerateAttachmentModels();

        p_audio.PlayLocalAudioClip(removeAttachmentSound);
    }


    public void CloseAttachmentMenu()
    {
        //generate
        GenerateAttachMentModifiers();

        //deactivate
        inAttachmentMenu = false;
        gunAnimator.SetBool("attachment", false);
        p_Movement.ForceWalk(false);

        if(createdattachmentUi.Count > 0)
        {
            foreach (GameObject item in createdattachmentUi)
            {
                Destroy(item);
            }
        }
        createdattachmentUi = new List<GameObject>();

        p_Mouselook.SetCursorLock(true);
        p_Mouselook.SetMouseLookOverride(false);
        playerObj.GetComponent<WeaponSway>().SetSwayDampen(false);
    }

    //attachment data settings
    private float calculated_attachment_hipfireaccuracy = 1f;
    private float calculated_attachment_adsAccuracy = 1f;
    private float calculated_attachment_damage = 1f;
    private float calculated_attachment_velocityMultiplier = 1f;

    private float sightOffset = 0f;
    private float backSightOffset = 0f;
    private float fovOverride = 0f;

    public void GenerateAttachMentModifiers()
    {
        //here we generate all the changes attachments make to gun stats
        int count = 0;

        float hipfireAdded = 0f;
        float adsAdded = 0f;
        float damageAdded = 0f;
        float rangeAdded = 0f;

        bool hasSight = false;

        string aAudioOverride = "";

        for (int i = 0; i < thisGunInstance.storedAttachments.Count; i++)
        {
            if (thisGunInstance.storedAttachments[i].occupied)
            {
                count++;
                AttachmentData useAttach = allitems.allItems[thisGunInstance.storedAttachments[i].toAttachedId].attachmentData;
                hipfireAdded += useAttach.hipfireaccuracyMultiplier;
                adsAdded += useAttach.adsAccuracyMultiplier;
                damageAdded += useAttach.damageMultiplier;
                rangeAdded += useAttach.rangeMultiplier;

                if(useAttach.overrideShootSound != "")
                {
                    aAudioOverride = useAttach.overrideShootSound;
                }

                if(useAttach.attachMentSlot == AttachmentData.AttachMentSlot.sight)
                {
                    float baseDist = Vector3.Distance(adsBase.position, adsCentre.position);
                    float distCompare = 0f;
                    foreach (AttachMentLocation aLocal in attachments)
                    {
                        if(aLocal.attachObjInstance != null && aLocal.attachType == AttachmentData.AttachMentSlot.sight)
                        {
                            AttacheMentObjectScript aScript = aLocal.attachObjInstance.GetComponent<AttacheMentObjectScript>();
                            distCompare = Vector3.Distance(aScript.sightBase.position, aScript.sightCentre.position);
                            break;
                        }
                    }

                    sightOffset = baseDist - distCompare;
                    fovOverride = useAttach.sightFov;
                    backSightOffset = useAttach.sightBackOffset;
                    hasSight = true;
                }
            }
        }

        if(aAudioOverride != "")
        {
            desiredShootSound = aAudioOverride;
        }
        else
        {
            desiredShootSound = shootSound;
        }

        if(hasSight == false)
        {
            sightOffset = 0f;
            fovOverride = 0f;
            backSightOffset = 0f;
        }

        if(count == 0)
        {
            calculated_attachment_hipfireaccuracy = 1f;
            calculated_attachment_adsAccuracy = 1f;
            calculated_attachment_damage = 1f;
            calculated_attachment_velocityMultiplier = 1f;
        }
        else
        {
            calculated_attachment_hipfireaccuracy = hipfireAdded / count;
            calculated_attachment_adsAccuracy = adsAdded / count;
            calculated_attachment_damage = damageAdded / count;
            calculated_attachment_velocityMultiplier = rangeAdded / count;
        }
    }

    public void InsectButton()
    {
        if(!inAds && !inAttachmentMenu)
        {
            gunAnimator.SetTrigger("Inspect");
        }
    }

   
    #endregion

}


[System.Serializable]
public class AttachMentLocation
{
    public Transform attachmentLocal;
    public GameObject defaultObj; //this is when there is already something there, e.g iron sights
    public AttachmentData.AttachMentSlot attachType;
    [HideInInspector] public GameObject attachObjInstance;
    [HideInInspector] public AttachmentClass currentattachmentClass;
}