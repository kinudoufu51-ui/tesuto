#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 企画書 §6 の全48丁(4兵科×10丁＋共通サイドアーム8丁)を1クリックで生成する(技術仕様書 §5)。
///
/// 個別の反動・リロード時間などは企画書に数値が無いため、7つの「アーキタイプ」ごとに
/// 手触りのテンプレート値を1つ持ち、同じアーキタイプの武器はそれを共有する。武器ごとの個性は
/// 企画書に明記されている値(RPM・装弾数・昏睡命中数・強制リスポーン待ち・銃身長)だけで表現する。
/// これは企画書自身が「すべて FPSWeaponData アセットとして1クリック生成に対応済み」と
/// 位置づけている通り、48丁を手作業で個別チューニングするのではなく体系的に生成する方針に沿う。
/// </summary>
public static class DiamondStraitsWeaponCatalogBuilder
{
    private const string WeaponRoot = "Assets/Resources/Weapons/DiamondStraits";
    private const string ClassDataRoot = "Assets/Resources/DiamondStraits/Classes";

    private enum Archetype { AutoLight, AutoMedium, AutoHeavy, SemiPrecision, BoltHeavy, Shotgun, Sidearm }

    private struct ArchetypeStats
    {
        public float firstShotMultiplier, realRecoilPitch, realRecoilYaw;
        public float visualKickbackZ, visualKickPitch, visualCameraRoll;
        public float smartRecoverySpeed, recoveryDelay;
        public float adsSpeed, adsFovMultiplier, swayWeight, breathSwayAmount;
        public float tacticalReloadTime, emptyReloadTime, baseSpreadAngle;
    }

    private static readonly Dictionary<Archetype, ArchetypeStats> Stats = new Dictionary<Archetype, ArchetypeStats>
    {
        [Archetype.AutoLight] = new ArchetypeStats
        {
            firstShotMultiplier = 1.2f, realRecoilPitch = 0.6f, realRecoilYaw = 0.5f,
            visualKickbackZ = 0.035f, visualKickPitch = 2.5f, visualCameraRoll = 0.8f,
            smartRecoverySpeed = 15f, recoveryDelay = 0.03f,
            adsSpeed = 15f, adsFovMultiplier = 0.85f, swayWeight = 0.55f, breathSwayAmount = 0.15f,
            tacticalReloadTime = 1.35f, emptyReloadTime = 1.85f, baseSpreadAngle = 0.28f,
        },
        [Archetype.AutoMedium] = new ArchetypeStats
        {
            firstShotMultiplier = 2.0f, realRecoilPitch = 1.2f, realRecoilYaw = 0.4f,
            visualKickbackZ = 0.06f, visualKickPitch = 4.5f, visualCameraRoll = 1.2f,
            smartRecoverySpeed = 12f, recoveryDelay = 0.05f,
            adsSpeed = 12f, adsFovMultiplier = 0.75f, swayWeight = 1.0f, breathSwayAmount = 0.25f,
            tacticalReloadTime = 1.6f, emptyReloadTime = 2.2f, baseSpreadAngle = 0.15f,
        },
        [Archetype.AutoHeavy] = new ArchetypeStats
        {
            firstShotMultiplier = 0.9f, realRecoilPitch = 0.9f, realRecoilYaw = 0.35f,
            visualKickbackZ = 0.05f, visualKickPitch = 3.5f, visualCameraRoll = 1.0f,
            smartRecoverySpeed = 8f, recoveryDelay = 0.08f,
            adsSpeed = 8f, adsFovMultiplier = 0.85f, swayWeight = 1.6f, breathSwayAmount = 0.3f,
            tacticalReloadTime = 2.2f, emptyReloadTime = 2.8f, baseSpreadAngle = 0.25f,
        },
        [Archetype.SemiPrecision] = new ArchetypeStats
        {
            firstShotMultiplier = 1.0f, realRecoilPitch = 1.8f, realRecoilYaw = 0.2f,
            visualKickbackZ = 0.07f, visualKickPitch = 6.0f, visualCameraRoll = 1.0f,
            smartRecoverySpeed = 20f, recoveryDelay = 0.04f,
            adsSpeed = 14f, adsFovMultiplier = 0.68f, swayWeight = 0.75f, breathSwayAmount = 0.35f,
            tacticalReloadTime = 1.4f, emptyReloadTime = 1.9f, baseSpreadAngle = 0.05f,
        },
        [Archetype.BoltHeavy] = new ArchetypeStats
        {
            firstShotMultiplier = 1.0f, realRecoilPitch = 8.0f, realRecoilYaw = 0.5f,
            visualKickbackZ = 0.15f, visualKickPitch = 16.0f, visualCameraRoll = 6.0f,
            smartRecoverySpeed = 3.5f, recoveryDelay = 0.25f,
            adsSpeed = 7f, adsFovMultiplier = 0.42f, swayWeight = 2.2f, breathSwayAmount = 1.15f,
            tacticalReloadTime = 2.4f, emptyReloadTime = 3.1f, baseSpreadAngle = 0.0f,
        },
        [Archetype.Shotgun] = new ArchetypeStats
        {
            firstShotMultiplier = 1.0f, realRecoilPitch = 5.5f, realRecoilYaw = 1.0f,
            visualKickbackZ = 0.12f, visualKickPitch = 12.0f, visualCameraRoll = 4.5f,
            smartRecoverySpeed = 6f, recoveryDelay = 0.15f,
            adsSpeed = 10f, adsFovMultiplier = 0.85f, swayWeight = 1.4f, breathSwayAmount = 0.2f,
            tacticalReloadTime = 2.0f, emptyReloadTime = 2.6f, baseSpreadAngle = 3.5f,
        },
        [Archetype.Sidearm] = new ArchetypeStats
        {
            firstShotMultiplier = 1.0f, realRecoilPitch = 2.0f, realRecoilYaw = 0.5f,
            visualKickbackZ = 0.04f, visualKickPitch = 5.0f, visualCameraRoll = 2.0f,
            smartRecoverySpeed = 18f, recoveryDelay = 0.03f,
            adsSpeed = 16f, adsFovMultiplier = 0.8f, swayWeight = 0.5f, breathSwayAmount = 0.1f,
            tacticalReloadTime = 1.0f, emptyReloadTime = 1.4f, baseSpreadAngle = 0.1f,
        },
    };

