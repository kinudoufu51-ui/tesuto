using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Diamond Straits の兵士をネットワーク上に成立させる。MereSoulsNetworkPlayer と同じ
/// 所有者権威の構造を踏襲しつつ、体力ではなく麻酔蓄積(Sedation)と昏睡状態を同期する。
///
/// 蘇生(看護兵/分隊)は「誰が起こすか」で結果が変わるため判定はローカルで行い、
/// 結果だけを対象のオーナーへ RPC で伝える(ReportHitServerRpc/ApplyHitClientRpc と対称的な構造)。
/// </summary>
[RequireComponent(typeof(UniversalFPSController))]
[RequireComponent(typeof(DiamondStraitsSoldierCondition))]
[RequireComponent(typeof(NetworkObject))]
public class DiamondStraitsNetworkPlayer : NetworkBehaviour
{
    [Tooltip("他のプレイヤーから見える身体。一人称の自分の視界には映さない。")]
    public GameObject remoteBody;

    private UniversalFPSController controller;
    private DiamondStraitsSoldierCondition condition;
    private DiamondStraitsRevivalController revivalController;
    private DiamondStraitsClassSelection classSelection;
    private FPSInteractionSystem interaction;
    private FPSTelemetryAndDecals telemetry;
    private FPSProceduralAudio proceduralAudio;
    private CharacterController cc;
    private Camera playerCamera;
    private AudioListener listener;

