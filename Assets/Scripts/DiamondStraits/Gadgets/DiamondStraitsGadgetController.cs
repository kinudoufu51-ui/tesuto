using UnityEngine;

/// <summary>
/// ガジェットを使う入口。兵科ごとに使えるガジェットが違う(企画書 §4)ので、
/// DiamondStraitsClassSelection.currentClass を見て、今どれを使うべきかをその場で決める。
///
/// 兵科につき2枠(主=Gキー、副=Vキー)まで。看護兵の蘇生キットは既存の DiamondStraitsRevive
/// (インタラクト)がその役を果たしているのでここには含めない。偵察カメラ・煙幕手榴弾・
/// ワイヤーカッター・弾薬補給ボックス・センサー地雷・偽装ネットは未実装(各兵科3枠目)。
/// </summary>
[RequireComponent(typeof(UniversalFPSController))]
public class DiamondStraitsGadgetController : MonoBehaviour
{
    public KeyCode primaryUseKey = KeyCode.G;
    public KeyCode secondaryUseKey = KeyCode.V;

    [Tooltip("MonoBehaviour かつ IDiamondStraitsGadget を実装したコンポーネントを割り当てる。")]
    public MonoBehaviour assaultGadgetPrimary;
    public MonoBehaviour assaultGadgetSecondary;
    public MonoBehaviour supportGadgetPrimary;
    public MonoBehaviour supportGadgetSecondary;
    public MonoBehaviour medicGadgetPrimary;
    public MonoBehaviour medicGadgetSecondary;
    public MonoBehaviour reconGadgetPrimary;
    public MonoBehaviour reconGadgetSecondary;

    private UniversalFPSController controller;
    private DiamondStraitsClassSelection classSelection;
    private float nextPrimaryUseTime;
    private float nextSecondaryUseTime;

    void Awake()
    {
        controller = GetComponent<UniversalFPSController>();
        classSelection = GetComponent<DiamondStraitsClassSelection>();
    }

    void Update()
    {
        if (Input.GetKeyDown(primaryUseKey) && Time.time >= nextPrimaryUseTime)
        {
            TryUse(ResolveGadget(primary: true), ref nextPrimaryUseTime);
        }

        if (Input.GetKeyDown(secondaryUseKey) && Time.time >= nextSecondaryUseTime)
        {
            TryUse(ResolveGadget(primary: false), ref nextSecondaryUseTime);
        }
    }

    private void TryUse(IDiamondStraitsGadget gadget, ref float nextUseTime)
    {
        if (gadget == null) return;

        gadget.Use(controller);
        nextUseTime = Time.time + gadget.CooldownSeconds;
    }

    private IDiamondStraitsGadget ResolveGadget(bool primary)
    {
        SoldierClass? classType = (classSelection != null && classSelection.currentClass != null)
            ? classSelection.currentClass.classType
            : (SoldierClass?)null;

        MonoBehaviour candidate = classType switch
        {
            SoldierClass.Assault => primary ? assaultGadgetPrimary : assaultGadgetSecondary,
            SoldierClass.Support => primary ? supportGadgetPrimary : supportGadgetSecondary,
            SoldierClass.Medic => primary ? medicGadgetPrimary : medicGadgetSecondary,
            SoldierClass.Recon => primary ? reconGadgetPrimary : reconGadgetSecondary,
            _ => null,
        };

        return candidate as IDiamondStraitsGadget;
    }
}
