using UnityEngine;

/// <summary>
/// 援護兵の簡易バリケード(企画書 §4)。高さ0.90mは素体の3段階自動段差乗り越えのうち
/// 「腰高のカウンター(0.85〜0.95m)」に合わせてあり、前進ジャンプで飛び越えられる遮蔽物として
/// 機能する(FPSWeaponData/UniversalFPSController の Vault 判定にそのまま乗る、追加コードは不要)。
/// </summary>
public class BarricadeGadget : MonoBehaviour, IDiamondStraitsGadget
{
    public string GadgetName => "簡易バリケード";
    public float CooldownSeconds => cooldownSeconds;

    public float cooldownSeconds = 20f;
    public float placeDistance = 2f;
    public Vector3 barricadeSize = new Vector3(1.6f, 0.9f, 0.3f);
    public float lifetimeSeconds = 60f;

    public void Use(UniversalFPSController player)
    {
        Camera cam = player.mainCamera;
        if (cam == null) return;

        Vector3 flatForward = cam.transform.forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude < 0.001f) flatForward = player.transform.forward;
        flatForward.Normalize();

        Vector3 position = player.transform.position + flatForward * placeDistance;
        position.y += barricadeSize.y * 0.5f;

        GameObject barricade = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barricade.name = "Barricade";
        barricade.transform.position = position;
        barricade.transform.rotation = Quaternion.LookRotation(flatForward);
        barricade.transform.localScale = barricadeSize;

        Object.Destroy(barricade, lifetimeSeconds);
    }
}
