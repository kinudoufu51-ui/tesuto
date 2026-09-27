using UnityEngine;

/// <summary>
/// 昏睡中の兵士の頭上に "Zzz..." を出す。数値のHPバーを一切出さないこの企画で、
/// 「眠っている」という唯一の合図をプレイヤーに伝える表示。
/// </summary>
[RequireComponent(typeof(DiamondStraitsSoldierCondition))]
public class DiamondStraitsZzzOverlay : MonoBehaviour
{
    public Vector3 headOffset = new Vector3(0f, 2.0f, 0f);
    public float bobAmplitude = 0.15f;
    public float bobSpeed = 1.6f;

    private DiamondStraitsSoldierCondition condition;
    private GUIStyle style;

    void Awake()
    {
        condition = GetComponent<DiamondStraitsSoldierCondition>();
    }

    void OnGUI()
    {
        if (condition == null || !condition.IsAsleep) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 worldPos = transform.position + headOffset + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
        if (screenPos.z <= 0f) return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor = new Color(0.75f, 0.85f, 1f);
        }

        float guiY = Screen.height - screenPos.y;
        GUI.Label(new Rect(screenPos.x - 40f, guiY - 20f, 80f, 40f), "Zzz...", style);
    }
}
