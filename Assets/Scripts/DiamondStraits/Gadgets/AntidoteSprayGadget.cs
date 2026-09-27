using UnityEngine;

/// <summary>
/// 看護兵の解毒スプレー(企画書 §4)。自分を中心とした範囲内で眠っている味方を、
/// 看護兵の蘇生(麻酔0%の万全復帰)と同じ結果でまとめて起こす「範囲一斉覚醒」。
///
/// DiamondStraitsRevive の E長押し1.2秒蘇生とは別経路だが、結果(Revive(0f))は完全に同じにして
/// 「看護兵が起こすと万全復帰」という一貫性を保つ。
/// </summary>
public class AntidoteSprayGadget : MonoBehaviour, IDiamondStraitsGadget
{
    public string GadgetName => "解毒スプレー";
    public float CooldownSeconds => cooldownSeconds;

    public float cooldownSeconds = 30f;
    public float radius = 5f;

    public void Use(UniversalFPSController player)
    {
        foreach (Collider hit in Physics.OverlapSphere(player.transform.position, radius))
        {
            DiamondStraitsSoldierCondition condition = hit.GetComponentInParent<DiamondStraitsSoldierCondition>();
            if (condition == null || !condition.IsAsleep) continue;

            DiamondStraitsNetworkPlayer netTarget = hit.GetComponentInParent<DiamondStraitsNetworkPlayer>();
            if (netTarget != null)
            {
                netTarget.RequestRevive(0f);
            }
            else
            {
                condition.Revive(0f);
            }
        }
    }
}
