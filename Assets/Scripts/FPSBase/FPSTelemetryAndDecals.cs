using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UniversalFPSController))]
public class FPSTelemetryAndDecals : MonoBehaviour
{
    public int maxDecals = 120;
    public float decalSize = 0.07f;
    public Color firstShotColor = new Color(1.0f, 0.2f, 0.2f);
    public Color burstShotColor = new Color(0.1f, 0.9f, 1.0f);
    public bool drawRecoilPatternLines = true;
    public bool showTelemetryHUD = true;

    private UniversalFPSController controller;

    private class DecalEntry
    {
        public GameObject markerObj;
        public Renderer rend;
        public LineRenderer line;
    }
    private List<DecalEntry> decalPool = new List<DecalEntry>();
    private int currentPoolIndex = 0;
    private Vector3 lastBurstHitPoint;
    private Transform decalContainer;

    private float hitMarkerAlpha = 0f;
    private float peakHorizontalSpeed = 0f;
    private GUIStyle boxStyle;
    private GUIStyle labelStyle;
    private GUIStyle headerStyle;
    private Texture2D whiteTex;

    void Awake()
    {
        controller = GetComponent<UniversalFPSController>();
        decalContainer = new GameObject("00_DebugDecalPool").transform;
        InitializeDecalPool();

        whiteTex = new Texture2D(1, 1);
        whiteTex.SetPixel(0, 0, Color.white);
        whiteTex.Apply();
    }

    void OnEnable()
    {
        if (controller != null) controller.OnBulletHit += HandleBulletHit;
    }

    void OnDisable()
    {
        if (controller != null) controller.OnBulletHit -= HandleBulletHit;
    }

    void Update()
    {
        float currentSpeed = controller.HorizontalVelocity.magnitude;
        if (currentSpeed > peakHorizontalSpeed) peakHorizontalSpeed = currentSpeed;

        if (Input.GetKeyDown(KeyCode.B))
        {
            ClearAllDecals();
            peakHorizontalSpeed = 0f;
        }

        if (Input.GetKeyDown(KeyCode.F1))
        {
            showTelemetryHUD = !showTelemetryHUD;
        }

        if (hitMarkerAlpha > 0f)
        {
            hitMarkerAlpha = Mathf.MoveTowards(hitMarkerAlpha, 0f, Time.deltaTime * 4.5f);
        }
    }

