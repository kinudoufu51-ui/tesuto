using System.Collections;
using UnityEngine;

public enum StanceState { Stand, Crouch, Prone }
public enum SurfaceType { Concrete, Metal, Wood, Dirt }

[RequireComponent(typeof(CharacterController))]
public class UniversalFPSController : MonoBehaviour
{
    [Header("=== 1. 階層参照 (Hierarchy) ===")]
    public Transform leanPivot;
    public Transform stancePivot;
    public Transform cameraShaker;
    public Camera mainCamera;
    public Transform weaponHolder;

    [Header("=== 2. ジャンルプリセット＆機能ON/OFFフラグ ===")]
    public bool enableSliding = true;
    public bool enableVaultMantle = true;
    public bool enableContextLean = true;
    public bool enableWallWeaponClipping = true;

    [Header("=== 3. 移動・物理パラメータ (Source / Titanfall / BFV式) ===")]
    public float walkSpeed = 4.8f;
    public float sprintSpeed = 7.2f;
    public float crouchSpeed = 2.7f;
    public float crouchSprintSpeed = 5.5f;
    public float proneSpeed = 1.4f;
    public float groundAcceleration = 55f;
    public float groundFriction = 12f;
    [Range(0f, 1f)] public float airControlRatio = 0.4f;
    public float jumpHeight = 1.25f;
    public float gravity = 22.0f;

    [Header("=== 4. スライディング＆快適化 (Titanfall / DOOM式) ===")]
    public float slideImpulseSpeed = 10.5f;
    public float slideFriction = 6.5f;
    public float coyoteTimeDuration = 0.12f;
    public float jumpBufferDuration = 0.12f;

    [Header("=== 5. 空間センサー＆レイキャスト設定 ===")]
    public LayerMask environmentMask = ~0;
    public float vaultReachDistance = 1.0f;
    public float leanProbeOffset = 0.38f;
    public float leanProbeDistance = 1.4f;
    public float leanOffsetDistance = 0.30f;
    public float leanRollAngle = 14.0f;

    [Header("=== 6. カメラ演出・呼吸・息止め (BF1式) ===")]
    public float baseFov = 75f;
    public float sprintFovAdd = 9.0f;
    public float crouchSprintFovAdd = 4.5f;
    public float headBobFrequency = 11.0f;
    public float headBobAmplitude = 0.04f;
    public float mouseSensitivity = 2.0f;
    public float maxHoldBreathDuration = 4.0f;

    [Header("=== 7. テスト銃スロット (数字キー1〜6で切替) ===")]
    public FPSWeaponData[] weaponSlots;
    public int currentWeaponIndex = 1;
    public Vector3 hipWeaponPos = new Vector3(0.2f, -0.22f, 0.4f);
    public Vector3 adsWeaponPos = new Vector3(0.0f, -0.14f, 0.35f);

    // === 8. 外部の状態システムからの補正 ===
    // 素体はこれらの数値の由来を知らない。3ステータスのようなゲーム固有の仕組みが
    // 毎フレーム書き込む前提で、素体を単体で使う場合は既定値のまま何も起きない。
    [HideInInspector] public float conditionSwayMultiplier = 1f;
    [HideInInspector] public float conditionSpeedMultiplier = 1f;
    [HideInInspector] public float conditionTremor = 0f;
    [HideInInspector] public float conditionReloadTimeMultiplier = 1f;
    [HideInInspector] public float conditionReloadFumbleChance = 0f;
    [HideInInspector] public float conditionBreathlessness = 0f;
    [HideInInspector] public Vector3 conditionWeaponPosOffset;
    [HideInInspector] public Vector3 conditionWeaponRotOffset;

    // --- イベント通知 ---
    public System.Action<Vector3, Vector3, int, bool> OnBulletHit;
    public System.Action<FPSWeaponData> OnWeaponFired;
    public System.Action<int> OnWeaponSwapped;
    public System.Action<bool> OnVaultTriggered;
    public System.Action<float> OnLanded;
    public System.Action<bool> OnReloadStarted;
    public System.Action OnReloadFumbled;

    /// <summary>被弾の唯一の入口。負傷などゲーム固有の処理はこれを購読して実装する。</summary>
    public System.Action<Vector3, float> OnDamageTaken;

