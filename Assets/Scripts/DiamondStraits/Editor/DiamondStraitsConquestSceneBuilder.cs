#if UNITY_EDITOR
using System.IO;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEngine;

/// <summary>
/// フェーズ3後半: 拠点争奪(コンクエスト)とネットワーク同期の検証シーンを構築する。
/// 素体のプレイヤー構築とネットワーク接続UI(MereSouls由来だがゲーム知識を持たない汎用部品)を
/// そのまま再利用し、舞台とゲームルールだけ Diamond Straits 用に差し替える。
/// </summary>
public static class DiamondStraitsConquestSceneBuilder
{
    private const string SoldierLayerName = "Soldier";
    private const string PrefabFolder = "Assets/Prefabs";
    private const string PlayerPrefabPath = PrefabFolder + "/DiamondStraitsSoldier.prefab";

    [MenuItem("Tools/Diamond Straits/⚡ コンクエスト対戦シーンを構築 (拠点争奪 + ネットワーク)")]
    public static void BuildConquestScene()
    {
        int soldierLayer = EnsureLayer(SoldierLayerName);
        if (soldierLayer < 0)
        {
            Debug.LogError($"[Diamond Straits] レイヤー '{SoldierLayerName}' を作れませんでした。空きレイヤーがありません。");
            return;
        }

        FPSWeaponData[] weapons = FPSBaseAutoSetupEditor.EnsureWeaponAssets();

        GameObject existingGym = GameObject.Find("FPSMetricGym");
        if (existingGym != null) Object.DestroyImmediate(existingGym);
        GameObject existingArena = GameObject.Find("ConquestArena");
        if (existingArena != null) Object.DestroyImmediate(existingArena);
        GameObject existingManagerObj = GameObject.Find("ConquestManager");
        if (existingManagerObj != null) Object.DestroyImmediate(existingManagerObj);

        Transform[] pointMarkers = BuildArena();
        GameObject prefab = BuildSoldierPrefab(weapons, soldierLayer);
        BuildConquestManager(pointMarkers);
        SetUpNetworkManager(prefab);

        // East を企画書の「C. アーケード拠点」に見立て、占領した陣営だけが豆戦車を使えるようにする。
        DiamondStraitsCapturePoint arcadePoint = pointMarkers[1].GetComponent<DiamondStraitsCapturePoint>();
        GameObject existingTank = GameObject.Find("MiniTank");
        if (existingTank != null) Object.DestroyImmediate(existingTank);
        BuildMiniTank(pointMarkers[1].position + new Vector3(-10f, 1.0f, 3f), arcadePoint);

        Selection.activeGameObject = GameObject.Find("NetworkManager");
        Debug.Log("✅ [Diamond Straits] コンクエスト対戦シーンを構築しました。\n" +
                  "・再生後、画面右上の「ホストとして開く」でホスト、別PCから相手のIPを入れて「参加する」。\n" +
                  "・接続順に陣営A/Bへ振り分けられます(偶数番がA、奇数番がB)。スポーン地点はまだ仮で、原点付近に散らばるだけです。\n" +
                  "・4つの拠点(N/E/S/W)は片方の陣営だけが滞在すると占領が進み、占領した拠点は相手のチケットを削ります。\n" +
                  "・チケットが0になった陣営の敗北です。左上のHUDで戦況を確認してください。\n" +
                  "・眠っている相手には近づいてEで蘇生できます(看護兵1.2秒/それ以外3.5〜6.0秒、Kキーで兵科切替)。\n" +
                  "・突撃兵でGキー → 麻酔グレネード投擲。援護兵でGキー → 簡易バリケード設置(高さ0.90m、飛び越え可能)。\n" +
                  "  看護兵でGキー → 狙った味方に興奮剤(移動・装填25%速く)。斥候兵でGキー → グラップリングフック。\n" +
                  "・Bキーで武器ロッカーを開けます(物理デスクはまだ未配置)。先に「全48丁の武器カタログを生成」していれば、\n" +
                  "  選んだ兵科の武器だけに絞り込まれます。\n" +
                  "・「MiniTank」はEast拠点(企画書のアーケード拠点)を占領している陣営だけが乗降できます。未占領/敵占領中は\n" +
                  "  乗降口が反応しません。操縦手が乗ると所有権がその人に移り、位置が他クライアントへ同期されます。");
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
    /// 4拠点をコンパスの位置に置いた簡易アリーナ。地形やカバーは無く、あくまでコンクエストと
    /// ネットワーク同期のロジック検証が目的(舞台の作り込みはフェーズ4以降)。
    /// </summary>
    private static Transform[] BuildArena()
    {
        GameObject arena = new GameObject("ConquestArena");

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(arena.transform, false);
        ground.transform.localScale = new Vector3(12f, 1f, 12f); // Unityの Plane は10x10単位=120x120m相当

        Vector3[] positions =
        {
            new Vector3(0f, 0f, 40f),
            new Vector3(40f, 0f, 0f),
            new Vector3(0f, 0f, -40f),
            new Vector3(-40f, 0f, 0f),
        };
        string[] names = { "CapturePoint_North", "CapturePoint_East", "CapturePoint_South", "CapturePoint_West" };

        Transform[] markers = new Transform[positions.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            GameObject marker = new GameObject(names[i]);
            marker.transform.SetParent(arena.transform, false);
            marker.transform.position = positions[i];

            SphereCollider trigger = marker.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 8f;

            marker.AddComponent<NetworkObject>();
            DiamondStraitsCapturePoint point = marker.AddComponent<DiamondStraitsCapturePoint>();
            point.pointName = names[i];

            GameObject flagVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            flagVisual.name = "Marker";
            flagVisual.transform.SetParent(marker.transform, false);
            flagVisual.transform.localScale = new Vector3(0.3f, 3f, 0.3f);
            flagVisual.transform.localPosition = new Vector3(0f, 3f, 0f);
            Object.DestroyImmediate(flagVisual.GetComponent<Collider>());

            markers[i] = marker.transform;
        }

        return markers;
    }

    private static void BuildConquestManager(Transform[] pointMarkers)
    {
        GameObject managerObj = new GameObject("ConquestManager");
        managerObj.AddComponent<NetworkObject>();
        DiamondStraitsConquestManager manager = managerObj.AddComponent<DiamondStraitsConquestManager>();

        DiamondStraitsCapturePoint[] points = new DiamondStraitsCapturePoint[pointMarkers.Length];
        for (int i = 0; i < pointMarkers.Length; i++) points[i] = pointMarkers[i].GetComponent<DiamondStraitsCapturePoint>();

        manager.capturePoints = points;
        // 企画書のナイトクラブ拠点(削り効率1.5倍)を仮に北へ配置。実マップが入れば座標ごと差し替える。
        manager.pointDrainMultipliers = new float[] { 1.5f, 1f, 1f, 1f };

        managerObj.AddComponent<DiamondStraitsConquestHud>().manager = manager;
    }

    private static GameObject BuildSoldierPrefab(FPSWeaponData[] weapons, int soldierLayer)
    {
        GameObject soldier = FPSBaseAutoSetupEditor.BuildPlayer(weapons, Vector3.zero);
        soldier.name = "DiamondStraitsSoldier";

        // 兵士を専用レイヤーに移す。地形を感知するレイが自分や他人の身体を拾うと、
        // 壁際の銃の引き戻しや自動リーンが他人に反応して誤作動する。
        SetLayerRecursively(soldier, soldierLayer);

        UniversalFPSController controller = soldier.GetComponent<UniversalFPSController>();
        controller.environmentMask = ~(1 << soldierLayer);
        controller.shootableMask = ~0;

        FPSInteractionSystem interaction = soldier.GetComponent<FPSInteractionSystem>();
        if (interaction != null) interaction.interactableMask = ~(1 << soldierLayer);

        soldier.AddComponent<DiamondStraitsSoldierCondition>();
        soldier.AddComponent<DiamondStraitsRevivalController>();
        soldier.AddComponent<DiamondStraitsRevive>();

        DiamondStraitsClassSelection classSelection = soldier.AddComponent<DiamondStraitsClassSelection>();
        classSelection.debugCycleClasses = LoadClassRoster();

        soldier.AddComponent<DiamondStraitsZzzOverlay>();

        DiamondStraitsLoadoutMenu loadoutMenu = soldier.AddComponent<DiamondStraitsLoadoutMenu>();
        loadoutMenu.resourcesFolder = "FPSWeaponPresets";

        DiamondStraitsGadgetController gadgetController = soldier.AddComponent<DiamondStraitsGadgetController>();
        gadgetController.assaultGadget = soldier.AddComponent<SedationGrenadeGadget>();
        gadgetController.supportGadget = soldier.AddComponent<BarricadeGadget>();
        gadgetController.medicGadget = soldier.AddComponent<StimulantInjectorGadget>();
        gadgetController.reconGadget = soldier.AddComponent<GrapplingHookGadget>();

        Camera soldierCam = soldier.GetComponentInChildren<Camera>();
        if (soldierCam != null) soldierCam.gameObject.AddComponent<DiamondStraitsScreenEffect>();

        // 他人から見える身体。当たり判定は CharacterController が持つのでコライダーは外す。
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "RemoteBody";
        body.transform.SetParent(soldier.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
        body.layer = soldierLayer;
        Object.DestroyImmediate(body.GetComponent<Collider>());

        soldier.AddComponent<NetworkObject>();
        soldier.AddComponent<OwnerNetworkTransform>();
        DiamondStraitsNetworkPlayer netPlayer = soldier.AddComponent<DiamondStraitsNetworkPlayer>();
        netPlayer.remoteBody = body;

        if (!Directory.Exists(PrefabFolder)) Directory.CreateDirectory(PrefabFolder);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(soldier, PlayerPrefabPath);

        Object.DestroyImmediate(soldier);

        return prefab;
    }

    private static void SetUpNetworkManager(GameObject playerPrefab)
    {
        GameObject existing = GameObject.Find("NetworkManager");
        if (existing != null) Object.DestroyImmediate(existing);

        GameObject go = new GameObject("NetworkManager");
        NetworkManager net = go.AddComponent<NetworkManager>();
        UnityTransport transport = go.AddComponent<UnityTransport>();

        net.NetworkConfig.NetworkTransport = transport;
        net.NetworkConfig.PlayerPrefab = playerPrefab;
        net.NetworkConfig.ConnectionApproval = false;

        go.AddComponent<MereSoulsNetworkUI>();
    }

    /// <summary>
    /// 豆戦車のプレースホルダー。見た目は仮の直方体で、コミカルな意匠はアートが入ってから差し替える。
    /// ハル本体は物理スケール(1.6×1.0×2.4)を持つが、座席・乗降口はスケールの影響を受けない
    /// ルート直下に置き、実寸メートルでそのまま配置できるようにしている。
    /// </summary>
    private static void BuildMiniTank(Vector3 position, DiamondStraitsCapturePoint deploymentGate)
    {
        GameObject root = new GameObject("MiniTank");
        root.transform.position = position;

        Rigidbody body = root.AddComponent<Rigidbody>();
        body.mass = 400f;
        body.linearDamping = 1f;
        body.angularDamping = 2f;

        MiniTankController controller = root.AddComponent<MiniTankController>();
        controller.deploymentGate = deploymentGate;

        // コンクエスト(ネットワーク)シーン向けの位置同期。操縦手が乗るとその人へ所有権が移り、
        // OwnerNetworkTransform が実際の物理をそのまま他クライアントへ伝える。
        root.AddComponent<NetworkObject>();
        root.AddComponent<OwnerNetworkTransform>();
        root.AddComponent<MiniTankNetworkSync>();

        GameObject hullVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hullVisual.name = "HullVisual";
        hullVisual.transform.SetParent(root.transform, false);
        hullVisual.transform.localScale = new Vector3(1.6f, 1.0f, 2.4f);
        Object.DestroyImmediate(hullVisual.GetComponent<Collider>());
        root.AddComponent<BoxCollider>().size = new Vector3(1.6f, 1.0f, 2.4f);

        controller.driverSeat = CreateChild(root.transform, "DriverSeatView", new Vector3(0.3f, 0.3f, 0.4f));
        controller.gunnerSeat = CreateChild(root.transform, "GunnerSeatView", new Vector3(-0.3f, 0.5f, -0.4f));
        controller.exitPoint = CreateChild(root.transform, "ExitPoint", new Vector3(1.2f, -0.3f, 0f));

        BuildSeatDoor(root.transform, "DriverDoor", new Vector3(0.9f, 0f, 0.5f), controller, true);
        BuildSeatDoor(root.transform, "GunnerDoor", new Vector3(-0.9f, 0f, -0.5f), controller, false);
    }

    private static Transform CreateChild(Transform parent, string name, Vector3 localPosition)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        return go.transform;
    }

    private static void BuildSeatDoor(Transform parent, string name, Vector3 localPosition, MiniTankController vehicle, bool isDriverSeat)
    {
        GameObject door = new GameObject(name);
        door.transform.SetParent(parent, false);
        door.transform.localPosition = localPosition;

        SphereCollider trigger = door.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.5f;

        MiniTankSeat seat = door.AddComponent<MiniTankSeat>();
        seat.vehicle = vehicle;
        seat.isDriverSeat = isDriverSeat;
    }

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    /// <summary>レイヤーが無ければ TagManager に作る。8番より前は組み込みなので触らない。</summary>
    private static int EnsureLayer(string name)
    {
        int existing = LayerMask.NameToLayer(name);
        if (existing >= 0) return existing;

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return -1;

        SerializedObject tagManager = new SerializedObject(assets[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        if (layers == null) return -1;

        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layer.stringValue))
            {
                layer.stringValue = name;
                tagManager.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                return i;
            }
        }
        return -1;
    }
}
#endif
