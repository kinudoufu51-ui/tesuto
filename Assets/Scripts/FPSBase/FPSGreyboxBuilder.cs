using UnityEngine;

/// <summary>
/// 設計仕様書 §7 の検証用グレーボックス(60m×80m、4ゾーン)をプロシージャルに構築する。
/// 外部アセット不要方針に合わせ、すべてプリミティブ形状のみで組み立てる。
/// エディタ専用ではない通常のMonoBehaviourとして実装(FPSBaseAutoSetupEditor から呼ばれるが、
/// プレイヤービルドに含めてもコンパイルエラーにならないようにするため)。
/// </summary>
public class FPSGreyboxBuilder : MonoBehaviour
{
    private Material groundMat;
    private Material propMat;
    private Material lockedWallMat;
    private Material interactMat;
    private Material targetMat;

    public void BuildGym()
    {
        CreateMaterials();
        BuildGround();
        BuildZoneA_VaultMantle();
        BuildZoneB_SlideAndDucts();
        BuildZoneC_LeanAndInteract();
        BuildZoneD_RecoilRange();
    }

    private void CreateMaterials()
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) lit = Shader.Find("Standard");
        if (lit == null) lit = Shader.Find("Diffuse");

        groundMat = new Material(lit) { color = new Color(0.35f, 0.35f, 0.38f) };
        propMat = new Material(lit) { color = new Color(0.55f, 0.5f, 0.42f) };
        lockedWallMat = new Material(lit) { color = new Color(0.6f, 0.15f, 0.15f) };
        interactMat = new Material(lit) { color = new Color(0.2f, 0.5f, 0.85f) };
        targetMat = new Material(lit) { color = new Color(0.9f, 0.85f, 0.2f) };
    }

    private void BuildGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground_60x80";
        ground.transform.SetParent(transform, false);
        ground.transform.localPosition = new Vector3(0f, -0.5f, 30f);
        ground.transform.localScale = new Vector3(60f, 1f, 80f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;
    }

    /// <summary>
    /// 底面(y=0)を基準にブロックを配置するヘルパー。localPos.y はブロックの「床からの高さ」。
    /// </summary>
    private GameObject CreateBlock(string name, Transform parent, Vector3 localPos, Vector3 size, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos + new Vector3(0f, size.y * 0.5f, 0f);
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    // --- ゾーンA (X=-18m): BFV式 段差乗り越え検証レーン ---
    private void BuildZoneA_VaultMantle()
    {
        Transform zone = new GameObject("ZoneA_VaultMantle").transform;
        zone.SetParent(transform, false);
        zone.localPosition = new Vector3(-18f, 0f, 0f);

        CreateBlock("01_Stair_0.30m", zone, new Vector3(0f, 0f, 4f), new Vector3(3f, 0.30f, 1.5f), propMat);
        CreateBlock("02_LowFence_0.85m_Vault", zone, new Vector3(0f, 0f, 10f), new Vector3(3f, 0.85f, 0.25f), propMat);
        CreateBlock("03_WaistBox_0.95m_TopWalk", zone, new Vector3(0f, 0f, 16f), new Vector3(3f, 0.95f, 2.5f), propMat);
        CreateBlock("04_ChestContainer_1.65m_Mantle", zone, new Vector3(0f, 0f, 24f), new Vector3(3f, 1.65f, 1.5f), propMat);
        CreateBlock("05_UnclimbableWall_2.30m_LockCheck", zone, new Vector3(0f, 0f, 32f), new Vector3(3f, 2.30f, 0.6f), lockedWallMat);
    }

    // --- ゾーンB (X=-6m): Titanfallスライディング斜面＆低天井ダクト ---
    private void BuildZoneB_SlideAndDucts()
    {
        Transform zone = new GameObject("ZoneB_SlideAndDucts").transform;
        zone.SetParent(transform, false);
        zone.localPosition = new Vector3(-6f, 0f, 0f);

        CreateBlock("01_RunupPlatform", zone, new Vector3(0f, 0f, 1f), new Vector3(4f, 3.0f, 3f), propMat);

        GameObject slope = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slope.name = "02_SlideSlope_18deg";
        slope.transform.SetParent(zone, false);
        slope.transform.localPosition = new Vector3(0f, 1.5f, 10f);
        slope.transform.localScale = new Vector3(4f, 0.4f, 14f);
        slope.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
        slope.GetComponent<Renderer>().sharedMaterial = propMat;

        CreateBlock("03_FlatRunoutArea", zone, new Vector3(0f, 0f, 22f), new Vector3(4f, 0.2f, 6f), groundMat);

        BuildDuct(zone, "04_CrouchSprintDuct_1.35m", new Vector3(0f, 0f, 30f), 1.35f, 6f);
        BuildDuct(zone, "05_ProneDuct_0.75m", new Vector3(0f, 0f, 38f), 0.75f, 6f);
    }

    private void BuildDuct(Transform parent, string name, Vector3 localPos, float clearHeight, float length)
    {
        GameObject duct = new GameObject(name);
        duct.transform.SetParent(parent, false);
        duct.transform.localPosition = localPos;

        Vector3 center = new Vector3(0f, 0f, length * 0.5f);
        CreateBlock("Duct_Ceiling", duct.transform, center + new Vector3(0f, clearHeight, 0f), new Vector3(3f, 0.15f, length), lockedWallMat);
        CreateBlock("Duct_WallL", duct.transform, center + new Vector3(-1.6f, 0f, 0f), new Vector3(0.2f, clearHeight + 0.3f, length), propMat);
        CreateBlock("Duct_WallR", duct.transform, center + new Vector3(1.6f, 0f, 0f), new Vector3(0.2f, clearHeight + 0.3f, length), propMat);
    }

    // --- ゾーンC (X=+6m): BF4自動リーンピラー＆壁干渉＆インタラクト実験室 ---
    private void BuildZoneC_LeanAndInteract()
    {
        Transform zone = new GameObject("ZoneC_LeanAndInteract").transform;
        zone.SetParent(transform, false);
        zone.localPosition = new Vector3(6f, 0f, 0f);

        CreateBlock("01_LeanPillar_L", zone, new Vector3(-1.2f, 0f, 6f), new Vector3(0.6f, 2.4f, 3f), propMat);
        CreateBlock("01_LeanPillar_R", zone, new Vector3(1.2f, 0f, 6f), new Vector3(0.6f, 2.4f, 3f), propMat);

        CreateBlock("02_WallFoldTestWall", zone, new Vector3(0f, 0f, 14f), new Vector3(4f, 2.4f, 0.3f), propMat);

        BuildDoor(zone, new Vector3(-1.5f, 0f, 20f));
        BuildSupplyCrate(zone, new Vector3(2.0f, 0f, 20f));
    }

    private void BuildDoor(Transform zone, Vector3 localPos)
    {
        CreateBlock("03_DoorFrame", zone, localPos, new Vector3(0.2f, 2.2f, 1.2f), propMat);

        GameObject doorPivot = new GameObject("03_Door");
        doorPivot.transform.SetParent(zone, false);
        doorPivot.transform.localPosition = localPos + new Vector3(0.5f, 0f, 0f);

        GameObject doorSlab = CreateBlock("DoorSlab", doorPivot.transform, new Vector3(0.5f, 0f, 0f), new Vector3(1.0f, 2.1f, 0.08f), interactMat);
        doorSlab.GetComponent<Collider>().enabled = true;

        doorPivot.AddComponent<FPSDoor>();
    }

    private void BuildSupplyCrate(Transform zone, Vector3 localPos)
    {
        GameObject crateRoot = new GameObject("04_SupplyCrate");
        crateRoot.transform.SetParent(zone, false);
        crateRoot.transform.localPosition = localPos;

        GameObject crateBaseMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crateBaseMesh.name = "CrateBase";
        crateBaseMesh.transform.SetParent(crateRoot.transform, false);
        crateBaseMesh.transform.localPosition = new Vector3(0f, 0.25f, 0f);
        crateBaseMesh.transform.localScale = new Vector3(0.8f, 0.5f, 0.8f);
        crateBaseMesh.GetComponent<Renderer>().sharedMaterial = propMat;

        // ヒンジ用ピボット(スケールなし)。ここを回転させて蓋が開く。
        GameObject lidPivot = new GameObject("LidPivot");
        lidPivot.transform.SetParent(crateRoot.transform, false);
        lidPivot.transform.localPosition = new Vector3(0f, 0.5f, -0.4f);

        GameObject lidMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lidMesh.name = "LidMesh";
        lidMesh.transform.SetParent(lidPivot.transform, false);
        lidMesh.transform.localPosition = new Vector3(0f, 0f, 0.4f);
        lidMesh.transform.localScale = new Vector3(0.82f, 0.06f, 0.8f);
        lidMesh.GetComponent<Renderer>().sharedMaterial = interactMat;

        FPSSupplyCrate crate = crateRoot.AddComponent<FPSSupplyCrate>();
        crate.lid = lidPivot.transform;

        // インタラクト用のコライダーはクレート全体(ルート)に持たせる
        BoxCollider interactCollider = crateRoot.AddComponent<BoxCollider>();
        interactCollider.center = new Vector3(0f, 0.5f, 0f);
        interactCollider.size = new Vector3(0.9f, 1.0f, 0.9f);
    }

    // --- ゾーンD (X=+18m): 5大テスト銃 リコイル計測ボード＆距離別射撃レンジ ---
    private void BuildZoneD_RecoilRange()
    {
        Transform zone = new GameObject("ZoneD_RecoilRange").transform;
        zone.SetParent(transform, false);
        zone.localPosition = new Vector3(18f, 0f, 0f);

        CreateBlock("01_RecoilBoard_10m", zone, new Vector3(0f, 0f, 10f), new Vector3(4f, 2.4f, 0.2f), groundMat);

        BuildTarget(zone, "02_Target_15m", new Vector3(0f, 1.5f, 15f));
        BuildTarget(zone, "03_Target_30m", new Vector3(0f, 1.5f, 30f));
        BuildTarget(zone, "04_Target_50m", new Vector3(0f, 1.5f, 50f));

        GameObject resetSwitch = CreateBlock("05_DecalResetSwitch", zone, new Vector3(-2.5f, 0.8f, 2f), new Vector3(0.3f, 0.3f, 0.15f), interactMat);
        resetSwitch.AddComponent<FPSDecalResetSwitch>();
    }

    private void BuildTarget(Transform parent, string name, Vector3 localPos)
    {
        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        target.name = name + "_Target";
        target.transform.SetParent(parent, false);
        target.transform.localPosition = localPos;
        target.transform.localScale = new Vector3(0.6f, 1.8f, 0.15f);
        target.GetComponent<Renderer>().sharedMaterial = targetMat;
    }
}
