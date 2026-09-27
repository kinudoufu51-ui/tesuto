using UnityEngine;

/// <summary>
/// MERE SOULS の中核システム。恐怖・疲労・負傷の3つの状態を持ち、
/// それを数値ではなく「手が震える / 息が上がる / 視野が狭まる」という動作と視界に変換する。
///
/// 設計上の約束: このコンポーネントは数値UIを一切描画しない。
/// プレイヤーが自分の状態を知る手段は、銃がぶれること・呼吸音・視界の狭まりだけ。
/// (デバッグHUDは開発用の別物として FPSTelemetryAndDecals 側に置く)
///
/// プレイヤーも敵も同じこのコンポーネントを持つ前提で組んである。
/// 「等しい命」というコンセプトは、両者が同じ劣化ルールで動くことで初めて成立する。
/// </summary>
[RequireComponent(typeof(UniversalFPSController))]
public class FPSSoldierCondition : MonoBehaviour
{
    [Header("恐怖 — 砲撃・仲間の死・至近弾で跳ね上がる")]
    public float fearDecayPerSecond = 0.06f;
    public float fearCalmDelay = 3.0f;
    [Tooltip("これを超える急上昇で一瞬硬直する。PvPで操作を完全に奪わないよう、入力を鈍らせるだけに留める。")]
    public float freezeSpikeThreshold = 0.22f;
    public float freezeDuration = 0.5f;

    [Header("疲労 — 走ると溜まり、止まると抜ける")]
    public float sprintFatiguePerSecond = 0.10f;
    public float walkRecoveryPerSecond = 0.08f;
    public float restRecoveryPerSecond = 0.16f;

    [Header("負傷 — 被弾で入り、手当てするまで抜けない")]
    public float maxHealth = 100f;
    public float bleedPerSecond = 2.5f;

    [Header("寒さ — 天候システムから注入される。手がかじかんで装填が遅れる")]
    [Range(0f, 1f)] public float coldness = 0f;

    // --- 状態 (0〜1) ---
    public float Fear { get; private set; }
    public float Fatigue { get; private set; }
    public float Wounded { get; private set; }
    public float Health { get; private set; }
    public bool IsAlive => Health > 0f;

    // --- 効果。コントローラ側はこれだけを読む ---

    /// <summary>
    /// 手の震えの振幅。恐怖が支配的で、負傷と寒さが上乗せされる。
    /// 上限を設けないと三つ揃ったときに銃口が暴れて何も狙えなくなるので頭打ちにする。
    /// </summary>
    public float HandTremor => Mathf.Min(1.2f, Fear * Fear * 1.0f + Wounded * 0.4f + coldness * 0.25f);

    /// <summary>照準のブレ倍率。疲労で息が上がるほど、恐怖で落ち着かないほど大きくなる。</summary>
    public float AimSwayMultiplier => 1f + Fatigue * 1.5f + Fear * 0.8f;

    /// <summary>移動速度の倍率。疲労で前に出られなくなり、負傷で引きずる。</summary>
    public float MoveSpeedMultiplier =>
        Mathf.Clamp((1f - Fatigue * 0.35f) * (1f - Wounded * 0.4f) * (isFrozen ? 0.35f : 1f), 0.25f, 1f);

    /// <summary>視野狭窄の強さ。恐怖で周辺が見えなくなる。</summary>
    public float TunnelVision => Mathf.Clamp01(Fear * 0.9f + Wounded * 0.25f);

    /// <summary>装填をしくじる確率。震える手ではクリップが入らない。</summary>
    public float ReloadFumbleChance => Mathf.Clamp01(Fear * Fear * 0.6f + coldness * 0.15f);

    /// <summary>装填にかかる時間の倍率。寒さでかじかむと目に見えて遅くなる。</summary>
    public float ReloadTimeMultiplier => 1f + Fear * 0.5f + Wounded * 0.6f + coldness * 0.45f;

    /// <summary>
    /// 息切れの強さ。呼吸音とカメラの上下動に使う。
    /// 走り出してすぐ喘ぐと滑稽なので、疲労が3割ほど溜まってから効き始めるようにしている。
    /// </summary>
    public float Breathlessness => Mathf.Clamp01(Fatigue * 1.45f - 0.42f);

    /// <summary>負傷が重いと片手でしか銃を扱えなくなる。</summary>
    public bool IsOneHanded => Wounded > 0.6f;

    public bool IsFrozen => isFrozen;

    /// <summary>被弾方向を伴う負傷の通知。エイムパンチや出血演出の起点になる。</summary>
    public System.Action<Vector3, float> OnWounded;
    public System.Action OnDied;

    private UniversalFPSController controller;
    private CharacterController cc;
    private float lastThreatTime = -99f;
    private float freezeUntil = -99f;
    private bool isFrozen;
    private bool deathReported;

    void Awake()
    {
        controller = GetComponent<UniversalFPSController>();
        cc = GetComponent<CharacterController>();
        Health = maxHealth;
    }

    void OnEnable()
    {
        controller.OnDamageTaken += HandleDamageTaken;
    }

