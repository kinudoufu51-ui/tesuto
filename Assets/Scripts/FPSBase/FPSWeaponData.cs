using UnityEngine;

public enum FireMode { Unarmed, SemiAuto, FullAuto, PumpOrBolt }

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "FPSBase/WeaponData")]
public class FPSWeaponData : ScriptableObject
{
    [Header("1. 基本スペック＆マガジン (BF4/Tarkov式 薬室+1対応)")]
    public string weaponName = "01_Standard_AR";
    public FireMode fireMode = FireMode.FullAuto;
    public float rpm = 650f;
    public int magCapacity = 30;
    public bool supportsChamberPlusOne = true;
    public float tacticalReloadTime = 1.65f;
    public float emptyReloadTime = 2.25f;
    public int pelletCount = 1;
    public float baseSpreadAngle = 0.15f;
    public float maxRange = 150f;

    [Header("2. 第1層：実カメラリコイル (BF4初弾倍率 × BFVバレル一致)")]
    public float firstShotMultiplier = 2.0f;
    public float realRecoilPitch = 1.2f;
    public float realRecoilYaw = 0.4f;

    [Header("3. 第2層：視覚バネリコイル (CoD: MW 2019式)")]
    public float visualKickbackZ = 0.06f;
    public float visualKickPitch = 4.5f;
    public float visualCameraRoll = 1.2f;

    [Header("4. 第3層：スマート反動回復 (Destiny 2式)")]
    public float smartRecoverySpeed = 12.0f;
    public float recoveryDelay = 0.05f;

    [Header("5. 質量・呼吸スウェイ・空間干渉 (BF1 / Ready or Not式)")]
    public float adsSpeed = 12.0f;
    public float adsFovMultiplier = 0.75f;
    public float swayWeight = 1.0f;
    public float breathSwayAmount = 0.25f;
    public float weaponLength = 0.85f;

    [ContextMenu("Preset 0: 素手・懐中電灯 (ホラー・探索用)")]
    public void ApplyPresetUnarmed()
    {
        weaponName = "00_Flashlight_Unarmed"; fireMode = FireMode.Unarmed;
        rpm = 60f; magCapacity = 0; supportsChamberPlusOne = false;
        pelletCount = 0; baseSpreadAngle = 0f;
        firstShotMultiplier = 1f; realRecoilPitch = 0f; realRecoilYaw = 0f;
        visualKickbackZ = 0f; visualKickPitch = 0f; visualCameraRoll = 0f;
        smartRecoverySpeed = 10f; adsSpeed = 8f; adsFovMultiplier = 0.9f;
        swayWeight = 1.3f; breathSwayAmount = 0.15f; weaponLength = 0.35f;
    }

    [ContextMenu("Preset 1: AR (標準ライフル - BF4/MW型)")]
    public void ApplyPresetAR()
    {
        weaponName = "01_Standard_AR"; fireMode = FireMode.FullAuto;
        rpm = 650f; magCapacity = 30; supportsChamberPlusOne = true;
        tacticalReloadTime = 1.6f; emptyReloadTime = 2.2f;
        pelletCount = 1; baseSpreadAngle = 0.15f;
        firstShotMultiplier = 2.0f; realRecoilPitch = 1.2f; realRecoilYaw = 0.4f;
        visualKickbackZ = 0.06f; visualKickPitch = 4.5f; visualCameraRoll = 1.2f;
        smartRecoverySpeed = 12.0f; recoveryDelay = 0.05f;
        adsSpeed = 12.0f; adsFovMultiplier = 0.75f;
        swayWeight = 1.0f; breathSwayAmount = 0.25f; weaponLength = 0.85f;
    }

    [ContextMenu("Preset 2: SMG (高レート - Titanfall/Apex型)")]
    public void ApplyPresetSMG()
    {
        weaponName = "02_HighRPM_SMG"; fireMode = FireMode.FullAuto;
        rpm = 900f; magCapacity = 35; supportsChamberPlusOne = true;
        tacticalReloadTime = 1.35f; emptyReloadTime = 1.85f;
        pelletCount = 1; baseSpreadAngle = 0.3f;
        firstShotMultiplier = 1.2f; realRecoilPitch = 0.6f; realRecoilYaw = 0.65f;
        visualKickbackZ = 0.035f; visualKickPitch = 2.5f; visualCameraRoll = 0.8f;
        smartRecoverySpeed = 16.0f; recoveryDelay = 0.03f;
        adsSpeed = 16.0f; adsFovMultiplier = 0.85f;
        swayWeight = 0.6f; breathSwayAmount = 0.15f; weaponLength = 0.60f;
    }

    [ContextMenu("Preset 3: HG/DMR (単発精密 - Destiny 2型)")]
    public void ApplyPresetDMR()
    {
        weaponName = "03_Tactical_DMR"; fireMode = FireMode.SemiAuto;
        rpm = 360f; magCapacity = 15; supportsChamberPlusOne = true;
        tacticalReloadTime = 1.4f; emptyReloadTime = 1.9f;
        pelletCount = 1; baseSpreadAngle = 0.05f;
        firstShotMultiplier = 1.0f; realRecoilPitch = 1.8f; realRecoilYaw = 0.2f;
        visualKickbackZ = 0.07f; visualKickPitch = 6.0f; visualCameraRoll = 1.0f;
        smartRecoverySpeed = 20.0f; recoveryDelay = 0.04f;
        adsSpeed = 14.0f; adsFovMultiplier = 0.68f;
        swayWeight = 0.75f; breathSwayAmount = 0.35f; weaponLength = 0.45f;
    }

    [ContextMenu("Preset 4: SG (ポンプ散弾 - BF1/DOOM型)")]
    public void ApplyPresetSG()
    {
        weaponName = "04_Combat_Shotgun"; fireMode = FireMode.PumpOrBolt;
        rpm = 80f; magCapacity = 8; supportsChamberPlusOne = true;
        tacticalReloadTime = 2.0f; emptyReloadTime = 2.6f;
        pelletCount = 10; baseSpreadAngle = 3.5f;
        firstShotMultiplier = 1.0f; realRecoilPitch = 5.5f; realRecoilYaw = 1.0f;
        visualKickbackZ = 0.12f; visualKickPitch = 12.0f; visualCameraRoll = 4.5f;
        smartRecoverySpeed = 6.0f; recoveryDelay = 0.15f;
        adsSpeed = 10.0f; adsFovMultiplier = 0.85f;
        swayWeight = 1.4f; breathSwayAmount = 0.20f; weaponLength = 0.90f;
    }

    [ContextMenu("Preset 5: SR (重量ボルトアクション - BF1型)")]
    public void ApplyPresetSR()
    {
        weaponName = "05_Heavy_Sniper"; fireMode = FireMode.PumpOrBolt;
        rpm = 50f; magCapacity = 5; supportsChamberPlusOne = true;
        tacticalReloadTime = 2.4f; emptyReloadTime = 3.1f;
        pelletCount = 1; baseSpreadAngle = 0.0f;
        firstShotMultiplier = 1.0f; realRecoilPitch = 8.0f; realRecoilYaw = 0.5f;
        visualKickbackZ = 0.15f; visualKickPitch = 16.0f; visualCameraRoll = 6.0f;
        smartRecoverySpeed = 3.5f; recoveryDelay = 0.25f;
        adsSpeed = 7.0f; adsFovMultiplier = 0.42f;
        swayWeight = 2.2f; breathSwayAmount = 1.15f; weaponLength = 1.15f;
    }
}
