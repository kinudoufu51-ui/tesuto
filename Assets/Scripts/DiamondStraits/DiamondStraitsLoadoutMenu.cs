using UnityEngine;

/// <summary>
/// 「ガンデスク」の中身: 武器一覧を表示して選び直せるメニュー。DiamondStraitsGunDesk から Open() を呼ぶ想定。
///
/// Resources 以下の FPSWeaponData を全部拾って並べるだけなので、今はテスト用の6丁しかなくても、
/// 技術仕様書 §5 の48丁カタログが Resources/Weapons/DiamondStraits 以下に増えたときこのスクリプトは
/// 変更なしでそのまま拾ってくれる。兵科ごとの絞り込み(フェーズ4)は資産の置き場所を分けて
/// resourcesFolder を兵科別に切り替えるだけで対応できる。
/// </summary>
[RequireComponent(typeof(UniversalFPSController))]
public class DiamondStraitsLoadoutMenu : MonoBehaviour
{
    [Tooltip("この階層以下の FPSWeaponData を全部メニューに並べる。空なら Resources 全体から拾う。")]
    public string resourcesFolder = "";

    [Tooltip("選んだ武器を入れるスロット番号。既定の1は素体の初期装備スロットに合わせている。")]
    public int equipSlotIndex = 1;

    public KeyCode closeKey = KeyCode.Escape;

    public bool IsOpen { get; private set; }

    private UniversalFPSController controller;
    private FPSWeaponData[] catalog = new FPSWeaponData[0];
    private Vector2 scroll;
    private GUIStyle titleStyle;
    private GUIStyle entryStyle;
    private GUIStyle boxStyle;
    private Texture2D panelBg;

    void Awake()
    {
        controller = GetComponent<UniversalFPSController>();
    }

    public void Open()
    {
        if (IsOpen) return;

        catalog = Resources.LoadAll<FPSWeaponData>(resourcesFolder);

        IsOpen = true;
        controller.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        if (!IsOpen) return;

        IsOpen = false;
        controller.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Toggle()
    {
        if (IsOpen) Close(); else Open();
    }

    void Update()
    {
        if (IsOpen && Input.GetKeyDown(closeKey)) Close();
    }

    void OnGUI()
    {
        if (!IsOpen) return;
        InitStyles();

        float panelW = 420f;
        float panelH = 480f;
        Rect panelRect = new Rect((Screen.width - panelW) * 0.5f, (Screen.height - panelH) * 0.5f, panelW, panelH);
        GUI.Box(panelRect, "", boxStyle);

        GUILayout.BeginArea(panelRect);
        GUILayout.Space(10f);
        GUILayout.Label("武器ロッカー", titleStyle);
        GUILayout.Space(6f);

        FPSWeaponData current = (controller.weaponSlots != null && equipSlotIndex < controller.weaponSlots.Length)
            ? controller.weaponSlots[equipSlotIndex] : null;
        string currentName = current != null ? current.weaponName : "(なし)";
        GUILayout.Label($"現在の装備: {currentName}", entryStyle);
        GUILayout.Space(8f);

        scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(panelH - 160f));
        foreach (FPSWeaponData weapon in catalog)
        {
            if (weapon == null) continue;

            bool isEquipped = weapon == current;
            string label = isEquipped ? $"▶ {weapon.weaponName}" : weapon.weaponName;
            if (GUILayout.Button(label, GUILayout.Height(28f)))
            {
                Equip(weapon);
            }
        }
        GUILayout.EndScrollView();

        GUILayout.Space(8f);
        if (GUILayout.Button("閉じる (Esc)", GUILayout.Height(26f))) Close();
        GUILayout.EndArea();
    }

    private void Equip(FPSWeaponData weapon)
    {
        controller.SetWeaponSlot(equipSlotIndex, weapon);
        controller.currentWeaponIndex = equipSlotIndex;
    }

    private void InitStyles()
    {
        if (boxStyle != null) return;

        panelBg = new Texture2D(1, 1);
        panelBg.SetPixel(0, 0, new Color(0.05f, 0.07f, 0.1f, 0.94f));
        panelBg.Apply();

        boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = panelBg;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = Color.white;

        entryStyle = new GUIStyle(GUI.skin.label) { fontSize = 13 };
        entryStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
    }
}
