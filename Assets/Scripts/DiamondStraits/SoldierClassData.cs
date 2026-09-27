using UnityEngine;

public enum SoldierClass
{
    Assault,
    Medic,
    Support,
    Recon,
}

/// <summary>
/// 兵科データ(技術仕様書 §3)。今のところ主武器の候補リストだけを持つ。
/// ガジェット(麻酔グレネード・バリケード・グラップリングフックなど)は看護兵の蘇生キット以外
/// まだ実体が無いため、意図的に含めていない(フェーズ4後半で GadgetData を新設して追加する)。
/// </summary>
[CreateAssetMenu(fileName = "NewSoldierClass", menuName = "DiamondStraits/SoldierClassData")]
public class SoldierClassData : ScriptableObject
{
    public SoldierClass classType;
    public FPSWeaponData[] primaryWeapons;
}
