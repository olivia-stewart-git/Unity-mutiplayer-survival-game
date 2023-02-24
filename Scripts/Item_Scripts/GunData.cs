using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class GunData : ScriptableObject
{
    [Header("Shooting settings")]
    public bool infiniteAmmo = false;
    public bool useSingleShots = false; //for if gun doesn't use magazine rather single bullets
    [Space]
    public float damageMultiplier = 1f;
    public float fireRate = 0.1f;
    public string ammunitionSize; //this allows for multi ammo types
    public float velocityMultiplier = 1f;
    public bool exitAdsOnShoot;
    [Space]
    public int buildPenetration = 0;

    [Header("ReloadSettings")]
    public float reloadTime;
    public float slideTime;
    public float slideTimeOffset;
    public float unloadTime;
    [Space]
    public bool unloadFirst;
    public bool useSlideReload;

    [Header("Accuracy settings")]
    public float accuracy = 0.9f;
    public float adsAccuracy = 0.5f;
    public float maxAccuracyMultiplier = 1f;
    public float camRecoilSpeed = 8f;
    public float shootRangeDeterminer = 100f;
    [Space]
    public float visualAimRecoilAimDamper = 0.3f;
    [Space]
    public Vector3 rotationalRecoil = new Vector3(10f, 5f, 7f);
    public Vector3 recoilKickback = new Vector3(0.015f, 0f, -0.2f);
    [Space]
    public float postionalReturnSpeed = 18f;
    public float rotationReturnSpeed = 30f;
    [Space]
    public float postionalSpeed = 8f;
    public float rotationSpeed = 8f;
    [Space]
    public int toMaxAccuracyShots = 4;
    public float returnAccuracyTime = 0.4f;
    public float returnAccuracySpeed = 3f;
    [Space]
    public float crosshairMultiplier = 3f;

    [Header("Camera recoil")]

    public AnimationCurve recoilPatternX;
    public AnimationCurve recoilPatternY;
    public AnimationCurve recoilPatternNegativeValues;
    [Space]
    public float ironSightsFov = 60f;
    [Space]
    public float cameraRecoilBaseAngle = 5f;
    public float camMultiplier = 0.5f;
    public float camAdsMultiplier = 0.3f;

    [Header("DurabilitySettings")]
    public float durabilityOnShoot = 0.5f;

    public enum ShootType { single, auto, burst}
    [Header("fire selection settings")]
    public ShootType shootType;
    public bool singleFireAllowed = true;
    public bool rapidFireAllowed = false;
    public bool burstFireAllowed = false;
    public int burstShots = 3;
    public float burstFireRate;

    [Header("Charge shot settings")]
    public bool chargeToFire; //to charge a shot e.g a bow
    public bool forceWalkOnCharge;
    public bool shootOnCharge; //shoot straight away when charged
    public bool allowAdsOnlyOnCharge; //can only aim down sights when weapon charged
    public bool resetChargeOnShot;
    [Space]
    public bool canShootEarly;  //things like bows shoot early
    [Range(0f, 1f)] public float earlyChargeThreshold;
    [Space]
    public float timeToCharge;
    public AnimationCurve modCurve; //how things are modified whenever you shoot early
}
