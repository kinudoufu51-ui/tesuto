using UnityEngine;

/// <summary>
/// プレイヤーが今どの兵科(SoldierClassData)を選んでいるかを保持する。
/// DiamondStraitsRevive はここから「看護兵かどうか」を判定し、DiamondStraitsLoadoutMenu は
/// ここから武器候補を絞り込む。DiamondStraitsMedicTag(暫定の目印)を置き換える本実装。
/// </summary>
public class DiamondStraitsClassSelection : MonoBehaviour
{
    public SoldierClassData currentClass;

    [Tooltip("1人での検証用: このキーで兵科を順番に切り替えられる。実プレイでは出撃前のクラス選択UIに置き換わる。")]
    public KeyCode debugCycleKey = KeyCode.K;
    public SoldierClassData[] debugCycleClasses;

    private int debugIndex = -1;

    public bool IsMedic => currentClass != null && currentClass.classType == SoldierClass.Medic;

    void Update()
    {
        if (debugCycleClasses == null || debugCycleClasses.Length == 0) return;
        if (!Input.GetKeyDown(debugCycleKey)) return;

        debugIndex = (debugIndex + 1) % debugCycleClasses.Length;
        currentClass = debugCycleClasses[debugIndex];
    }
}
