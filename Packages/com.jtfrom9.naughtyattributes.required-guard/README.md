# NaughtyAttributes Required Field Guard

[`[Required]`](https://github.com/dbrizov/NaughtyAttributes#required)（NaughtyAttributes）が付いた
ObjectReference フィールドが **未割り当て** のとき、**エディタの Play 開始** と **実機ビルド** の
両方を止めるオプトイン拡張です。

NaughtyAttributes 本体には手を入れず、本体の public API のみを利用します。

## 仕組み

| 部品 | 役割 |
|------|------|
| `RequiredFieldChecker` | GUI を持たない検出ロジック（常時コンパイル・テスト対象） |
| `RequiredPlayModeGuard` | 開いているシーンを検証し、違反があれば Play をキャンセル |
| `RequiredBuildGuard` | 有効なビルドシーンを順に検証し、違反があればビルドを失敗させる |

`RequiredPlayModeGuard` / `RequiredBuildGuard` は `NAUGHTY_REQUIRED_GUARD` 定義で囲まれています。
検出ロジック `RequiredFieldChecker` は常時コンパイルされ、EditMode テストの対象です。

## インストール

このパッケージは NaughtyAttributes (`com.dbrizov.naughtyattributes`) に依存します。
NaughtyAttributes は Unity の標準レジストリには無いため、**依存を解決できる導入経路を
利用側プロジェクトで用意する**必要があります（下記いずれか）。これを満たさないと、
本パッケージの依存（`package.json` の `2.1.4`）が解決できずコンパイル前に失敗します。

### 経路A: Git URL で NaughtyAttributes を明示インストール（最も手軽）

`Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.example.naughtyattributes-required-guard": "file:../path/to/this/package",
    "com.dbrizov.naughtyattributes": "https://github.com/dbrizov/NaughtyAttributes.git#v2.1.4"
  }
}
```

### 経路B: OpenUPM のスコープ付きレジストリ

```json
{
  "scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": ["com.dbrizov.naughtyattributes"]
    }
  ],
  "dependencies": {
    "com.example.naughtyattributes-required-guard": "file:../path/to/this/package",
    "com.dbrizov.naughtyattributes": "2.1.4"
  }
}
```

## 有効化（オプトイン）

ゲートを効かせるには scripting define を追加します:

- **Project Settings → Player → Scripting Define Symbols** に `NAUGHTY_REQUIRED_GUARD` を追加。

定義を付けない限り Play/Build は従来どおり通ります（本体ユーザーの挙動は不変）。

## 走査範囲（初版）

| 対象 | Play | Build |
|------|------|-------|
| 開いているシーンの GameObject/Component | ✅ | — |
| `EditorBuildSettings.scenes`（有効）のシーン | — | ✅ 各シーンを開いて検証 |
| Prefab / ScriptableObject 単体アセット | 対象外 | 対象外 |

## 制限事項（v0.1）

- 検出対象は **ObjectReference 型の `[Required]` フィールド**（およびネストした
  `[Serializable]` 内の同種フィールド）。**配列／List の要素**は初版では対象外。
- 非 ObjectReference 型（string/int 等）に付いた `[Required]` は無視されます。
- Prefab / ScriptableObject 単体アセットは走査対象外（上表参照）。

## テスト

`Tests/Editor/RequiredFieldCheckerTest.cs` を Unity Test Runner（EditMode）で実行します。
CLI からは:

```sh
Unity.exe -batchmode -runTests -projectPath <project> -testPlatform EditMode \
  -testResults results.xml
```

（`-runTests` と `-quit` は併用しないこと。テストランナーが完了後に自動終了します。）