    private readonly NetworkVariable<byte> netStance = new NetworkVariable<byte>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private readonly NetworkVariable<float> netSedation = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private readonly NetworkVariable<bool> netIsAsleep = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private readonly NetworkVariable<float> netSedationDepthSeconds = new NetworkVariable<float>(
        16f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    /// <summary>接続順で機械的に決まる陣営。偶数番のクライアント(0,2,...)がA、奇数番がB。</summary>
    public DiamondStraitsFaction Faction => (OwnerClientId % 2UL) == 0UL ? DiamondStraitsFaction.A : DiamondStraitsFaction.B;

    void Awake()
    {
        controller = GetComponent<UniversalFPSController>();
        condition = GetComponent<DiamondStraitsSoldierCondition>();
        revivalController = GetComponent<DiamondStraitsRevivalController>();
        classSelection = GetComponent<DiamondStraitsClassSelection>();
        interaction = GetComponent<FPSInteractionSystem>();
        telemetry = GetComponent<FPSTelemetryAndDecals>();
        proceduralAudio = GetComponent<FPSProceduralAudio>();
        cc = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>(true);
        listener = GetComponentInChildren<AudioListener>(true);

        // 所有者が決まる前に一旦すべて止める。AudioListener が一瞬でも複数動くと警告が出るため、
        // OnNetworkSpawn を待たずにここで落としておく。
        SetLocalSimulation(false);
    }

    public override void OnNetworkSpawn()
    {
        SetLocalSimulation(IsOwner);

        if (remoteBody != null) remoteBody.SetActive(!IsOwner);

        if (IsOwner)
        {
            controller.OnBulletHit += HandleOwnerBulletHit;
            TeleportToSpawn();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner) controller.OnBulletHit -= HandleOwnerBulletHit;
    }

    private void SetLocalSimulation(bool active)
    {
        controller.enabled = active;
        if (condition != null) condition.enabled = active;
        if (revivalController != null) revivalController.enabled = active;
        if (classSelection != null) classSelection.enabled = active;
        if (interaction != null) interaction.enabled = active;
        if (telemetry != null) telemetry.enabled = active;
        if (proceduralAudio != null) proceduralAudio.enabled = active;
        if (playerCamera != null) playerCamera.enabled = active;
        if (listener != null) listener.enabled = active;
    }

    /// <summary>
    /// 拠点選択はまだ無いので、接続順にばらけた位置へ出す(コンクエストの本実装が入るまでの仮措置)。
    /// </summary>
    private void TeleportToSpawn()
    {
        Vector3 offset = new Vector3((OwnerClientId % 4UL) * 2f - 3f, 0f, -(float)(OwnerClientId / 4UL) * 2f);

        bool wasEnabled = cc.enabled;
        cc.enabled = false;
        transform.position += offset;
        cc.enabled = wasEnabled;
    }

    void Update()
    {
        if (IsOwner)
        {
            netStance.Value = (byte)controller.CurrentStance;
            if (condition != null)
            {
                netSedation.Value = condition.Sedation;
                netIsAsleep.Value = condition.IsAsleep;
                netSedationDepthSeconds.Value = condition.SedationDepthSeconds;
            }
            return;
        }

        ApplyRemoteStance();
        ApplyRemoteSleepPose();

        // 非所有者側にも本人の状態を反映する。Zzz表示や蘇生の可否判定は誰から見ても
        // 同じ答えを返す必要があるため、condition.enabled=false でも直接呼んで同期する。
        if (condition != null) condition.SyncFromNetwork(netSedation.Value, netIsAsleep.Value, netSedationDepthSeconds.Value);
    }

    /// <summary>
    /// 遠隔の兵士の高さを合わせる。見た目だけでなく当たり判定も変えないと、
    /// しゃがんで隠れたはずの相手が撃たれてしまう。
    /// </summary>
    private void ApplyRemoteStance()
    {
        float target = StanceHeight((StanceState)netStance.Value);
        cc.height = Mathf.Lerp(cc.height, target, Time.deltaTime * 14f);
        cc.center = new Vector3(0f, cc.height * 0.5f, 0f);

        if (remoteBody != null)
        {
            remoteBody.transform.localPosition = new Vector3(0f, cc.height * 0.5f, 0f);
            remoteBody.transform.localScale = new Vector3(0.7f, cc.height * 0.5f, 0.7f);
        }
    }

    /// <summary>昏睡中の相手は横倒しにする。数値を出さないこの企画では姿勢そのものが唯一の合図。</summary>
    private void ApplyRemoteSleepPose()
    {
        if (remoteBody == null) return;

        float targetRoll = netIsAsleep.Value ? 90f : 0f;
        Quaternion targetRot = Quaternion.Euler(0f, 0f, targetRoll);
        remoteBody.transform.localRotation = Quaternion.Slerp(remoteBody.transform.localRotation, targetRot, Time.deltaTime * 6f);
    }

    private static float StanceHeight(StanceState stance)
    {
        if (stance == StanceState.Crouch) return 1.15f;
        if (stance == StanceState.Prone) return 0.6f;
        return 1.8f;
    }

    private void HandleOwnerBulletHit(RaycastHit hit, int shotIndex, bool isTarget)
    {
        DiamondStraitsNetworkPlayer victim = hit.collider.GetComponentInParent<DiamondStraitsNetworkPlayer>();
        if (victim == null || victim == this) return;

        FPSWeaponData wp = controller.CurrentWeaponData;
        float amount = wp != null ? wp.sedationPerHit : 25f;
        float lockSeconds = wp != null ? wp.forcedRespawnLockSeconds : 16f;

        victim.ReportHitServerRpc(amount, lockSeconds);
    }

    /// <summary>
    /// 撃った側から被弾側へ通す。撃たれた本人のクライアントだけが自分の麻酔を処理するので、
    /// サーバーは中継だけを行う。
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void ReportHitServerRpc(float amount, float lockSeconds)
    {
        ApplyHitClientRpc(amount, lockSeconds);
    }

    [ClientRpc]
    private void ApplyHitClientRpc(float amount, float lockSeconds)
    {
        if (!IsOwner) return;
        condition.ApplySedation(amount, lockSeconds);
    }

    /// <summary>
    /// 看護兵/分隊蘇生の結果を本人のオーナーへ伝える。DiamondStraitsRevive から呼ばれる。
    /// 「誰が起こすか」で決まる残留麻酔量は呼び出し側(ローカル)で計算済みのものを渡すだけ。
    /// </summary>
    public void RequestRevive(float residualSedation)
    {
        ReviveServerRpc(residualSedation);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReviveServerRpc(float residualSedation)
    {
        ReviveClientRpc(residualSedation);
    }

    [ClientRpc]
    private void ReviveClientRpc(float residualSedation)
    {
        if (!IsOwner) return;
        condition.Revive(residualSedation);
    }

    /// <summary>
    /// 被弾以外の経路(麻酔ガス散布砲など)から麻酔を与えたいときの入口。
    /// ReportHitServerRpc と同じ経路をそのまま使い回す。
    /// </summary>
    public void RequestSedation(float amount, float lockSeconds)
    {
        ReportHitServerRpc(amount, lockSeconds);
    }

    /// <summary>看護兵の興奮剤注射器から、味方本人へバフを届ける入口。</summary>
    public void RequestStimulant(float speedMultiplier, float reloadSpeedMultiplier, float duration)
    {
        StimulantServerRpc(speedMultiplier, reloadSpeedMultiplier, duration);
    }

    [ServerRpc(RequireOwnership = false)]
    private void StimulantServerRpc(float speedMultiplier, float reloadSpeedMultiplier, float duration)
    {
        StimulantClientRpc(speedMultiplier, reloadSpeedMultiplier, duration);
    }

    [ClientRpc]
    private void StimulantClientRpc(float speedMultiplier, float reloadSpeedMultiplier, float duration)
    {
        if (!IsOwner) return;
        condition.ApplyStimulant(speedMultiplier, reloadSpeedMultiplier, duration);
    }
}
