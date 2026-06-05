# Required Field Guard — 設計メモ

`[Required]`（NaughtyAttributes）が付いた未割り当ての参照フィールドがあるとき、
**エディタの Play 開始** と **実機ビルド** の両方を阻止する仕組み。

NaughtyAttributes 本体には手を入れず、**本体に依存する別パッケージ（拡張）**として実装する。

---

## 1. 背景・現状

NaughtyAttributes の `[Required]` は **インスペクタ描画時の検証** にすぎない。

- `RequiredPropertyValidator.ValidateProperty()` が `objectReferenceValue == null` を見て
  **赤い HelpBox を描画するだけ**。ビルドもプレイも止めない。
- 呼び出し経路: `NaughtyInspector.OnInspectorGUI`
  → `NaughtyEditorGUI.PropertyField_Implementation`
  → `validatorAttribute.GetValidator().ValidateProperty(property)`

つまり「Required が未設定でもビルドは通り、Play もできる」状態。

---

## 2. ゴール（要求仕様）

| ID | 要求 | 補足 |
|----|------|------|
| R1 | `[Required]` の ObjectReference が `null` のとき **エディタの Play を開始させない** | Play ボタンを押しても再生に入る前にキャンセル |
| R2 | 同条件で **実機ビルドを失敗させる** | `BuildFailedException` でビルド中断、CI でも非ゼロ終了 |
| R3 | 違反フィールドを **コンソールにエラー出力** | どのオブジェクトのどのフィールドかを `Debug.LogError(context)` で特定可能に |
| R4 | NaughtyAttributes 本体を **改変しない** | 本体は public API のみ利用 |
| R5 | **オプトイン** | 入れた人だけ効く。本体ユーザーの既存挙動は不変 |

### 非ゴール（やらないこと）

- **「コンパイルエラー」そのもの化はしない（原理的に不可能）。**
  コンパイルエラーは C# コンパイラがソースコードを解析して出すもの。
  「参照が null か」はシリアライズ済みデータの値であり、コンパイル時には存在しない。
  → 実質目的である「Play/Build を止める」を、ビルド/プレイのフックで実現する。
- 値の自動補完・自動割り当てはしない（検出と中断のみ）。

---

## 3. なぜ別パッケージにできるのか

拡張に必要な API がすべて **public** だから。本体の private には触れない。

- `NaughtyAttributes.RequiredAttribute` … `public class`
- `NaughtyAttributes.Editor.PropertyUtility.GetAttribute<T>()` … `public static`

この2つで「`SerializedObject` を走査し、`[Required]` かつ ObjectReference かつ
`objectReferenceValue == null` を集める」検出ロジックが外部から書ける。

パッケージ分離により **「インストール＝オプトイン」**（R5）が自然に満たされる。
本体ユーザーの `[Required]` 挙動は不変、拡張を入れた人だけブロックが効く。

---

## 4. アーキテクチャ

部品は3つ。**検出（ヘッドレス）**を共通化し、**Play ゲート**と **Build ゲート**が再利用する。

```
                 ┌─────────────────────────────┐
   Play 押下 ───▶│ RequiredPlayModeGuard       │──┐
                 │ (playModeStateChanged)      │  │
                 └─────────────────────────────┘  │   ┌──────────────────────┐
                                                   ├──▶│ RequiredFieldChecker │
                 ┌─────────────────────────────┐  │   │ (GUI なし・検出のみ) │
   Build 実行 ─▶│ RequiredBuildGuard          │──┘   │  → List<Error>       │
                 │ (IPreprocessBuildWithReport)│      └──────────────────────┘
                 └─────────────────────────────┘
                         │ errors.Count > 0
                         ▼
            Play: isPlaying = false / Build: throw BuildFailedException
```

### ① RequiredFieldChecker（検出ロジック・常時コンパイル）

現状の `RequiredPropertyValidator` は **検出と HelpBox 描画が一体**。
ゲート用途では「描画」ではなく「エラー収集」が必要なので、検出だけを切り出す。

