using UnityEngine;

/// <summary>
/// 看護兵の興奮剤注射器(企画書 §4)。狙った先の味方に、移動・装填速度のバフを一定時間与える。
/// ネットワーク越しの相手ならオーナー本人へ RPC で届け、ローカルの検証ダミーには直接適用する
/// (麻酔ガス散布砲・麻酔グレネードと同じ振り分け方)。
/// </summary>
public class StimulantInjectorGadget : MonoBehaviour, IDiamondStraitsGadget
{
    public string GadgetName => "興奮剤注射器";
    public float CooldownSeconds => cooldownSeconds;

    public float cooldownSeconds = 20f;
    public float range = 3f;
    [Tooltip("移動・装填速度が何倍になるか。1.25なら25%速くなる。")]
    public float speedMultiplier = 1.25f;
    public float reloadSpeedMultiplier = 1.25f;
    public float buffDuration = 15f;

    public void Use(UniversalFPSController player)
    {
        Camera cam = player.mainCamera;
        if (cam == null) return;

        if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, range)) return;

        DiamondStraitsSoldierCondition target = hit.collider.GetComponentInParent<DiamondStraitsSoldierCondition>();
        if (target == null || target.IsAsleep) return;

        DiamondStraitsNetworkPlayer netTarget = hit.collider.GetComponentInParent<DiamondStraitsNetworkPlayer>();
        if (netTarget != null)
        {
            netTarget.RequestStimulant(speedMultiplier, reloadSpeedMultiplier, buffDuration);
        }
        else
        {
            target.ApplyStimulant(speedMultiplier, reloadSpeedMultiplier, buffDuration);
        }
    }
}