    private struct WeaponEntry
    {
        public string name;
        public FireMode fireMode;
        public float rpm;
        public int magCapacity;
        public bool chamberPlus1;
        public int pelletCount;
        public int sedationHitsToSleep;
        public float lockSeconds;
        public float weaponLength;
        public Archetype archetype;
        public bool drowsyDebuff;

        public WeaponEntry(string name, FireMode fireMode, float rpm, int magCapacity, bool chamberPlus1,
            int pelletCount, int sedationHitsToSleep, float lockSeconds, float weaponLength,
            Archetype archetype, bool drowsyDebuff)
        {
            this.name = name; this.fireMode = fireMode; this.rpm = rpm; this.magCapacity = magCapacity;
            this.chamberPlus1 = chamberPlus1; this.pelletCount = pelletCount;
            this.sedationHitsToSleep = sedationHitsToSleep; this.lockSeconds = lockSeconds;
            this.weaponLength = weaponLength; this.archetype = archetype; this.drowsyDebuff = drowsyDebuff;
        }
    }

    // 企画書 §6-1。
    private static readonly WeaponEntry[] AssaultWeapons =
    {
        new WeaponEntry("MP 18", FireMode.FullAuto, 550, 32, false, 1, 4, 14f, 0.80f, Archetype.AutoLight, false),
        new WeaponEntry("M1897 Trench Gun", FireMode.PumpOrBolt, 120, 6, false, 9, 1, 14f, 0.95f, Archetype.Shotgun, true),
        new WeaponEntry("Fedorov Avtomat", FireMode.FullAuto, 400, 25, true, 1, 3, 17f, 1.02f, Archetype.AutoMedium, false),
        new WeaponEntry("StG 44", FireMode.FullAuto, 600, 30, true, 1, 3, 16f, 0.92f, Archetype.AutoMedium, false),
        new WeaponEntry("M1A1 Thompson", FireMode.FullAuto, 750, 30, false, 1, 4, 14f, 0.78f, Archetype.AutoLight, false),
        new WeaponEntry("AK-47 / AKM", FireMode.FullAuto, 600, 30, true, 1, 3, 18f, 0.88f, Archetype.AutoMedium, false),
        new WeaponEntry("XM177E2 Commando", FireMode.FullAuto, 780, 30, true, 1, 4, 15f, 0.70f, Archetype.AutoLight, false),
        new WeaponEntry("HK416 (M416)", FireMode.FullAuto, 800, 30, true, 1, 3, 16f, 0.76f, Archetype.AutoMedium, false),
        new WeaponEntry("AEK-971", FireMode.FullAuto, 900, 30, true, 1, 4, 15f, 0.88f, Archetype.AutoLight, false),
        new WeaponEntry("Benelli M4 (M1014)", FireMode.SemiAuto, 220, 7, true, 8, 1, 13f, 0.88f, Archetype.Shotgun, true),
    };

