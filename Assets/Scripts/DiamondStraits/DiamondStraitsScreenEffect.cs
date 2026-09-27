using UnityEngine;

/// <summary>
/// 麻酔蓄積が進むにつれて画面から色が抜け、昏睡すると完全にモノクロになり音が遠のく。
/// MereSoulsScreenEffect と同じシェーダー(_Saturation/_Darkness)を Sedation/IsAsleep で駆動する。
///
/// 彩度を落とす処理は IMGUI オーバーレイでは実現できない(描画済みの映像に重ねることしかできない)ため、
/// カメラに付けて OnRenderImage で処理する。
/// </summary>
[RequireComponent(typeof(Camera))]
public class DiamondStraitsScreenEffect : MonoBehaviour
{
    [Tooltip("この麻酔蓄積を超えると色が抜け始める。")]
    public float desaturateStartSedation = 60f;

    [Tooltip("昏睡時に音がどこまでこもるか。意識が遠のく表現であって、実際に耳が聞こえなくなるわけではない。")]
    public float sleepLowPassHz = 480f;

    public float sleepFadeSpeed = 1.6f;

    private DiamondStraitsSoldierCondition condition;
    private Material effectMaterial;
    private AudioLowPassFilter lowPass;
    private float sleepFade;

    private static readonly int SaturationId = Shader.PropertyToID("_Saturation");
    private static readonly int DarknessId = Shader.PropertyToID("_Darkness");

    void Awake()
    {
        condition = GetComponentInParent<DiamondStraitsSoldierCondition>();

        Shader shader = Resources.Load<Shader>("Shaders/MereSoulsScreenCondition");
        if (shader != null && shader.isSupported)
        {
            effectMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        if (GetComponent<AudioListener>() != null)
        {
            lowPass = GetComponent<AudioLowPassFilter>();
            if (lowPass == null) lowPass = gameObject.AddComponent<AudioLowPassFilter>();
            lowPass.cutoffFrequency = 22000f;
        }
    }

    void OnDestroy()
    {
        if (effectMaterial != null) Destroy(effectMaterial);
    }

    void Update()
    {
        if (condition == null) return;

        float target = condition.IsAsleep ? 1f : 0f;
        sleepFade = Mathf.MoveTowards(sleepFade, target, Time.deltaTime * sleepFadeSpeed);

        if (lowPass != null)
        {
            lowPass.cutoffFrequency = Mathf.Lerp(22000f, sleepLowPassHz, sleepFade);
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (effectMaterial == null || condition == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        float drowsy = Mathf.Clamp01(condition.Sedation / Mathf.Max(1f, desaturateStartSedation));
        float saturation = Mathf.Clamp01(1f - Mathf.Max(drowsy, sleepFade));
        float darkness = sleepFade * 0.6f;

        if (saturation > 0.999f && darkness < 0.001f)
        {
            Graphics.Blit(source, destination);
            return;
        }

        effectMaterial.SetFloat(SaturationId, saturation);
        effectMaterial.SetFloat(DarknessId, darkness);
        Graphics.Blit(source, destination, effectMaterial);
    }
}
