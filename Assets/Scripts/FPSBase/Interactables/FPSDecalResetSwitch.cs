using UnityEngine;

/// <summary>
/// 検証ジム ゾーンD用: 単押しで弾痕を一括リセットするスイッチ(IFPSInteractable)。
/// </summary>
public class FPSDecalResetSwitch : MonoBehaviour, IFPSInteractable
{
    public string GetInteractionPrompt() => "弾痕をリセット";
    public float GetHoldDuration() => 0f;
    public bool CanInteract(UniversalFPSController player) => true;

    public void OnInteract(UniversalFPSController player)
    {
        FPSTelemetryAndDecals telemetry = player.GetComponent<FPSTelemetryAndDecals>();
        if (telemetry != null) telemetry.ClearAllDecals();
    }
}