    // 企画書 §6-2。
    private static readonly WeaponEntry[] MedicWeapons =
    {
        new WeaponEntry("Mondragón M1908", FireMode.SemiAuto, 280, 10, false, 1, 2, 17f, 1.05f, Archetype.SemiPrecision, false),
        new WeaponEntry("C96 Trench Carbine", FireMode.SemiAuto, 450, 20, false, 1, 3, 15f, 0.62f, Archetype.AutoLight, false),
        new WeaponEntry("Winchester M1895", FireMode.PumpOrBolt, 110, 5, true, 1, 1, 19f, 0.96f, Archetype.BoltHeavy, false),
        new WeaponEntry("M1 Carbine", FireMode.SemiAuto, 450, 15, true, 1, 3, 16f, 0.78f, Archetype.SemiPrecision, false),
        new WeaponEntry("MP 40", FireMode.FullAuto, 550, 32, false, 1, 4, 14f, 0.65f, Archetype.AutoLight, false),
        new WeaponEntry("H&K MP5A3", FireMode.FullAuto, 800, 30, true, 1, 4, 14f, 0.62f, Archetype.AutoLight, false),
        new WeaponEntry("AKS-74U Krinkov", FireMode.FullAuto, 730, 30, true, 1, 3, 16f, 0.58f, Archetype.AutoLight, false),
        new WeaponEntry("FN P90", FireMode.FullAuto, 900, 50, true, 1, 4, 14f, 0.50f, Archetype.AutoLight, false),
        new WeaponEntry("KRISS Vector", FireMode.FullAuto, 1200, 25, true, 1, 4, 14f, 0.52f, Archetype.AutoLight, false),
        new WeaponEntry("AAC Honey Badger", FireMode.FullAuto, 780, 30, true, 1, 3, 16f, 0.56f, Archetype.AutoLight, false),
    };

    // 企画書 §6-3。援護兵は全丁 hasDrowsyDebuff=true(着弾時に眠気デバフを撒く)。
    private static readonly WeaponEntry[] SupportWeapons =
    {
        new WeaponEntry("Lewis Gun", FireMode.FullAuto, 550, 47, false, 1, 4, 19f, 1.05f, Archetype.AutoHeavy, true),
        new WeaponEntry("BAR M1918", FireMode.FullAuto, 600, 20, false, 1, 3, 18f, 0.98f, Archetype.AutoHeavy, true),
        new WeaponEntry("Chauchat M1915", FireMode.FullAuto, 320, 20, false, 1, 3, 20f, 1.04f, Archetype.AutoHeavy, true),
        new WeaponEntry("MG 42", FireMode.FullAuto, 1100, 100, false, 1, 4, 20f, 1.12f, Archetype.AutoHeavy, true),
        new WeaponEntry("FG 42", FireMode.FullAuto, 750, 20, false, 1, 3, 18f, 0.90f, Archetype.AutoMedium, true),
        new WeaponEntry("M60 \"The Pig\"", FireMode.FullAuto, 550, 100, false, 1, 3, 21f, 1.10f, Archetype.AutoHeavy, true),
        new WeaponEntry("RPK-74 Drum", FireMode.FullAuto, 650, 75, true, 1, 4, 18f, 0.96f, Archetype.AutoHeavy, true),
        new WeaponEntry("M249 SAW", FireMode.FullAuto, 800, 200, false, 1, 4, 19f, 0.92f, Archetype.AutoHeavy, true),
        new WeaponEntry("PKP Pecheneg", FireMode.FullAuto, 650, 100, false, 1, 3, 21f, 1.06f, Archetype.AutoHeavy, true),
        new WeaponEntry("M27 IAR", FireMode.FullAuto, 750, 45, true, 1, 3, 17f, 0.84f, Archetype.AutoMedium, true),
    };

