using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 2〜3人のホストクライアント対戦で、1人の兵士をネットワーク上に成立させる。
///
/// 移動は所有者権威。少人数の身内対戦でチート対策は要らないので、サーバーで移動を検証する
/// 複雑さを持ち込まず、各自が自分の兵士を動かして結果だけを配る。
///
/// 姿勢の同期だけは見た目の問題ではない。しゃがんだ相手が胸壁の陰に隠れられるかは
/// 当たり判定の高さで決まるので、CharacterController の高さまで合わせる必要がある。
/// </summary>
[RequireComponent(typeof(UniversalFPSController))]
[RequireComponent(typeof(NetworkObject))]
public class MereSoulsNetworkPlayer : NetworkBehaviour
{
    [Tooltip("他のプレイヤーから見える身体。一人称の自分の視界には映さない。")]
    public GameObject remoteBody;

    private UniversalFPSController controller;
    private FPSSoldierCondition condition;
    private FPSConditionOverlay overlay;
    private FPSInteractionSystem interaction;
    private FPSTelemetryAndDecals telemetry;
    private FPSProceduralAudio proceduralAudio;
    private CharacterController cc;
    private Camera playerCamera;
    private AudioListener listener;

    private readonly NetworkVariable<byte> netStance = new NetworkVariable<byte>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private readonly NetworkVariable<float> netHealth = new NetworkVariable<float>(
        100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    /// <summary>他人から見たこの兵士の体力。衛生兵が誰を担ぐか判断するのに使う。</summary>
    public float NetworkedHealth => netHealth.Value;

    void Awake()
    {
        controller = GetComponent<UniversalFPSController>();
        condition = GetComponent<FPSSoldierCondition>();
        overlay = GetComponent<FPSConditionOverlay>();
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

    /// <summary>
    /// 接続順に南北の塹壕へ振り分ける。偶数番が南、奇数番が北で、そのまま対戦が成立する。
    /// </summary>
    private void TeleportToSpawn()
    {
        bool south = (OwnerClientId % 2UL) == 0UL;
        string prefix = south ? "Spawn_TrenchLine_South" : "Spawn_TrenchLine_North";

        GameObject sector = GameObject.Find("TrenchSector");
        if (sector == null) return;

        var candidates = new System.Collections.Generic.List<Transform>();
        foreach (Transform t in sector.GetComponentsInChildren<Transform>())
        {
            if (t.name.StartsWith(prefix)) candidates.Add(t);
        }
        if (candidates.Count == 0) return;

        Transform target = candidates[(int)(OwnerClientId / 2UL) % candidates.Count];

        // CharacterController が有効なままだと transform への代入が打ち消される。
        bool wasEnabled = cc.enabled;
        cc.enabled = false;
        transform.SetPositionAndRotation(target.position, Quaternion.Euler(0f, south ? 0f : 180f, 0f));
        cc.enabled = wasEnabled;
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner) controller.OnBulletHit -= HandleOwnerBulletHit;
    }

    private void SetLocalSimulation(bool active)
    {
        controller.enabled = active;
        if (condition != null) condition.enabled = active;
        if (overlay != null) overlay.enabled = active;
        if (interaction != null) interaction.enabled = active;
        if (telemetry != null) telemetry.enabled = active;
        if (proceduralAudio != null) proceduralAudio.enabled = active;
        if (playerCamera != null) playerCamera.enabled = active;
        if (listener != null) listener.enabled = active;
    }

    void Update()
    {
        if (IsOwner)
        {
            netStance.Value = (byte)controller.CurrentStance;
            if (condition != null) netHealth.Value = condition.Health;
            return;
        }

        ApplyRemoteStance();
    }

    /// <summary>
    /// 遠隔の兵士の高さを合わせる。見た目だけでなく当たり判定も変えないと、
    /// しゃがんで胸壁に隠れたはずの相手が撃たれてしまう。
    /// </summary>
    private void ApplyRemoteStance()
    {
        float target = StanceHeight((StanceState)netStance.Value);
        cc.height = Mathf.Lerp(cc.height, target, Time.deltaTime * 14f);
        cc.center = new Vector3(0f, cc.height * 0.5f, 0f);

        if (remoteBody != null)
        {
            // Unityのカプセルは高さ2が既定なので、スケールは実寸の半分を入れる。
            remoteBody.transform.localPosition = new Vector3(0f, cc.height * 0.5f, 0f);
            remoteBody.transform.localScale = new Vector3(0.7f, cc.height * 0.5f, 0.7f);
        }
    }

    private static float StanceHeight(StanceState stance)
    {
        if (stance == StanceState.Crouch) return 1.15f;
        if (stance == StanceState.Prone) return 0.6f;
        return 1.8f;
    }

    private void HandleOwnerBulletHit(RaycastHit hit, int shotIndex, bool isTarget)
    {
        MereSoulsNetworkPlayer victim = hit.collider.GetComponentInParent<MereSoulsNetworkPlayer>();
        if (victim == null || victim == this) return;

        FPSWeaponData wp = controller.CurrentWeaponData;
        float impact = wp != null ? wp.hitImpact : 1.0f;
        Vector3 direction = (hit.point - transform.position).normalized;

        victim.ReportHitServerRpc(direction, impact);
    }

    /// <summary>
    /// 撃った側から被弾側へ通す。撃たれた本人のクライアントだけが自分の負傷を処理するので、
    /// サーバーは中継だけを行う。
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void ReportHitServerRpc(Vector3 direction, float impact)
    {
        ApplyHitClientRpc(direction, impact);
    }

    [ClientRpc]
    private void ApplyHitClientRpc(Vector3 direction, float impact)
    {
        if (!IsOwner) return;

        // 被弾の唯一の入口を通す。ここから恐怖・負傷・装填の中断がまとめて起きる。
        controller.ApplyDamageAimPunch(direction, impact);
    }
}
