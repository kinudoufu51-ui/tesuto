using UnityEngine;

/// <summary>
/// 検証ジム ゾーンC用: 単押しで開閉するドア(IFPSInteractable)。
/// この GameObject 自身をヒンジ(回転軸)として使うので、ドア板は原点からオフセットした子として配置する。
/// </summary>
public class FPSDoor : MonoBehaviour, IFPSInteractable
{
    public float openAngle = 100f;
    public float openSpeed = 3.0f;
    public string promptOpen = "ドアを開く";
    public string promptClose = "ドアを閉じる";

    private bool isOpen = false;
    private float currentAngle = 0f;
    private Quaternion closedRotation;

    void Awake()
    {
        closedRotation = transform.localRotation;
    }

    void Update()
    {
        float targetAngle = isOpen ? openAngle : 0f;
        currentAngle = Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime * openSpeed);
        transform.localRotation = closedRotation * Quaternion.Euler(0f, currentAngle, 0f);
    }

    public string GetInteractionPrompt() => isOpen ? promptClose : promptOpen;
    public float GetHoldDuration(UniversalFPSController player) => 0f;
    public bool CanInteract(UniversalFPSController player) => true;

    public void OnInteract(UniversalFPSController player)
    {
        isOpen = !isOpen;
    }
}
