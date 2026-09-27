# Diamond Straits: Skirmish — 技術仕様書 (v2.0)

企画書（`diamond_straits_skirmish_企画書.md`）で定めた設計を、既存の `FPSBase` / `MereSouls` コードベースにどう落とし込むかを定める。素体（FPSBase）は今回もゲーム固有の知識を持たない。麻酔・蘇生・陣営・拠点はすべてゲーム層（新設する `DiamondStraits` レイヤー）に実装し、素体へは既存の条件付き修正フィールド（`conditionSwayMultiplier` 等）とダメージの単一入口（`OnDamageTaken`）を通して書き込むだけにする。

## 0. 方針

* **死亡はない。あるのは「昏睡」だけ。** `FPSSoldierCondition` の Wounded/Health による死亡フローを、麻酔蓄積 0〜100% による昏睡フローに置き換える。100% に達した瞬間はダメージ処理と同じ `OnDamageTaken` 経由で検知できるので、素体側の変更は最小限で済む。
* **武器は全部同じ「弾」を撃つ。** 弾速・射程・命中判定は既存の `FPSWeaponData` / レイキャストをそのまま使う。銃ごとの違いは「1発あたりの麻酔蓄積量」「強制リスポーン待ち秒数」という新しいデータ2列に集約する。BF系のTTK設計をそのまま「昏睡までの命中数」に転用できる。
* **兵科は装備プリセット＋ガジェットスロットであって、コントローラー側の別クラスではない。** `UniversalFPSController` は兵科を知らない。兵科ごとの武器リストとガジェット3種を持つ `SoldierClassData` (ScriptableObject) をプレイヤーに割り当てるだけ。

## 1. 麻酔システム（素体との接続）

### 1.1 新規フィールド：`FPSWeaponData.cs`

```csharp
[Header("6. 麻酔仕様 (Diamond Straits)")]
[Tooltip("この武器の1発が与える麻酔蓄積(0-100換算)。昏睡命中数から逆算する。")]
public float sedationPerHit = 25f;
[Tooltip("被弾者が100%に達した後、拠点から再出撃できるまでの強制ロック秒数。")]
public float forcedRespawnLockSeconds = 16f;
[Tooltip("かすった程度(100%未満)でも一定時間だけ移動・視点を鈍らせる範囲デバフ。LMG/SG向け。")]
public bool hasDrowsyDebuff = false;
public float drowsyDebuffSeconds = 3f;
```

`hitImpact` (既存) は麻酔蓄積の生の物理量として引き続き使い、`sedationPerHit` は企画書の「昏睡命中数」から `100 / 命中数` で埋める（例：ボルトアクション=1発→100、アサルト=3〜4発→25〜33）。既存のプリセットメソッド (`ApplyPresetAR` 等) はそのまま残し、48丁は個別の `ScriptableObject` アセットとして一括生成する（後述 §5）。

### 1.2 `FPSSoldierCondition.cs` の置き換え

現行の Fear/Fatigue/Wounded の3軸に、4本目の軸として `Sedation`（麻酔蓄積、0〜100）を追加する。Wounded 由来の「出血で弱る」演出は使わず、Sedation が 100 に達した瞬間を昏睡トリガーにする。

```csharp
public float Sedation { get; private set; }      // 0-100
public bool IsAsleep { get; private set; }

// OnDamageTaken 経由で武器の sedationPerHit を積む
private void HandleDamage(Vector3 dir, float sedationAmount)
{
    if (IsAsleep) return;
    Sedation = Mathf.Min(100f, Sedation + sedationAmount);
    Fear = Mathf.Min(1f, Fear + 0.25f); // 被弾ヒヤリ演出は流用
    if (Sedation >= 100f) EnterSleep();
}
```

`PushToController()` に Sedation 由来の眠気表現を追加する。既存の条件フィールドをそのまま転用できる：

