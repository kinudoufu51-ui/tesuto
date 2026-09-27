using UnityEngine;

/// <summary>
/// 昏睡した相手を起こす2ルートをまとめて扱う (企画書 §3-2)。
///
/// ① 看護兵: E長押し1.2秒で、麻酔蓄積0%・デバフなしの万全状態へ即復帰。
/// ② 分隊員: 被弾の深さ(SedationDepthSeconds)に応じて3.5〜6.0秒かけて起こすが、
///    起きた直後も麻酔が50%残った「寝起き状態」になる。
///
/// FPSInteractionSystem は対象1体につき IFPSInteractable を1つしか拾わないため、
/// 「誰が起こすか」で長押し時間と結果を分岐する形でこの1コンポーネントにまとめている。
/// 兵科選択(フェーズ4)が入るまでは DiamondStraitsMedicTag の有無で仮に判定する。
/// </summary>
[RequireComponent(typeof(DiamondStraitsSoldierCondition))]
public class DiamondStraitsRevive : MonoBehaviour, IFPSInteractable
{
    public float medicHoldDuration = 1.2f;
    public float squadHoldMin = 3.5f;
    public float squadHoldMax = 6.0f;

    [Tooltip("分隊蘇生で目覚めた直後に残る麻酔量(寝起き状態)。")]
    public float squadReviveResidualSedation = 50f;

    [Tooltip("被弾の深さ(forcedRespawnLockSeconds)をこの範囲に正規化して分隊蘇生の所要時間を補間する。武器一覧のロック秒数レンジに合わせる。")]
    public float depthRangeMin = 11f;
    public float depthRangeMax = 25f;

    private DiamondStraitsSoldierCondition condition;

    void Awake()
    {
        condition = GetComponent<DiamondStraitsSoldierCondition>();
    }

    public string GetInteractionPrompt() => condition.IsAsleep ? "眠っている兵士を起こす" : "";

    public float GetHoldDuration(UniversalFPSController player)
    {
        if (!condition.IsAsleep) return 0f;
        return IsMedic(player) ? medicHoldDuration : SquadHoldDuration();
    }

    public bool CanInteract(UniversalFPSController player) => condition.IsAsleep;

    public void OnInteract(UniversalFPSController player)
    {
        condition.Revive(IsMedic(player) ? 0f : squadReviveResidualSedation);
    }

    private bool IsMedic(UniversalFPSController player)
    {
        DiamondStraitsMedicTag tag = player.GetComponent<DiamondStraitsMedicTag>();
        return tag != null && tag.isMedic;
    }

    private float SquadHoldDuration()
    {
        float depthT = Mathf.InverseLerp(depthRangeMin, depthRangeMax, condition.SedationDepthSeconds);
        return Mathf.Lerp(squadHoldMin, squadHoldMax, depthT);
    }
}