    private void InitializeDecalPool()
    {
        Shader unlitShader = Shader.Find("Unlit/Color");
        if (unlitShader == null) unlitShader = Shader.Find("Standard");

        for (int i = 0; i < maxDecals; i++)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = $"BulletDecal_{i:000}";
            sphere.transform.SetParent(decalContainer);
            Destroy(sphere.GetComponent<Collider>());

            Renderer r = sphere.GetComponent<Renderer>();
            r.material = new Material(unlitShader);

            LineRenderer lr = sphere.AddComponent<LineRenderer>();
            lr.material = new Material(unlitShader);
            lr.material.color = new Color(1f, 0.9f, 0.2f, 0.8f);
            lr.startWidth = 0.012f;
            lr.endWidth = 0.012f;
            lr.positionCount = 2;
            lr.enabled = false;

            sphere.SetActive(false);
            decalPool.Add(new DecalEntry { markerObj = sphere, rend = r, line = lr });
        }
    }

    private void HandleBulletHit(Vector3 point, Vector3 normal, int shotIndex, bool isTarget)
    {
        DecalEntry entry = decalPool[currentPoolIndex];
        currentPoolIndex = (currentPoolIndex + 1) % maxDecals;

        entry.markerObj.SetActive(true);
        entry.markerObj.transform.position = point + normal * 0.01f;
        entry.markerObj.transform.rotation = Quaternion.LookRotation(normal);
        entry.markerObj.transform.localScale = new Vector3(decalSize, decalSize, decalSize * 0.25f);

        bool isFirstShot = (shotIndex == 0);
        entry.rend.material.color = isFirstShot ? firstShotColor : burstShotColor;

        FPSWeaponData wp = controller.CurrentWeaponData;
        bool isSinglePellet = (wp == null || wp.pelletCount <= 1);

        if (drawRecoilPatternLines && !isFirstShot && isSinglePellet && Vector3.Distance(lastBurstHitPoint, point) < 3.0f)
        {
            entry.line.enabled = true;
            entry.line.SetPosition(0, lastBurstHitPoint + normal * 0.015f);
            entry.line.SetPosition(1, point + normal * 0.015f);
        }
        else
        {
            entry.line.enabled = false;
        }

        lastBurstHitPoint = point;
        if (isTarget) hitMarkerAlpha = 1.0f;
    }

    public void ClearAllDecals()
    {
        foreach (var d in decalPool)
        {
            d.markerObj.SetActive(false);
            d.line.enabled = false;
        }
    }

    void OnGUI()
    {
        InitStyles();
        DrawCenterCrosshairAndHitMarker();

        if (!showTelemetryHUD) return;

        GUILayout.BeginArea(new Rect(16, 16, 405, 610), boxStyle);

        GUILayout.Label("■ FPS BASE COMPLETE TELEMETRY [F1:HUD / B:弾痕消去]", headerStyle);
        GUILayout.Space(6);

        string moveState = GetActiveMovementStateLabel();
        float curSpeed = controller.HorizontalVelocity.magnitude;
        GUILayout.Label($"【1. 移動・姿勢・材質 (BFV × Titanfall)】", headerStyle);
        GUILayout.Label($"  現在ステート : <b><color=#44FF88>{moveState}</color></b> (材質: {controller.CurrentSurface})", labelStyle);
        GUILayout.Label($"  水平移動速度 : <b>{curSpeed:F2} m/s</b>  (最高記録: {peakHorizontalSpeed:F2} m/s)", labelStyle);
        DrawBar(curSpeed / 10.5f, new Color(0.2f, 0.8f, 1.0f));
        GUILayout.Space(6);

        GUILayout.Label($"【2. 空間センサー調停 (BF4自動リーン vs 壁干渉)】", headerStyle);
        string leanText = controller.TargetLeanDirection < -0.1f ? "<color=#FFCC00>◀ 左自動リーン発動中</color>" :
                          controller.TargetLeanDirection >  0.1f ? "<color=#FFCC00>▶ 右自動リーン発動中</color>" : "中央 (なし)";
        string cornerLock = controller.IsAtWallCorner ? "<color=#44FF88>ON (角検知：銃の壁引き戻しをロック中)</color>" : "OFF";

        GUILayout.Label($"  遮蔽物エッジ : {cornerLock}", labelStyle);
        GUILayout.Label($"  自動リーン   : <b>{leanText}</b>  / 壁折り畳み: <b>{(controller.WallObstructionRatio * 100f):F0}%</b>", labelStyle);
        DrawBar(controller.WallObstructionRatio, new Color(1.0f, 0.4f, 0.3f));
        GUILayout.Space(6);

        FPSWeaponData wp = controller.CurrentWeaponData;
        string wpName = wp != null ? wp.weaponName : "None";
        int maxMag = wp != null ? wp.magCapacity : 0;
        string ammoStr = controller.IsReloading ? "<color=#FFCC00>RELOADING...</color>" : $"<b>{controller.CurrentAmmo}</b> / {maxMag} {(controller.CurrentAmmo > maxMag ? "<color=#44FF88>(+1薬室)</color>" : "")}";

        GUILayout.Label($"【3. テスト銃・三層リコイル・薬室+1 (MW × Destiny 2)】", headerStyle);
        GUILayout.Label($"  装備スロット : <b><color=#FFDD44>[{controller.currentWeaponIndex + 1}] {wpName}</color></b>", labelStyle);
        GUILayout.Label($"  残弾 / 装填  : {ammoStr}  [R:リロード / H:被弾パンチ]", labelStyle);
        GUILayout.Label($"  ADS & 息止め : ADS {(controller.AdsWeight * 100f):F0}% / 息止めスタミナ {(controller.BreathStaminaRatio * 100f):F0}%", labelStyle);
        GUILayout.Label($"  実リコイル角 : Pitch <b>{controller.CurrentRealRecoil.y:F2}°</b> / Yaw <b>{controller.CurrentRealRecoil.x:F2}°</b>", labelStyle);
        GUILayout.Label($"  スマート回復負債 (Recoil Debt): <b><color=#FF8844>{controller.RecoilDebt.y:F2}°</color></b>", labelStyle);
        DrawBar(controller.RecoilDebt.y / 12.0f, new Color(1.0f, 0.55f, 0.2f));

        GUILayout.Space(6);
        GUILayout.Label("<color=#AAAAAA>[1]〜[6]:銃切替 / [C]:スライド・しゃがみ / [Z]:伏せ / [E]:操作</color>", labelStyle);

        GUILayout.EndArea();
    }

    private string GetActiveMovementStateLabel()
    {
        if (controller.IsVaulting) return "VAULT / MANTLE (段差乗り越え中)";
        if (controller.IsSliding) return "SLIDING (Titanfall式 運動量スライド)";
        if (controller.CurrentStance == StanceState.Prone)
            return controller.IsSupineProne ? "PRONE - SUPINE (BFV式 仰向け伏せ)" : "PRONE (通常うつ伏せ)";
        if (controller.CurrentStance == StanceState.Crouch)
            return controller.IsSprinting ? "CROUCH SPRINT (BFV式 しゃがみダッシュ!)" : "CROUCH WALK (しゃがみ歩き)";
        return controller.IsSprinting ? "STANDING SPRINT (立ちダッシュ)" : "STANDING WALK / IDLE";
    }

    private void DrawBar(float normalizedValue, Color barColor)
    {
        Rect r = GUILayoutUtility.GetRect(365, 12);
        GUI.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        GUI.DrawTexture(r, whiteTex);
        Rect fill = new Rect(r.x + 1, r.y + 1, Mathf.Clamp01(normalizedValue) * (r.width - 2), r.height - 2);
        GUI.color = barColor;
        GUI.DrawTexture(fill, whiteTex);
        GUI.color = Color.white;
    }

    private void DrawCenterCrosshairAndHitMarker()
    {
        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;
        float spreadGap = Mathf.Lerp(10f + controller.HorizontalVelocity.magnitude * 1.5f, 2f, controller.AdsWeight);
        GUI.color = new Color(1f, 1f, 1f, 1f - controller.AdsWeight * 0.7f);

        GUI.DrawTexture(new Rect(cx - spreadGap - 6f, cy - 1f, 6f, 2f), whiteTex);
        GUI.DrawTexture(new Rect(cx + spreadGap, cy - 1f, 6f, 2f), whiteTex);
        GUI.DrawTexture(new Rect(cx - 1f, cy - spreadGap - 6f, 2f, 6f), whiteTex);
        GUI.DrawTexture(new Rect(cx - 1f, cy + spreadGap, 2f, 6f), whiteTex);

        if (hitMarkerAlpha > 0f)
        {
            GUI.color = new Color(1f, 0.25f, 0.25f, hitMarkerAlpha);
            float hmOffset = 8f;
            GUI.DrawTexture(new Rect(cx - hmOffset - 4f, cy - hmOffset - 4f, 5f, 5f), whiteTex);
            GUI.DrawTexture(new Rect(cx + hmOffset - 1f, cy - hmOffset - 4f, 5f, 5f), whiteTex);
            GUI.DrawTexture(new Rect(cx - hmOffset - 4f, cy + hmOffset - 1f, 5f, 5f), whiteTex);
            GUI.DrawTexture(new Rect(cx + hmOffset - 1f, cy + hmOffset - 1f, 5f, 5f), whiteTex);
        }
        GUI.color = Color.white;
    }

    private void InitStyles()
    {
        if (boxStyle != null) return;
        boxStyle = new GUIStyle(GUI.skin.box);
        Texture2D bg = new Texture2D(1, 1);
        bg.SetPixel(0, 0, new Color(0.05f, 0.07f, 0.1f, 0.84f));
        bg.Apply();
        boxStyle.normal.background = bg;
        boxStyle.padding = new RectOffset(12, 12, 10, 10);

        labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, richText = true };
        labelStyle.normal.textColor = new Color(0.92f, 0.92f, 0.92f);

        headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, richText = true };
        headerStyle.normal.textColor = new Color(0.6f, 0.85f, 1.0f);
    }
}
