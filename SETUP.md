# 汎用FPSベースコントローラー — セットアップ手順

設計仕様は [`DESIGN_DOC.md`](DESIGN_DOC.md) を参照。このプロジェクトはワンクリック構築ツールが
プレイヤー・カメラ階層・テスト銃6種・検証用グレーボックス(4ゾーン)まで全自動で組むので、
手作業でのGameObject結線は不要。

## 0. プロジェクト作成

1. Unity Hub → New Project → **3D (Built-in) または 3D (URP)** → 場所を `D:\Projects\FPSBaseController` に
   （既に `Assets/Scripts` がある状態でHubが新規作成を拒む場合は、別フォルダで作成後、
   この `Assets/Scripts` フォルダを新プロジェクトの `Assets/` にコピーする）。
2. **Project Settings → Player → Active Input Handling** を「Both」または「Input Manager (Old)」に設定。
   スクリプトはレガシー `Input` クラスを使用しているため、新Input Systemのみだと実行時に例外になる。

## 1. ワンクリック構築

メニューから **Tools → FPS Base → ⚡ 1. One-Click Build Complete Test Scene** を実行するだけで:

- テスト銃6種（素手/AR/SMG/DMR/SG/SR）の `FPSWeaponData` アセットを `Assets/Resources/FPSWeaponPresets/` に生成
- `PlayerRoot`（LeanPivot→StancePivot→CameraShaker→MainCamera/WeaponHolder の6階層）を構築
- `FPSMetricGym`（60m×80m、ゾーンA〜D）を構築
- 各種コンポーネント（Controller/Interaction/ProceduralAudio/Telemetry）を結線

そのまま再生(▶)すれば操作キー一覧([DESIGN_DOC.md](DESIGN_DOC.md)§8参照)でテストプレイできる。

**このボタンは何度実行しても安全**（既存の武器アセットの手動調整値は上書きしない）。
プリセット値に完全に戻したい場合だけ、別メニューの
**Tools → FPS Base → ⚠ Reset All Weapon Presets to Defaults** を使う（確認ダイアログあり、既存の調整値は失われる）。

## 2. レビューで見つかった修正の反映状況

コードレビュー段階で見つかった問題は実装済み。詳細は [`DESIGN_DOC.md`](DESIGN_DOC.md) 末尾
「実装レビューで確認済みの修正事項」を参照。

- 武器切替時の連射カウンタ(`consecutiveShots`)リセット漏れ → 修正済み(`UniversalFPSController.HandleWeaponHotkeys`)
- 壁折り畳みとリロード/しゃがみダッシュのポーズオフセットが単純加算されて衝突 → `otherPoseDamp`で緩和済み(`UniversalFPSController.UpdateProceduralHierarchy`)
- ワンクリック構築が既存の武器チューニングを毎回上書きする事故 → 新規作成時のみプリセット適用に変更、明示的リセットは別メニューへ分離

## 3. 未検証・次のステップ

- `Assets/Scripts/FPSBase/Interactables/` の `FPSDoor` / `FPSSupplyCrate` / `FPSDecalResetSwitch` は
  ゾーンC・D用に今回新規実装したもの（元の設計書には仕様のみで実装コードが無かったため補完）。
  実機で開閉・長押し判定の感触を確認すること。
- `FPSGreyboxBuilder` の寸法は設計書§7の数値をそのまま反映した一次組み（斜面と平地の継ぎ目など、
  見た目の整合は簡易実装）。実機で乗り越え・スライディングの挙動を見ながら微調整が必要。
- プロシージャル音響（特に銃声）は「歴史的傑作FPSと同等の手触り」に届くかどうか、実機での聴感評価が必要
  （レビュー時に指摘した技術的リスク）。
