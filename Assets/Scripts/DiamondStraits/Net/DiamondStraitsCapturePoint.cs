using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 拠点(コンクエストポイント)。トリガー範囲に滞在する陣営を見て占領度を進め、
/// 結果を NetworkVariable で全員に配る。判定はサーバー(ホスト)だけが行う(技術仕様書 §6)。
///
/// 片方の陣営だけが範囲内にいる間だけ占領が進み、両陣営が同時にいる、または誰もいない間は
/// 競り合い/現状維持で止まる(BF系コンクエストの定石)。
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class DiamondStraitsCapturePoint : NetworkBehaviour
{
    public string pointName = "Point";
    [Tooltip("中立から完全占領までの秒数。")]
    public float captureSeconds = 12f;

    private readonly NetworkVariable<float> netOwnership = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>-1〜+1。-1=A完全占領、+1=B完全占領、0=中立。</summary>
    public float Ownership => netOwnership.Value;

    public DiamondStraitsFaction? OwningFaction =>
        netOwnership.Value <= -0.999f ? DiamondStraitsFaction.A :
        netOwnership.Value >= 0.999f ? DiamondStraitsFaction.B :
        (DiamondStraitsFaction?)null;

    private readonly HashSet<DiamondStraitsNetworkPlayer> occupantsA = new HashSet<DiamondStraitsNetworkPlayer>();
    private readonly HashSet<DiamondStraitsNetworkPlayer> occupantsB = new HashSet<DiamondStraitsNetworkPlayer>();

    void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        DiamondStraitsNetworkPlayer soldier = other.GetComponentInParent<DiamondStraitsNetworkPlayer>();
        if (soldier == null) return;

        (soldier.Faction == DiamondStraitsFaction.A ? occupantsA : occupantsB).Add(soldier);
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;

        DiamondStraitsNetworkPlayer soldier = other.GetComponentInParent<DiamondStraitsNetworkPlayer>();
        if (soldier == null) return;

        occupantsA.Remove(soldier);
        occupantsB.Remove(soldier);
    }

    void Update()
    {
        if (!IsServer) return;

        // 昏睡・切断などでコライダーが消えずに残る取りこぼしを掃除する。
        occupantsA.RemoveWhere(p => p == null);
        occupantsB.RemoveWhere(p => p == null);

        int countA = occupantsA.Count;
        int countB = occupantsB.Count;

        if (countA > 0 && countB == 0)
        {
            netOwnership.Value = Mathf.Clamp(netOwnership.Value - Time.deltaTime / captureSeconds, -1f, 1f);
        }
        else if (countB > 0 && countA == 0)
        {
            netOwnership.Value = Mathf.Clamp(netOwnership.Value + Time.deltaTime / captureSeconds, -1f, 1f);
        }
    }
}
