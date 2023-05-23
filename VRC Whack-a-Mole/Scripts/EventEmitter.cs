
using UdonSharp;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking.Types;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class EventEmitter : UdonSharpBehaviour
{
    public UdonSharpBehaviour Behaviour;
    public string Event;

    public bool Networked = false;
    public NetworkEventTarget NetworkOptions = NetworkEventTarget.Owner;

    public Collider ObjectCollider;

    [UdonSynced]
    private bool Interactive = true;

    public override void Interact()
    {
        // Based on: ActivaterUdonEvent.cs:Interact
        // Copyright (c) 2021 Hannah Giovanna Dawson
        // MIT License

        if (Behaviour != null)
        {
            if (Networked)
            {
                if (NetworkOptions == NetworkEventTarget.All)
                    Behaviour.SendCustomNetworkEvent(NetworkEventTarget.All, Event);
                else
                    Behaviour.SendCustomNetworkEvent(NetworkEventTarget.Owner, Event);
            }
            else
                Behaviour.SendCustomEvent(Event);
        }
    }

    public void SetInteractions(bool interactive)
    {
        Interactive = interactive;
        OnDeserialization();
        RequestSerialization();
    }

    public override void OnDeserialization()
    {
        if (ObjectCollider != null)
            ObjectCollider.enabled = Interactive;
    }
}
