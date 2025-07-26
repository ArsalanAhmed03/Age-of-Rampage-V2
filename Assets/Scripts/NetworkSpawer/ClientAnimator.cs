using UnityEngine;
using Unity.Netcode.Components;

public class ClientAnimator : NetworkAnimator
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
