using UnityEngine;

/// <summary>
/// Controls the static world-tree centerpiece in Eden_Shared.unity.
/// Plays the falling-leaf particle system on start; tree is not interactable.
/// Set up the mesh and particle system in the Inspector — see whatNext.md.
/// </summary>
public class WorldTreeController : MonoBehaviour
{
    [SerializeField] private ParticleSystem fallingLeaves;

    private void Start()
    {
        fallingLeaves?.Play();
    }
}
