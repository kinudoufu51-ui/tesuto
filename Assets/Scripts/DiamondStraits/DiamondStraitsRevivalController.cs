using UnityEngine;

/// <summary>
/// 誰の手も借りずに戦線へ戻る2ルートを管理する (企画書 §3-2)。
/// ①看護兵蘇生・②分隊蘇生は DiamondStraitsRevive (IFPSInteractable) が担当し、
/// これはその外側にある③自然リスポーン・④自力覚醒だけを扱う。
///
/// 拠点選択やチケット消費を伴う本物のコンクエスト実装はフェーズ3後半の対象で、
/// ここでは「ロックが明けたら再出撃できる」というタイミングだけを検証する。
/// </summary>
[RequireComponent(typeof(DiamondStraitsSoldierCondition))]
public class DiamondStraitsRevivalController : MonoBehaviour
{
    [Tooltip("自力覚醒までの秒数は強制リスポーン待ちの何倍か。技術仕様書 §7 の概算値。")]
    public float selfWakeMultiplier = 2.4f;

    [Tooltip("自然リスポーンをテストする際のキー。実際のゲームではリスポーン画面のUIから選ぶ。")]
    public KeyCode respawnKey = KeyCode.J;

    /// <summary>強制ロックが明け、プレイヤーの意思で再出撃できる状態になったか。</summary>
    public bool NaturalRespawnReady { get; private set; }

    private DiamondStraitsSoldierCondition condition;
    private float sleepStartTime;
    private float lockSeconds;

    void Awake()
    {
        condition = GetComponent<DiamondStraitsSoldierCondition>();
    }

    void OnEnable()
    {
        condition.OnEnterSleep += HandleEnterSleep;
        condition.OnRevived += HandleRevived;
    }

    void OnDisable()
    {
        condition.OnEnterSleep -= HandleEnterSleep;
        condition.OnRevived -= HandleRevived;
    }

    private void HandleEnterSleep()
    {
        sleepStartTime = Time.time;
        lockSeconds = Mathf.Max(1f, condition.SedationDepthSeconds);
        NaturalRespawnReady = false;
    }

    private void HandleRevived()
    {
        NaturalRespawnReady = false;
    }

    void Update()
    {
        if (!condition.IsAsleep) return;

        float elapsed = Time.time - sleepStartTime;
        NaturalRespawnReady = elapsed >= lockSeconds;

        // ③ 自然リスポーン: ロックが明けたら、あきらめて再出撃できる。
        if (NaturalRespawnReady && Input.GetKeyDown(respawnKey))
        {
            condition.Revive(0f);
            return;
        }

        // ④ 自力覚醒: 誰にも手当てされず放置された場合の最終救済。
        if (elapsed >= lockSeconds * selfWakeMultiplier)
        {
            condition.Revive(0f);
        }
    }
}