    // --- 公開プロパティ (テレメトリHUD用) ---
    public StanceState CurrentStance => currentStance;
    public bool IsSprinting => isSprinting;
    public bool IsSliding => isSliding;
    public bool IsVaulting => isVaulting;
    public bool IsReloading => isReloading;
    public bool IsHoldingBreath => isHoldingBreath;
    public bool IsSupineProne => isSupineProne;
    public Vector3 HorizontalVelocity => horizontalVelocity;
    public Vector2 CurrentRealRecoil => currentRealRecoil;
    public Vector2 RecoilDebt => recoilDebt;
    public float AdsWeight => adsWeight;
    public float TargetLeanDirection => targetLeanDirection;
    public bool IsAtWallCorner => isAtWallCorner;
    public float WallObstructionRatio => wallObstructionRatio;
    public int ConsecutiveShots => consecutiveShots;
    public int CurrentAmmo => (slotAmmo != null && currentWeaponIndex < slotAmmo.Length) ? slotAmmo[currentWeaponIndex] : 0;
    public float BreathStaminaRatio => breathStamina / maxHoldBreathDuration;
    public SurfaceType CurrentSurface => currentSurface;
    public FPSWeaponData CurrentWeaponData => ActiveWeapon;

    private CharacterController cc;
    private StanceState currentStance = StanceState.Stand;
    private SurfaceType currentSurface = SurfaceType.Concrete;
    private bool isSprinting;
    private bool isSliding;
    private bool isVaulting;
    private bool isReloading;
    private bool reloadAborted;
    private bool isHoldingBreath;
    private bool isSupineProne;

    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float lastGroundedTime;
    private float lastJumpPressedTime = -10f;
    private float headBobTimer;
    private float breathTimer;
    private float breathStamina;

    private float baseCameraPitch;
    private Vector2 currentRealRecoil;
    private Vector2 recoilDebt;
    private float lastFireTime;
    private int consecutiveShots;
    private float adsWeight;
    private int[] slotAmmo;

    private float targetLeanDirection;
    private bool isAtWallCorner;
    private float wallObstructionRatio;

    private ProceduralSpring cameraLandSpring = new ProceduralSpring(140f, 16f);
    private ProceduralSpring aimPunchSpring   = new ProceduralSpring(170f, 14f);
    private ProceduralSpring weaponPosSpring  = new ProceduralSpring(180f, 20f);
    private ProceduralSpring weaponRotSpring  = new ProceduralSpring(160f, 18f);

    private FPSWeaponData ActiveWeapon =>
        (weaponSlots != null && weaponSlots.Length > 0) ? weaponSlots[Mathf.Clamp(currentWeaponIndex, 0, weaponSlots.Length - 1)] : null;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        breathStamina = maxHoldBreathDuration;
        InitializeAmmoSlots();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void InitializeAmmoSlots()
    {
        if (weaponSlots == null) return;
        slotAmmo = new int[weaponSlots.Length];
        for (int i = 0; i < weaponSlots.Length; i++)
        {
            if (weaponSlots[i] != null)
                slotAmmo[i] = weaponSlots[i].magCapacity;
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;

        HandleWeaponHotkeys();
        HandleLookAndSmartRecoil(dt);
        EvaluateSpatialSensors();

        if (!isVaulting)
        {
            HandleMovementAndStance(dt);
        }

        HandleReloadAndAimPunchTest();
        HandleWeaponFiring(dt);
        UpdateProceduralHierarchy(dt);
    }

    private void HandleLookAndSmartRecoil(float dt)
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        // Destiny 2式 スマート反動回復：手動のマウス下移動分で自動回復負債(recoilDebt)を相殺
        if (mouseY < 0f && recoilDebt.y > 0f)
        {
            float manualPullDown = -mouseY;
            float absorbed = Mathf.Min(recoilDebt.y, manualPullDown);
            recoilDebt.y -= absorbed;
            currentRealRecoil.y -= absorbed;
            baseCameraPitch = Mathf.Clamp(baseCameraPitch - (mouseY + absorbed), -88f, 88f);
        }
        else
        {
            baseCameraPitch = Mathf.Clamp(baseCameraPitch - mouseY, -88f, 88f);
        }

        // 8の字呼吸スウェイ ＆ Shift息止め
        FPSWeaponData wp = ActiveWeapon;
        float breathAmt = (wp != null ? wp.breathSwayAmount : 0.2f) * adsWeight * conditionSwayMultiplier;
        isHoldingBreath = adsWeight > 0.7f && Input.GetKey(KeyCode.LeftShift) && breathStamina > 0.1f;

        if (isHoldingBreath)
        {
            breathStamina = Mathf.Max(0f, breathStamina - dt);
            breathAmt *= 0.08f;
        }
        else
        {
            breathStamina = Mathf.Min(maxHoldBreathDuration, breathStamina + dt * 0.6f);
        }

        breathTimer += dt * 1.6f;
        float breathPitch = Mathf.Sin(breathTimer * 2.0f) * breathAmt * dt * 2.5f;
        float breathYaw   = Mathf.Cos(breathTimer) * breathAmt * dt * 2.5f;
        baseCameraPitch = Mathf.Clamp(baseCameraPitch + breathPitch, -88f, 88f);

        transform.Rotate(Vector3.up * (mouseX + breathYaw));

        float recovDelay = wp != null ? wp.recoveryDelay : 0.06f;
        float recovSpeed = wp != null ? wp.smartRecoverySpeed : 12f;

        if (Time.time > lastFireTime + recovDelay && recoilDebt.sqrMagnitude > 0.0001f)
        {
            Vector2 nextDebt = Vector2.Lerp(recoilDebt, Vector2.zero, dt * recovSpeed);
            Vector2 deltaRecovery = recoilDebt - nextDebt;
            currentRealRecoil -= deltaRecovery;
            recoilDebt = nextDebt;
        }
    }

