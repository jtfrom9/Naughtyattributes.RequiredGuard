# NaughtyAttributes Required Field Guard

[`[Required]`](https://github.com/dbrizov/NaughtyAttributes#required)（NaughtyAttributes）が付いた
ObjectReference フィールドが **未割り当て** のとき、**エディタの Play 開始** と **実機ビルド** の
両方を止める拡張です。

NaughtyAttributes 本体には手を入れず、本体の `[Required]` 属性をそのまま使い、**ふるまい
（Play/Build の阻止）だけ**を足します。**インストール＝オプトイン**で、入れれば必ず効きます
（嫌なら入れない）。

## 想定する使い方（`#nullable enable` とセット）

このパッケージの主目的は、**C# の nullable reference types を有効にしたまま、
シリアライズ参照を「非 null 前提」で書く**ことです。

`#nullable enable` 下では、インスペクタ割り当て前提の参照フィールドはコンパイラに
「コンストラクタ終了時に null」と警告されます。これを `= null!`（null 許容しない、と
コンパイラに約束する）で抑えますが、**本当に割り当てられている保証**は別途必要です。
その保証を `[Required]` ＋ 本ガードが与えます。

```csharp
#nullable enable
using NaughtyAttributes;
using UnityEngine;

public class Player : MonoBehaviour
{
    // 非 null 前提で書ける。null! でコンパイラを黙らせ、未割り当てなら
    // Play/Build がこのフィールドを理由に止まる（= null! の約束が守られる）。
    [SerializeField, Required] private Rigidbody _body = null!;

    private void FixedUpdate()
    {
        _body.AddForce(Vector3.up); // null チェック不要
    }
}
```

これにより「`null!` と書いたのに実際は未割り当て」という事故を、コンパイル後の
ランタイムではなく **Play/Build の時点で** 潰せます。

## 例外を許す: `[RequiredGuardIgnore]`

「`[Required]`（インスペクタ警告）は出したいが、Play/Build は止めたくない」フィールド
（ランタイムで代入する等）には、併記する除外マーカーを使います。

```csharp
#nullable enable
using NaughtyAttributes;
using NaughtyAttributes.RequiredGuard;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    // インスペクタには赤警告が出るが、ガードは止めない（実行時に代入する想定）。
    [SerializeField, Required, RequiredGuardIgnore] private GameObject _spawned = null!;
}
```

## 仕組み

| 部品 | アセンブリ | 役割 |
|------|-----------|------|
| `RequiredGuardIgnoreAttribute` | Runtime | Play/Build 阻止からの除外マーカー |
| `RequiredFieldChecker` | Editor | GUI を持たない検出ロジック（テスト対象） |
| `RequiredPlayModeGuard` | Editor | 開いているシーンを検証し、違反があれば Play をキャンセル |
| `RequiredBuildGuard` | Editor | 有効なビルドシーンを順に検証し、違反があればビルドを失敗させる |

## インストール

このパッケージは NaughtyAttributes (`com.dbrizov.naughtyattributes`) に依存します。
NaughtyAttributes は Unity の標準レジストリには無いため、**依存を解決できる導入経路を
利用側プロジェクトで用意**してください（下記いずれか）。

### 経路A: Git URL（最も手軽）

`Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.jtfrom9.naughtyattributes.required-guard": "https://github.com/jtfrom9/Naughtyattributes.RequiredGuard.git?path=/Packages/com.jtfrom9.naughtyattributes.required-guard#v0.1.0",
    "com.dbrizov.naughtyattributes": "https://github.com/dbrizov/NaughtyAttributes.git#v2.1.4"
  }
}
```

> Unity の `?path=` クエリでリポジトリのサブフォルダを直接インストールできます。

### 経路B: OpenUPM のスコープ付きレジストリ

```json
{
  "scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.jtfrom9",
        "com.dbrizov.naughtyattributes"
      ]
    }
  ],
  "dependencies": {
    "com.jtfrom9.naughtyattributes.required-guard": "0.1.0",
    "com.dbrizov.naughtyattributes": "2.1.4"
  }
}
```

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
- `[RequiredGuardIgnore]` を付けたフィールドは Play/Build 阻止の対象外（インスペクタ
  警告は NaughtyAttributes により表示されます）。

## 開発・テスト

このリポジトリ自体が Unity プロジェクトで、パッケージは
`Packages/com.jtfrom9.naughtyattributes.required-guard/` に埋め込まれています。
EditMode テストは Unity Test Runner、または CLI で:

```sh
Unity.exe -batchmode -runTests -projectPath <repo> -testPlatform EditMode \
  -testResults results.xml
```

（`-runTests` と `-quit` は併用しないこと。テストランナーが完了後に自動終了します。）
