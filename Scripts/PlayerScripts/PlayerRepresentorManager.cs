using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using UnityEngine.UI;

public class PlayerRepresentorManager : NetworkBehaviour
{
    [Header("Settings")]
    public Transform rootBone;
    private ItemReference allitems;
    [SerializeField] private GameObject emptyRepresentor;

    private Animator instanceAnimator;
    private GameObject representorInstance;

    private Dictionary<string, Transform> boneMap;
    private Dictionary<string, Transform> mpboneMap;

    private SkinnedMeshRenderer skinnedMeshTarget;
    private SkinnedMeshRenderer mpskinnedMeshTarget;

    [SerializeField] private GameObject multiplayerRepresentor;

    private RawImage representorImage;
    private int indexAmount = 5;

    private bool canRotate; //for rotating character model in inventory
    private Slider rotationSlider;
    private Transform toRotate;



    public void InitialiseUiCharacter(UiReference uiRef)
    {

        if (!base.IsOwner) return;

        representorImage = uiRef.camerRenderImage; //we recieve reference
        rotationSlider = uiRef.rotationSlider;

    }
    public void SetItemReference(ItemReference iRef)
    {
        allitems = iRef;
    }
    private void Start()
    {
        Transform intantiateHolder = GameObject.Find("RepresentorContainer").transform;
        representorInstance = Instantiate(emptyRepresentor, intantiateHolder);

        PlayerRepresentorObjectScript pRepresentObj = representorInstance.GetComponent<PlayerRepresentorObjectScript>();
        PlayerRepresentorObjectScript mpPrepobj = multiplayerRepresentor.GetComponent<PlayerRepresentorObjectScript>();

        skinnedMeshTarget = pRepresentObj.meshRendererHolder.GetComponent<SkinnedMeshRenderer>();
        mpskinnedMeshTarget = mpPrepobj.meshRendererHolder.GetComponent<SkinnedMeshRenderer>();

        instanceAnimator = pRepresentObj.thisAnimator;
        toRotate = pRepresentObj.toRotate;

        //make bone map off all bones
        boneMap = new Dictionary<string, Transform>();
        foreach (Transform bone in skinnedMeshTarget.bones)
            boneMap[bone.gameObject.name] = bone;

        mpboneMap = new Dictionary<string, Transform>();
        foreach (Transform mpbone in mpskinnedMeshTarget.bones)
            mpboneMap[mpbone.gameObject.name] = mpbone;

        representorInstance.SetActive(false);
    }
    // Update is called once per frame
    void Update()
    {
        if (!base.IsOwner) return;

        //handle rotation
        if (canRotate)
        {
            toRotate.transform.localRotation = Quaternion.Euler(0, rotationSlider.value, 0);
        }
    }

    public void OnRot()
    {

    }

    public void SetClothes(Clothing_Data cData)
    {

    }


    public GameObject BindMeshToUi(Clothing_Data cData)
    {
            GameObject instance = Instantiate(cData.clothingObject, representorInstance.transform.position, representorInstance.transform.rotation, representorInstance.transform);

            //for ui object
            SkinnedMeshRenderer myRenderer = instance.GetComponent<ClotheObjectScript>().clotheMesh.GetComponent<SkinnedMeshRenderer>();

            instance.GetComponent<ClotheObjectScript>().clotheMesh.layer = 11;

            Transform[] newBones = new Transform[myRenderer.bones.Length];
            for (int i = 0; i < myRenderer.bones.Length; i++)
            {
                GameObject bone = myRenderer.bones[i].gameObject;
                if (!boneMap.TryGetValue(bone.name, out newBones[i]))
                {
                    Debug.Log("Unable to map bone \"" + bone.name + "\" to target skeleton.");
                    break;
                }
            }
            myRenderer.bones = newBones;
        return instance;
    }
    
    
    public GameObject BindObjectToMesh(Clothing_Data cData)
    {

        GameObject _instance = Instantiate(cData.clothingObject, multiplayerRepresentor.transform.position, multiplayerRepresentor.transform.rotation, multiplayerRepresentor.transform);

        //for normal object
        SkinnedMeshRenderer _myRenderer = _instance.GetComponent<ClotheObjectScript>().clotheMesh.GetComponent<SkinnedMeshRenderer>();
        _myRenderer.rootBone = rootBone;
        if (base.IsOwner)
        {
            _instance.GetComponent<ClotheObjectScript>().clotheMesh.layer = 7;
        }
        else
        {
           _instance.GetComponent<ClotheObjectScript>().clotheMesh.layer = 6;
        }

        Transform[] _newBones = new Transform[_myRenderer.bones.Length];
        for (int i = 0; i < _myRenderer.bones.Length; i++)
        {
            GameObject bone = _myRenderer.bones[i].gameObject;
            if (!mpboneMap.TryGetValue(bone.name, out _newBones[i]))
            {
                Debug.Log("Unable to map bone \"" + bone.name + "\" to target skeleton.");
                break;
            }
        }
        _myRenderer.bones = _newBones;
        Debug.Log("bound to mesh_" + allitems.allItems[cData.clothingId].itemName);
        return _instance;
    }

    [ObserversRpc]void RpcMultiplayerBindToMesh(int clotheId)
    {
        ItemData iData = allitems.allItems[clotheId];
        Clothing_Data cData = iData.clothingData;
        //do binding
    }



    #region Torso and legs
    public void SetTorso(bool value)
    {
        representorInstance.GetComponent<PlayerRepresentorObjectScript>().torsoMesh.SetActive(value);
        if (IsServer)
        {
            SetMultiplayerTorso(value);
        }
        else
        {
            Cmd_SetMPTorso(value);
        }
    }
    [ServerRpc]
    void Cmd_SetMPTorso(bool value){SetMultiplayerTorso(value); }
    [ObserversRpc]
    private void SetMultiplayerTorso(bool value)
    {
        multiplayerRepresentor.GetComponent<PlayerRepresentorObjectScript>().torsoMesh.SetActive(value);
    }
    public void SetBottom(bool value)
    {
        representorInstance.GetComponent<PlayerRepresentorObjectScript>().bottomMesh.SetActive(value);
        if (IsServer)
        {
            SetMultiplayerBottom(value);
        }
        else
        {
            Cmd_SetMPBottom(value);
        }
    }
    [ServerRpc]
    void Cmd_SetMPBottom(bool value) { SetMultiplayerTorso(value); }
    [ObserversRpc]
    private void SetMultiplayerBottom(bool value)
    {
        multiplayerRepresentor.GetComponent<PlayerRepresentorObjectScript>().bottomMesh.SetActive(value);
    }
    #endregion 

    public void InventoryOpened()
    {
        representorInstance.SetActive(true);

     //   int indexToUse = Random.Range(0, indexAmount);
    //    instanceAnimator.SetInteger("ChooseInt", indexToUse);
       // instanceAnimator.SetTrigger("SwitchIdle");

        canRotate = true;
        rotationSlider.value = 0f;
    }

    public void InventoryClosed()
    {
        representorInstance.SetActive(false);

        canRotate = false;
    }

    public void SetMutliplayerLayers()
    {

    }
}
