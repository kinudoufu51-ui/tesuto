using System.Collections;
using UnityEngine;

/// <summary>
/// 斥候兵のグラップリングフック(企画書 §4)。狙った先まで一気に引き寄せる基礎実装。
/// ロープの物理シミュレーションはせず、素体の Vault と同じ手法(CharacterController を一時的に
/// 無効化して位置を Lerp する)で移動を実現する — 新しい移動コードを素体側に足さずに済む。
/// </summary>
public class GrapplingHookGadget : MonoBehaviour, IDiamondStraitsGadget
{
    public string GadgetName => "グラップリングフック";
    public float CooldownSeconds => cooldownSeconds;

    public float cooldownSeconds = 12f;
    public float maxRange = 20f;
    public float pullDuration = 0.6f;
    public LayerMask grappleMask = ~0;

    public void Use(UniversalFPSController player)
    {
        Camera cam = player.mainCamera;
        if (cam == null) return;

        if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, maxRange, grappleMask)) return;

        StartCoroutine(PullRoutine(player, hit.point));
    }

    private IEnumerator PullRoutine(UniversalFPSController player, Vector3 targetPoint)
    {
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc == null) yield break;

        // 着弾点はフックが引っかかった面そのものなので、足元の高さまで少し下げてから着地させる。
        Vector3 landingPoint = targetPoint - Vector3.up * (cc.height * 0.5f);
        Vector3 startPos = player.transform.position;
        float elapsed = 0f;

        cc.enabled = false;
        while (elapsed < pullDuration)
        {
            elapsed += Time.deltaTime;
            player.transform.position = Vector3.Lerp(startPos, landingPoint, elapsed / pullDuration);
            yield return null;
        }
        player.transform.position = landingPoint;
        cc.enabled = true;
    }
}
