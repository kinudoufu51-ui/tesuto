using UnityEngine;

/// <summary>
/// Gキーでガジェットを使う入口。兵科ごとに使えるガジェットが違う(企画書 §4)ので、
/// DiamondStraitsClassSelection.currentClass を見て、今どれを使うべきかをその場で決める。
///
/// 基礎実装では兵科につき1種類だけ(突撃兵=麻酔グレネード、援護兵=簡易バリケード、
/// 看護兵=興奮剤注射器、斥候兵=グラップリングフック)。看護兵の蘇生キットは既存の
/// DiamondStraitsRevive(インタラクト)がその役を果たしているのでここには含めない。
/// 偵察カメラ・煙幕手榴弾・ワイヤーカッター・偽装ネットなど残りのガジェットは未実装。
/// </summary>
[RequireComponent(typeof(UniversalFPSController))]
public class DiamondStraitsGadgetController : MonoBehaviour
{
    public KeyCode useKey = KeyCode.G;

    [Tooltip("MonoBehaviour かつ IDiamondStraitsGadget を実装したコンポーネントを割り当てる。")]
    public MonoBehaviour assaultGadget;
    public MonoBehaviour supportGadget;
    public MonoBehaviour medicGadget;
    public MonoBehaviour reconGadget;

    private UniversalFPSController controller;
    private DiamondStraitsClassSelection classSelection;
    private float nextUseTime;

    void Awake()
    {
        controller = GetComponent<UniversalFPSController>();
        classSelection = GetComponent<DiamondStraitsClassSelection>();
    }

    void Update()
    {
        if (!Input.GetKeyDown(useKey) || Time.time < nextUseTime) return;

        IDiamondStraitsGadget gadget = ResolveGadget();
        if (gadget == null) return;

        gadget.Use(controller);
        nextUseTime = Time.time + gadget.CooldownSeconds;
    }

    private IDiamondStraitsGadget ResolveGadget()
    {
        SoldierClass? classType = (classSelection != null && classSelection.currentClass != null)
            ? classSelection.currentClass.classType
            : (SoldierClass?)null;

        MonoBehaviour candidate = classType switch
        {
            SoldierClass.Assault => assaultGadget,
            SoldierClass.Support => supportGadget,
            SoldierClass.Medic => medicGadget,
            SoldierClass.Recon => reconGadget,
            _ => null,
        };

        return candidate as IDiamondStraitsGadget;
    }
}
