using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RockNodeScript : MonoBehaviour
{
    public int maxRockHealth;

   [SerializeField] private Renderer m_Renderer;
    
   private MaterialPropertyBlock _PropBlock;


    private void Awake()
    {
        _PropBlock = new MaterialPropertyBlock();
    }

    public void OnHealthUpdated(int amount)
    {
        //we set to the amount
        m_Renderer.GetPropertyBlock(_PropBlock);
        float ar = (float)amount / maxRockHealth;

      //  Debug.Log("rockar " + ar + "amount " + amount + "m rock health " + maxRockHealth + " div " + (amount / maxRockHealth));

        _PropBlock.SetFloat("_RevealValue", ar);

        _PropBlock.SetFloat("_CrackValue", 1 - ar);

        m_Renderer.SetPropertyBlock(_PropBlock);
    }    
}
