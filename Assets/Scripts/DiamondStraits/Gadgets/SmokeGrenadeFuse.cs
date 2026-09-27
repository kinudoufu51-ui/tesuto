using UnityEngine;

/// <summary>
/// 投げた煙幕手榴弾の導火線。時間が来たら実体のある煙玉を残して消える。
///
/// 既定では通常コライダー(トリガーではない)にしている。銃弾や視線のレイキャストは
/// 既定でトリガーを無視するため、遮蔽物として機能させるには実体コライダーが必要になる。
/// 副作用として歩兵の移動そのものも塞ぐ(トリガーにすれば通り抜けられるが、その場合
/// 既定のレイキャストは煙を素通りしてしまう) — 見た目より先に「視界を遮る」ことを
/// 優先した基礎実装のトレードオフとして残している。
/// </summary>
public class SmokeGrenadeFuse : MonoBehaviour
{
    public float fuseSeconds = 1.0f;
    public float smokeRadius = 3f;
    public float smokeDuration = 12f;

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < fuseSeconds) return;

        GameObject smoke = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        smoke.name = "SmokeCloud";
        smoke.transform.position = transform.position;
        smoke.transform.localScale = Vector3.one * smokeRadius * 2f;

        Destroy(smoke, smokeDuration);
        Destroy(gameObject);
    }
}
