using UnityEngine;

[RequireComponent(typeof(UniversalFPSController))]
public class FPSProceduralAudio : MonoBehaviour
{
    [Range(0f, 1f)] public float masterVolume = 0.85f;
    [Range(0f, 1f)] public float footstepVolume = 0.65f;
    [Range(0f, 1f)] public float foleyGearVolume = 0.35f;
    [Range(0f, 1f)] public float gunshotVolume = 0.90f;

    public AudioClip customFootstepClip;
    public AudioClip customGearFoleyClip;
    public AudioClip customSlideLoopClip;
    public AudioClip customHitmarkerClip;
    public AudioClip[] customGunshotClips = new AudioClip[6];

    private UniversalFPSController controller;
    private AudioSource stepSource;
    private AudioSource foleySource;
    private AudioSource slideSource;
    private AudioSource gunSource;

    private AudioClip genFootstep;
    private AudioClip genFoley;
    private AudioClip genVaultHands;
    private AudioClip genSlideLoop;
    private AudioClip genHitmarker;
    private AudioClip genIndoorTail;
    private AudioClip[] genGunshots = new AudioClip[6];

    private float stepCycleDistance;
    private int stepSide = 1;

    void Awake()
    {
        controller = GetComponent<UniversalFPSController>();
        stepSource  = CreateChildSource("Audio_Footsteps", false);
        foleySource = CreateChildSource("Audio_FoleyGear", false);
        slideSource = CreateChildSource("Audio_SlideLoop", true);
        gunSource   = CreateChildSource("Audio_WeaponFire", false);

        SynthesizeDefaultAudioClips();
        slideSource.clip = customSlideLoopClip != null ? customSlideLoopClip : genSlideLoop;
    }

    void OnEnable()
    {
        if (controller == null) return;
        controller.OnWeaponFired    += HandleWeaponFired;
        controller.OnWeaponSwapped  += HandleWeaponSwapped;
        controller.OnVaultTriggered += HandleVaultTriggered;
        controller.OnLanded         += HandleLanded;
        controller.OnBulletHit      += HandleBulletHit;
        controller.OnReloadStarted  += HandleReloadStarted;
    }

    void OnDisable()
    {
        if (controller == null) return;
        controller.OnWeaponFired    -= HandleWeaponFired;
        controller.OnWeaponSwapped  -= HandleWeaponSwapped;
        controller.OnVaultTriggered -= HandleVaultTriggered;
        controller.OnLanded         -= HandleLanded;
        controller.OnBulletHit      -= HandleBulletHit;
        controller.OnReloadStarted  -= HandleReloadStarted;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        UpdateFootstepsAndStride(dt);
        UpdateSlideFrictionSound(dt);
    }

    private void UpdateFootstepsAndStride(float dt)
    {
        float speed = controller.HorizontalVelocity.magnitude;
        bool isGrounded = controller.GetComponent<CharacterController>().isGrounded;

        if (!isGrounded || controller.IsSliding || controller.IsVaulting || speed < 0.4f) return;

        float strideLength = 1.90f;
        float volumeScale = 0.85f;
        float basePitch = 0.98f;

        if (controller.CurrentStance == StanceState.Stand && controller.IsSprinting)
        {
            strideLength = 2.45f; volumeScale = 1.35f; basePitch = 1.08f;
        }
        else if (controller.CurrentStance == StanceState.Crouch)
        {
            if (controller.IsSprinting) { strideLength = 1.55f; volumeScale = 0.95f; basePitch = 1.15f; }
            else                        { strideLength = 1.35f; volumeScale = 0.35f; basePitch = 0.88f; }
        }
        else if (controller.CurrentStance == StanceState.Prone)
        {
            strideLength = 1.10f; volumeScale = 0.45f; basePitch = 0.72f;
        }

        if (controller.CurrentSurface == SurfaceType.Metal) basePitch *= 1.22f;
        else if (controller.CurrentSurface == SurfaceType.Wood) basePitch *= 0.90f;

        stepCycleDistance += speed * dt;
        if (stepCycleDistance >= strideLength)
        {
            stepCycleDistance -= strideLength;
            stepSide = -stepSide;
            PlayFootstepAndFoley(volumeScale, basePitch);
        }
    }

