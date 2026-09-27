using UnityEngine;

/// <summary>
/// 看護兵の最強の特権: どんな深さの麻酔で寝ていても、E長押し1.2秒でその場から
/// 麻酔蓄積0%・デバフなしの万全状態に即復帰させる (企画書 §3-2 ルート①)。
///
/// 昏睡していない相手には反応しない。FPSInteractionSystem の既存ホールド式インタラクトに
/// そのまま乗るので、素体側の変更は不要。
/// </summary>
[RequireComponent(typeof(DiamondStraitsSoldierCondition))]
public class DiamondStraitsMedicRevive : MonoBehaviour, IFPSInteractable
{
    public float holdDuration = 1.2f;

    private DiamondStraitsSoldierCondition condition;

    void Awake()
    {
        condition = GetComponent<DiamondStraitsSoldierCondition>();
    }

    public string GetInteractionPrompt() => condition.IsAsleep ? "眠っている兵士を蘇生" : "";
    public float GetHoldDuration() => holdDuration;
    public bool CanInteract(UniversalFPSController player) => condition.IsAsleep;

    public void OnInteract(UniversalFPSController player)
    {
        condition.Revive(0f);
    }
}
