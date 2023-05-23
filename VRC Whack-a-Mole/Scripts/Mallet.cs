
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

[RequireComponent(typeof(VRCPickup))]
[RequireComponent(typeof(VRCObjectSync))]
[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class Mallet : UdonSharpBehaviour
{
    private VRCPickup pickup;
    private VRCObjectSync objectSync;

    void Start()
    {
        pickup = (VRCPickup)gameObject.GetComponent(typeof(VRCPickup));
        objectSync = (VRCObjectSync)gameObject.GetComponent(typeof(VRCObjectSync));

        // Be damn sure players aren't allowed to steal the mallet
        pickup.DisallowTheft = true;
    }

    public void OnGameEnd()
    {
        pickup.Drop();
        objectSync.Respawn();
    }

    public VRCPickup GetPickup() => pickup;
}