    // 企画書 §6-4。
    private static readonly WeaponEntry[] ReconWeapons =
    {
        new WeaponEntry("SMLE Mk III", FireMode.PumpOrBolt, 68, 10, true, 1, 1, 23f, 1.10f, Archetype.BoltHeavy, false),
        new WeaponEntry("M1903 Springfield", FireMode.PumpOrBolt, 48, 5, true, 1, 1, 25f, 1.18f, Archetype.BoltHeavy, false),
        new WeaponEntry("Kar98k", FireMode.PumpOrBolt, 55, 5, true, 1, 1, 25f, 1.10f, Archetype.BoltHeavy, false),
        new WeaponEntry("ZH-29", FireMode.SemiAuto, 180, 5, true, 1, 2, 21f, 1.12f, Archetype.SemiPrecision, false),
        new WeaponEntry("De Lisle Carbine", FireMode.PumpOrBolt, 75, 8, true, 1, 1, 20f, 0.88f, Archetype.BoltHeavy, false),
        new WeaponEntry("SVD Dragunov", FireMode.SemiAuto, 240, 10, true, 1, 2, 21f, 1.20f, Archetype.SemiPrecision, false),
        new WeaponEntry("Vz.61 Skorpion", FireMode.FullAuto, 850, 20, true, 1, 6, 11f, 0.42f, Archetype.AutoLight, false),
        new WeaponEntry("Mk 14 EBR", FireMode.SemiAuto, 320, 20, true, 1, 2, 20f, 0.98f, Archetype.SemiPrecision, false),
        new WeaponEntry("L96A1 / AWM", FireMode.PumpOrBolt, 52, 5, true, 1, 1, 25f, 1.18f, Archetype.BoltHeavy, false),
        new WeaponEntry("M200 Intervention", FireMode.PumpOrBolt, 40, 7, true, 1, 1, 25f, 1.30f, Archetype.BoltHeavy, false),
    };

    // 企画書 §6-5。全兵科共通。強制リスポーン待ちは企画書に明記が無いため、
    // 拳銃としての立ち位置(主武器より短い)に合わせて仮の値を置く。
    private static readonly WeaponEntry[] Sidearms =
    {
        new WeaponEntry("Mauser C96", FireMode.SemiAuto, 300, 10, false, 1, 3, 10f, 0.35f, Archetype.Sidearm, false),
        new WeaponEntry("Webley Mk VI", FireMode.SemiAuto, 90, 6, false, 1, 3, 10f, 0.32f, Archetype.Sidearm, false),
        new WeaponEntry("Colt M1911A1", FireMode.SemiAuto, 300, 7, true, 1, 3, 10f, 0.30f, Archetype.Sidearm, false),
        new WeaponEntry("Luger P08", FireMode.SemiAuto, 300, 8, true, 1, 3, 10f, 0.30f, Archetype.Sidearm, false),
        new WeaponEntry("TT-33 Tokarev", FireMode.SemiAuto, 300, 8, true, 1, 3, 9f, 0.28f, Archetype.Sidearm, false),
        new WeaponEntry("Colt Python .357", FireMode.SemiAuto, 150, 6, false, 1, 2, 12f, 0.33f, Archetype.Sidearm, false),
        new WeaponEntry("Glock 18C", FireMode.FullAuto, 1100, 19, true, 1, 4, 9f, 0.29f, Archetype.Sidearm, false),
        new WeaponEntry("Desert Eagle .50AE", FireMode.SemiAuto, 180, 7, true, 1, 2, 13f, 0.38f, Archetype.Sidearm, false),
    };

