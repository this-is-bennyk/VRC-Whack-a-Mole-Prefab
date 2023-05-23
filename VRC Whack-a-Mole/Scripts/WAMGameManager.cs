
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class WAMGameManager : UdonSharpBehaviour
{
    [Header("Game Objects")]

    [Tooltip("The first mallet to use to play. Required since the game needs at least one.")]
    public Mallet RequiredMallet;
    [Tooltip("The second mallet to use to play. Optional, can be used for two handed play.")]
    public Mallet OptionalMallet;

    [Tooltip("The game configuration to use.")]
    public WAMConfig[] Configurations;

    [Tooltip("The display of the current score.")]
    public Text CurScoreText;
    [Tooltip("The display of the high score.")]
    public Text HighScoreText;
    [Tooltip("The display of the name of the player who got the high score.")]
    public Text HighScoreNameText;

    [Tooltip("The animator that plays an animation before the game starts and after the game ends.")]
    public Animator CabinetAnimator;

    [Tooltip("The button that starts the game.")]
    public Button PlayButton_UnityUI;
    [Tooltip("The button that starts the game.")]
    public EventEmitter PlayButton_EventEmitter;

    [Header("Game Variables")]

    [Tooltip("The amount of time in seconds to play for.")]
    public float PlayTime = 60f;

    [Tooltip("The index of the game configuration that is currently in use.")]
    public uint CurConfigurationIndex = 0;

    [Tooltip("The maximum number of moles that can pop up per round.")]
    public uint MaxMolesPerRound = 3;
    [Tooltip("The amount of points each mole should give for being hit.")]
    public uint PointsPerMole = 10;

    [Tooltip("The string to display when no one has achieved a high score yet.")]
    public string NoHighScorePlayerString = "---";

    [Header("Animation Trigger Names - You MUST have a trigger with the EXACT SAME name as the animation state it corresponds to!")]

    [Tooltip("The animation to play before starting the game.")]
    public string StartGameAnimationTrigger = "Start";
    [Tooltip("The animation to play after ending the game.")]
    public string EndGameAnimationTrigger = "End";

    // Pretty cringe of Unity to not be able to find the length of next states tbh

    [Header("Animation Clip Names")]
    [Tooltip("The name of the animation clip that plays before starting the game.")]
    public string StartGameAnimationClip = "Start";
    [Tooltip("The name of the animation clip that plays after ending the game.")]
    public string EndGameAnimationClip = "End";

    [Header("World Properties")]

    [Tooltip("The maximum number of people allowed in this world this prefab is in.")]
    public uint HardCap = 32;

    [UdonSynced]
    private uint currentScore = 0;

    [UdonSynced]
    private uint highScore = 0;
    [UdonSynced]
    private string highScoreName = "";

    [UdonSynced]
    private float currentTime = 0f;

    [UdonSynced]
    private uint numMolesInRound = 0;

    // Remark: This might mean that if one person's version of WAM is broken, everyone's is.
    // Could change in the future
    [UdonSynced]
    private bool initializedProperly = true;

    //[UdonSynced]
    //private int remotePlayerID = -1;
    [UdonSynced]
    private string remoteDisplayName = string.Empty;
    //private int localPlayerID;

    [UdonSynced]
    private string curAnimationSynced = "";
    [UdonSynced]
    private uint curAnimationTimesPlayedSynced = 0;

    private string curAnimationLocal = "";
    private uint curAnimationTimesPlayedLocal = 0;

    private VRCPlayerApi localPlayerInfo;

    private float startAnimTime = -1f;
    private float endAnimTime = -1f;

    void Start()
    {
        int playerCount = VRCPlayerApi.GetPlayerCount();

        bool shouldShutdown = false;

        localPlayerInfo = Networking.LocalPlayer;

        if (playerCount > 1)
        {
            OnDeserialization();

            AnimationClip[] clips = CabinetAnimator.runtimeAnimatorController.animationClips;

            foreach (AnimationClip clip in clips)
            {
                if (clip.name == StartGameAnimationClip)
                    startAnimTime = clip.length;
                else if (clip.name == EndGameAnimationClip)
                    endAnimTime = clip.length;
            }

            for (uint curConfig = 0; curConfig < Configurations.Length; ++curConfig)
            {
                var config = Configurations[curConfig];

                config.Initialize(this);

                if (curConfig == CurConfigurationIndex)
                    config.gameObject.SetActive(true);
                else
                    config.gameObject.SetActive(false);
            }
        }
        else
        {
            // So we can serialize the initial data
            Networking.SetOwner(Networking.LocalPlayer, gameObject);

            //localPlayerID = localPlayerInfo.playerId;
            //remotePlayerID = -1; // Ensure that we haven't started a game yet
            remoteDisplayName = string.Empty;

            highScoreName = NoHighScorePlayerString;

            curAnimationSynced = "";
            curAnimationLocal = "";

            curAnimationTimesPlayedSynced = 0;
            curAnimationTimesPlayedLocal = 0;

            UpdateCurrentScore(0);
            UpdateHighScore(0);
            HighScoreNameText.text = NoHighScorePlayerString;

            RequestSerialization();

            if (RequiredMallet == null)
            {
                Debug.LogError($"{name}: Need at least one mallet (in the RequiredMallet variable)!");
                shouldShutdown = true;
            }
            if (CurScoreText == null)
            {
                Debug.LogError($"{name}: No current score text found!");
                shouldShutdown = true;
            }
            if (HighScoreText == null)
            {
                Debug.LogError($"{name}: No high score text found!");
                shouldShutdown = true;
            }
            if (HighScoreNameText == null)
            {
                Debug.LogError($"{name}: No high score name text found!");
                shouldShutdown = true;
            }

            if (CabinetAnimator == null)
            {
                Debug.LogError($"{name}: No cabinet animator found!");
                shouldShutdown = true;
            }
            else
            {
                AnimationClip[] clips = CabinetAnimator.runtimeAnimatorController.animationClips;

                foreach (AnimationClip clip in clips)
                {
                    if (clip.name == StartGameAnimationClip)
                        startAnimTime = clip.length;
                    else if (clip.name == EndGameAnimationClip)
                        endAnimTime = clip.length;
                }

                if (startAnimTime == -1f)
                {
                    Debug.LogError($"{name}: No start game animation clip found!");
                    shouldShutdown = true;
                }
                if (endAnimTime == -1f)
                {
                    Debug.LogError($"{name}: No end game animation clip found!");
                    shouldShutdown = true;
                }
            }

            if (PlayButton_UnityUI == null && PlayButton_EventEmitter == null)
            {
                Debug.LogError($"{name}: No play button found!");
                shouldShutdown = true;
            }
            else if (PlayButton_UnityUI != null && PlayButton_EventEmitter != null)
            {
                Debug.LogError($"{name}: Only one play button allowed!");
                shouldShutdown = true;
            }

            if (Configurations == null || Configurations.Length == 0)
            {
                Debug.LogError($"{name}: No configurations found!");
                shouldShutdown = true;
            }

            for (uint curConfig = 0; curConfig < Configurations.Length; ++curConfig)
            {
                var config = Configurations[curConfig];

                if (!config.Initialize(this))
                    shouldShutdown = true;

                if (curConfig == CurConfigurationIndex)
                    config.gameObject.SetActive(true);
                else
                    config.gameObject.SetActive(false);
            }

            if (shouldShutdown)
                PrematureShutdown();
        }
    }

    void Update()
    {
        if (!initializedProperly || !LocalPlayerIsPlaying || currentTime <= 0f)
            return;

        currentTime -= Time.deltaTime;
        RequestSerialization();
    }

    public bool InitializedProperly { get { return initializedProperly; } }

    public bool AnyoneIsPlaying { get { return remoteDisplayName != string.Empty; } }

    public bool LocalPlayerIsPlaying { get { return remoteDisplayName == localPlayerInfo.displayName; } }

    //public Text leftText;

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        if (!LocalPlayerIsPlaying)
        {
            if (VRCPlayerApi.GetPlayerCount() == 0)
            {
                //remotePlayerID = -1;
                remoteDisplayName = string.Empty;
                currentTime = 0f;
                RequestSerialization();
            }
            else
            {
                VRCPlayerApi defaultPlayer = null;
                VRCPlayerApi[] players = new VRCPlayerApi[HardCap + 3]; // Hard cap + instance/world creator or group owner
                VRCPlayerApi.GetPlayers(players);

                foreach (VRCPlayerApi availablePlayer in players)
                {
                    if (availablePlayer != null)
                    {
                        if (defaultPlayer == null)
                            defaultPlayer = availablePlayer;

                        if (remoteDisplayName == availablePlayer.displayName)
                        {
                            //leftText.text = "Player found: " + availablePlayer.displayName;
                            // In the case we're testing locally, 2+ players can have the same display name
                            // In practice this shouldn't be possible (or so VRC claims)
                            if (!Networking.IsOwner(availablePlayer, gameObject))
                            {
                                Networking.SetOwner(availablePlayer, gameObject);
                                foreach (Mole mole in Configurations[CurConfigurationIndex].Moles)
                                    Networking.SetOwner(availablePlayer, mole.gameObject);

                                remoteDisplayName = availablePlayer.displayName;

                                RequestSerialization();
                            }
                            return;
                        }
                    }
                }

                //if (leftText != null)
                //    leftText.text = "Player left: " + remoteDisplayName;

                // If absolutely everyone has left, kill the game
                if (defaultPlayer == null)
                {
                    //remotePlayerID = -1;
                    remoteDisplayName = string.Empty;
                    currentTime = 0f;
                }
                else
                {
                    // If the player who was using the WAM machine left / got disconnected,
                    // use the default player we found and make them the current player
                    Networking.SetOwner(defaultPlayer, gameObject);
                    foreach (Mole mole in Configurations[CurConfigurationIndex].Moles)
                        Networking.SetOwner(defaultPlayer, mole.gameObject);

                    //remotePlayerID = defaultPlayer.playerId;
                    remoteDisplayName = defaultPlayer.displayName;

                    //currentTime = 0f;
                    //if (numMolesInRound == 0)
                    //    EndGame();
                }

                RequestSerialization();
            }
        }
        // This fucking sucks, why do the test apps not just have made up profiles
        // Why duplicate yourself
        // That will never happen and it only makes this code way more complicated than it needs to be
        // Anyways if we're in test mode and share the same name as the user who just left,
        // Overtake their position and continue playing
        else
        {
            if (Utilities.IsValid(Networking.LocalPlayer) && !Networking.IsOwner(Networking.LocalPlayer, gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
                foreach (Mole mole in Configurations[CurConfigurationIndex].Moles)
                    Networking.SetOwner(Networking.LocalPlayer, mole.gameObject);

                remoteDisplayName = Networking.LocalPlayer.displayName;

                RequestSerialization();
            }
        }
    }

    public bool IsMallet(GameObject gameObject)
    {
        return (RequiredMallet.GetPickup().IsHeld && gameObject == RequiredMallet.gameObject)
            || (OptionalMallet && RequiredMallet.GetPickup().IsHeld && gameObject == OptionalMallet.gameObject);
    }

    public uint GetCurrentScore() => currentScore;

    public void StartGame()
    {
        if (!InitializedProperly || AnyoneIsPlaying)
            return;

        Debug.Log($"{name}: Starting game");

        // Record that we've started the game by saying who's currently playing
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        foreach (Mole mole in Configurations[CurConfigurationIndex].Moles)
            Networking.SetOwner(Networking.LocalPlayer, mole.gameObject);

        //remotePlayerID = localPlayerID;
        remoteDisplayName = Networking.LocalPlayer.displayName;
        SetAnimation(StartGameAnimationTrigger);

        RequestSerialization();

        CabinetAnimator.SetTrigger(StartGameAnimationTrigger);

        SendCustomEventDelayedSeconds("StartNewRound", startAnimTime);
        SendCustomEventDelayedSeconds("StartGameTimer", startAnimTime);

        //PlayButton_UnityUI.interactable = false;
        SetPlayButton(false);
    }

    public void StartGameTimer()
    {
        if (!InitializedProperly || !LocalPlayerIsPlaying)
            return;

        currentTime = PlayTime;

        RequestSerialization();
    }

    public void StartNewRound()
    {
        if (!InitializedProperly || !LocalPlayerIsPlaying)
            return;

        WAMConfig config = Configurations[CurConfigurationIndex];
        uint maxNumMoles = Convert.ToUInt32(config.Moles.Length);

        float chosenNumMoles = UnityEngine.Random.Range(1, MaxMolesPerRound);
        float checkedNumMoles = Mathf.Min(chosenNumMoles, maxNumMoles);

        numMolesInRound = Convert.ToUInt32(Mathf.FloorToInt(checkedNumMoles));

        Debug.Log($"{name}: Spawning {numMolesInRound} moles");

        uint molePool = numMolesInRound;

        for (uint curMole = 0; curMole < maxNumMoles; ++curMole)
        {
            if (molePool > 0 && (maxNumMoles - curMole <= molePool || UnityEngine.Random.Range(0f, 1f) <= 0.5f))
            {
                config.Moles[curMole].PopUp();
                --molePool;
            }
        }

        RequestSerialization();
    }

    public void OnMoleHit()
    {
        if (!InitializedProperly || !LocalPlayerIsPlaying)
            return;

        UpdateCurrentScore(currentScore + PointsPerMole);

        if (currentScore > highScore)
            UpdateHighScore(currentScore);

        OnMoleDone();
    }

    public void OnMoleDone()
    {
        if (!InitializedProperly || !LocalPlayerIsPlaying)
            return;

        --numMolesInRound;
        RequestSerialization();

        Debug.Log($"{name}: Moles left: {numMolesInRound}");
    }

    public void OnMoleDown()
    {
        if (!InitializedProperly || !LocalPlayerIsPlaying)
            return;

        if (numMolesInRound == 0)
        {
            if (currentTime > 0f)
                StartNewRound();
            else if (curAnimationSynced != EndGameAnimationTrigger)
                EndGame();
        }
    }

    public void EndGame()
    {
        if (!InitializedProperly || !LocalPlayerIsPlaying)
            return;

        SetAnimation(EndGameAnimationTrigger);
        RequestSerialization();

        CabinetAnimator.SetTrigger(EndGameAnimationTrigger);
        SendCustomEventDelayedSeconds("Cleanup", endAnimTime);
    }

    public void Cleanup()
    {
        if (!InitializedProperly || !LocalPlayerIsPlaying)
            return;

        RequiredMallet.OnGameEnd();
        if (OptionalMallet)
            OptionalMallet.OnGameEnd();

        // Record that we've ended the game by saying that no one is playing
        //remotePlayerID = -1;
        remoteDisplayName = string.Empty;

        UpdateCurrentScore(0);
        //PlayButton_UnityUI.interactable = true;
        SetPlayButton(true);

        RequestSerialization();

        Debug.Log($"{name}: Ending game");
    }

    private void PrematureShutdown()
    {
        Debug.LogError($"{name}: Whack-a-Mole ran into an error. Try reloading, or report the error!");

        SetPlayButton(false);

        // So we can serialize the initial data
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        initializedProperly = false;

        RequestSerialization();
    }

    private void UpdateCurrentScore(uint newCurScore)
    {
        currentScore = newCurScore;
        CurScoreText.text = "" + currentScore;

        // Assumes that RequestSerialization is called after this
    }

    private void UpdateHighScore(uint newHighScore)
    {
        highScore = newHighScore;

        if (highScore > 0)
            highScoreName = Networking.GetOwner(gameObject).displayName;
        else
            highScoreName = NoHighScorePlayerString;

        HighScoreText.text = "" + highScore;
        HighScoreNameText.text = highScoreName;

        // Assumes that RequestSerialization is called after this
    }

    public override void OnDeserialization()
    {
        if (Networking.IsOwner(gameObject))
            return;

        CurScoreText.text = "" + currentScore;
        HighScoreText.text = "" + highScore;
        HighScoreNameText.text = highScoreName;

        if (curAnimationLocal != curAnimationSynced || curAnimationTimesPlayedLocal != curAnimationTimesPlayedSynced)
        {
            CabinetAnimator.SetTrigger(curAnimationSynced);

            curAnimationLocal = curAnimationSynced;
            curAnimationTimesPlayedLocal = curAnimationTimesPlayedSynced;
        }

        SetPlayButton(remoteDisplayName == string.Empty);
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

    public void SetPlayButton(bool interactive)
    {
        if (PlayButton_UnityUI != null)
            PlayButton_UnityUI.interactable = interactive;

        if (PlayButton_EventEmitter != null)
            PlayButton_EventEmitter.SetInteractions(interactive);
    }
}
