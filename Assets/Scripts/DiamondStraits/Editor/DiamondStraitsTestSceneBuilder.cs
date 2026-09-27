#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// フェーズ1「シングルプレイ・麻酔コア検証」用のシーンを構築する。
/// 素体(FPSBase)の検証ジムとプレイヤー構築をそのまま再利用し、Diamond Straits の麻酔コンポーネントと
/// テストNPCダミーを追加するだけに留める(技術仕様書 §7 フェーズ1)。
/// </summary>
public static class DiamondStraitsTestSceneBuilder
{
    [MenuItem("Tools/Diamond Straits/⚡ 麻酔コア検証シーンを構築 (FPSMetricGym + テストNPC)")]
    public static void BuildSedationTestScene()
    {
        FPSWeaponData[] weapons = FPSBaseAutoSetupEditor.EnsureWeaponAssets();

        GameObject existingGym = GameObject.Find("FPSMetricGym");
        if (existingGym != null) Object.DestroyImmediate(existingGym);

        GameObject gymObj = new GameObject("FPSMetricGym");
        gymObj.AddComponent<FPSGreyboxBuilder>().BuildGym();

        GameObject playerRoot = FPSBaseAutoSetupEditor.BuildPlayer(weapons, new Vector3(0f, 1.05f, 0f));
        playerRoot.AddComponent<DiamondStraitsSoldierCondition>();
        playerRoot.AddComponent<DiamondStraitsLocalHitRouter>();

        Camera playerCam = playerRoot.GetComponentInChildren<Camera>();
        if (playerCam != null) playerCam.gameObject.AddComponent<DiamondStraitsScreenEffect>();

        GameObject existingDummies = GameObject.Find("SedationTestDummies");
        if (existingDummies != null) Object.DestroyImmediate(existingDummies);

        GameObject dummyGroup = new GameObject("SedationTestDummies");
        BuildDummy(dummyGroup.transform, "TestDummy_A", new Vector3(0f, 1.0f, 5f));
        BuildDummy(dummyGroup.transform, "TestDummy_B", new Vector3(2.5f, 1.0f, 7f));
        BuildDummy(dummyGroup.transform, "TestDummy_C", new Vector3(-2.5f, 1.0f, 7f));

        Selection.activeGameObject = playerRoot;
        Debug.Log("✅ [Diamond Straits] 麻酔コア検証シーンを構築しました。\n" +
                  "・武器(既存6プリセット)をテストNPCに向けて撃つと麻酔が蓄積し、満量で頭上に Zzz が出て動かなくなります。\n" +
                  "・眠っているNPCに近づき E を1.2秒長押しすると、看護兵の蘇生で麻酔0%の万全状態に戻ります。\n" +
                  "・Hキーで自分を被弾させると自分の画面も彩度が落ちます。JキーはテストNPCがいない場合の自分用の目覚まし(デバッグ専用)です。");
    }

    private static void BuildDummy(Transform parent, string name, Vector3 position)
    {
        GameObject dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        dummy.name = name;
        dummy.transform.SetParent(parent, false);
        dummy.transform.position = position;
        dummy.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);

        // 動かない的なので Rigidbody は不要。CapsuleCollider だけで被弾・インタラクトの両方を受けられる。
        dummy.AddComponent<DiamondStraitsSoldierCondition>();
        dummy.AddComponent<DiamondStraitsMedicRevive>();
        dummy.AddComponent<DiamondStraitsZzzOverlay>();
    }
}
#endif