    void OnDisable()
    {
        controller.OnDamageTaken -= HandleDamageTaken;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        isFrozen = Time.time < freezeUntil;

        UpdateFear(dt);
        UpdateFatigue(dt);
        UpdateBleeding(dt);
        PushToController();
    }

    /// <summary>
    /// 素体は状態の意味を知らないので、効いた結果だけを毎フレーム渡す。
    /// これで FPSBase 側は MERE SOULS を一切参照せずに済む。
    /// </summary>
    private void PushToController()
    {
        controller.conditionSwayMultiplier = AimSwayMultiplier;
        controller.conditionSpeedMultiplier = MoveSpeedMultiplier;
        controller.conditionTremor = HandTremor;
        controller.conditionReloadTimeMultiplier = ReloadTimeMultiplier;
        controller.conditionReloadFumbleChance = ReloadFumbleChance;
        controller.conditionBreathlessness = Breathlessness;

        // 片手では銃を支えきれず、銃口が下がって内側に傾く。切り替わりが唐突だと嘘っぽいので補間する。
        Vector3 targetPos = IsOneHanded ? new Vector3(0.05f, -0.09f, -0.04f) : Vector3.zero;
        Vector3 targetRot = IsOneHanded ? new Vector3(12f, 6f, 24f) : Vector3.zero;
        controller.conditionWeaponPosOffset = Vector3.Lerp(
            controller.conditionWeaponPosOffset, targetPos, Time.deltaTime * 5f);
        controller.conditionWeaponRotOffset = Vector3.Lerp(
            controller.conditionWeaponRotOffset, targetRot, Time.deltaTime * 5f);
    }

    private void HandleDamageTaken(Vector3 incomingDir, float intensity)
    {
        ApplyWound(incomingDir, intensity * 0.35f);
    }

    private void UpdateFear(float dt)
    {
        // 脅威が去ってしばらく経たないと落ち着かない。砲撃の合間に回復しきらないのが狙い。
        if (Time.time - lastThreatTime > fearCalmDelay)
        {
            Fear = Mathf.Max(0f, Fear - fearDecayPerSecond * dt);
        }
    }

    private void UpdateFatigue(float dt)
    {
        if (controller.IsSprinting)
        {
            Fatigue = Mathf.Min(1f, Fatigue + sprintFatiguePerSecond * dt);
            return;
        }

        // 伏せ・しゃがみで止まっているときが一番回復する。塹壕の底で息を整える動作に対応する。
        bool resting = controller.CurrentStance != StanceState.Stand
                       && cc != null && cc.velocity.sqrMagnitude < 0.5f;
        float recovery = resting ? restRecoveryPerSecond : walkRecoveryPerSecond;
        Fatigue = Mathf.Max(0f, Fatigue - recovery * dt);
    }

    private void UpdateBleeding(float dt)
    {
        if (Wounded <= 0f || !IsAlive) return;

        Health = Mathf.Max(0f, Health - bleedPerSecond * Wounded * dt);
        if (Health <= 0f && !deathReported)
        {
            deathReported = true;
            OnDied?.Invoke();
        }
    }

    /// <summary>恐怖を加える。大きな跳ね上がりは一瞬の硬直を伴う。</summary>
    public void AddFear(float amount)
    {
        if (amount <= 0f) return;

        float before = Fear;
        Fear = Mathf.Clamp01(Fear + amount);
        lastThreatTime = Time.time;

        if (Fear - before >= freezeSpikeThreshold)
        {
            freezeUntil = Time.time + freezeDuration;
        }
    }

    /// <summary>
    /// 近くの爆発。距離で減衰させる。至近弾は恐怖が跳ね上がり、体が勝手に縮こまる。
    /// </summary>
    public void OnNearbyExplosion(Vector3 point, float blastRadius)
    {
        float distance = Vector3.Distance(transform.position, point);
        if (distance > blastRadius) return;

        float closeness = 1f - (distance / blastRadius);
        AddFear(closeness * closeness * 0.55f);
    }

    /// <summary>目の前で仲間が倒れたときの反応。一瞬動きが止まる。</summary>
    public void OnComradeDown()
    {
        AddFear(0.3f);
    }

    /// <summary>弾が至近を通過した。当たっていなくても恐怖は入る。</summary>
    public void OnNearMiss()
    {
        AddFear(0.12f);
    }

    /// <summary>被弾。負傷が蓄積し、恐怖も同時に跳ねる。</summary>
    public void ApplyWound(Vector3 fromDirection, float severity)
    {
        if (!IsAlive) return;

        Wounded = Mathf.Clamp01(Wounded + severity);
        Health = Mathf.Max(0f, Health - severity * 45f);
        AddFear(severity * 0.5f);

        OnWounded?.Invoke(fromDirection, severity);

        if (Health <= 0f && !deathReported)
        {
            deathReported = true;
            OnDied?.Invoke();
        }
    }

    /// <summary>衛生兵の処置。出血を止め、負傷を軽くする。体力までは戻らない。</summary>
    public void TreatWound(float effectiveness)
    {
        Wounded = Mathf.Max(0f, Wounded - effectiveness);
        Health = Mathf.Min(maxHealth, Health + effectiveness * 20f);
    }
}
