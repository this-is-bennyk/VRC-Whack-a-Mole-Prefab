
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DemoCabinetAnimator : UdonSharpBehaviour
{
    public AudioRandomizer QuarterInsertSound;

    public void OnGameBeginning()
    {
        QuarterInsertSound.PlayRandomSound();
    }

    public void PlaceholderEvent() { }
}
