using UnityEngine;

/// <summary>
/// 体力が減るにつれて画面から色が抜け、死ぬとモノクロになり音が遠のく (GDD §11)。
///
/// 彩度を落とす処理だけは IMGUI のオーバーレイでは実現できない。オーバーレイは既に描かれた
/// 映像の上に重ねることしかできないため、描画後の画像そのものを変換する必要がある。
/// カメラに付けて OnRenderImage で処理する。
/// </summary>
[RequireComponent(typeof(Camera))]
public class MereSoulsScreenEffect : MonoBehaviour
{
    [Tooltip("この体力を下回ると色が抜け始める。満タンから徐々に褪せるより、追い詰められてから効くほうが伝わる。")]
    public float desaturateStartHealth = 60f;

    [Tooltip("死亡時に音がどこまでこもるか。実際に耳が遠くなるのではなく、意識が遠のく表現。")]
    public float deathLowPassHz = 480f;

    public float deathFadeSpeed = 1.6f;

    private FPSSoldierCondition condition;
    private Material effectMaterial;
    private AudioLowPassFilter lowPass;
    private float deathFade;

    private static readonly int SaturationId = Shader.PropertyToID("_Saturation");
    private static readonly int DarknessId = Shader.PropertyToID("_Darkness");

    void Awake()
    {
        condition = GetComponentInParent<FPSSoldierCondition>();

        Shader shader = Resources.Load<Shader>("Shaders/MereSoulsScreenCondition");
        if (shader != null && shader.isSupported)
        {
            effectMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        // 音のこもりは AudioListener と同じ GameObject に付いていないと効かない。
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

        // 死は一瞬で切り替えず、意識が落ちていく速さで進める。
        float target = condition.IsAlive ? 0f : 1f;
        deathFade = Mathf.MoveTowards(deathFade, target, Time.deltaTime * deathFadeSpeed);

        if (lowPass != null)
        {
            lowPass.cutoffFrequency = Mathf.Lerp(22000f, deathLowPassHz, deathFade);
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (effectMaterial == null || condition == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        float wounded = 1f - Mathf.Clamp01(condition.Health / Mathf.Max(1f, desaturateStartHealth));
        float saturation = Mathf.Clamp01(1f - Mathf.Max(wounded, deathFade));
        float darkness = deathFade * 0.6f;

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
