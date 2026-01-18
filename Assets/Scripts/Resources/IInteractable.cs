namespace ProjectLeaf.Interfaces
{
    public interface IInteractable
    {
        string InteractionPrompt { get; }
        void Interact();
    }
}
