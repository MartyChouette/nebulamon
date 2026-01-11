using UnityEngine;

namespace Nebula
{
    public interface IInteractable
    {
        void Interact(GameObject interactor);
        string GetPrompt();
        Transform GetTransform();
    }
}