    private void EvaluateSpatialSensors()
    {
        targetLeanDirection = 0f;
        isAtWallCorner = false;
        wallObstructionRatio = 0f;

        if (Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out RaycastHit floorHit, 0.6f, environmentMask))
        {
            string colName = floorHit.collider.name;
            if (colName.Contains("Metal") || colName.Contains("Container")) currentSurface = SurfaceType.Metal;
            else if (colName.Contains("Wood") || colName.Contains("Crate")) currentSurface = SurfaceType.Wood;
            else if (colName.Contains("Dirt")) currentSurface = SurfaceType.Dirt;
            else currentSurface = SurfaceType.Concrete;
        }

        if (mainCamera == null) return;
        Transform camT = mainCamera.transform;
        Vector3 fwd = camT.forward;
        Vector3 right = camT.right;

        bool hitCenter = Physics.Raycast(camT.position, fwd, out RaycastHit centerHit, leanProbeDistance, environmentMask);
        bool hitLeft   = Physics.Raycast(camT.position - right * leanProbeOffset, fwd, leanProbeDistance, environmentMask);
        bool hitRight  = Physics.Raycast(camT.position + right * leanProbeOffset, fwd, leanProbeDistance, environmentMask);

        if (hitCenter && (!hitLeft || !hitRight))
        {
            isAtWallCorner = true;
            if (enableContextLean && Input.GetMouseButton(1))
            {
                if (!hitLeft && !Physics.Raycast(camT.position, -right, 0.5f, environmentMask))
                    targetLeanDirection = -1f;
                else if (!hitRight && !Physics.Raycast(camT.position, right, 0.5f, environmentMask))
                    targetLeanDirection = 1f;
            }
        }

