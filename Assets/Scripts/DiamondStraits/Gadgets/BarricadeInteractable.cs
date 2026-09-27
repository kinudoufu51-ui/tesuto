using UnityEngine;

/// <summary>
/// 突撃兵のワイヤーカッター(企画書 §4)。援護兵のバリケードに E 長押し1.0秒で穴を開ける。
/// ガジェットの消費/クールダウンを持たない「突撃兵なら誰でもできる」恒常能力として扱うため、
/// 専用のガジェットコンポーネントではなく、バリケード自身に直接この IFPSInteractable を乗せる。
/// </summary>
public class BarricadeInteractable : MonoBehaviour, IFPSInteractable
{
    public float holdDuration = 1.0f;

    public string GetInteractionPrompt() => "ワイヤーカッターで破壊";
    public float GetHoldDuration(UniversalFPSController player) => holdDuration;

    public bool CanInteract(UniversalFPSController player)
    {
        DiamondStraitsClassSelection selection = player.GetComponent<DiamondStraitsClassSelection>();
        return selection != null && selection.currentClass != null
            && selection.currentClass.classType == SoldierClass.Assault;
    }

    public void OnInteract(UniversalFPSController player)
    {
        Destroy(gameObject);
    }
}
