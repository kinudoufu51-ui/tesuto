#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class FPSBaseAutoSetupEditor
{
    private static readonly string[] AssetNames = {
        "00_Flashlight_Unarmed",
        "01_Standard_AR",
        "02_HighRPM_SMG",
        "03_Tactical_DMR",
        "04_Combat_Shotgun",
        "05_Heavy_Sniper"
    };

    [MenuItem("Tools/FPS Base/⚡ 1. One-Click Build Complete Test Scene (全自動構築)")]
    public static void BuildCompleteScene()
    {
        string folderPath = "Assets/Resources/FPSWeaponPresets";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        FPSWeaponData[] weapons = new FPSWeaponData[6];

        for (int i = 0; i < 6; i++)
        {
            string assetPath = $"{folderPath}/{AssetNames[i]}.asset";
            FPSWeaponData data = AssetDatabase.LoadAssetAtPath<FPSWeaponData>(assetPath);

            // 新規作成時のみプリセット値を適用する。既存アセットがあれば、
            // Inspectorで手動調整した値を毎回のシーン再構築で上書きしないよう保持する。
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<FPSWeaponData>();
                AssetDatabase.CreateAsset(data, assetPath);
                ApplyPresetByIndex(data, i);
                EditorUtility.SetDirty(data);
            }

            weapons[i] = data;
        }
        AssetDatabase.SaveAssets();

        Camera existingCam = Object.FindFirstObjectByType<Camera>();
        if (existingCam != null && existingCam.transform.root.name != "PlayerRoot")
        {
            existingCam.gameObject.SetActive(false);
        }

        GameObject existingGym = GameObject.Find("FPSMetricGym");
        if (existingGym != null) Object.DestroyImmediate(existingGym);

        GameObject gymObj = new GameObject("FPSMetricGym");
        gymObj.AddComponent<FPSGreyboxBuilder>().BuildGym();

        GameObject existingPlayer = GameObject.Find("PlayerRoot");
        if (existingPlayer != null) Object.DestroyImmediate(existingPlayer);

        GameObject playerRoot = new GameObject("PlayerRoot");
        playerRoot.transform.position = new Vector3(0f, 1.05f, 0f);
        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        playerRoot.layer = ignoreRaycastLayer;

        CharacterController cc = playerRoot.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.38f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.stepOffset = 0.32f;

        Transform leanPivot   = CreateChildPivot("LeanPivot", playerRoot.transform, Vector3.zero);
        Transform stancePivot = CreateChildPivot("StancePivot", leanPivot, new Vector3(0f, 1.65f, 0f));
        Transform camShaker   = CreateChildPivot("CameraShaker", stancePivot, Vector3.zero);

        GameObject camObj = new GameObject("MainCamera");
        camObj.tag = "MainCamera";
        camObj.transform.SetParent(camShaker, false);
        Camera mainCam = camObj.AddComponent<Camera>();
        mainCam.nearClipPlane = 0.03f;
        mainCam.fieldOfView = 75f;
        camObj.AddComponent<AudioListener>();

        Transform weaponHolder = CreateChildPivot("WeaponHolder", camShaker, new Vector3(0.2f, -0.22f, 0.4f));
        BuildPlaceholderGunModel(weaponHolder);

        UniversalFPSController fpsCtrl = playerRoot.AddComponent<UniversalFPSController>();
        fpsCtrl.leanPivot = leanPivot;
        fpsCtrl.stancePivot = stancePivot;
        fpsCtrl.cameraShaker = camShaker;
        fpsCtrl.mainCamera = mainCam;
        fpsCtrl.weaponHolder = weaponHolder;
        fpsCtrl.weaponSlots = weapons;
        fpsCtrl.currentWeaponIndex = 1;
        fpsCtrl.environmentMask = ~(1 << ignoreRaycastLayer);

        FPSInteractionSystem interactSys = playerRoot.AddComponent<FPSInteractionSystem>();
        interactSys.interactableMask = ~(1 << ignoreRaycastLayer);

        playerRoot.AddComponent<FPSProceduralAudio>();
        playerRoot.AddComponent<FPSTelemetryAndDecals>();

        Selection.activeGameObject = playerRoot;
        Debug.Log("✅ [FPS Base Complete Edition] 全自動構築が完了しました！そのまま再生(▶)ボタンを押してください。");
    }

    [MenuItem("Tools/FPS Base/⚠ Reset All Weapon Presets to Defaults (手動調整を破棄)")]
    public static void ResetAllWeaponPresets()
    {
        if (!EditorUtility.DisplayDialog(
            "プリセットのリセット確認",
            "6種のテスト銃をすべてデフォルト値に戻します。Inspectorで手動調整した数値は失われます。よろしいですか？",
            "リセットする", "キャンセル"))
        {
            return;
        }

        string folderPath = "Assets/Resources/FPSWeaponPresets";
        for (int i = 0; i < 6; i++)
        {
            string assetPath = $"{folderPath}/{AssetNames[i]}.asset";
            FPSWeaponData data = AssetDatabase.LoadAssetAtPath<FPSWeaponData>(assetPath);
            if (data == null) continue;
            ApplyPresetByIndex(data, i);
            EditorUtility.SetDirty(data);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("✅ 6種のテスト銃をデフォルト値にリセットしました。");
    }

    private static void ApplyPresetByIndex(FPSWeaponData data, int i)
    {
        if (i == 0) data.ApplyPresetUnarmed();
        if (i == 1) data.ApplyPresetAR();
        if (i == 2) data.ApplyPresetSMG();
        if (i == 3) data.ApplyPresetDMR();
        if (i == 4) data.ApplyPresetSG();
        if (i == 5) data.ApplyPresetSR();
    }

    private static Transform CreateChildPivot(string name, Transform parent, Vector3 localPos)
    {
        GameObject go = new GameObject(name);
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        return go.transform;
    }

    private static void BuildPlaceholderGunModel(Transform weaponHolder)
    {
        GameObject receiver = GameObject.CreatePrimitive(PrimitiveType.Cube);
        receiver.name = "GunReceiver";
        receiver.transform.SetParent(weaponHolder, false);
        receiver.transform.localPosition = Vector3.zero;
        receiver.transform.localScale = new Vector3(0.06f, 0.10f, 0.38f);
        Object.DestroyImmediate(receiver.GetComponent<Collider>());

        GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barrel.name = "GunBarrel";
        barrel.transform.SetParent(weaponHolder, false);
        barrel.transform.localPosition = new Vector3(0f, 0.02f, 0.28f);
        barrel.transform.localScale = new Vector3(0.025f, 0.025f, 0.32f);
        Object.DestroyImmediate(barrel.GetComponent<Collider>());

        GameObject sight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sight.name = "FrontSightPost";
        sight.transform.SetParent(weaponHolder, false);
        sight.transform.localPosition = new Vector3(0f, 0.065f, 0.40f);
        sight.transform.localScale = new Vector3(0.008f, 0.03f, 0.01f);
        Object.DestroyImmediate(sight.GetComponent<Collider>());
    }
}
#endif
