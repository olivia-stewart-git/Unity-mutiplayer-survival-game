using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GunInstance
{
    public bool magLoaded = false;
    public int magObjectId;
    public List<int> loadedMagazineIds = new List<int>();
}