        if (enableWallWeaponClipping && !isAtWallCorner && hitCenter)
        {
            float wLen = ActiveWeapon != null ? ActiveWeapon.weaponLength : 0.8f;
            if (centerHit.distance < wLen)
            {
                wallObstructionRatio = Mathf.Clamp01(1.0f - (centerHit.distance / wLen));
            }
        }
    }

    private void HandleMovementAndStance(float dt)
    {
        bool isGrounded = cc.isGrounded;
        if (isGrounded)
        {
            if (verticalVelocity < -4.0f)
            {
                cameraLandSpring.AddImpulse(new Vector3(0f, Mathf.Max(verticalVelocity * 0.18f, -2.2f), 0f));
                OnLanded?.Invoke(Mathf.Abs(verticalVelocity));
            }
            lastGroundedTime = Time.time;
            if (verticalVelocity < 0f) verticalVelocity = -2f;
        }

        Vector2 moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        Vector3 wishDir = Vector3.ClampMagnitude(transform.right * moveInput.x + transform.forward * moveInput.y, 1f);
        bool sprintHeld = Input.GetKey(KeyCode.LeftShift) && moveInput.y > 0.1f;

        if (Input.GetButtonDown("Jump")) lastJumpPressedTime = Time.time;

        bool hasLowCeiling = CheckCeilingBlocked();

        if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftControl))
        {
            if (enableSliding && isGrounded && currentStance == StanceState.Stand &&
                horizontalVelocity.magnitude >= sprintSpeed * 0.85f)
            {
                StartSlide(wishDir);
            }
            else
            {
                StanceState prevStance = currentStance;
                if (currentStance == StanceState.Crouch && !hasLowCeiling)
                    currentStance = StanceState.Stand;
                else
                    currentStance = StanceState.Crouch;

                // BF1式の「体重が乗る」カメラ沈み込み：しゃがみ/伏せの切り替え時も
                // 着地やスライド開始と同じバネ演出を入れ、単純なLerpだけの不自然な動きを防ぐ。
                if (currentStance != prevStance)
                    cameraLandSpring.AddImpulse(new Vector3(0f, currentStance == StanceState.Crouch ? -0.12f : 0.08f, 0f));
            }
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            StanceState prevStance = currentStance;
            currentStance = (currentStance == StanceState.Prone && !hasLowCeiling) ? StanceState.Stand : StanceState.Prone;
            isSupineProne = (currentStance == StanceState.Prone) &&
                            (moveInput.y < -0.1f || Physics.Raycast(transform.position + Vector3.up * 0.5f, -transform.forward, 0.9f, environmentMask));

            if (currentStance != prevStance)
                cameraLandSpring.AddImpulse(new Vector3(0f, currentStance == StanceState.Prone ? -0.26f : 0.18f, 0f));
        }

        if (hasLowCeiling && currentStance == StanceState.Stand)
        {
            currentStance = StanceState.Crouch;
        }

        if (isSliding)
        {
            float slopeAccel = 0f;
            if (Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out RaycastHit groundHit, 0.6f, environmentMask))
            {
                Vector3 slopeDir = Vector3.ProjectOnPlane(Vector3.down, groundHit.normal).normalized;
                slopeAccel = Vector3.Dot(horizontalVelocity.normalized, slopeDir) * 14f;
            }

            float speed = horizontalVelocity.magnitude;
            speed = Mathf.Max(0f, speed + (slopeAccel - slideFriction) * dt);
            horizontalVelocity = horizontalVelocity.normalized * speed;

            if (speed <= crouchSprintSpeed || !isGrounded)
            {
                isSliding = false;
                currentStance = StanceState.Crouch;
            }
        }
        else
        {
            isSprinting = sprintHeld && !Input.GetMouseButton(1) && currentStance != StanceState.Prone;

            float targetSpeed = walkSpeed;
            if (currentStance == StanceState.Stand)
                targetSpeed = isSprinting ? sprintSpeed : walkSpeed;
            else if (currentStance == StanceState.Crouch)
                targetSpeed = isSprinting ? crouchSprintSpeed : crouchSpeed;
            else if (currentStance == StanceState.Prone)
                targetSpeed = proneSpeed;

            targetSpeed *= conditionSpeedMultiplier;

            if (isGrounded)
            {
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, wishDir * targetSpeed,
                    (wishDir.sqrMagnitude > 0.01f ? groundAcceleration : groundFriction * 4f) * dt);
            }
            else
            {
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, wishDir * targetSpeed,
                    groundAcceleration * airControlRatio * dt);
            }
        }

        bool canCoyoteJump = (Time.time - lastGroundedTime) <= coyoteTimeDuration;
        bool hasBufferedJump = (Time.time - lastJumpPressedTime) <= jumpBufferDuration;

        if (hasBufferedJump)
        {
            if (enableVaultMantle && moveInput.y > 0.2f && !Input.GetMouseButton(0) && TryExecuteVaultOrMantle())
            {
                lastJumpPressedTime = -10f;
                return;
            }
            else if (canCoyoteJump)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * 2f * gravity);
                lastJumpPressedTime = -10f;
                lastGroundedTime = -10f;
                isSliding = false;
                if (currentStance == StanceState.Prone && !hasLowCeiling) currentStance = StanceState.Crouch;
            }
        }

        verticalVelocity -= gravity * dt;
        UpdateCapsuleHeight(dt);
        cc.Move((horizontalVelocity + Vector3.up * verticalVelocity) * dt);
    }

    private void StartSlide(Vector3 dir)
    {
        isSliding = true;
        currentStance = StanceState.Crouch;
        Vector3 slideDir = dir.sqrMagnitude > 0.01f ? dir : transform.forward;
        horizontalVelocity = slideDir * Mathf.Max(horizontalVelocity.magnitude, slideImpulseSpeed);
        cameraLandSpring.AddImpulse(new Vector3(0f, -0.35f, 0f));
    }

    private bool CheckCeilingBlocked()
    {
        Vector3 origin = transform.position + Vector3.up * 1.0f;
        return Physics.SphereCast(origin, 0.3f, Vector3.up, out _, 0.75f, environmentMask);
    }

    private void UpdateCapsuleHeight(float dt)
    {
        float targetHeight = currentStance == StanceState.Stand ? 1.8f : (currentStance == StanceState.Crouch ? 1.15f : 0.6f);
        cc.height = Mathf.Lerp(cc.height, targetHeight, dt * 14f);
        cc.center = new Vector3(0f, cc.height * 0.5f, 0f);
    }

    private bool TryExecuteVaultOrMantle()
    {
        Vector3 pos = transform.position;
        Vector3 fwd = transform.forward;

        bool hitShin  = Physics.Raycast(pos + Vector3.up * 0.4f, fwd, out RaycastHit shinHit, vaultReachDistance, environmentMask);
        bool hitWaist = Physics.Raycast(pos + Vector3.up * 1.1f, fwd, out RaycastHit waistHit, vaultReachDistance, environmentMask);
        bool hitHead  = Physics.Raycast(pos + Vector3.up * 2.0f, fwd, vaultReachDistance, environmentMask);

        if ((!hitShin && !hitWaist) || hitHead) return false;

        RaycastHit wallHit = hitWaist ? waistHit : shinHit;
        if (Vector3.Angle(-wallHit.normal, fwd) > 45f) return false;

        Vector3 probeOrigin = pos + Vector3.up * 2.0f + fwd * (wallHit.distance + 0.35f);
        if (Physics.SphereCast(probeOrigin, 0.25f, Vector3.down, out RaycastHit ledgeHit, 1.7f, environmentMask))
        {
            Vector3 targetLandingPos = ledgeHit.point + Vector3.up * 0.05f;
            if (!Physics.CheckCapsule(targetLandingPos + Vector3.up * 0.35f, targetLandingPos + Vector3.up * 1.0f, 0.32f, environmentMask))
            {
                OnVaultTriggered?.Invoke(hitWaist);
                StartCoroutine(VaultRoutine(targetLandingPos, hitWaist ? 0.42f : 0.28f));
                return true;
            }
        }
        return false;
    }

    private IEnumerator VaultRoutine(Vector3 targetPos, float duration)
    {
        isVaulting = true;
        isSliding = false;
        cc.enabled = false;

        Vector3 startPos = transform.position;
        Vector3 preservedMomentum = horizontalVelocity * 0.75f;

        weaponRotSpring.AddImpulse(new Vector3(25f, -15f, -20f));
        cameraLandSpring.AddImpulse(new Vector3(0f, -0.4f, 0f));

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            Vector3 current = Vector3.Lerp(startPos, targetPos, t);
            current.y += Mathf.Sin(t * Mathf.PI) * 0.22f;
            transform.position = current;
            yield return null;
        }

        transform.position = targetPos;
        cc.enabled = true;
        horizontalVelocity = preservedMomentum;
        verticalVelocity = 0f;
        isVaulting = false;
    }

    private void HandleReloadAndAimPunchTest()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            ApplyDamageAimPunch(transform.forward, 1.0f);
        }

        FPSWeaponData wp = ActiveWeapon;
        if (wp != null && wp.fireMode != FireMode.Unarmed && !isReloading && Input.GetKeyDown(KeyCode.R))
        {
            int curAmmo = slotAmmo[currentWeaponIndex];
            int maxPossible = wp.magCapacity + (wp.supportsChamberPlusOne && curAmmo > 0 ? 1 : 0);
            if (curAmmo < maxPossible)
            {
                StartCoroutine(ReloadRoutine(wp, curAmmo > 0));
            }
        }
    }

    /// <summary>
    /// 装填は2段階に分かれ、どちらで中断されたかで結果が変わる。
    /// 前半で止まればクリップは銃に入っておらず一発も増えず、後半で止まれば半分だけ入る。
    /// 震える手ではクリップを取り落とし、拾い直す間ずっと無防備になる。
    /// </summary>
    private IEnumerator ReloadRoutine(FPSWeaponData wp, bool isTactical)
    {
        isReloading = true;
        reloadAborted = false;
        OnReloadStarted?.Invoke(isTactical);

        float duration = (isTactical ? wp.tacticalReloadTime : wp.emptyReloadTime) * conditionReloadTimeMultiplier;

        weaponPosSpring.AddImpulse(new Vector3(-0.15f, -0.55f, -0.1f));
        weaponRotSpring.AddImpulse(new Vector3(35f, -25f, 40f));

        yield return ReloadWait(duration * 0.65f);
        if (reloadAborted) { EndReload(0); yield break; }

        if (Random.value < conditionReloadFumbleChance)
        {
            OnReloadFumbled?.Invoke();
            weaponPosSpring.AddImpulse(new Vector3(0.12f, -0.35f, 0f));
            weaponRotSpring.AddImpulse(new Vector3(-32f, 22f, -38f));

            yield return ReloadWait(0.9f);
            if (reloadAborted) { EndReload(0); yield break; }
        }

        weaponPosSpring.AddImpulse(new Vector3(0.05f, 0.45f, 0.1f));
        weaponRotSpring.AddImpulse(new Vector3(-25f, 10f, -20f));
        cameraLandSpring.AddImpulse(new Vector3(0f, 0.15f, 0f));

        yield return ReloadWait(duration * 0.35f);
        if (reloadAborted) { EndReload(wp.magCapacity / 2); yield break; }

        EndReload(wp.magCapacity + (isTactical && wp.supportsChamberPlusOne ? 1 : 0));
    }

    private IEnumerator ReloadWait(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds && !reloadAborted)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>中断しても、もともと入っていた弾まで減ることはない。散らばるのは装填中のクリップだけ。</summary>
    private void EndReload(int roundsLoaded)
    {
        slotAmmo[currentWeaponIndex] = Mathf.Max(slotAmmo[currentWeaponIndex], roundsLoaded);
        isReloading = false;
        reloadAborted = false;
    }

    public void ApplyDamageAimPunch(Vector3 incomingDir, float intensity = 1.0f)
    {
        // 装填中に撃たれれば手元が飛ぶ。クリップは散らばり、入りかけの弾は失われる。
        if (isReloading) reloadAborted = true;

        float side = Vector3.Dot(transform.right, incomingDir);
        aimPunchSpring.AddImpulse(new Vector3(-18f * intensity, side * 14f * intensity, -side * 22f * intensity));
        cameraLandSpring.AddImpulse(new Vector3(0f, -0.35f * intensity, 0f));
        weaponRotSpring.AddImpulse(new Vector3(-20f * intensity, 12f * intensity, 18f * intensity));

        OnDamageTaken?.Invoke(incomingDir, intensity);
    }

    private void HandleWeaponFiring(float dt)
    {
        FPSWeaponData wp = ActiveWeapon;
        if (wp == null || wp.fireMode == FireMode.Unarmed) return;

        if (isReloading)
        {
            // 装填中に撃とうとすると、そこで切り上げてしまう。急いだ分だけ弾は入っていない。
            if (Input.GetMouseButtonDown(0)) reloadAborted = true;
            return;
        }

        if (wallObstructionRatio > 0.75f) return;

        float fireInterval = 60f / Mathf.Max(1f, wp.rpm);
        bool triggerRequested = wp.fireMode == FireMode.FullAuto
            ? Input.GetMouseButton(0)
            : Input.GetMouseButtonDown(0);

        if (triggerRequested && Time.time >= lastFireTime + fireInterval)
        {
            if (slotAmmo[currentWeaponIndex] <= 0)
            {
                StartCoroutine(ReloadRoutine(wp, false));
                return;
            }

            if (Time.time - lastFireTime > fireInterval * 1.8f)
                consecutiveShots = 0;

            slotAmmo[currentWeaponIndex]--;
            ExecuteShot(wp);
            lastFireTime = Time.time;
            consecutiveShots++;
        }
    }

    private void ExecuteShot(FPSWeaponData wp)
    {
        OnWeaponFired?.Invoke(wp);

        float firstMult = (consecutiveShots == 0) ? wp.firstShotMultiplier : 1.0f;
        float pitchKick = wp.realRecoilPitch * firstMult;
        float yawKick   = Random.Range(-wp.realRecoilYaw, wp.realRecoilYaw) * firstMult;

        currentRealRecoil.y += pitchKick;
        currentRealRecoil.x += yawKick;
        recoilDebt.y += pitchKick;
        recoilDebt.x += yawKick;

        weaponPosSpring.AddImpulse(new Vector3(0f, 0.015f, -wp.visualKickbackZ * 12f));
        float randomRoll = (Random.value > 0.5f ? 1f : -1f) * wp.visualCameraRoll * 15f;
        weaponRotSpring.AddImpulse(new Vector3(-wp.visualKickPitch * 14f, yawKick * 8f, randomRoll));

        int pellets = Mathf.Max(1, wp.pelletCount);
        for (int i = 0; i < pellets; i++)
        {
            Vector3 shotDir = mainCamera.transform.forward;
            if (wp.baseSpreadAngle > 0f)
            {
                shotDir = Quaternion.Euler(
                    Random.Range(-wp.baseSpreadAngle, wp.baseSpreadAngle),
                    Random.Range(-wp.baseSpreadAngle, wp.baseSpreadAngle), 0f) * shotDir;
            }

            if (Physics.Raycast(mainCamera.transform.position, shotDir, out RaycastHit hit, wp.maxRange, environmentMask))
            {
                Debug.DrawLine(mainCamera.transform.position, hit.point, Color.yellow, 0.25f);
                bool isTarget = hit.collider.name.Contains("Target");
                OnBulletHit?.Invoke(hit.point, hit.normal, consecutiveShots, isTarget);
            }
        }
    }

    private void UpdateProceduralHierarchy(float dt)
    {
        FPSWeaponData wp = ActiveWeapon;
        float adsSpeed = wp != null ? wp.adsSpeed : 10f;
        float swayWeight = (wp != null ? wp.swayWeight : 1.0f) * conditionSwayMultiplier;

        bool wantsAds = Input.GetMouseButton(1) && !isSprinting && !isSliding && !isReloading && wallObstructionRatio < 0.5f;
        adsWeight = Mathf.MoveTowards(adsWeight, wantsAds ? 1.0f : 0.0f, dt * adsSpeed);
        float swayDampener = Mathf.Lerp(1.0f, 0.15f, adsWeight);

        if (leanPivot != null)
        {
            Vector3 targetLeanPos = new Vector3(targetLeanDirection * leanOffsetDistance, 0f, 0f);
            Quaternion targetLeanRot = Quaternion.Euler(0f, 0f, -targetLeanDirection * leanRollAngle);
            leanPivot.localPosition = Vector3.Lerp(leanPivot.localPosition, targetLeanPos, dt * 10f);
            leanPivot.localRotation = Quaternion.Slerp(leanPivot.localRotation, targetLeanRot, dt * 10f);
        }

        if (stancePivot != null)
        {
            float targetEyeY = 1.65f;
            if (isSliding) targetEyeY = 0.85f;
            else if (currentStance == StanceState.Crouch) targetEyeY = isSprinting ? 1.12f : 1.0f;
            else if (currentStance == StanceState.Prone) targetEyeY = 0.4f;

            stancePivot.localPosition = Vector3.Lerp(stancePivot.localPosition, new Vector3(0f, targetEyeY, 0f), dt * 12f);
        }

        if (cameraShaker != null)
        {
            float moveSpeed = new Vector2(cc.velocity.x, cc.velocity.z).magnitude;
            Vector3 bobOffset = Vector3.zero;
            if (cc.isGrounded && moveSpeed > 0.5f && !isSliding)
            {
                headBobTimer += dt * headBobFrequency * (moveSpeed / walkSpeed);
                bobOffset.x = Mathf.Cos(headBobTimer * 0.5f) * headBobAmplitude * swayDampener;
                bobOffset.y = Mathf.Sin(headBobTimer) * headBobAmplitude * swayDampener;
            }

            Vector3 landOffset = cameraLandSpring.Evaluate(dt);
            Vector3 punchRot   = aimPunchSpring.Evaluate(dt);
            cameraShaker.localPosition = bobOffset + landOffset;

            float slideTilt = isSliding ? -6.0f : 0f;
            cameraShaker.localRotation = Quaternion.Euler(
                baseCameraPitch - currentRealRecoil.y + punchRot.x,
                currentRealRecoil.x + punchRot.y,
                slideTilt + punchRot.z
            );
        }

        if (mainCamera != null)
        {
            float targetFov = baseFov;
            if (isSliding || (isSprinting && currentStance == StanceState.Stand))
                targetFov += sprintFovAdd;
            else if (isSprinting && currentStance == StanceState.Crouch)
                targetFov += crouchSprintFovAdd;

            float adsFovMult = wp != null ? wp.adsFovMultiplier : 0.8f;
            targetFov = Mathf.Lerp(targetFov, baseFov * adsFovMult, adsWeight);
            mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, targetFov, dt * 10f);
        }

        if (weaponHolder != null)
        {
            Vector3 basePos = Vector3.Lerp(hipWeaponPos, adsWeaponPos, adsWeight);
            Vector3 baseRot = Vector3.zero;

            // 壁折り畳みが強く働いているほど、しゃがみダッシュ/リロードの姿勢オフセットを弱めて
            // 三者が単純加算されて銃が不自然な角度に折れるのを防ぐ(実装レビューで追加した調停ルール)。
            float otherPoseDamp = Mathf.Lerp(1f, 0.3f, wallObstructionRatio);

            if (isSprinting && currentStance == StanceState.Crouch)
            {
                basePos += new Vector3(-0.06f, -0.05f, 0f) * otherPoseDamp;
                baseRot += new Vector3(8f, -18f, 22f) * otherPoseDamp;
            }

            if (isReloading)
            {
                basePos += new Vector3(-0.08f, -0.10f, -0.05f) * otherPoseDamp;
                baseRot += new Vector3(18f, -22f, 28f) * otherPoseDamp;
            }

            // 外部状態が指定する構え(片手操作など)。他の姿勢と同じく壁干渉で減衰させる。
            basePos += conditionWeaponPosOffset * otherPoseDamp;
            baseRot += conditionWeaponRotOffset * otherPoseDamp;

            if (wallObstructionRatio > 0f)
            {
                basePos += new Vector3(-0.05f * wallObstructionRatio, -0.08f * wallObstructionRatio, -0.22f * wallObstructionRatio);
                if (wallObstructionRatio > 0.45f)
                {
                    float foldT = (wallObstructionRatio - 0.45f) / 0.55f;
                    baseRot += new Vector3(-55f * foldT, 0f, 20f * foldT);
                }
            }

            // 手の震え。銃口が定まらなくなる。規則的な揺れだと機械的に見えるので、
            // 周波数の違うノイズを軸ごとに重ねる。構えると多少は抑えられるが消えはしない。
            if (conditionTremor > 0.001f)
            {
                float t = Time.time;
                float amp = conditionTremor * Mathf.Lerp(1f, 0.55f, adsWeight);
                // 固定側の座標を格子点から外しておく。Perlinは整数格子上で値が偏ることがある。
                baseRot += new Vector3(
                    (Mathf.PerlinNoise(t * 13f, 0.37f) - 0.5f) * 9f * amp,
                    (Mathf.PerlinNoise(5.11f, t * 11f) - 0.5f) * 9f * amp,
                    (Mathf.PerlinNoise(t * 7f, t * 5f) - 0.5f) * 6f * amp);
                basePos += new Vector3(
                    (Mathf.PerlinNoise(t * 9f, 2.63f) - 0.5f) * 0.022f * amp,
                    (Mathf.PerlinNoise(8.29f, t * 10f) - 0.5f) * 0.022f * amp, 0f);
            }

            float swayX = Mathf.Clamp(-Input.GetAxisRaw("Mouse X") * 1.8f * swayWeight * swayDampener, -8f, 8f);
            float swayY = Mathf.Clamp(-Input.GetAxisRaw("Mouse Y") * 1.8f * swayWeight * swayDampener, -8f, 8f);

            weaponPosSpring.SetTarget(basePos);
            weaponRotSpring.SetTarget(baseRot + new Vector3(swayY, swayX, -swayX * 1.2f));

            weaponHolder.localPosition = weaponPosSpring.Evaluate(dt);
            weaponHolder.localRotation = Quaternion.Euler(weaponRotSpring.Evaluate(dt));
        }
    }

    public void PlayInteractHandFeedback(bool isHeavyHold)
    {
        float mult = isHeavyHold ? 1.4f : 1.0f;
        weaponPosSpring.AddImpulse(new Vector3(-0.18f, -0.45f, 0.15f) * mult);
        weaponRotSpring.AddImpulse(new Vector3(22f, -18f, 25f) * mult);
        cameraLandSpring.AddImpulse(new Vector3(0f, -0.15f * mult, 0f));
        OnVaultTriggered?.Invoke(false);
    }

    private void HandleWeaponHotkeys()
    {
        for (int i = 0; i < 6; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) && weaponSlots != null && i < weaponSlots.Length)
            {
                StopAllCoroutines();
                isReloading = false;
                isVaulting = false;
                cc.enabled = true;
                currentWeaponIndex = i;

                // 武器切替の瞬間に連射カウンタを引き継がないようリセット。
                // (旧武器の残り時間で新武器の初弾倍率/弾痕色判定が誤るのを防ぐ)
                consecutiveShots = 0;
                lastFireTime = -10f;

                weaponPosSpring.AddImpulse(new Vector3(0f, -0.8f, 0f));
                weaponRotSpring.AddImpulse(new Vector3(30f, 0f, -15f));
                OnWeaponSwapped?.Invoke(i);
            }
        }
    }
}
