using UnityEngine;

/// <summary>
/// 検証ジム ゾーンC用: 1.2秒長押しで開封する物資クレート(IFPSInteractable)。
/// `lid` にはヒンジ用の空Transform(蓋メッシュ自体ではなく、その親ピボット)を割り当てる。
/// </summary>
public class FPSSupplyCrate : MonoBehaviour, IFPSInteractable
{
    public float holdDuration = 1.2f;
    public Transform lid;
    public float lidOpenAngle = 100f;

    private bool isOpened = false;
    private float lidAngle = 0f;

    void Update()
    {
        if (lid == null) return;
        float target = isOpened ? lidOpenAngle : 0f;
        lidAngle = Mathf.Lerp(lidAngle, target, Time.deltaTime * 4f);
        lid.localRotation = Quaternion.Euler(-lidAngle, 0f, 0f);
    }

    public string GetInteractionPrompt() => isOpened ? "開封済み" : "物資クレートを開封";
    public float GetHoldDuration(UniversalFPSController player) => holdDuration;
    public bool CanInteract(UniversalFPSController player) => !isOpened;

    public void OnInteract(UniversalFPSController player)
    {
        isOpened = true;
    }
}
