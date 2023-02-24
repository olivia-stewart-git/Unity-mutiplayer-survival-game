using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

public class PlayerFootStepManager : NetworkBehaviour
{
    private GameObject lastObserved;
    private WorldSurface lastSurface;

    [SerializeField] private float rayLength = 0.1f;
    [SerializeField] private LayerMask surfaceMask;
    [SerializeField] private Transform rayFrom;

    private PlayerAudioManager p_Audio;

    bool initialised = false;
    public void Initialise(PlayerAudioManager pAudio)
    {
        p_Audio = pAudio;
        initialised = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!base.IsOwner || !initialised) return;

        RaycastHit hit;
        float fromAmount = rayFrom.position.y - transform.position.y + rayLength; //this is so we can still detect water
        if (Physics.Raycast(rayFrom.position, Vector3.down, out hit, fromAmount, surfaceMask))
        {
            if (lastObserved != hit.transform.gameObject)
            {
                lastSurface = hit.transform.gameObject.GetComponent<WorldSurface>();
            }
            Debug.DrawLine(rayFrom.position, hit.point);
            lastObserved = hit.transform.gameObject;
        }
    }

    public void PlayFootStepSound()
    {
        if (lastObserved == null || lastSurface == null) return;

        switch (lastSurface.surface)
        {
            case WorldSurface.SurfaceType.DirtyGround:
                p_Audio.PlaySound("FootStep_DirtyGround", transform.position);
                break;
            case WorldSurface.SurfaceType.Grass:
                p_Audio.PlaySound("FootStep_Grass", transform.position);
                break;
            case WorldSurface.SurfaceType.Gravel:
                p_Audio.PlaySound("FootStep_Gravel", transform.position);
                break;
            case WorldSurface.SurfaceType.Leaves:
                p_Audio.PlaySound("FootStep_Leaves", transform.position);
                break;
            case WorldSurface.SurfaceType.Metal:
                p_Audio.PlaySound("FootStep_Metal", transform.position);
                break;
            case WorldSurface.SurfaceType.Sand:
                p_Audio.PlaySound("FootStep_Sand", transform.position);
                break;
            case WorldSurface.SurfaceType.Wood:
                p_Audio.PlaySound("FootStep_Wood", transform.position);
                break;
            case WorldSurface.SurfaceType.Water:
                p_Audio.PlaySound("FootStep_Water", transform.position);
                break;
            case WorldSurface.SurfaceType.Snow:
                p_Audio.PlaySound("FootStep_Snow", transform.position);
                break;
            case WorldSurface.SurfaceType.Tile:
                p_Audio.PlaySound("FootStep_Tile", transform.position);
                break;
            case WorldSurface.SurfaceType.Rock:
                p_Audio.PlaySound("FoodStep_Rock", transform.position);
                break;
            case WorldSurface.SurfaceType.Mud:
                p_Audio.PlaySound("FootStep_Mud", transform.position);
                break;
        }
    }
    public void PlayJumpSound()
    {
        if (lastObserved == null || lastSurface == null) return;

        switch (lastSurface.surface)
        {
            case WorldSurface.SurfaceType.DirtyGround:
                p_Audio.PlaySound("FootStep_DirtyGround_Jump", transform.position);
                break;
            case WorldSurface.SurfaceType.Grass:
                p_Audio.PlaySound("FootStep_Grass_Jump", transform.position);
                break;
            case WorldSurface.SurfaceType.Gravel:
                p_Audio.PlaySound("FootStep_Gravel_Jump", transform.position);
                break;
            case WorldSurface.SurfaceType.Leaves:
                p_Audio.PlaySound("FootStep_Leaves_Jump", transform.position);
                break;
            case WorldSurface.SurfaceType.Metal:
                p_Audio.PlaySound("FootStep_Metal_Jump", transform.position);
                break;
            case WorldSurface.SurfaceType.Sand:
                p_Audio.PlaySound("FootStep_Sand_Jump", transform.position);
                break;
            case WorldSurface.SurfaceType.Wood:
                p_Audio.PlaySound("FootStep_Wood_Jump", transform.position);
                break;
            case WorldSurface.SurfaceType.Water:
                p_Audio.PlaySound("FootStep_Water_Jump", transform.position);
                break;
            case WorldSurface.SurfaceType.Snow:
                p_Audio.PlaySound("FootStep_Snow", transform.position);
                break;
            case WorldSurface.SurfaceType.Tile:
                p_Audio.PlaySound("FootStep_Tile_Jump", transform.position);
                break;
            case WorldSurface.SurfaceType.Rock:
                p_Audio.PlaySound("FootStep_Rock_Jump", transform.position);
                break;
            case WorldSurface.SurfaceType.Mud:
                break;
        }
    }

    public void PlayJumpLandSound()
    {
        if (lastObserved == null || lastSurface == null) return;

        switch (lastSurface.surface)
        {
            case WorldSurface.SurfaceType.DirtyGround:
                p_Audio.PlaySound("FootStep_DirtyGround_Jump_Land", transform.position);
                break;
            case WorldSurface.SurfaceType.Grass:
                p_Audio.PlaySound("FootStep_Grass_Jump_Land", transform.position);
                break;
            case WorldSurface.SurfaceType.Gravel:
                p_Audio.PlaySound("FootStep_Gravel_Jump_Land", transform.position);
                break;
            case WorldSurface.SurfaceType.Leaves:
                p_Audio.PlaySound("FootStep_Leaves_Jump_Land", transform.position);
                break;
            case WorldSurface.SurfaceType.Metal:
                p_Audio.PlaySound("FootStep_Metal_Jump_Land", transform.position);
                break;
            case WorldSurface.SurfaceType.Sand:
                p_Audio.PlaySound("FootStep_Sand_Jump_Land", transform.position);
                break;
            case WorldSurface.SurfaceType.Wood:
                p_Audio.PlaySound("FootStep_Wood_Jump_Land", transform.position);
                break;
            case WorldSurface.SurfaceType.Water:
                p_Audio.PlaySound("FootStep_Water_Jump_Land", transform.position);
                break;
            case WorldSurface.SurfaceType.Snow:
                p_Audio.PlaySound("FootStep_Snow", transform.position);
                break;
            case WorldSurface.SurfaceType.Tile:
                p_Audio.PlaySound("FootStep_Tile_Jump_Land", transform.position);
                break;
            case WorldSurface.SurfaceType.Rock:
                p_Audio.PlaySound("FootStep_Rock_Jump_Land", transform.position);
                break;
            case WorldSurface.SurfaceType.Mud:
                p_Audio.PlaySound("FootStep_DirtyGround_Jump_Land", transform.position);
                break;
        }
    }
}
