public interface IFPSInteractable
{
    string GetInteractionPrompt();
    /// <summary>長押し時間。誰が操作しているかで変わる場合があるので呼び出し元を渡す(分隊蘇生など)。</summary>
    float GetHoldDuration(UniversalFPSController player);
    bool CanInteract(UniversalFPSController player);
    void OnInteract(UniversalFPSController player);
}
