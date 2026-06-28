using UnityEngine;

public class HandColliderForwarder : MonoBehaviour
{
    private EvilTree parentTree;

    private void Start()
    {
        parentTree = GetComponentInParent<EvilTree>();
        if (parentTree == null)
            Debug.LogError($"HandColliderForwarder on {gameObject.name}: No EvilTree found!");
        else
            Debug.Log($"HandColliderForwarder found parent: {parentTree.name}");
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"HandColliderForwarder: {gameObject.name} collided with {collision.gameObject.name}");
        if (parentTree != null)
            parentTree.OnHandCollisionEnter(collision);
        else
            Debug.LogWarning("HandColliderForwarder: parentTree is null!");
    }
}