* `conditionSwayMultiplier` — Sedation が上がるほど視点がふらつく
* `conditionSpeedMultiplier` — `hasDrowsyDebuff` のかすり被弾で 3 秒だけ低下
* `conditionTremor` — Sedation 50% 超で手の震え増加（照準が合わせにくくなる）
* `conditionBreathlessness` — あくび音のトリガーに転用（`FPSProceduralAudio.genBreath` を「あくび」クリップに差し替え）

`EnterSleep()` は死亡ではなくコントローラーを無力化する演出のみ行う：

```csharp
private void EnterSleep()
{
    IsAsleep = true;
    controller.enabled = false;      // 入力を止める（無効化のみ、Destroyしない）
    onEnterSleep?.Invoke();          // Zzz オーバーレイ・SEをゲーム層が拾う
}
```

昏睡から復帰するのは看護兵・分隊蘇生・自然リスポーンのいずれか（§2）。復帰時は `Sedation` を用途に応じてリセットし、`controller.enabled = true` に戻す。

### 1.3 画面演出：`MereSoulsScreenEffect.cs` の転用

現行の「体力が減ると徐々に色が抜ける」ロジックはそのまま Sedation 用に転用する（`desaturateStartHealth` を `desaturateStartSedation` に読み替え、100% で完全グレー＋ Zzz オーバーレイ）。ただし死亡時のオーディオローパス演出（22kHz→480Hz）は「意識が遠のく」表現としてそのまま使え、昏睡→蘇生で元に戻す。

## 2. 蘇生・リスポーン（4段階）

新規コンポーネント `RevivalController.cs`（`MereSouls` → `DiamondStraits` 名前空間に配置）を `FPSSoldierCondition` と併置する。

| ルート | 実装 | 所要時間 | 復帰後のSedation |
|---|---|---|---|
| ① 看護兵蘇生 | 看護兵が昏睡者に近づき `E` 長押し1.2秒。`FPSInteractionSystem` のホールド式インタラクトをそのまま使う | 1.2秒 | 0%（完全回復） |
| ② 分隊蘇生 | 任意の味方が近づき `E` 長押し。時間は `Mathf.Lerp(3.5f, 6.0f, sedationDepth)` で被弾銃の深さに応じ可変 | 3.5〜6.0秒 | 50% |
| ③ 自然リスポーン | プレイヤーがリスポーン画面から選択。`forcedRespawnLockSeconds` 経過後に拠点選択可能、選択後は拠点からの移動が発生 | 11〜25秒＋拠点間移動 | 0%（新規スポーン） |
| ④ 自力覚醒 | タイマーのみ、入力不要 | 38〜70秒（`forcedRespawnLockSeconds * 2.4` 程度で算出） | 0% |

`RevivalController` は昏睡開始時に4つのタイマー/条件を同時に立て、最初に成立したものを採用する（看護兵に起こされた瞬間、③④のタイマーは破棄）。ネットワーク越しの蘇生は §4 で扱う。

## 3. 兵科・ガジェット

新規 ScriptableObject `SoldierClassData.cs`：

```csharp
public enum SoldierClass { Assault, Medic, Support, Recon }

[CreateAssetMenu(menuName = "DiamondStraits/SoldierClassData")]
public class SoldierClassData : ScriptableObject
{
    public SoldierClass classType;
    public FPSWeaponData[] primaryWeapons;   // 各兵科10丁
    public GadgetData[] gadgets;             // 3種
}
```

ガジェット（麻酔グレネード、蘇生キット、バリケード、グラップリングフックなど）は素体の `FPSInteractionSystem` / 既存の投擲・設置フックには存在しないため、最小限の `GadgetData` (ScriptableObject) + `GadgetController`（プレイヤーに1つ、現在選択中のガジェットを Use する）を新設する。フェーズ1では看護兵の「蘇生キット」1種のみ実装し、他8種はフェーズ4に回す（§7 ロードマップ参照）。

## 4. ネットワーク同期の追加分

`MereSoulsNetworkPlayer.cs` に以下を追加する：

