#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// MERE SOULS の検証シーンを構築する。素体(FPSBase)のプレイヤー構築をそのまま再利用し、
/// 舞台だけ検証ジムから塹壕セクターに差し替える。
/// </summary>
public static class MereSoulsSceneBuilder
{
    [MenuItem("Tools/MERE SOULS/⚡ 塹壕セクターを構築 (対峙する2本の前線塹壕)")]
    public static void BuildTrenchScene()
    {
        FPSWeaponData[] weapons = FPSBaseAutoSetupEditor.EnsureWeaponAssets();

        GameObject existingGym = GameObject.Find("FPSMetricGym");
        if (existingGym != null) Object.DestroyImmediate(existingGym);

        GameObject existingSector = GameObject.Find("TrenchSector");
        if (existingSector != null) Object.DestroyImmediate(existingSector);

        GameObject sector = new GameObject("TrenchSector");
        sector.AddComponent<TrenchSectorBuilder>().BuildSector();

        // 南側の射撃区画の床に立たせる。床はY=-1.8、PlayerRootの原点は足元。
        GameObject playerRoot = FPSBaseAutoSetupEditor.BuildPlayer(weapons, new Vector3(1f, -1.75f, -1f));

        Selection.activeGameObject = playerRoot;
        Debug.Log("✅ [MERE SOULS] 塹壕セクターを構築しました。\n" +
                  "床に立つと胸壁の0.75m下で外が見えません。火点(0.76m)に上がると覗けます。" +
                  "現状は火点への昇降が素体のVault(Space+前進)頼りなので、専用の昇降動作は次の作業です。");
    }
}
#endif
