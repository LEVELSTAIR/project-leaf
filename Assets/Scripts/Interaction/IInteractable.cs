using UnityEngine;

public interface IInteractable
{
    string InteractionPrompt { get; }
    void Highlight(bool active);
    void Interact();
}
