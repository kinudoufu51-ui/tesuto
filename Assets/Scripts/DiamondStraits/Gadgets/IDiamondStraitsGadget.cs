/// <summary>
/// 兵科ガジェットの共通契約。MonoBehaviour として実装し、DiamondStraitsGadgetController が
/// 現在の兵科に応じてどれを使うか選ぶ(企画書 §4「兵科の役割・ガジェット」)。
/// </summary>
public interface IDiamondStraitsGadget
{
    string GadgetName { get; }
    float CooldownSeconds { get; }
    void Use(UniversalFPSController player);
}
