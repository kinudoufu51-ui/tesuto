#if UNITY_EDITOR
using System.IO;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 2〜3人で塹壕を撃ち合うための対戦シーンを構築する。
/// 素体のプレイヤー構築をそのまま使い、ネットワーク用の部品とスポーン地点だけを足す。
/// </summary>
public static class MereSoulsNetworkSceneBuilder
{
    private const string SoldierLayerName = "Soldier";
    private const string PrefabFolder = "Assets/Prefabs";
    private const string PlayerPrefabPath = PrefabFolder + "/MereSoulsSoldier.prefab";

    [MenuItem("Tools/MERE SOULS/⚡ 対戦シーンを構築 (塹壕 + ネットワーク)")]
    public static void BuildNetworkedScene()
    {
        int soldierLayer = EnsureLayer(SoldierLayerName);
        if (soldierLayer < 0)
        {
            Debug.LogError($"[MERE SOULS] レイヤー '{SoldierLayerName}' を作れませんでした。空きレイヤーがありません。");
            return;
        }

        FPSWeaponData[] weapons = FPSBaseAutoSetupEditor.EnsureWeaponAssets();

        GameObject existingGym = GameObject.Find("FPSMetricGym");
        if (existingGym != null) Object.DestroyImmediate(existingGym);

        GameObject existingSector = GameObject.Find("TrenchSector");
        if (existingSector != null) Object.DestroyImmediate(existingSector);

        GameObject sector = new GameObject("TrenchSector");
        sector.AddComponent<TrenchSectorBuilder>().BuildSector();

        GameObject prefab = BuildSoldierPrefab(weapons, soldierLayer);
        SetUpNetworkManager(prefab);

        Selection.activeGameObject = GameObject.Find("NetworkManager");
        Debug.Log("✅ [MERE SOULS] 対戦シーンを構築しました。\n" +
                  "・再生後、画面右上の「ホストとして開く」でホスト、別PCから相手のIPを入れて「参加する」。\n" +
                  "・接続順に南北の塹壕へ振り分けられます(偶数番が南、奇数番が北)。\n" +
                  "・同一PCで試す場合はビルドした実行ファイルとエディタを併用してください。");
    }

    /// <summary>
    /// 兵士のプレハブを作る。素体のプレイヤー構築をそのまま使い、ネットワーク部品を足してから保存する。
    /// </summary>
    private static GameObject BuildSoldierPrefab(FPSWeaponData[] weapons, int soldierLayer)
    {
        GameObject soldier = FPSBaseAutoSetupEditor.BuildPlayer(weapons, Vector3.zero);
        soldier.name = "MereSoulsSoldier";

        // 兵士を専用レイヤーに移す。地形を感知するレイが自分や他人の身体を拾うと、
        // 壁際の銃の引き戻しや自動リーンが他人に反応して誤作動する。
        SetLayerRecursively(soldier, soldierLayer);

        UniversalFPSController controller = soldier.GetComponent<UniversalFPSController>();
        controller.environmentMask = ~(1 << soldierLayer);
        controller.shootableMask = ~0;

        FPSInteractionSystem interaction = soldier.GetComponent<FPSInteractionSystem>();
        if (interaction != null) interaction.interactableMask = ~(1 << soldierLayer);

        soldier.AddComponent<FPSSoldierCondition>();
        soldier.AddComponent<FPSConditionOverlay>();

        Camera soldierCam = soldier.GetComponentInChildren<Camera>();
        if (soldierCam != null) soldierCam.gameObject.AddComponent<MereSoulsScreenEffect>();

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
        MereSoulsNetworkPlayer netPlayer = soldier.AddComponent<MereSoulsNetworkPlayer>();
        netPlayer.remoteBody = body;

        if (!Directory.Exists(PrefabFolder)) Directory.CreateDirectory(PrefabFolder);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(soldier, PlayerPrefabPath);

        // プレハブ化したらシーン上の実体は不要。接続時に NetworkManager が生成する。
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

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    /// <summary>
    /// レイヤーが無ければ TagManager に作る。8番より前は組み込みなので触らない。
    /// </summary>
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
