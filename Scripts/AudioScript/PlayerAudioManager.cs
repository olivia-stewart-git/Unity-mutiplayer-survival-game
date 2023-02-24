using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

public class PlayerAudioManager : NetworkBehaviour
{
    ObjectPooler objPool;

    private Dictionary<string, AClip> audioRegistry;
    public AClip[] playableAudio;

    public AudioSource localSource;

    private void Awake()
    {
        objPool = ObjectPooler.Instance;
    }

    public override void OnStartClient()
    {
        audioRegistry = new Dictionary<string, AClip>();
        for (int i = 0; i < playableAudio.Length; i++)
        {
            audioRegistry.Add(playableAudio[i].name, playableAudio[i]);
        }
    }

    float beginPitch = 0f;

    float beginVolume;

    //we play sound from pool at position
    public void PlaySound(string audioTag, Vector3 position)
    {
        if (IsServer)
        {
            MakeNoise(audioTag, position);
        }
        else
        {
            SendNoiseCommand(audioTag, position);
        }
    }

    [ServerRpc]
    void SendNoiseCommand(string audioTag, Vector3 position)
    {
        MakeNoise(audioTag, position);
    }

    [ObserversRpc]
    public void MakeNoise(string audioTag, Vector3 position)
    {

        GameObject audioInstance = objPool.SpawnFromPool("audio_object", position, Quaternion.identity);
        if (audioInstance == null)
        {
            Debug.LogError("Could not spawn pooled Object of name " + audioTag);
            return;
        }

        AudioSource a_Source = audioInstance.GetComponent<AudioSource>();
        
        //check if we should make noise 2d or 3d for plyer

        GameObject playerObj = base.Owner.FirstObject.gameObject;

        float distance = Vector3.Distance(position, playerObj.transform.position);
        if(distance < 2f)
        {
            a_Source.spatialBlend = 0f;
        }
        else
        {
            a_Source.spatialBlend = 1f;
        }

           //change le audio values
        if (beginPitch == 0f)
        {
            beginPitch = a_Source.pitch;
        }
        if (beginVolume == 0f)
        {
            beginVolume = a_Source.volume;
        }

        AClip targetClip = audioRegistry[audioTag];

        if (targetClip.clip.Length == 0) return; //for uninitailed audio stuff

        if (targetClip.pitchVariance > 0)
        {
            a_Source.pitch = beginPitch + Random.Range(-targetClip.pitchVariance, targetClip.pitchVariance);
        }
        else
        {
            a_Source.pitch = beginPitch;
        }

        if (targetClip == null)
        {
            Debug.Log("could not find audio of name " + audioTag);
            return;
        }

        a_Source.maxDistance = targetClip.radius;

        a_Source.volume = targetClip.volumeMultiplier * beginVolume;
        a_Source.PlayOneShot(targetClip.clip[Random.Range(0, targetClip.clip.Length)]);
    }

    public void PlayLocalAudioClip(AudioClip clip)
    {
        localSource.PlayOneShot(clip);   
    }
}

[System.Serializable]
public class AClip
{
    public string name;
    public AudioClip[] clip;
    public float volumeMultiplier = 1;
    public float radius = 1;
    public float pitchVariance = 0f;
}
