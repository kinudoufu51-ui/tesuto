using UnityEngine;

/// <summary>
/// 突撃兵の煙幕手榴弾(企画書 §4)。麻酔グレネードと同じ投擲パターンを使うが、
/// 着弾点に残すのはガスではなく視界を物理的に遮る実体の球。
/// </summary>
public class SmokeGrenadeGadget : MonoBehaviour, IDiamondStraitsGadget
{
    public string GadgetName => "煙幕手榴弾";
    public float CooldownSeconds => cooldownSeconds;

    public float cooldownSeconds = 20f;
    public float throwSpeed = 12f;
    public float fuseSeconds = 1.0f;
    public float smokeRadius = 3f;
    public float smokeDuration = 12f;

    public void Use(UniversalFPSController player)
    {
        Camera cam = player.mainCamera;
        if (cam == null) return;

        GameObject grenade = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        grenade.name = "SmokeGrenade";
        grenade.transform.position = cam.transform.position + cam.transform.forward * 0.5f;
        grenade.transform.localScale = Vector3.one * 0.15f;

        Rigidbody body = grenade.AddComponent<Rigidbody>();
        body.linearVelocity = cam.transform.forward * throwSpeed;

        SmokeGrenadeFuse fuse = grenade.AddComponent<SmokeGrenadeFuse>();
        fuse.fuseSeconds = fuseSeconds;
        fuse.smokeRadius = smokeRadius;
        fuse.smokeDuration = smokeDuration;
    }
}