- 入力: `UnityEngine.Object`（MonoBehaviour / ScriptableObject）
- 処理: `new SerializedObject(obj)` で SerializedProperty を走査し、
  `PropertyUtility.GetAttribute<RequiredAttribute>(prop)` が非 null かつ
  `prop.propertyType == ObjectReference` かつ `prop.objectReferenceValue == null` を収集
- 出力: `List<Error>`（`Error { Object Context; string Message; }`）

```csharp
public static class RequiredFieldChecker
{
    public readonly struct Error
    {
        public readonly UnityEngine.Object Context;
        public readonly string Message;
        public Error(UnityEngine.Object ctx, string msg) { Context = ctx; Message = msg; }
    }

    public static void CollectErrors(UnityEngine.Object obj, List<Error> errors)
    {
        if (obj == null) return;

        using var so = new SerializedObject(obj);
        SerializedProperty it = so.GetIterator();
        bool enter = true;
        while (it.NextVisible(enter))
        {
            enter = true; // 子（ネストした Serializable）も辿る
            if (it.propertyType != SerializedPropertyType.ObjectReference) continue;

            var required = PropertyUtility.GetAttribute<RequiredAttribute>(it);
            if (required == null) continue;
            if (it.objectReferenceValue != null) continue;

            string msg = string.IsNullOrEmpty(required.Message)
                ? $"{obj.name}.{it.propertyPath} is required but not assigned"
                : $"{obj.name}.{it.propertyPath}: {required.Message}";
            errors.Add(new Error(obj, msg));
        }
    }
}
```

### ② RequiredPlayModeGuard（Play ゲート・define で有効化）

```csharp
#if NAUGHTY_REQUIRED_GUARD
[InitializeOnLoad]
internal static class RequiredPlayModeGuard
{
    static RequiredPlayModeGuard()
        => EditorApplication.playModeStateChanged += OnChange;

    private static void OnChange(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode) return;

        var errors = new List<RequiredFieldChecker.Error>();
        // 「いま開いているシーン」の全コンポーネントを検証（Play で動くのはそれ）
        CollectOpenScenes(errors);

        if (errors.Count == 0) return;
        foreach (var e in errors) Debug.LogError(e.Message, e.Context);
        EditorApplication.isPlaying = false; // 再生開始を取り消す
    }
}
#endif
```

### ③ RequiredBuildGuard（Build ゲート・define で有効化）

```csharp
#if NAUGHTY_REQUIRED_GUARD
internal sealed class RequiredBuildGuard : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        var errors = new List<RequiredFieldChecker.Error>();
        // EditorBuildSettings.scenes（有効分）を順に開いて検証。
        // 開く前に現在の SceneSetup を退避し、検証後に必ず復元する。
        CollectEnabledBuildScenes(errors);

        if (errors.Count == 0) return;
        foreach (var e in errors) Debug.LogError(e.Message, e.Context);
        throw new BuildFailedException($"{errors.Count} required field(s) are unassigned");
    }
}
#endif
```

---

## 5. 走査範囲の方針

ここが最も挙動に効く判断。シンプルさ優先で次の通り。

| 対象 | Play (R1) | Build (R2) |
|------|-----------|-----------|
| 開いているシーンの GameObject/Component | ✅ | — |
| `EditorBuildSettings.scenes`（有効）のシーン | — | ✅ 各シーンを開いて検証 |
| Prefab / ScriptableObject 単体アセット | ❌ 初版では対象外 | ❌ 初版では対象外 |

- **Play**: 実際に動くのは開いているシーンのみ。全 prefab/SO まで見ると遅く誤検出も増える。
- **Build**: 有効ビルドシーンを順に開く。**開く前に `EditorSceneManager` の SceneSetup を退避し、
  検証後に必ず復元**（中断時も `finally` で戻す）。