    [MenuItem("Tools/Diamond Straits/⚡ 全48丁の武器カタログを生成")]
    public static void BuildCatalog()
    {
        EnsureFolder(WeaponRoot);
        EnsureFolder(ClassDataRoot);

        FPSWeaponData[] assault = BuildGroup(AssaultWeapons, "Assault");
        FPSWeaponData[] medic = BuildGroup(MedicWeapons, "Medic");
        FPSWeaponData[] support = BuildGroup(SupportWeapons, "Support");
        FPSWeaponData[] recon = BuildGroup(ReconWeapons, "Recon");
        FPSWeaponData[] sidearms = BuildGroup(Sidearms, "Sidearm");

        BuildClassData(SoldierClass.Assault, assault, sidearms);
        BuildClassData(SoldierClass.Medic, medic, sidearms);
        BuildClassData(SoldierClass.Support, support, sidearms);
        BuildClassData(SoldierClass.Recon, recon, sidearms);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ [Diamond Straits] 武器カタログを生成しました。" +
                  $"({assault.Length + medic.Length + support.Length + recon.Length + sidearms.Length}丁、" +
                  $"{WeaponRoot} 以下に保存)\n" +
                  "・DiamondStraitsLoadoutMenu は Resources から自動で拾うので、次にガンデスクを開くと反映されます。\n" +
                  "・4兵科分の SoldierClassData を " + ClassDataRoot + " に生成しました。" +
                  "DiamondStraitsClassSelection.debugCycleClasses に割り当てて使ってください。");
    }

    private static FPSWeaponData[] BuildGroup(WeaponEntry[] entries, string subfolder)
    {
        string folder = $"{WeaponRoot}/{subfolder}";
        EnsureFolder(folder);

        FPSWeaponData[] result = new FPSWeaponData[entries.Length];
        for (int i = 0; i < entries.Length; i++)
        {
            result[i] = BuildWeapon(entries[i], folder);
        }
        return result;
    }

    private static FPSWeaponData BuildWeapon(WeaponEntry entry, string folder)
    {
        string safeName = entry.name.Replace("/", "-");
        string assetPath = $"{folder}/{safeName}.asset";

        FPSWeaponData data = AssetDatabase.LoadAssetAtPath<FPSWeaponData>(assetPath);
        bool isNew = data == null;
        if (isNew) data = ScriptableObject.CreateInstance<FPSWeaponData>();

        ArchetypeStats stats = Stats[entry.archetype];

        data.weaponName = entry.name;
        data.fireMode = entry.fireMode;
        data.rpm = entry.rpm;
        data.magCapacity = entry.magCapacity;
        data.supportsChamberPlusOne = entry.chamberPlus1;
        data.pelletCount = entry.pelletCount;
        data.baseSpreadAngle = stats.baseSpreadAngle;
        data.maxRange = 150f;
        data.hitImpact = entry.pelletCount > 1 ? 0.3f : 1.0f;

        data.tacticalReloadTime = stats.tacticalReloadTime;
        data.emptyReloadTime = stats.emptyReloadTime;

        data.firstShotMultiplier = stats.firstShotMultiplier;
        data.realRecoilPitch = stats.realRecoilPitch;
        data.realRecoilYaw = stats.realRecoilYaw;

        data.visualKickbackZ = stats.visualKickbackZ;
        data.visualKickPitch = stats.visualKickPitch;
        data.visualCameraRoll = stats.visualCameraRoll;

        data.smartRecoverySpeed = stats.smartRecoverySpeed;
        data.recoveryDelay = stats.recoveryDelay;

        data.adsSpeed = stats.adsSpeed;
        data.adsFovMultiplier = stats.adsFovMultiplier;
        data.swayWeight = stats.swayWeight;
        data.breathSwayAmount = stats.breathSwayAmount;
        data.weaponLength = entry.weaponLength;

        data.sedationPerHit = 100f / Mathf.Max(1, entry.sedationHitsToSleep);
        data.forcedRespawnLockSeconds = entry.lockSeconds;
        data.hasDrowsyDebuff = entry.drowsyDebuff;
        data.drowsyDebuffSeconds = 3f;

        if (isNew) AssetDatabase.CreateAsset(data, assetPath);
        else EditorUtility.SetDirty(data);

        return data;
    }

    private static void BuildClassData(SoldierClass classType, FPSWeaponData[] primaryWeapons, FPSWeaponData[] sidearms)
    {
        FPSWeaponData[] combined = new FPSWeaponData[primaryWeapons.Length + sidearms.Length];
        primaryWeapons.CopyTo(combined, 0);
        sidearms.CopyTo(combined, primaryWeapons.Length);

        string assetPath = $"{ClassDataRoot}/{classType}.asset";
        SoldierClassData data = AssetDatabase.LoadAssetAtPath<SoldierClassData>(assetPath);
        bool isNew = data == null;
        if (isNew) data = ScriptableObject.CreateInstance<SoldierClassData>();

        data.classType = classType;
        data.primaryWeapons = combined;

        if (isNew) AssetDatabase.CreateAsset(data, assetPath);
        else EditorUtility.SetDirty(data);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string folderName = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }
}
#endif
