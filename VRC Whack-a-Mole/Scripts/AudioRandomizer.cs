
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(VRCSpatialAudioSource))]
[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class AudioRandomizer : UdonSharpBehaviour
{
    public AudioClip[] clips;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayRandomSound()
    {
        audioSource.clip = clips[Random.Range(0, clips.Length)];
        audioSource.Play();
    }

    public void StopSound()
    {
        audioSource.Stop();
    }
}
