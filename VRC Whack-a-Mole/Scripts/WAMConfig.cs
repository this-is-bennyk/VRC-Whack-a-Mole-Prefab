
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class WAMConfig : UdonSharpBehaviour
{
    public GameObject Faceplate;
    public Mole[] Moles;

    public bool Initialize(WAMGameManager gameManager)
    {
        // Remark: What the fuck is wrong with you unity
        // {gameObject}.transform.parent.gameObject? Seriously?
        // That's how i'm supposed to get the parent of an object?

        // Sanity check that the faceplate is one of our children
        if (Faceplate != null && Faceplate.transform.parent.gameObject != gameObject)
        {
            Debug.LogError($"{name}: Given faceplate is not a child of this configuration!");
            return false;
        }

        // Sanity check that we have moles
        if (Moles.Length == 0)
        {
            Debug.LogError($"{name}: No moles in this configuration!");
            return false;
        }

        // Sanity check that all the moles are our children and initialize them
        for (uint curMole = 0; curMole < Moles.Length; ++curMole)
        {
            Mole mole = Moles[curMole];

            if (mole == null || mole.transform.parent.gameObject != gameObject)
            {
                Debug.LogError($"{name}: Given mole is not a child of this configuration!");
                return false;
            }

            mole.Initialize(gameManager);
        }

        return true;
    }
}
