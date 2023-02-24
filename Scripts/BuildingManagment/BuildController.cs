using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BuildController : MonoBehaviour, I_EquipedItem, I_ExtraInput
{
    //note build controller is half a melee controller!
    [Header("debug")]
    public bool noResourceRequirement;

    [Header("required inputs")]
    private PlayerBuildingManager p_BuildManager;
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

    private Camera mCam;


    //melee settings
    private bool isAttacking = false;
    private bool drawn = false;

    private float lastAttack;

    private Coroutine attackingCoroutine;

    [Header("ui settings")]
    [SerializeField] private PlayerBuildingManager.BuildType defaulOpenType;
    [SerializeField] private GameObject emptyBuildOption;
    private BuildUiObject buildUi;

    private ItemReference itemReference;
    [Space]
    public Color canBuildColor;
    public Color unableToBuildColor;
    public Color defaultOptionBacking;
    public Color highlightOptionBacking;
    [Space]
    public Color editOptionsDefault;
    public Color editOptionsHighlight;

    private bool inBuildMode = false;

    private PlayerMenuManager p_Menu;

    [Header("Check settings")]
    public float snapIterationTime = 0.05f;
    private float lastSnapTime;
    private bool snappedLast;
    [Space]
    public float snapAngle = 45f;
    public Vector3 checkRange = Vector3.one;
    public float buildRange = 1f;
    public LayerMask snapQuery;
    public LayerMask positionLayerMask;
    public LayerMask buildLayerMask;

    [Header("Inputs")]
    public float rotAmount = 15f;

    [Header("BuildVisual Settings")]
    public AnimationClip buildAnimation;
    public string buildSound_Key;

    [Header("Edit Settings")]
    public float editMenuViewingAngle = 35f;
    public float editRayDistance = 2f;
    public float minAllowEditDistance = 1f;
    [Space]
    public AudioClip enterEditModeSound;
    public AudioClip enterEditSelectionSound;
    public void DeEquip()
    {
        StopAllCoroutines();

        if (inBuildMode)
        {
            ExitBuildMode();
        }

        if (inBuildMenu)
        {
            p_Movement.ForceWalk(false);
            CloseBuildOptions();

            p_Mouselook.SetMouseLookOverride(true);
            p_Mouselook.SetCursorLock(false);
        }

        foreach (BuildCategory cat in buildUi.buildCategories)
        {
            cat.associatedUiObject.SetInitialised(false, this);
        }


        buildUi.editOptionsHolder.SetActive(false);
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
        p_BuildManager = player.GetComponent<PlayerBuildingManager>();
        p_Menu = player.GetComponent<PlayerMenuManager>();
        harvestManager = player.GetComponent<HarvestingManager>();

        mCam = p_Mouselook.GetCamera();


        if (pSync != null)
        {
            pSync.Initialise(p_Movement);
        }

        buildUi = p_BuildManager.BuildUi();
        buildUi.overachingUi.SetActive(true);
        buildUi.buildMenuHolder.SetActive(false);
        buildUi.buildDataHolder.SetActive(false);
        buildUi.editOptionsHolder.SetActive(false);
        buildUi.editSelectionsHolder.SetActive(false);

        editSelectionOptions = buildUi.availableEditOptions.ToList<EditOptionScript>();

        itemReference = p_BuildManager.GetItemReference();

        foreach (BuildCategory cat in buildUi.buildCategories)
        {
            cat.associatedUiObject.SetInitialised(true, this);
        }
    }

    #region input 
    public void LeftButtonDown()
    {
        if (!drawn) return;
        //we try attempt light attack
        if (isAttacking == false && inBuildMenu == false && !inBuildMode && !inEditMode)
        {
            if (Time.time > lastAttack)
            {
                curIndex = 0;
            }

            if (attackingCoroutine != null)
            {
                StopCoroutine(attackingCoroutine);
            }
            attackingCoroutine = StartCoroutine(AttackCoroutine(itemData.mData.lightTime));
        }

        if(isAttacking == false && inBuildMode && allowedBuildCol && !inBuildMenu)
        {
            BuildItem();
        }
    }

    public void LeftButtonUp()
    {

    }
    public void RightButtonUp()
    {
        if (inEditMode)
        {
            CalculateEndEditMode();
        }
    }

    private bool inBuildMenu = false;

    public void RightButtonDown()
    {
        if (inBuildMode && !inEditMode)
        {
            rotOffset += rotAmount;
        }
        if(canEditSelection && !inBuildMode && !inBuildMenu)
        {
            // we do the edit
            EnterEditMode();
        }
    }


    public void ExtraButton1Down()
    {
        if (!drawn) return;
        if (isAttacking == false)
        {
            if (inBuildMenu)
            {
                CloseBuildOptions();
                inBuildMenu = false;
            }
            else
            {
                OpenBuildOptions();
                inBuildMenu = true;
            }
        }
    }

    public void ExtraButton1Up()
    {

    }

    public void ExtraButton2Down()
    {
        if (inBuildMode)
        {
            ExitBuildMode();
        }
    }

    public void ExtraButton2Up()
    {

    }
    #endregion
    #region melee component
    public void RegisterAttack()
    {
        crosshairManager.ExpandCrosshair(2.5f, 1f);
        p_Mouselook.RecoilCamera(new Vector2(Random.Range(-itemData.mData.recoilVector.x, itemData.mData.recoilVector.x), Random.Range(-itemData.mData.recoilVector.y, itemData.mData.recoilVector.y)), itemData.mData.recoilSpeed);
        MakeAttackHitbox(itemData.mData.lightAttackRadius, itemData.mData.lightDamage, itemData.mData.lightAttackDistance);
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
            if (Physics.Raycast(mCam.transform.position, (hitColls[i].point - mCam.transform.position).normalized, out hit, Vector3.Distance(mCam.transform.position, hitColls[i].transform.position) + 0.2f, itemData.mData.collisionMask,  QueryTriggerInteraction.Ignore))
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
        if (harvestObject != null)
        {
            if (harvestObject.GetComponent<HarvestNode>().harvestType == itemData.mData.harvestType)
            {
                crosshairManager.DoHitmarker();

                harvestManager.HarvestCall(harvestObject, itemData.mData.harvestDamage, mCam.transform.forward, harvestpt, harvestNormal, itemData.mData.harvestType, itemData.itemId, mCam.transform.position);

                if (harvestObject.GetComponent<WorldSurface>() != null)
                {
                    WorldSurface surface = harvestObject.GetComponent<WorldSurface>();
                    p_Projectile.CreateCollsionEffect(surface.surface, itemData.mData.colP, harvestpt, harvestNormal);
                    p_Projectile.SpawnDecal(itemData.mData.colP, harvestpt, harvestNormal, surface.surface);
                }

                return;
            }
        }

        if (closest != null)
        {
            crosshairManager.DoHitmarker();

            if (closest.GetComponent<WorldSurface>() != null)
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

        if (curIndex == itemData.mData.lightAttackAnimations.Length - 1)
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

    public void LightSwingSound()
    {
        if (itemData.mData.swingAudioKey != "") p_audio.PlaySound(itemData.mData.swingAudioKey, transform.position);
    }

    public void MoveCamera(AnimationEvent a_Event)
    {
        p_Mouselook.RecoilCamera(new Vector2(Random.Range(-(float)a_Event.intParameter, (float)a_Event.intParameter), Random.Range(-(float)a_Event.intParameter, (float)a_Event.intParameter)), a_Event.floatParameter);

        EZCameraShake.CameraShaker.Instance.ShakeOnce(0.4f, 0.3f, 0f, 0.1f);
    }
    #endregion

    #region build component
    void OpenBuildOptions()
    {
        buildUi.buildMenuHolder.SetActive(true);
        
        p_Mouselook.SetCursorLock(false);
        p_Mouselook.SetMouseLookOverride(true);

        p_Menu.SetOverrideInventory(true);

        thisAnimator.SetBool("BuildMenu", true);
        SelectCategory(currentType); //set the default
    }

    private PlayerBuildingManager.BuildType currentType;

    private List<GameObject> createdOptions = new List<GameObject>();


    public void SelectCategory(PlayerBuildingManager.BuildType type)
    {
        currentType = type;
        //generate the options
        if(createdOptions.Count > 0)
        {
            foreach (GameObject g in createdOptions)
            {
                Destroy(g);
            }
        }

        createdOptions = new List<GameObject>();

        foreach (KeyValuePair<int, BuildData> build in itemReference.allBuildItems)
        {
            if(build.Value.buildType == currentType)
            {
                //create the instance and set its data
                GameObject createdInstance = Instantiate(emptyBuildOption, buildUi.optionHolder);
                createdOptions.Add(createdInstance);

                BuilSelectionScript bSelect = createdInstance.GetComponent<BuilSelectionScript>();
                bSelect.SetData(build.Value);
                bSelect.Initialise(this, build.Value, itemReference);

                bSelect.SetIconImage(build.Value.buildIcon);
            }
        }


        //set category text
        buildUi.categoryTitle.text = type.ToString();

        UpdateBuildAvailabilty();
    }

    //we check the whole menu for ability to build
    public void UpdateBuildAvailabilty()
    {
        //find menu 
        foreach ( GameObject obj in createdOptions)
        {
            BuilSelectionScript bSelect = obj.GetComponent<BuilSelectionScript>();
            BuildData useData = bSelect.GetBuildData();
            bool buildable = p_Inventory.CanBuild(useData);

            if (buildable)
            {
                bSelect.SetColorBacking(canBuildColor);
            }
            else
            {
                bSelect.SetColorBacking(unableToBuildColor);
            }
        }
    }

    public void OnCategoryClick(BuildCategoryUiObject categoryObj)
    {
        //check if we slect
        if(categoryObj.bType != currentType)
        {
            SelectCategory(categoryObj.bType);
        }
    }

    void CloseBuildOptions()
    {
        buildUi.buildMenuHolder.SetActive(false);
        p_Mouselook.SetCursorLock(true);
        p_Mouselook.SetMouseLookOverride(false);
        p_Menu.SetOverrideInventory(false);

        //destroy the created option
        //generate the options
        if (createdOptions.Count > 0)
        {
            foreach (GameObject g in createdOptions)
            {
                Destroy(g);
            }
        }

        createdOptions = new List<GameObject>();
        thisAnimator.SetBool("BuildMenu", false);
    }

    public void OnOptionEnter(BuilSelectionScript buildSelection)
    {
        buildSelection.SetBackingColor(highlightOptionBacking);
    }


    public void OnOptionExit(BuilSelectionScript buildSelection)
    {
        buildSelection.SetBackingColor(defaultOptionBacking);
    }

    private GameObject createdRepresentorInstance;

    private int currentBuildId = 100;

    private bool representorOn = false;
    public void OnOptionClick(BuilSelectionScript buildSelection)
    {
        //select the build optiuon
        if (representorOn && currentBuildId == buildSelection.GetBuildData().buildId) return;

        representorOn = true;

        if(createdRepresentorInstance != null)
        {
            Destroy(createdRepresentorInstance);
        }

        createdRepresentorInstance = Instantiate(buildSelection.GetBuildData().representObject);
        bRep = createdRepresentorInstance.GetComponent<BuildRepresentor>();
        currentBuildId = buildSelection.GetBuildData().buildId;

        //not enter build mode
        if (!inBuildMode)
        {
            EnterBuildMode();
        }

        rotOffset = 0f;
    }
    
    void EnterBuildMode()
    {
        //set ui
        buildUi.buildDataHolder.SetActive(true);
        Transform[] children = buildUi.buildDataHolder.transform.GetComponentsInChildren<Transform>();
        if (children != null && children.Length > 0)
        {
            foreach (Transform g in children)
            {
                if (g != buildUi.buildDataHolder.transform)
                {
                    Destroy(g.gameObject);
                }
            }
        }

        //set the build details
        for (int i = 0; i < itemReference.allBuildItems[currentBuildId].inputs.Length; i++)
        {
            GameObject creatededObj = Instantiate(buildUi.buildDataPrefab, buildUi.buildDataHolder.transform);
            BuildUiDataScript dataUiScript = creatededObj.GetComponent<BuildUiDataScript>();
            dataUiScript.amountText.text = itemReference.allBuildItems[currentBuildId].inputs[i].quantity.ToString();
            dataUiScript.icon.sprite = itemReference.allItems[itemReference.allBuildItems[currentBuildId].inputs[i].id].iconSprite;
        }

        inBuildMode = true;
        thisAnimator.SetBool("BuildMode", true);
    }

    void ExitBuildMode()
    {
        buildUi.buildDataHolder.SetActive(false);

        inBuildMode = false;
        thisAnimator.SetBool("BuildMode", false);
        representorOn = false;


        if (createdRepresentorInstance != null)
        {
            Destroy(createdRepresentorInstance);
        }
    }

    BuildRepresentor bRep;
    private float rotOffset = 0f;
    private void Update()
    {
        //we do the calculations for the building object
        if (representorOn)
        {
            if (bRep == null) return;

            BuildData bData = itemReference.allBuildItems[currentBuildId];

            Vector3 bottomPos = new Vector3();
            Vector3 inFront = mCam.transform.position + (mCam.transform.forward * buildRange);

            RaycastHit hit;

            bool groundedBuild = false;
            if (Physics.Raycast(mCam.transform.position, mCam.transform.forward, out hit, buildRange, positionLayerMask))
            {
                bottomPos = hit.point;
            }
            else
            {
                //we raycast down to see if ground
                RaycastHit baseHit;
                if (Physics.Raycast(inFront, Vector3.down, out baseHit, buildRange / 3f, positionLayerMask))
                {
                    bottomPos = baseHit.point;
                    if(bData.offsetFromFloor == true)
                    {
                        bottomPos.y += bData.floorOffset;
                    }
                    groundedBuild = true;
                }
                else
                {
                    bottomPos = inFront;
                }
            }

            bool didSnap = false;
            //set rot
            if (bData.snapBuild)
            {
                bool snapped = lastSnapped;
                if (Time.time >= lastSnapTime)
                {
                    lastSnapTime = Time.time + snapIterationTime;
                    snapped = SnapBuild(bRep, bData);
                    lastSnapped = snapped;
                    if (snapped)
                    {
                        Debug.Log("snapped! " + currentSnapTo);
                        
                    }
                }

                if (snapped == true)
                {
                    didSnap = true;
                    createdRepresentorInstance.transform.position = targetSnapPos;
                    Debug.DrawLine(targetSnapPos, curSnapFromPos, Color.magenta);
                    createdRepresentorInstance.transform.rotation = targetSnapRot;       
                }
                else
                {
                    Vector3 forwardPlace = mCam.transform.forward;
                    forwardPlace.y = 0;
                    Quaternion targetRot = Quaternion.LookRotation(forwardPlace);
                    targetRot *= Quaternion.AngleAxis(rotOffset, Vector3.up);
                    createdRepresentorInstance.transform.rotation = targetRot;
                }
            }
            else
            {
                Vector3 forwardPlace = mCam.transform.forward;
                forwardPlace.y = 0;
                Quaternion targetRot = Quaternion.LookRotation(forwardPlace);
                targetRot *= Quaternion.AngleAxis(rotOffset, Vector3.up);
                createdRepresentorInstance.transform.rotation = targetRot;
            }

            bool showBuildAvailabilty = !RepresentorCollision(createdRepresentorInstance.transform.rotation, bRep, bRep.transform.position);


            if (bData.freeBuild == false && !didSnap)
            {
                showBuildAvailabilty = false;
            }

            if (bData.requireGrounded && groundedBuild == false && !didSnap)
            {
                showBuildAvailabilty = false;
            }
            //sheck collisons
            if (showBuildAvailabilty == false)
            {
                Debug.DrawLine(createdRepresentorInstance.transform.position, createdRepresentorInstance.transform.position + Vector3.up * 2f, Color.gray);
                allowedBuildCol = false;
                bRep.SetRepresentorColor(unableToBuildColor);
            }
            else
            {
                //we arent colliding
                allowedBuildCol = true;
                bRep.SetRepresentorColor(canBuildColor);
            }

            if (!didSnap)
            {
                createdRepresentorInstance.transform.position = bottomPos;
            }
        }
        else
        {
            //we handle attempting to edit builds etc
            if (!inBuildMenu && !inBuildMode && !inEditMode)
            {
                //this hits triggers and is design for the same colliders that are used for finding snappoint
                RaycastHit hit;
                if (Physics.Raycast(mCam.transform.position, mCam.transform.forward, out hit, editRayDistance, snapQuery, QueryTriggerInteraction.Collide))
                {
                    if (hit.transform.CompareTag("Build"))
                    {
                        if(lastObservedBuild != null)
                        {
                            if(lastObservedBuild != hit.transform.gameObject)
                            {
                                lastObservedBuild = hit.transform.gameObject;
                                lastViewBuild = hit.transform.GetComponent<BuildObject>();
                            }
                        }

                        if (!editMenuVisiblelast)
                        {
                            editMenuVisiblelast = true;
                            lastViewBuild = hit.transform.GetComponent<BuildObject>();
                        }
                        //set the postion and scale of the menu
                        if (lastViewBuild != null)
                        {
                            if (lastViewBuild.showEditMenu)
                            {

                                lastViewPostion = lastViewBuild.editCentrePosition.position; //for anchoring edit menu

                            float observedDistance = Vector3.Distance(lastViewBuild.editCentrePosition.position, mCam.transform.position);

                            //we wanna use the dotProduct to deduce the angle of viewing
                            float dot = Vector3.Dot(mCam.transform.forward, lastViewBuild.editCentrePosition.position - mCam.transform.position);
                            dot /= observedDistance;

                            float viewAngle = Mathf.Acos(dot);
                            viewAngle *= Mathf.Rad2Deg;

                                if (viewAngle < editMenuViewingAngle)
                                {
                                    if (!buildUi.editOptionsHolder.activeSelf)
                                    {
                                        buildUi.editOptionsHolder.SetActive(true);
                                    }

                                    Vector3 targetPos = mCam.WorldToScreenPoint(lastViewBuild.editCentrePosition.position);
                                    buildUi.editOptionsHolder.transform.position = targetPos;

                                    //handling the dynamic scaling
                                    float anglePercent = Mathf.Clamp01(1 - (viewAngle / editMenuViewingAngle));

                                    Vector3 desireScale = Vector3.Lerp(Vector3.one * 0.2f, Vector3.one * 1.3f, Mathf.Clamp01((editRayDistance - observedDistance) / editRayDistance));
                                    desireScale *= anglePercent;

                                    buildUi.editOptionsHolder.transform.localScale = desireScale;

                                    //set the data
                                    buildUi.buildHealthRepresentor.fillAmount = lastViewBuild.HealthPercent();

                                    //Handle the setting selection of the view 
                                    if(viewAngle < 2.5f && Vector3.Distance(mCam.transform.position, lastViewBuild.editCentrePosition.position) < 1.5f)
                                    {
                                        buildUi.centreHoverSpriteObj.color = canBuildColor;
                                        canEditSelection = true;
                                    }
                                    else
                                    {
                                        buildUi.centreHoverSpriteObj.color = defaultOptionBacking;
                                        canEditSelection = false;
                                    }
                                }
                                else
                                {
                                    if (buildUi.editOptionsHolder.activeSelf)
                                    {
                                        buildUi.editOptionsHolder.SetActive(false);
                                    }
                                }
                            }
                            else 
                            {
                                if (buildUi.editOptionsHolder.activeSelf)
                                {
                                    buildUi.editOptionsHolder.SetActive(false);
                                }

                            }
                        }
                    }
                }
                else
                {
                    if(editMenuVisiblelast == true)
                    {
                        lastSeen = Time.time + 0.3f;
                    }
                    editMenuVisiblelast = false;
                    lastViewBuild = null;
                }

                //to de enable the ui after looking away for a second
                if(Time.time > lastSeen && editMenuVisiblelast == false)
                {
                    buildUi.editOptionsHolder.SetActive(false);
                }
            }

            if (inEditMode)
            {

                //set scale
                Vector3 desireScale = Vector3.Lerp(buildUi.editOptionsHolder.transform.localScale, Vector3.one * 1.3f, 7f * Time.deltaTime);
                buildUi.editOptionsHolder.transform.localScale = desireScale;

                //set position
                Vector3 targetPos = mCam.WorldToScreenPoint(editModeAnchor);
                buildUi.editOptionsHolder.transform.position = targetPos;


                //take distances
                Vector3 curlook = mCam.WorldToScreenPoint(mCam.transform.position + mCam.transform.forward);

                float anchorFromDistance = Vector3.Distance(curlook, targetPos); //the base distance of view


                //to make the animation blend nicely
                float blendVal = Mathf.Clamp01(anchorFromDistance / 400f);
                thisAnimator.SetFloat("EditBlend", blendVal);

                //set visuals of the icon
                float h = buildUi.editViewIndicator.transform.position.y - targetPos.y;
                float l = buildUi.editViewIndicator.transform.position.x - targetPos.x;
                float angle = Mathf.Atan(h / l) * Mathf.Rad2Deg;
                if(l < 0)
                {
                    angle += 180f;
                }
                buildUi.editViewIndicator.transform.rotation = Quaternion.Euler(0, 0, angle);

                //find what the closest option is
                if (anchorFromDistance > 100f)
                {
                    editSelectionOptions = editSelectionOptions.OrderBy(x => Vector3.SqrMagnitude(x.transform.position - curlook)).ToList<EditOptionScript>();
                    if (hoverOption != null && hoverOption != editSelectionOptions[0])
                    {
                        p_audio.PlayLocalAudioClip(enterEditSelectionSound);
                    }
                    hoverOption = editSelectionOptions[0];

                    editSelectionOptions[0].optionBacking.color = editOptionsHighlight;
                    editSelectionOptions[0].transform.localScale = Vector3.one * 1.2f;
                    for (int i = 1; i < editSelectionOptions.Count; i++)
                    {
                        editSelectionOptions[i].optionBacking.color = editOptionsDefault;
                        editSelectionOptions[i].transform.localScale = Vector3.one;
                    }

                    canChooseOption = true;
                }
                else
                {
                    if (canChooseOption)
                    {
                        for (int i = 0; i < editSelectionOptions.Count; i++)
                        {
                            editSelectionOptions[i].optionBacking.color = editOptionsDefault;
                            editSelectionOptions[i].transform.localScale = Vector3.one;
                        }
                    }

                    canChooseOption = false;
                }


                //check we don't move too far away
                float fromplayerdistance = Vector3.Distance(mCam.transform.position, editModeAnchor);
                if (fromplayerdistance > 2f)
                {
                    CalculateEndEditMode();
                }
                else
                {
                    //we wanna use the dotProduct to deduce the angle of viewing
                    float dot = Vector3.Dot(mCam.transform.forward, (editModeAnchor - mCam.transform.position).normalized);
                    float viewAngle = Mathf.Acos(dot) * Mathf.Rad2Deg;
                    if(viewAngle > 35f)
                    {
                        CalculateEndEditMode();
                    }
                }
            }
        }
    }

    private GameObject lastObservedBuild;

    private bool canEditSelection;

    private BuildObject lastViewBuild;
    private float lastSeen = 0f;
    private bool editMenuVisiblelast = false;

    private bool allowedBuildCol;
    private bool lastSnapped = false;

    private Vector3 gizmoSize;
    private Vector3 gizmoCentre;

    private HashSet<Collider> lastSnap = new HashSet<Collider>();
    
    private bool inEditMode = false;
    private Vector3 lastViewPostion;
    List<EditOptionScript> editSelectionOptions;
    private Vector3 editModeAnchor;

    private EditOptionScript hoverOption;
    private bool canChooseOption;

    //edit mode handling
    void EnterEditMode()
    {
        inEditMode = true;
        editModeAnchor = lastViewPostion;

        p_audio.PlayLocalAudioClip(enterEditModeSound);

        buildUi.editSelectionsHolder.SetActive(true);
        buildUi.editViewIndicator.SetActive(true);

        buildUi.centreHoverSpriteObj.color = unableToBuildColor;
        thisAnimator.SetBool("EditMode", true);
    }

    void CalculateEndEditMode()
    {
        inEditMode = false;

        buildUi.editSelectionsHolder.SetActive(false);
        buildUi.editViewIndicator.SetActive(false);

        buildUi.centreHoverSpriteObj.color = defaultOptionBacking;

        //we check to see which one to select
        if(canChooseOption && hoverOption!= null)
        {
            switch (hoverOption.e_Type)
            {
                case EditOptionScript.EditType.destroy:
                    //enter destroy mode

                    break;
                case EditOptionScript.EditType.repair:
                    //repair for one
                    DoBuildRepair(lastViewBuild);
                    break;

                case EditOptionScript.EditType.returnToMain:
                    break;
            }
        }

        thisAnimator.SetBool("EditMode", false);
    }


    void DoBuildRepair(BuildObject target)
    {
        Debug.Log("Do build repair: " + target);
        //play animation 

        //play particle

        //use ressources

        //do repiar
        float repairPercent = 1f;
        p_BuildManager.RepairBuild(target.gameObject, repairPercent);
    }


    private GameObject currentSnapTo;
    private Vector3 curSnapFromPos;
    //must stop the requirement to rotate the hit detector
    bool SnapBuild(BuildRepresentor rep, BuildData bData)
    {
        Collider[] snapHitDetect = Physics.OverlapBox(mCam.transform.position + (mCam.transform.forward * buildRange / 2), checkRange, Quaternion.LookRotation(mCam.transform.forward), snapQuery, QueryTriggerInteraction.Collide);
        HashSet<Collider> snapHash = snapHitDetect.ToHashSet<Collider>();

        if(snapHitDetect != null && snapHash != lastSnap)
        {
            lastSnap = snapHash;

            List<SnapPoint> queriedSnaps = new List<SnapPoint>();

            if(snapHitDetect.Length > 0)
            {
                foreach (Collider item in snapHitDetect)
                {
                    if (item.transform.CompareTag("Build"))
                    {
                        BuildObject bObj = item.gameObject.GetComponent<BuildObject>();

                        foreach (SnapPoint p in bObj.snapPoint)
                        {  
                                Vector3 m_Forward = mCam.transform.forward;
                                m_Forward.y = 0;

                                Vector3 m_Between = p.point.position - mCam.transform.position;
                                m_Between.y = 0;
                                float checkBetween = Vector3.Dot(m_Forward, m_Between);
                                checkBetween /= Vector3.Magnitude(m_Between);

                                float angle = Mathf.Acos(checkBetween) * Mathf.Rad2Deg; 

                                if (angle <= snapAngle)
                                {
                                    queriedSnaps.Add(p);
                                }
                   
                        }                     
                    }
                }
            }

            //sort the list by distance to player
            

            queriedSnaps = queriedSnaps.OrderBy(x => SnapCoefficient(x)).ToList<SnapPoint>();

            //we go through each of the reps types
            foreach (SnapPoint sPoint in rep.snapPoints)
            {
                if (!sPoint.exludeFromSnappingTo)
                {
                    SnapPoint.SnapType chooseS = SnapPoint.SnapType.foundation;
                    switch (sPoint.sType)
                    {
                        case SnapPoint.SnapType.foundation:
                            chooseS = SnapPoint.SnapType.foundation;
                            break;
                        case SnapPoint.SnapType.floorSocket:

                            break;
                        case SnapPoint.SnapType.toFoundation:
                            chooseS = SnapPoint.SnapType.foundation;
                            break;
                        case SnapPoint.SnapType.wall:
                            chooseS = SnapPoint.SnapType.wall;
                            break;
                        case SnapPoint.SnapType.toWall:
                            chooseS = SnapPoint.SnapType.wall;
                            break;
                        case SnapPoint.SnapType.window:
                            break;
                        case SnapPoint.SnapType.corner:

                            break;
                        case SnapPoint.SnapType.toCorner:
                            chooseS = SnapPoint.SnapType.corner;
                            break;
                        case SnapPoint.SnapType.other:

                            break;
                        case SnapPoint.SnapType.door:
                            chooseS = SnapPoint.SnapType.doorSocket;
                            break;

                        case SnapPoint.SnapType.doorSocket:

                            break;
                        default:
                            break;
                    }

                    for (int i = 0; i < queriedSnaps.Count; i++)
                    {
                        if (queriedSnaps[i].connected && !sPoint.allowConnectToConnected)
                        {
                            //we don't want to connect to connected
                        }
                        else
                        {
                            Vector3 offset = Vector3.zero;
                            //so we can start floors from top of walls
                            if(queriedSnaps[i].sType == SnapPoint.SnapType.floorSocket && chooseS == SnapPoint.SnapType.foundation && !bData.isFoundation)
                            {
                                chooseS = SnapPoint.SnapType.floorSocket;
                                offset.y = bData.floorOffset;
                            }
                            if (queriedSnaps[i].sType == chooseS)
                            {
                                bool doable = TestSnapPosition(sPoint, queriedSnaps[i], rep, offset);
                                if (doable)
                                {
                                    lastSnapped = true;
                                    currentSnapTo = queriedSnaps[i].point.gameObject;
                                    curSnapFromPos = sPoint.point.position;
                                    return true;
                                }
                            }else
                            {
                             if(i == queriedSnaps.Count)
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
        }
        else
        {
             if(snapHash == lastSnap && lastSnapped)
            {
                Debug.Log("Equal snap hash");
                return true;
            }
        }

        return false;
    }

    float SnapCoefficient(SnapPoint x)
    {
        float z = Vector3.SqrMagnitude(x.point.position - mCam.transform.position);
        //get the angle between
        float dot = Vector3.Dot(mCam.transform.forward, x.point.position - mCam.transform.position);
        float angle = Mathf.Acos(dot / Vector3.Magnitude(x.point.position - mCam.transform.position));
        angle /= Mathf.Rad2Deg;
        z *= angle;
        return z;
    }

    bool TestSnapPosition(SnapPoint from, SnapPoint to, BuildRepresentor rep, Vector3 toOffset)
    {
        //note : too offset is so we can account for the thickness of floors etc

        //handle rotation  
        //we need to decide how to rotate
        float angle = Vector3.Angle(new Vector3(from.point.forward.x, 0f, from.point.forward.z), new Vector3(-to.point.forward.x, 0f, -to.point.forward.z));

        float testAngle = Vector3.Angle(new Vector3(rep.transform.forward.x, 0f, rep.transform.forward.z), Vector3.right);
        float compareAngle = Vector3.Angle(new Vector3(rep.transform.forward.x, 0f, rep.transform.forward.z), new Vector3(-to.point.forward.x, 0f, -to.point.forward.z));
        if(testAngle < compareAngle)
        {
           // angle *= -1f;
        }
  

        Quaternion rot = Quaternion.AngleAxis(angle, Vector3.up);
      //  rep.transform.position = rot * (rep.transform.position - pivotPoint) + pivotPoint;
        Quaternion lookTo = rot * rep.transform.rotation;

        //  rep.transform.rotation = lookTo;
        //rep.transform.rotation = lookTo;

        //handle positions
      
        Vector3 rotatedPointAround = rot * (from.point.position - rep.transform.position);// rot * from.point.localPosition; //rotates the local postiion
        rotatedPointAround += rep.transform.position;
        Vector3 offsetAdd = rep.transform.position - rotatedPointAround; //(addedPos - rep.transform.position) + (to.point.position - addedPos);
        
        Vector3 finalPos = to.point.position + offsetAdd + toOffset;


        Debug.DrawRay(from.point.position, Vector3.up, Color.blue, snapIterationTime);
        Debug.DrawRay(rotatedPointAround, Vector3.up, Color.red, snapIterationTime);
        Debug.DrawRay(to.point.position, Vector3.up, Color.green, snapIterationTime);

        bool collided = RepresentorCollision(lookTo,rep, finalPos); //update this 
        if (!collided)
        {
            targetSnapPos = finalPos;
            targetSnapRot = lookTo;
            
            return true;
        }
        if (collided)
        {
            return false;
        }
        lastSnapped = false;
        return false;
    }

    private Quaternion targetSnapRot;
    private Vector3 targetSnapPos;
    
    bool RepresentorCollision( Quaternion rot, BuildRepresentor representor, Vector3 position)
    {
        foreach (CollisionBounds col_bound in representor.bounds)
        {
            Vector3 centre = (position + (col_bound.bottomBounds.position - position) + ((col_bound.topBounds.position - col_bound.bottomBounds.position) * 0.5f));
            Vector3 size = (col_bound.topBounds.localPosition - col_bound.bottomBounds.localPosition);

            size.x = Mathf.Abs(size.x);
            size.y = Mathf.Abs(size.y);
            size.z = Mathf.Abs(size.z);

            gizmoCentre = centre;
            gizmoSize = size;

      

            Collider[] overlaps = Physics.OverlapBox(centre, size * 0.5f, rot, buildLayerMask, QueryTriggerInteraction.Ignore);

            if (overlaps != null && overlaps.Length > 0)
            {
                foreach (Collider col in overlaps)
                {
                    Debug.DrawLine(col.transform.position, col.transform.position + Vector3.up * 2f);
                }
               // Debug.Log("overlapping!");
                return true;
            }
        }

        return false;
    }

    public void BuildItem()
    {
        if (createdRepresentorInstance == null) return;
        //perform checks
        BuildData useBuild = itemReference.allBuildItems[currentBuildId];
        bool buildable = p_Inventory.CanBuild(useBuild);
        if (noResourceRequirement)
        {
            buildable = true;
        }

        //send build request
        if (buildable)
        {
            //various viusal settings
            p_audio.PlaySound(buildSound_Key, mCam.transform.position);
            thisAnimator.Play(buildAnimation.name, 0, 0f);

            EZCameraShake.CameraShaker.Instance.ShakeOnce(1f, 1f, 0f, 0.5f);

            //actually build
            p_BuildManager.SendBuildItemRequest(currentBuildId, createdRepresentorInstance.transform.position, createdRepresentorInstance.transform.rotation);
            if (!noResourceRequirement)
            {
                foreach (CraftingInput input in useBuild.inputs)
                {
                    for (int i = 0; i < input.quantity; i++)
                    {
                        p_Inventory.RemoveById(input.id, true);
                    }
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (inBuildMode)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(mCam.transform.position + (mCam.transform.forward * buildRange / 2), checkRange);

            Gizmos.color = Color.grey;
             Gizmos.matrix = Matrix4x4.TRS(gizmoCentre, createdRepresentorInstance.transform.rotation, gizmoSize);
             Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }

    public void InsectButton()
    {
        
    }
    #endregion
}