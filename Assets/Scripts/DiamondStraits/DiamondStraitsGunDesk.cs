using UnityEngine;

/// <summary>
/// 前線の「ガンデスク」。単押しで武器ロッカー(DiamondStraitsLoadoutMenu)を開く。
/// 長押しではなく単押しにしているのは、開くこと自体にリスクを持たせる必要がないため。
/// </summary>
public class DiamondStraitsGunDesk : MonoBehaviour, IFPSInteractable
{
    public string prompt = "武器ロッカーを開く";

    public string GetInteractionPrompt() => prompt;
    public float GetHoldDuration(UniversalFPSController player) => 0f;
    public bool CanInteract(UniversalFPSController player) => true;

    public void OnInteract(UniversalFPSController player)
    {
        DiamondStraitsLoadoutMenu menu = player.GetComponent<DiamondStraitsLoadoutMenu>();
        if (menu != null) menu.Open();
    }
}
