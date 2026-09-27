using UnityEngine;

/// <summary>
/// チケットと拠点の占領状況を表示するだけの簡易HUD。
/// MERE SOULS は状態を数値で見せない流儀だが、コンクエストの残りチケットは
/// 戦況把握そのものなので、ここは素直に数字で出す。
/// </summary>
public class DiamondStraitsConquestHud : MonoBehaviour
{
    public DiamondStraitsConquestManager manager;

    private GUIStyle labelStyle;

    void OnGUI()
    {
        if (manager == null) return;
        InitStyles();

        GUILayout.BeginArea(new Rect(16f, 16f, 280f, 160f));
        GUILayout.Label($"チケット A: {manager.ticketsA.Value:F0}   B: {manager.ticketsB.Value:F0}", labelStyle);

        if (manager.capturePoints != null)
        {
            foreach (DiamondStraitsCapturePoint point in manager.capturePoints)
            {
                if (point == null) continue;
                string owner = point.OwningFaction == null ? "中立" : point.OwningFaction.Value.ToString();
                GUILayout.Label($"{point.pointName}: {owner} ({point.Ownership:F2})", labelStyle);
            }
        }

        if (manager.matchOver.Value)
        {
            GUILayout.Label($"陣営 {(DiamondStraitsFaction)manager.winningFaction.Value} の勝利!", labelStyle);
        }

        GUILayout.EndArea();
    }

    private void InitStyles()
    {
        if (labelStyle != null) return;

        labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
        labelStyle.normal.textColor = Color.white;
    }
}
