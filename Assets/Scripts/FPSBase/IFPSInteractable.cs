public interface IFPSInteractable
{
    string GetInteractionPrompt();
    float GetHoldDuration();
    bool CanInteract(UniversalFPSController player);
    void OnInteract(UniversalFPSController player);
}
