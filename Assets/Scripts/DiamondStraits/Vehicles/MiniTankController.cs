using UnityEngine;

/// <summary>
/// 豆戦車の基礎コントローラー(企画書 §5)。歩兵の UniversalFPSController には一切依存しない、
/// 完全に独立した Rigidbody ベースの乗り物にする(技術仕様書 §6)。横転を「壊れ」ではなく
/// 「よくある一場面」として許容するため、CharacterController のような姿勢を保証する仕組みは
/// あえて持たせない — 無理に段差へ乗り上げれば普通に転ぶ。
///
/// 含まれないもの(基礎実装のスコープ外): ガンナーの麻酔ガス砲・煙幕、背面吸気口への
/// 麻酔カウンター、拠点占領によるアーケード出撃制限。いずれもガジェット/コンクエスト統合が
/// 先に必要なので、乗って・走って・転んで・押し起こすところまでをまず成立させる。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class MiniTankController : MonoBehaviour, IFPSInteractable
{
    [Header("操縦席・ガンナー席・降車位置")]
    public Transform driverSeat;
    public Transform gunnerSeat;
    public Transform exitPoint;

    [Header("走行性能")]
    public float driveForce = 3000f;
    public float turnTorque = 1500f;
    public float maxSpeed = 6.0f;

    [Header("横転判定・押し起こし")]
    [Tooltip("transform.up と world up の内積がこれを下回ると横転扱いになり、操縦を受け付けなくなる。1=直立、0=真横。")]
    public float uprightDotThreshold = 0.5f;
    public float rightingHoldDuration = 1.5f;

    public KeyCode exitKey = KeyCode.F;

    private Rigidbody body;

    private UniversalFPSController driver;
    private UniversalFPSController gunner;
    private CharacterController driverCc;
    private CharacterController gunnerCc;
    private Transform driverOriginalCameraParent;
    private Transform gunnerOriginalCameraParent;

    public bool IsFlipped => Vector3.Dot(transform.up, Vector3.up) < uprightDotThreshold;
    public bool HasDriver => driver != null;
    public bool HasGunner => gunner != null;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        FollowOccupants();

        if (driver == null || IsFlipped) return;

        float throttle = Input.GetAxis("Vertical");
        float steer = Input.GetAxis("Horizontal");

        if (body.linearVelocity.magnitude < maxSpeed)
        {
            body.AddForce(transform.forward * throttle * driveForce, ForceMode.Force);
        }
        body.AddTorque(transform.up * steer * turnTorque, ForceMode.Force);
    }

    void Update()
    {
        if (driver != null && Input.GetKeyDown(exitKey)) ExitDriver();
        if (gunner != null && Input.GetKeyDown(exitKey)) ExitGunner();
    }

    /// <summary>乗員をシートへ張り付かせる。CharacterController は物理駆動ではないので毎フレーム合わせる。</summary>
    private void FollowOccupants()
    {
        if (driver != null && driverSeat != null)
        {
            driver.transform.position = driverSeat.position;
            driver.transform.rotation = driverSeat.rotation;
        }
        if (gunner != null && gunnerSeat != null)
        {
            gunner.transform.position = gunnerSeat.position;
            gunner.transform.rotation = gunnerSeat.rotation;
        }
    }

    /// <summary>座席への搭乗。MiniTankSeat から呼ばれる。</summary>
    public void Enter(UniversalFPSController player, bool asDriver)
    {
        if (asDriver && driver != null) return;
        if (!asDriver && gunner != null) return;

        CharacterController cc = player.GetComponent<CharacterController>();
        Camera cam = player.mainCamera;
        Transform seat = asDriver ? driverSeat : gunnerSeat;

        player.enabled = false;
        if (cc != null) cc.enabled = false;

        Transform originalParent = null;
        if (cam != null && seat != null)
        {
            originalParent = cam.transform.parent;
            cam.transform.SetParent(seat, false);
            cam.transform.localPosition = Vector3.zero;
            cam.transform.localRotation = Quaternion.identity;
        }

        if (asDriver)
        {
            driver = player;
            driverCc = cc;
            driverOriginalCameraParent = originalParent;
        }
        else
        {
            gunner = player;
            gunnerCc = cc;
            gunnerOriginalCameraParent = originalParent;
        }
    }

    public void ExitDriver()
    {
        if (driver == null) return;
        RestoreCamera(driver, driverOriginalCameraParent);
        ReleaseOccupant(driver, driverCc);
        driver = null;
        driverCc = null;
        driverOriginalCameraParent = null;
    }

    public void ExitGunner()
    {
        if (gunner == null) return;
        RestoreCamera(gunner, gunnerOriginalCameraParent);
        ReleaseOccupant(gunner, gunnerCc);
        gunner = null;
        gunnerCc = null;
        gunnerOriginalCameraParent = null;
    }

    private void RestoreCamera(UniversalFPSController player, Transform originalParent)
    {
        Camera cam = player.mainCamera;
        if (cam == null) return;

        cam.transform.SetParent(originalParent, false);
        cam.transform.localPosition = Vector3.zero;
        cam.transform.localRotation = Quaternion.identity;
    }

    private void ReleaseOccupant(UniversalFPSController player, CharacterController cc)
    {
        Vector3 exitPos = exitPoint != null ? exitPoint.position : transform.position + transform.right * 2f;
        player.transform.position = exitPos;

        if (cc != null) cc.enabled = true;
        player.enabled = true;
    }

    /// <summary>
    /// 味方の手押し復帰(企画書 §5 ギミック①)。横転していない限り反応しない。
    /// IFPSInteractable として車体本体に直接乗せる(座席とは別の当たり判定)。
    /// </summary>
    public string GetInteractionPrompt() => IsFlipped ? "豆戦車を押し起こす" : "";
    public float GetHoldDuration(UniversalFPSController player) => rightingHoldDuration;
    public bool CanInteract(UniversalFPSController player) => IsFlipped;

    public void OnInteract(UniversalFPSController player)
    {
        Vector3 uprightEuler = transform.eulerAngles;
        uprightEuler.x = 0f;
        uprightEuler.z = 0f;
        transform.rotation = Quaternion.Euler(uprightEuler);
        body.angularVelocity = Vector3.zero;
        body.linearVelocity = Vector3.zero;
    }
}
