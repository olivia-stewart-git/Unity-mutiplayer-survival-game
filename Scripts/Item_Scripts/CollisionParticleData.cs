using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class CollisionParticleData : ScriptableObject
{
    [Header("particles")]
    public string[] particlesDirtyGround;
    public string[] particlesGrass;
    public string[] particlesGravel;
    public string[] particlesLeaves;
    public string[] particlesMetal;
    public string[] particlesSand;
    public string[] particlesWood;
    public string[] particlesWater;
    public string[] particlesSnow;
    public string[] particlesTile;
    public string[] particlesRock;
    public string[] particlesMud;
    public string[] particlesFlesh;
    [Space]
    public string clip_DirtyGround;
    public string clip_Grass;
    public string clip_Gravel;
    public string clip_Leaves;
    public string clip_Metal;
    public string clip_Sand;
    public string clip_Wood;
    public string clip_Water;
    public string clip_Snow;
    public string clip_Tile;
    public string clip_Rock;
    public string clip_Mud;
    public string clip_Flesh;
    [Space]
    public string decal_DirtyGround;
    public string decal_Grass;
    public string decal_Gravel;
    public string decal_Leaves;
    public string decal_Metal;
    public string decal_Sand;
    public string decal_Wood;
    public string decal_Water;
    public string decal_Snow;
    public string decal_Tile;
    public string decal_Rock;
    public string decal_Mud;
    public string decal_Flesh;
}
