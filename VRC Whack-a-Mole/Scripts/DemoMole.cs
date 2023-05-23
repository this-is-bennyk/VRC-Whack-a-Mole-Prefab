
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DemoMole : Mole
{
    [Header("Additional Stuff")]

    public ParticleSystem HitParticles;

    public override void OnMoleHit()
    {
        base.OnMoleHit();
        HitParticles.Play();
    }
}
