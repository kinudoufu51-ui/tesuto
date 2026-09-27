using UnityEngine;

/// <summary>
/// 豆戦車ガンナーの麻酔ガス散布砲が着弾点に発生させる持続効果範囲(企画書 §4/§5)。
/// 「範囲内の敵に持続的な眠気蓄積を与える」の実装で、一撃で眠らせる主武器とは違い、
/// 範囲内に留まり続けるほど麻酔が積み上がる。
///
/// 素体にもDiamondStraitsSoldierConditionにも依存を強制しない: ネットワーク越しの相手が
/// 見つかればそちら経由(サーバーへ中継)、無ければローカルの条件コンポーネントへ直接適用する。
/// </summary>
public class MiniTankGasCloud : MonoBehaviour
{
    public float radius = 4f;
    public float duration = 5f;
    public float sedationPerSecond = 8f;
    [Tooltip("この霧で眠った場合の強制リスポーン待ち秒数。主武器より軽めに設定する想定。")]
    public float lockSeconds = 12f;

    private const float TickInterval = 0.5f;
    private float elapsed;
    private float tickTimer;

    void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= duration)
        {
            Destroy(gameObject);
            return;
        }

        tickTimer += Time.deltaTime;
        if (tickTimer < TickInterval) return;
        tickTimer = 0f;

        float amount = sedationPerSecond * TickInterval;
        foreach (Collider hit in Physics.OverlapSphere(transform.position, radius))
        {
            ApplyTo(hit, amount);
        }
    }

    private void ApplyTo(Collider hit, float amount)
    {
        DiamondStraitsNetworkPlayer netTarget = hit.GetComponentInParent<DiamondStraitsNetworkPlayer>();
        if (netTarget != null)
        {
            netTarget.RequestSedation(amount, lockSeconds);
            return;
        }

        DiamondStraitsSoldierCondition condition = hit.GetComponentInParent<DiamondStraitsSoldierCondition>();
        if (condition != null && !condition.IsAsleep)
        {
            condition.ApplySedation(amount, lockSeconds);
        }
    }
}