* `NetworkVariable<float> netSedation`（Owner書き込み、Everyone読み取り）— 既存の `netHealth` を置き換える
* `NetworkVariable<bool> netIsAsleep` — remoteBody のポーズ切り替え（立ち姿 → 横たわりカプセル）に使う
* `ReviveServerRpc(ulong targetNetworkObjectId, ReviveKind kind)` — 蘇生成立をサーバー経由で対象クライアントへ中継し、対象側の `RevivalController` を呼ぶ。既存の `ReportHitServerRpc` / `ApplyHitClientRpc` と対称的な構造にする
* 拠点占領・チケット管理は各クライアントの権威ではなくホストのみが持つ `ConquestManager`（`NetworkBehaviour`、`IsServer` でのみロジック実行、結果を `NetworkVariable` でブロードキャスト）として新設する。2〜4人規模なのでサーバー権威で十分であり、チート対策より「唯一の真実の値」を保つ方が重要

## 5. 48丁の武器データ生成

既存の `FPSBaseAutoSetupEditor.EnsureWeaponAssets()` は現行6プリセットのみ生成している。企画書 §6 の48丁テーブルをそのまま配列化し、`DiamondStraitsWeaponCatalog.cs`（Editorスクリプト）で一括アセット生成する：

```csharp
private static readonly WeaponEntry[] AssaultWeapons = {
    new("MP18", FireMode.FullAuto, 550, 32, sedationPerHit:25f, lockSeconds:14f, weaponLength:0.80f),
    // ... 企画書 6-1〜6-5 の全48行をそのまま定数化
};
```

各エントリは `FPSWeaponData` アセットとして `Assets/Resources/Weapons/DiamondStraits/<兵科>/` 配下に保存する。命名は企画書の「武器名（ベース銃）」をそのまま使う（実銃名の意匠を変えたコミカル外観にする点は企画書 §1-1 のとおりで、データ側の命名とは独立）。

## 6. マップ・拠点・豆戦車

これらは今回のフェーズ1では扱わない（§7 参照）。技術的な要点だけ先に固めておく：

* 拠点（コンクエストポイント）は `CapturePoint.cs`（`Collider` トリガー＋滞在陣営カウント）を新設し、`ConquestManager` に占領状況を報告する。
* 豆戦車は `CharacterController` ではなく `Rigidbody` ベースの物理挙動が必要（横転を許容するため）。既存の `UniversalFPSController` とは完全に別のコントローラー（`MiniTankController.cs`、`Rigidbody` + `WheelCollider` またはカプセルコライダー4点接地の簡易版）として新設し、乗車中は歩兵コントローラーを無効化してカメラだけ切り替える。素体への依存はゼロにする。

## 7. 実装ロードマップ（企画書 §8 に対応）

1. **フェーズ1（今回着手）**：Sedation軸の追加（§1.2）、看護兵1.2秒蘇生（§2 ルート①のみ）、`FPSMetricGym` 上のテストNPCへの適用、Zzz演出。武器は既存6プリセットのままでよく、48丁化とガジェット8種は後回し。
2. **フェーズ2**：コード変更なし（プレイ動画撮影・開発元相談）。
3. **フェーズ3**：`RevivalController` の②③④ルート、`ConquestManager`、`CapturePoint`、ネットワーク同期の拡張（§4）。
4. **フェーズ4**：`MiniTankController`、残り兵科ガジェット、48丁フルカタログ生成（§5）、着せ替え（見た目のみのプレハブ差し替えなので `SoldierCosmeticSet.cs` で完結、ステータスには一切触れない）。

## 8. 既存ドキュメントとの関係

`DESIGN_DOC.md` / `SETUP.md` は `FPSBase` 単体の説明のままで変更不要（素体はこの企画に依存しない設計を維持する）。本書と企画書 (`diamond_straits_skirmish_企画書.md`) が `MereSouls` レイヤーの後継として `DiamondStraits` レイヤーの設計を定める。既存の `MereSouls` 塹壕プロトタイプ（PR #1）は技術検証として現存させ、新規コードは並行する `Assets/Scripts/DiamondStraits/` 以下に追加する。