- prefab/SO アセットの走査は将来拡張（`AssetDatabase.FindAssets("t:Prefab" / "t:ScriptableObject")`）。

---

## 6. オプトインの形（R5）

別パッケージなので **インストール自体がオプトイン**。
加えて、入れたまま一時的に無効化できるよう **scripting define `NAUGHTY_REQUIRED_GUARD`** で
ゲート②③をラップする。

- ゲートを効かせる: Project Settings → Player → Scripting Define Symbols に
  `NAUGHTY_REQUIRED_GUARD` を追加（または `versionDefines` で自動付与）。
- **検出ロジック①は define で囲まない**（常時コンパイル）。
  → テスト（§8）が define の有無に関わらず走る。

---

## 7. パッケージ構成（UPM）

NaughtyAttributes に依存する独立 UPM パッケージとして配置。

```
com.example.naughtyattributes-required-guard/
├── package.json
├── README.md
├── CHANGELOG.md
├── LICENSE
├── Editor/
│   ├── NaughtyAttributes.RequiredGuard.Editor.asmdef
│   ├── RequiredFieldChecker.cs        # ① 常時コンパイル
│   ├── RequiredPlayModeGuard.cs       # ② #if NAUGHTY_REQUIRED_GUARD
│   └── RequiredBuildGuard.cs          # ③ #if NAUGHTY_REQUIRED_GUARD
└── Tests/
    └── Editor/
        ├── NaughtyAttributes.RequiredGuard.Editor.Tests.asmdef
        └── RequiredFieldCheckerTest.cs
```

### package.json（依存定義）

```json
{
  "name": "com.example.naughtyattributes-required-guard",
  "version": "0.1.0",
  "displayName": "NaughtyAttributes Required Field Guard",
  "description": "Blocks Play/Build when a [Required] field is unassigned.",
  "unity": "2022.3",
  "dependencies": {
    "com.dbrizov.naughtyattributes": "2.1.4"
  }
}
```

### Editor asmdef（本体参照）

```json
{
  "name": "NaughtyAttributes.RequiredGuard.Editor",
  "references": [
    "NaughtyAttributes.Core",
    "NaughtyAttributes.Editor"
  ],
  "includePlatforms": ["Editor"],
  "autoReferenced": true
}
```

> NaughtyAttributes 本体の現行 asmdef 名は `NaughtyAttributes.Core` / `NaughtyAttributes.Editor`。
> 本拡張はこれらを参照するだけで、本体ソースは一切変更しない（R4）。

---

## 8. テスト方針（TDD）

エディタフック②③自体は自動テストが難しいが、
**検出ロジック① は EditMode テストで TDD 可能**。

- フィクスチャ: `[Required]` 付きフィールドを持つ ScriptableObject（割当あり/なし両方）。
- 検証: `CollectErrors` が返す Error 件数・対象パスをアサート。
  - 未割り当て1件 → errors に1件、`propertyPath` が一致
  - 割り当て済み → 0件
  - カスタムメッセージ → Message に反映
  - ネストした `[Serializable]` 内の `[Required]` → 検出される
- ②③（playModeStateChanged / IPreprocessBuildWithReport の配線）は手動確認。

> 注意: このリポジトリ環境では Unity を起動できないため、Red→Green の実行確認は
> Unity Editor 上で行う必要がある。コードは TDD 順（テスト先行）で用意する。

---

## 9. 既知の注意点・将来拡張

- **ネスト/配列**: `NextVisible(true)` で子を辿る。巨大配列で重くなる場合は対象を絞る余地あり。
- **シーン開閉のコスト**（Build）: 多数シーンを開くと遅い。差分検証や対象限定は将来課題。
- **意図的に null**: ランタイムで代入する Required を null のまま運用するケースは誤検出になり得る。
  除外手段（専用属性 or 無視リスト）は将来拡張。
- **prefab/SO アセット走査**: §5 の通り初版は対象外。`FindAssets` で拡張可能。
</content>
</invoke>
