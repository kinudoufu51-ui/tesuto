using UnityEngine;

/// <summary>
/// 突撃兵の麻酔グレネード(企画書 §4)。投げてから少し経つと、豆戦車の麻酔ガス散布砲と
/// 同じ MiniTankGasCloud を発生させる。「範囲内に留まるほど眠る」という効き方を
/// ガジェットと車両の両方で共有することで、麻酔の挙動を一箇所に保つ。
/// </summary>
public class SedationGrenadeGadget : MonoBehaviour, IDiamondStraitsGadget
{
    public string GadgetName => "麻酔グレネード";
    public float CooldownSeconds => cooldownSeconds;

    public float cooldownSeconds = 15f;
    public float throwSpeed = 12f;
    public float fuseSeconds = 1.5f;
    public float cloudRadius = 4f;
    public float cloudDuration = 5f;
    public float sedationPerSecond = 10f;
    public float lockSeconds = 12f;

    public void Use(UniversalFPSController player)
    {
        Camera cam = player.mainCamera;
        if (cam == null) return;

        GameObject grenade = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        grenade.name = "SedationGrenade";
        grenade.transform.position = cam.transform.position + cam.transform.forward * 0.5f;
        grenade.transform.localScale = Vector3.one * 0.15f;

        Rigidbody body = grenade.AddComponent<Rigidbody>();
        body.linearVelocity = cam.transform.forward * throwSpeed;

        SedationGrenadeFuse fuse = grenade.AddComponent<SedationGrenadeFuse>();
        fuse.fuseSeconds = fuseSeconds;
        fuse.cloudRadius = cloudRadius;
        fuse.cloudDuration = cloudDuration;
        fuse.sedationPerSecond = sedationPerSecond;
        fuse.lockSeconds = lockSeconds;
    }
}
