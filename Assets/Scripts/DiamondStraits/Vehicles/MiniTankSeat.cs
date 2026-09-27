using UnityEngine;

/// <summary>
/// 豆戦車の乗降口。座席1つにつき1つ、車体の外側(ドアの位置)に当たり判定ごと配置する。
/// 実際に視点が移る先(車内の座席位置)は MiniTankController.driverSeat/gunnerSeat が持つので、
/// このコンポーネント自身は「どちらの席の乗降口か」を示すだけでよい。
/// </summary>
public class MiniTankSeat : MonoBehaviour, IFPSInteractable
{
    public MiniTankController vehicle;
    public bool isDriverSeat = true;

    public string GetInteractionPrompt()
    {
        bool occupied = isDriverSeat ? vehicle.HasDriver : vehicle.HasGunner;
        if (occupied) return "";
        return isDriverSeat ? "操縦席に乗る" : "ガンナー席に乗る";
    }

    public float GetHoldDuration(UniversalFPSController player) => 0f;

    public bool CanInteract(UniversalFPSController player) =>
        isDriverSeat ? !vehicle.HasDriver : !vehicle.HasGunner;

    public void OnInteract(UniversalFPSController player)
    {
        vehicle.Enter(player, isDriverSeat);
    }
}
