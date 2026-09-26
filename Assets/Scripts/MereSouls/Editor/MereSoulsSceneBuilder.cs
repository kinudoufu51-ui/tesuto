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

        playerRoot.AddComponent<FPSSoldierCondition>();
        playerRoot.AddComponent<FPSConditionOverlay>();

        // 彩度を落とす処理はカメラ側でないと効かない。
        Camera playerCam = playerRoot.GetComponentInChildren<Camera>();
        if (playerCam != null) playerCam.gameObject.AddComponent<MereSoulsScreenEffect>();

        Selection.activeGameObject = playerRoot;
        Debug.Log("✅ [MERE SOULS] 塹壕セクターを構築しました。\n" +
                  "・床に立つと胸壁の0.75m下で外が見えません。火点(0.76m)に上がり、銃眼の正面からだけ覗けます。\n" +
                  "・走り続けると疲労が溜まり、息が上がって照準がぶれます(数値は出ません。音と手元で判断してください)。\n" +
                  "・Hキーで被弾すると恐怖と負傷が入り、視界が狭まって装填をしくじるようになります。\n" +
                  "・火点への昇降は現状まだ素体のVault(Space+前進)頼りです。");
    }
}
#endif