    private void PlayFootstepAndFoley(float volumeMult, float basePitch)
    {
        stepSource.pitch = basePitch + (stepSide * 0.04f) + Random.Range(-0.03f, 0.03f);
        stepSource.PlayOneShot(customFootstepClip != null ? customFootstepClip : genFootstep, masterVolume * footstepVolume * volumeMult);

        foleySource.pitch = Random.Range(0.92f, 1.12f);
        foleySource.PlayOneShot(customGearFoleyClip != null ? customGearFoleyClip : genFoley, masterVolume * foleyGearVolume * volumeMult);
    }

    private void UpdateSlideFrictionSound(float dt)
    {
        if (controller.IsSliding)
        {
            if (!slideSource.isPlaying) slideSource.Play();
            float speedRatio = Mathf.Clamp01(controller.HorizontalVelocity.magnitude / 12.0f);
            slideSource.volume = Mathf.Lerp(slideSource.volume, masterVolume * 0.75f * speedRatio, dt * 12f);
            slideSource.pitch  = Mathf.Lerp(0.80f, 1.35f, speedRatio);
        }
        else if (slideSource.isPlaying)
        {
            slideSource.volume = Mathf.MoveTowards(slideSource.volume, 0f, dt * 10f);
            if (slideSource.volume <= 0.01f) slideSource.Stop();
        }
    }

    private void HandleLanded(float impactSpeed)
    {
        float intensity = Mathf.Clamp(impactSpeed / 12.0f, 0.4f, 1.6f);
        stepSource.pitch = 0.75f;
        stepSource.PlayOneShot(customFootstepClip != null ? customFootstepClip : genFootstep, masterVolume * footstepVolume * intensity);
        foleySource.pitch = 0.85f;
        foleySource.PlayOneShot(customGearFoleyClip != null ? customGearFoleyClip : genFoley, masterVolume * foleyGearVolume * intensity * 1.4f);
    }

    private void HandleVaultTriggered(bool isHighMantle)
    {
        foleySource.pitch = isHighMantle ? 0.88f : 1.12f;
        foleySource.PlayOneShot(genVaultHands, masterVolume * 0.8f);
        foleySource.PlayOneShot(customGearFoleyClip != null ? customGearFoleyClip : genFoley, masterVolume * 0.7f);
    }

    private void HandleReloadStarted(bool isTactical)
    {
        foleySource.pitch = isTactical ? 1.15f : 0.95f;
        foleySource.PlayOneShot(genVaultHands, masterVolume * 0.75f);
        foleySource.PlayOneShot(genFoley, masterVolume * 0.8f);
    }

    private void HandleWeaponSwapped(int slotIndex)
    {
        FPSWeaponData wp = controller.CurrentWeaponData;
        float weight = wp != null ? wp.swayWeight : 1.0f;
        foleySource.pitch = Mathf.Clamp(1.3f - (weight * 0.25f), 0.65f, 1.35f);
        foleySource.PlayOneShot(genVaultHands, masterVolume * 0.55f);
    }

    private void HandleWeaponFired(FPSWeaponData wp)
    {
        int idx = Mathf.Clamp(controller.currentWeaponIndex, 0, 5);
        AudioClip shotClip = (customGunshotClips != null && idx < customGunshotClips.Length && customGunshotClips[idx] != null)
            ? customGunshotClips[idx]
            : genGunshots[idx];

        if (shotClip == null) return;

        gunSource.pitch = Random.Range(0.96f, 1.04f);
        gunSource.PlayOneShot(shotClip, masterVolume * gunshotVolume);

        float indoorEnclosure = CalculateIndoorEnclosure();
        if (indoorEnclosure > 0.25f)
        {
            gunSource.PlayOneShot(genIndoorTail, masterVolume * gunshotVolume * indoorEnclosure * 0.75f);
        }
    }

    private float CalculateIndoorEnclosure()
    {
        Vector3 origin = transform.position + Vector3.up * 1.5f;
        int hitCount = 0;
        if (Physics.Raycast(origin, Vector3.up, 6.0f, controller.environmentMask)) hitCount++;
        if (Physics.Raycast(origin, -transform.right, 5.0f, controller.environmentMask)) hitCount++;
        if (Physics.Raycast(origin, transform.right, 5.0f, controller.environmentMask)) hitCount++;
        return hitCount / 3.0f;
    }

