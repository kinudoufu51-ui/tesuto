using UnityEngine;

/// <summary>
/// 同一マシン内で、撃った側の武器命中を相手の麻酔蓄積へ橋渡しする。
///
/// ネットワーク越しの中継 (MereSoulsNetworkPlayer.ReportHitServerRpc 相当) は
/// フェーズ3のマルチプレイで必要になるが、フェーズ1のシングルプレイ検証
/// (FPSMetricGym でテストNPCへ実際に武器を撃つ) にはこれで十分。
/// </summary>
[RequireComponent(typeof(UniversalFPSController))]
public class DiamondStraitsLocalHitRouter : MonoBehaviour
{
    private UniversalFPSController controller;

    void Awake()
    {
        controller = GetComponent<UniversalFPSController>();
    }

    void OnEnable()
    {
        controller.OnBulletHit += HandleBulletHit;
    }

    void OnDisable()
    {
        controller.OnBulletHit -= HandleBulletHit;
    }

    private void HandleBulletHit(RaycastHit hit, int shotIndex, bool isTarget)
    {
        DiamondStraitsSoldierCondition victim = hit.collider.GetComponentInParent<DiamondStraitsSoldierCondition>();
        if (victim == null) return;

        FPSWeaponData wp = controller.CurrentWeaponData;
        float amount = wp != null ? wp.sedationPerHit : 25f;
        float lockSeconds = wp != null ? wp.forcedRespawnLockSeconds : 16f;
        victim.ApplySedation(amount, lockSeconds);
    }
}
