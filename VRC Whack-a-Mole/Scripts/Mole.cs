
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Animator))]
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class Mole : UdonSharpBehaviour
{
    public const float ExpectedDirection = 1.0f;
    public const float ExpectedDirectionError = 0.3f;

    [Header("Animation Trigger Names - You MUST have a trigger with the EXACT SAME name as the animation state it corresponds to!")]

    [Tooltip("The name of the trigger that makes the mole do its default motion (should be pop up, wait, and back down).")]
    public string MoveAnimationTrigger = "Move";
    [Tooltip("The name of the trigger that makes the mole back down prematurely after being hit.")]
    public string HitAnimationTrigger = "Hit";

    [Header("Sounds")]
    public AudioRandomizer[] MoveSoundLayers;
    public AudioRandomizer[] HitSoundLayers;

    // The game manager that parents this mole
    // Remark: Yes this creates coupling issues. No I'm not fixing them bc U# is too limiting lol
    private WAMGameManager gameManager;

    private Animator animator;

    [Header("Game Properties")]

    [Tooltip("The impulse the player must exert to hit the mole's top.")]
    [UdonSynced]
    public float MinimumImpulseY = 2.0f;
    [Tooltip("The maximum range (+/-) of the impulse the player is allowed to exert on the X-axis.")]
    [UdonSynced]
    public float MaximumAbsImpulseX = 20.0f;
    [Tooltip("The maximum range (+/-) of the impulse the player is allowed to exert on the Z-axis.")]
    [UdonSynced]
    public float MaximumAbsImpulseZ = 20.0f;

    [UdonSynced]
    private bool inPlay = false;
    [UdonSynced]
    private string curAnimationSynced = "";
    [UdonSynced]
    private uint curAnimationTimesPlayedSynced = 0;

    private string curAnimationLocal = "";
    private uint curAnimationTimesPlayedLocal = 0;

    void Start()
    {
        animator = GetComponent<Animator>();

        inPlay = false;
        curAnimationSynced = "";
        curAnimationLocal = "";

        curAnimationTimesPlayedSynced = 0;
        curAnimationTimesPlayedLocal = 0;

        RequestSerialization();
    }

    public void Initialize(WAMGameManager manager)
    {
        gameManager = manager;
    }

    public void PopUp()
    {
        if (!(gameManager.InitializedProperly && gameManager.LocalPlayerIsPlaying))
            return;

        inPlay = true;
        SetAnimation(MoveAnimationTrigger);
        RequestSerialization();

        animator.SetTrigger(MoveAnimationTrigger);
    }

    public void OnCollisionEnter(Collision collision)
    {
        float relativeDirection = Vector3.Dot(collision.GetContact(0).normal, Vector3.down);
        Vector3 impulse = collision.impulse;

        if (!(gameManager.InitializedProperly
            && gameManager.LocalPlayerIsPlaying
            && inPlay
            && gameManager.IsMallet(collision.gameObject)
            && (relativeDirection >= ExpectedDirection - ExpectedDirectionError)
            && (relativeDirection <= ExpectedDirection + ExpectedDirectionError)
            && impulse.y >= Mathf.Abs(MinimumImpulseY) // + equals downward direction, so force the min. impulse y to be positive
            && Mathf.Abs(impulse.x) <= Mathf.Abs(MaximumAbsImpulseX)
            && Mathf.Abs(impulse.z) <= Mathf.Abs(MaximumAbsImpulseZ)
            ))
            return;

        Debug.Log($"{name}: Mole hit!");
        Debug.Log($"{name}: With impulse: {collision.impulse}");

        // Some code for sending a message to the game + getting hit
        inPlay = false;
        SetAnimation(HitAnimationTrigger);
        RequestSerialization();

        animator.SetTrigger(HitAnimationTrigger);
        gameManager.OnMoleHit();
    }

    public void OnAboutToBeDone()
    {
        if (!(gameManager.InitializedProperly && gameManager.LocalPlayerIsPlaying && inPlay))
            return;

        Debug.Log($"{name}: Mole miss!");

        inPlay = false;
        RequestSerialization();

        gameManager.OnMoleDone();
    }

    public void OnMoleFinished()
    {
        if (!(gameManager.InitializedProperly && gameManager.LocalPlayerIsPlaying))
            return;

        Debug.Log($"{name}: Mole exit!");

        gameManager.OnMoleDown();
    }

    public override void OnDeserialization()
    {
        if (Networking.IsOwner(gameObject))
            return;

        if (curAnimationLocal != curAnimationSynced || curAnimationTimesPlayedSynced != curAnimationTimesPlayedLocal)
        {
            animator.SetTrigger(curAnimationSynced);

            curAnimationLocal = curAnimationSynced;
            curAnimationTimesPlayedLocal = curAnimationTimesPlayedSynced;
        }
    }

    public void SetAnimation(string newValue)
    {
        if (curAnimationSynced == newValue)
            ++curAnimationTimesPlayedSynced;
        else
        {
            curAnimationSynced = newValue;
            curAnimationTimesPlayedSynced = 0;
        }
    }

    public void OnMoleMove()
    {
        foreach (AudioRandomizer layer in MoveSoundLayers)
            layer.PlayRandomSound();
    }

    public virtual void OnMoleHit()
    {
        foreach (AudioRandomizer layer in MoveSoundLayers)
            layer.StopSound();

        foreach (AudioRandomizer layer in HitSoundLayers)
            layer.PlayRandomSound();
    }
}
