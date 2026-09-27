using UnityEngine;

/// <summary>
/// 恐怖・疲労・負傷を画面だけで伝える。数値も、ゲージも、アイコンも出さない。
/// プレイヤーが自分の状態を知る手段は「視野が狭まった」「赤くにじむ」「呼吸で画面が沈む」だけ。
///
/// 外部アセット依存ゼロの方針に従い、使う画像は起動時に放射状グラデーションとして合成する。
/// ポストプロセスのパッケージも使わないので、IMGUIのオーバーレイで完結させている。
/// </summary>
[RequireComponent(typeof(FPSSoldierCondition))]
public class FPSConditionOverlay : MonoBehaviour
{
    [Range(0f, 1f)] public float maxVignetteAlpha = 0.93f;
    [Range(0f, 1f)] public float maxBloodAlpha = 0.5f;
    public float breathCycleSpeed = 2.6f;

    private FPSSoldierCondition condition;
    private Texture2D radialTex;
    private Texture2D flatTex;
    private float breathPhase;

    void Awake()
    {
        condition = GetComponent<FPSSoldierCondition>();
        radialTex = BuildRadialTexture(128, 0.34f);

        flatTex = new Texture2D(1, 1);
        flatTex.SetPixel(0, 0, Color.white);
        flatTex.Apply();
    }

    void Update()
    {
        // 息切れが強いほど呼吸が速くなる。画面の沈み込みがその周期に同期する。
        breathPhase += Time.deltaTime * breathCycleSpeed * (1f + condition.Breathlessness * 1.4f);
    }

    /// <summary>中心が透明で外周が不透明な円形グラデーション。色はGUI.color側で乗せる。</summary>
    private static Texture2D BuildRadialTexture(int size, float innerRadius)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(innerRadius, 1f, d));
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;

        float tunnel = condition.TunnelVision;
        float breathPulse = (Mathf.Sin(breathPhase) * 0.5f + 0.5f) * condition.Breathlessness * 0.22f;
        float darkness = Mathf.Clamp01(tunnel * 0.85f + breathPulse);

        if (darkness > 0.01f)
        {
            DrawVignette(new Color(0f, 0f, 0f, darkness * maxVignetteAlpha), tunnel);
        }

        if (condition.Wounded > 0.01f)
        {
            // 負傷は視界の縁に血がにじむ形で出す。狭まりではなく色で区別させる。
            DrawVignette(new Color(0.42f, 0.03f, 0.03f, condition.Wounded * maxBloodAlpha), 0f);
        }

        // 死の直前。視界から色が失せ、外周から閉じていく。
        float fading = 1f - Mathf.Clamp01(condition.Health / 25f);
        if (condition.IsAlive && fading > 0.01f)
        {
            DrawVignette(new Color(0.05f, 0.05f, 0.06f, fading * 0.8f), fading * 0.9f);
        }
    }

    /// <summary>
    /// 視野狭窄。テクスチャを画面より小さく描くほど透明な中心が縮み、視界が閉じる。
    /// 縮めるとテクスチャが画面を覆いきれなくなるので、外側は同じ色で塗り足す。
    /// </summary>
    private void DrawVignette(Color tint, float closeIn)
    {
        float scale = Mathf.Lerp(2.2f, 0.78f, Mathf.Clamp01(closeIn));
        float w = Screen.width * scale;
        float h = Screen.height * scale;
        float x = (Screen.width - w) * 0.5f;
        float y = (Screen.height - h) * 0.5f;

        Color prev = GUI.color;
        GUI.color = tint;
        GUI.DrawTexture(new Rect(x, y, w, h), radialTex);

        if (scale < 1f)
        {
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, y), flatTex);
            GUI.DrawTexture(new Rect(0f, y + h, Screen.width, Screen.height - (y + h)), flatTex);
            GUI.DrawTexture(new Rect(0f, y, x, h), flatTex);
            GUI.DrawTexture(new Rect(x + w, y, Screen.width - (x + w), h), flatTex);
        }

        GUI.color = prev;
    }
}
