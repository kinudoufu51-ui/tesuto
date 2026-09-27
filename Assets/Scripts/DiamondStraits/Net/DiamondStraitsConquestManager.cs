using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 陣営のチケット(戦力ゲージ)を管理する。占領している拠点は相手のチケットを削り、
/// 0になった陣営が敗北する。判定はサーバー(ホスト)だけで行い、結果は NetworkVariable で
/// 全員に配る(技術仕様書 §4, §6)。
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class DiamondStraitsConquestManager : NetworkBehaviour
{
    public float startingTickets = 200f;
    [Tooltip("1拠点を占領している陣営が1秒あたりに相手から削るチケット量。")]
    public float ticketDrainPerPointPerSecond = 1f;
    [Tooltip("拠点ごとのチケット削り効率倍率。capturePoints と同じ並び順で対応させる(企画書のナイトクラブ拠点=1.5倍など)。")]
    public float[] pointDrainMultipliers;

    public DiamondStraitsCapturePoint[] capturePoints;

    public readonly NetworkVariable<float> ticketsA = new NetworkVariable<float>(
        200f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<float> ticketsB = new NetworkVariable<float>(
        200f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<bool> matchOver = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<int> winningFaction = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        ticketsA.Value = startingTickets;
        ticketsB.Value = startingTickets;
        matchOver.Value = false;
        winningFaction.Value = -1;
    }

    void Update()
    {
        if (!IsServer || matchOver.Value || capturePoints == null) return;

        float drainA = 0f;
        float drainB = 0f;

        for (int i = 0; i < capturePoints.Length; i++)
        {
            DiamondStraitsCapturePoint point = capturePoints[i];
            if (point == null || point.OwningFaction == null) continue;

            float multiplier = (pointDrainMultipliers != null && i < pointDrainMultipliers.Length) ? pointDrainMultipliers[i] : 1f;
            float drain = ticketDrainPerPointPerSecond * multiplier * Time.deltaTime;

            // 占領している側が相手のチケットを削る。
            if (point.OwningFaction == DiamondStraitsFaction.A) drainB += drain;
            else drainA += drain;
        }

        if (drainA > 0f) ticketsA.Value = Mathf.Max(0f, ticketsA.Value - drainA);
        if (drainB > 0f) ticketsB.Value = Mathf.Max(0f, ticketsB.Value - drainB);

        if (ticketsA.Value <= 0f)
        {
            matchOver.Value = true;
            winningFaction.Value = (int)DiamondStraitsFaction.B;
        }
        else if (ticketsB.Value <= 0f)
        {
            matchOver.Value = true;
            winningFaction.Value = (int)DiamondStraitsFaction.A;
        }
    }
}
