using UnityEngine;

/// <summary>
/// 投げた麻酔グレネードの導火線。時間が来たら(見た目の破裂表現はまだ無いまま)ガス雲を残して消える。
/// </summary>
public class SedationGrenadeFuse : MonoBehaviour
{
    public float fuseSeconds = 1.5f;
    public float cloudRadius = 4f;
    public float cloudDuration = 5f;
    public float sedationPerSecond = 10f;
    public float lockSeconds = 12f;

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < fuseSeconds) return;

        GameObject cloudObj = new GameObject("SedationGasCloud");
        cloudObj.transform.position = transform.position;

        MiniTankGasCloud cloud = cloudObj.AddComponent<MiniTankGasCloud>();
        cloud.radius = cloudRadius;
        cloud.duration = cloudDuration;
        cloud.sedationPerSecond = sedationPerSecond;
        cloud.lockSeconds = lockSeconds;

        Destroy(gameObject);
    }
}
