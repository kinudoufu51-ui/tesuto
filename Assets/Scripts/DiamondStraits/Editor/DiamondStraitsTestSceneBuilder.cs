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
        playerRoot.AddComponent<DiamondStraitsRevivalController>();
        playerRoot.AddComponent<DiamondStraitsLocalHitRouter>();

        DiamondStraitsClassSelection classSelection = playerRoot.AddComponent<DiamondStraitsClassSelection>();
        classSelection.debugCycleClasses = LoadClassRoster();

        DiamondStraitsLoadoutMenu loadoutMenu = playerRoot.AddComponent<DiamondStraitsLoadoutMenu>();
        loadoutMenu.resourcesFolder = "FPSWeaponPresets";

        Camera playerCam = playerRoot.GetComponentInChildren<Camera>();
        if (playerCam != null) playerCam.gameObject.AddComponent<DiamondStraitsScreenEffect>();

        GameObject existingDummies = GameObject.Find("SedationTestDummies");
        if (existingDummies != null) Object.DestroyImmediate(existingDummies);

        GameObject dummyGroup = new GameObject("SedationTestDummies");
        BuildDummy(dummyGroup.transform, "TestDummy_A", new Vector3(0f, 1.0f, 5f));
        BuildDummy(dummyGroup.transform, "TestDummy_B", new Vector3(2.5f, 1.0f, 7f));
        BuildDummy(dummyGroup.transform, "TestDummy_C", new Vector3(-2.5f, 1.0f, 7f));

        GameObject existingDesk = GameObject.Find("GunDesk");
        if (existingDesk != null) Object.DestroyImmediate(existingDesk);
        BuildGunDesk(new Vector3(-2f, 0.5f, -2f));

        Selection.activeGameObject = playerRoot;
        Debug.Log("✅ [Diamond Straits] 麻酔コア検証シーンを構築しました。\n" +
                  "・武器(既存6プリセット、または先に「全48丁の武器カタログを生成」していればそちら)をテストNPCに撃つと麻酔が蓄積し、\n" +
                  "  満量で頭上に Zzz が出て動かなくなります。\n" +
                  "・Kキーで兵科を切り替えられます(未選択/看護兵の間はEを1.2秒長押しで麻酔0%の万全復帰=ルート①)。\n" +
                  "・看護兵以外を選ぶとEの長押しが3.5〜6.0秒(被弾銃が深いほど長い)になり、麻酔50%の寝起き状態で復帰します(ルート②)。\n" +
                  "・Hキーで自分を被弾させると自分も眠ります。ロック秒数が経過すると自分にJキーで自然リスポーン(ルート③)、\n" +
                  "  さらに長く放置すると自動で自力覚醒します(ルート④)。\n" +
                  "・スポーン地点そばの「GunDesk」に近づきEで武器ロッカーを開き、一覧から武器を選ぶと即座に持ち替わります" +
                  "(兵科を選んでいればその兵科の武器だけに絞られます)。");
    }

    /// <summary>
    /// DiamondStraitsWeaponCatalogBuilder が生成した4兵科分の SoldierClassData を拾う。
    /// まだ生成していなければ空配列を返し、Kキーでの兵科切り替えは単に何も起きないだけにする。
    /// </summary>
    private static SoldierClassData[] LoadClassRoster()
    {
        string root = "Assets/Resources/DiamondStraits/Classes";
        var classes = new System.Collections.Generic.List<SoldierClassData>();
        foreach (SoldierClass classType in System.Enum.GetValues(typeof(SoldierClass)))
        {
            SoldierClassData data = AssetDatabase.LoadAssetAtPath<SoldierClassData>($"{root}/{classType}.asset");
            if (data != null) classes.Add(data);
        }
        return classes.ToArray();
    }

    /// <summary>
    /// 武器ロッカーを開くための目印。見た目は仮の直方体で、実際の意匠はアートが入ってから差し替える。
    /// </summary>
    private static void BuildGunDesk(Vector3 position)
    {
        GameObject desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
        desk.name = "GunDesk";
        desk.transform.position = position;
        desk.transform.localScale = new Vector3(1.2f, 1.0f, 0.6f);
        desk.AddComponent<DiamondStraitsGunDesk>();
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
        dummy.AddComponent<DiamondStraitsRevive>();
        dummy.AddComponent<DiamondStraitsZzzOverlay>();
    }
}
#endif
