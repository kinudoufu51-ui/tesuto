using UnityEngine;

/// <summary>
/// Diamond Straits: Skirmish の中核。麻酔蓄積(Sedation, 0-100)と昏睡状態(IsAsleep)だけを持つ。
///
/// MERE SOULS の FPSSoldierCondition (恐怖・疲労・負傷) とは完全に独立しており、互いを参照しない。
/// 「死亡」はこのゲームに存在せず、100%に達すると昏睡するだけで、看護兵・分隊・自然経過のいずれかで
/// 必ず戦線へ戻れる (技術仕様書 §1-2, §2 参照)。
///
/// UniversalFPSController を持つプレイヤーにも、持たない静止したテストNPCにも同じものを使う。
/// コントローラーが無ければ素体への書き込みは単に skip する。
/// </summary>
public class DiamondStraitsSoldierCondition : MonoBehaviour
{
    [Tooltip("controller.OnDamageTaken の intensity=1.0 のヒット(既存Hキーデバッグ等)が与える麻酔蓄積量。" +
             "実際のPvPでは武器ごとの sedationPerHit を DiamondStraitsLocalHitRouter 経由で使う。")]
    public float sedationPerFullIntensityHit = 34f;

    public float Sedation { get; private set; }
    public bool IsAsleep { get; private set; }

    /// <summary>
    /// 昏睡に落ちた瞬間の武器の forcedRespawnLockSeconds。分隊蘇生の所要時間や
    /// 自然リスポーン・自力覚醒の待ち時間はすべてこの値から計算する(企画書 §3-2)。
    /// </summary>
    public float SedationDepthSeconds { get; private set; } = 16f;

    private float pendingLockSeconds = 16f;

    /// <summary>昏睡に入った瞬間の通知。Zzz演出や画面効果、リスポーンタイマー起動の起点になる。</summary>
    public System.Action OnEnterSleep;
    /// <summary>蘇生された瞬間の通知。</summary>
    public System.Action OnRevived;

    // --- 効果。コントローラーを持つ場合のみ意味を持つ ---

    /// <summary>照準のブレ倍率。麻酔が回るほど狙いが定まらなくなる。</summary>
    public float AimSwayMultiplier => 1f + (Sedation / 100f) * 1.2f;

    /// <summary>移動速度の倍率。ふらつく脚で全力疾走はできない。</summary>
    public float MoveSpeedMultiplier => Mathf.Clamp(1f - (Sedation / 100f) * 0.5f, 0.4f, 1f);

    /// <summary>手の震え。上限を設けないと満量近くで銃口が暴れて何も狙えなくなる。</summary>
    public float HandTremor => Mathf.Min(1.2f, Mathf.Pow(Sedation / 100f, 2f) * 1.1f);

    /// <summary>あくび・呼吸演出のトリガーに転用する値。素体の息切れ表現をそのまま使う。</summary>
    public float Breathlessness => Mathf.Clamp01((Sedation / 100f) - 0.4f);

    private UniversalFPSController controller;

    void Awake()
    {
        controller = GetComponent<UniversalFPSController>();
    }

    void OnEnable()
    {
        if (controller != null) controller.OnDamageTaken += HandleDamageTaken;
    }

    void OnDisable()
    {
        if (controller != null) controller.OnDamageTaken -= HandleDamageTaken;
    }

    void Update()
    {
        if (controller != null) PushToController();
    }

    /// <summary>
    /// 素体は麻酔の意味を知らないので、効いた結果だけを毎フレーム渡す。
    /// FPSSoldierCondition の PushToController と同じ約束事に従う。
    /// </summary>
    private void PushToController()
    {
        controller.conditionSwayMultiplier = AimSwayMultiplier;
        controller.conditionSpeedMultiplier = MoveSpeedMultiplier;
        controller.conditionTremor = HandTremor;
        controller.conditionBreathlessness = Breathlessness;
    }

    private void HandleDamageTaken(Vector3 incomingDir, float intensity)
    {
        ApplySedation(intensity * sedationPerFullIntensityHit);
    }

    /// <summary>麻酔を積む。武器の sedationPerHit、またはデバッグ換算値から呼ばれる。</summary>
    public void ApplySedation(float amount, float lockSeconds = 16f)
    {
        if (IsAsleep || amount <= 0f) return;

        Sedation = Mathf.Min(100f, Sedation + amount);
        pendingLockSeconds = lockSeconds;
        if (Sedation >= 100f) EnterSleep();
    }

    private void EnterSleep()
    {
        IsAsleep = true;
        SedationDepthSeconds = pendingLockSeconds;
        if (controller != null) controller.enabled = false;
        OnEnterSleep?.Invoke();
    }

    /// <summary>
    /// 看護兵の蘇生・分隊蘇生・自然リスポーンのいずれかから呼ばれる。
    /// 復帰後に残す麻酔量はルートごとに違う(看護兵=0、分隊=50、自然リスポーン=0で新規スポーン)。
    /// </summary>
    public void Revive(float sedationAfterRevive)
    {
        if (!IsAsleep) return;

        IsAsleep = false;
        Sedation = Mathf.Clamp(sedationAfterRevive, 0f, 99f);
        if (controller != null) controller.enabled = true;
        OnRevived?.Invoke();
    }
}