    private void HandleBulletHit(Vector3 point, Vector3 normal, int shotIndex, bool isTarget)
    {
        if (isTarget)
        {
            gunSource.PlayOneShot(customHitmarkerClip != null ? customHitmarkerClip : genHitmarker, masterVolume * 0.7f);
        }
    }

    private void SynthesizeDefaultAudioClips()
    {
        int sr = 44100;
        genFootstep = CreateProceduralClip("Synth_Footstep", 0.14f, sr, (t, norm) =>
        {
            float env = Mathf.Exp(-norm * 24f);
            float thump = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(130f, 42f, norm) * t);
            float grit = (Random.value * 2f - 1f) * 0.35f * Mathf.Exp(-norm * 35f);
            return (thump * 0.8f + grit) * env;
        });

        genFoley = CreateProceduralClip("Synth_Foley", 0.18f, sr, (t, norm) =>
        {
            float env = Mathf.Sin(norm * Mathf.PI) * Mathf.Exp(-norm * 6f);
            return (Random.value * 2f - 1f) * 0.25f * env;
        });

        genVaultHands = CreateProceduralClip("Synth_VaultHands", 0.20f, sr, (t, norm) =>
        {
            float env = Mathf.Exp(-norm * 18f);
            return (Mathf.Sin(2f * Mathf.PI * 95f * t) * 0.6f + (Random.value * 2f - 1f) * 0.4f) * env;
        });

        genSlideLoop = CreateProceduralClip("Synth_SlideLoop", 1.0f, sr, (t, norm) =>
        {
            return (Random.value * 2f - 1f) * 0.3f * (0.8f + 0.2f * Mathf.Sin(2f * Mathf.PI * 6f * t));
        });

        genHitmarker = CreateProceduralClip("Synth_Hitmarker", 0.07f, sr, (t, norm) =>
        {
            float env = Mathf.Exp(-norm * 45f);
            return (Mathf.Sin(2f * Mathf.PI * 2400f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 3800f * t) * 0.4f) * env;
        });

        genIndoorTail = CreateProceduralClip("Synth_IndoorTail", 0.65f, sr, (t, norm) =>
        {
            float env = Mathf.Exp(-norm * 5.5f) * (1f - Mathf.Exp(-norm * 30f));
            return (Random.value * 2f - 1f) * 0.45f * env;
        });

        genGunshots[0] = genHitmarker;
        genGunshots[1] = CreateGunshotClip("Synth_AR",  0.26f, sr, 160f, 16f, 1.0f);
        genGunshots[2] = CreateGunshotClip("Synth_SMG", 0.16f, sr, 230f, 26f, 0.8f);
        genGunshots[3] = CreateGunshotClip("Synth_DMR", 0.30f, sr, 190f, 14f, 1.1f);
        genGunshots[4] = CreateGunshotClip("Synth_SG",  0.45f, sr, 85f,  8f,  1.4f);
        genGunshots[5] = CreateGunshotClip("Synth_SR",  0.60f, sr, 65f,  5.5f, 1.6f);
    }

    private AudioClip CreateGunshotClip(string clipName, float duration, int sr, float startFreq, float decayRate, float punchGain)
    {
        return CreateProceduralClip(clipName, duration, sr, (t, norm) =>
        {
            float punch = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(startFreq, 30f, norm) * t) * Mathf.Exp(-norm * decayRate * 2.2f) * punchGain;
            float noise = (Random.value * 2f - 1f) * Mathf.Exp(-norm * decayRate) * 0.85f;
            return Mathf.Clamp(punch + noise, -1f, 1f);
        });
    }

    private AudioClip CreateProceduralClip(string clipName, float duration, int sampleRate, System.Func<float, float, float> generator)
    {
        int sampleCount = Mathf.Max(1, Mathf.RoundToInt(duration * sampleRate));
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            samples[i] = Mathf.Clamp(generator((float)i / sampleRate, (float)i / sampleCount), -1f, 1f);
        }
        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioSource CreateChildSource(string objName, bool loop)
    {
        GameObject child = new GameObject(objName);
        child.transform.SetParent(this.transform);
        child.transform.localPosition = Vector3.zero;
        AudioSource src = child.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        src.spatialBlend = 0.0f;
        return src;
    }
}
