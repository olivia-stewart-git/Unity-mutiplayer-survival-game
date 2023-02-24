using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

public class PlayerReferencer : NetworkBehaviour
{ 
    private GameObject player;
    private void Start()
    {        
        player = base.Owner.FirstObject.gameObject;
    }

    public GameObject GetPlayer()
    {
        return player;
    }
}
