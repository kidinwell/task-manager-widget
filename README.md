# TaskManagerTrayWidget

Windows のタスクバー通知領域（システムトレイ）に常駐し、**CPU使用率**と**メモリ（RAM）使用率**をリアルタイムに監視・可視化する軽量なデスクトップウィジェットです。

Windows タスクマネージャーの最小化トレイアイコンと同じテイスト（ダークグレー枠の背景に緑の縦棒グラフ）を踏襲しつつ、**左側にCPU・右側にメモリの棒グラフが並ぶ**ことで、両方のリソース状態を一目で把握できます。

---

## 🌟 主な機能

- **タスクマネージャー風トレイアイコン**:
  - 外枠: ダークグレー枠とダークチャコール背景。
  - 左カラム: **CPU 使用率**（下から上へ伸びる縦棒グラフ）。
  - 右カラム: **メモリ（RAM）使用率**（下から上へ伸びる縦棒グラフ）。
  - 背景にはタスクマネージャー純正のような薄いグリッド線を描画し、高い視認性を確保。
- **閾値カラーアラート**:
  - **通常 (< 80%)**: タスクマネージャー純正の鮮やかなグリーン (`#00E676`)
  - **警告 (80% 〜 89%)**: アンバー / イエロー (`#FFB300`)
  - **危険 (>= 90%)**: アラートレッド (`#EF5350`)
- **タスクマネージャー即時起動**:
  - トレイアイコンを**左クリック**すると、Windows 標準のタスクマネージャー (`taskmgr.exe`) が即座に起動／前面表示されます。
- **詳細ツールチップ**:
  - トレイアイコンにマウスを重ねると、CPU・メモリの正確な使用率がツールチップとして表示されます（例: `CPU: 14% | RAM: 58% [Dual]`）。
- **右クリックメニュー**:
  - **Task Manager**: タスクマネージャーを起動。
  - **Display モード切替**:
    - `Dual (CPU & RAM)`: 左右2連バー表示（デフォルト）
    - `CPU Only`: CPU単体バー表示（Windows純正風）
    - `RAM Only`: メモリ単体バー表示
  - **Exit**: トレイアイコンを消去し、安全にバックグラウンドプロセスを終了。
- **軽量・安全設計**:
  - メインウィンドウを持たない完全なバックグラウンド常駐（`ApplicationContext`）。
  - Win32 API (`GetSystemTimes`, `GlobalMemoryStatusEx`) を使用した超軽量なリソースサンプリング（1.5秒間隔）。
  - GDI オブジェクトの破棄 (`DestroyIcon`) を徹底し、長時間の連続稼働でもメモリやハンドルのリークが発生しません。

---

## 💻 動作要件

- **OS**: Windows 10 / Windows 11 (x64)
- **ランタイム**: .NET 9.0 Runtime (Windows Desktop)
- **ビルド環境**: .NET 9.0 SDK

---

## 🔨 ビルド方法

リポジトリルート（`TaskManagerTrayWidget.sln` があるフォルダ）でコマンドを実行します。

### 1. 通常ビルド (Debug)

開発・動作確認用です。

```powershell
dotnet build
```

- 出力先: `.\TaskManagerTrayWidget\bin\Debug\net9.0-windows\TaskManagerTrayWidget.exe`

### 2. リリースビルド (Release)

最適化されたバイナリを作成します。

```powershell
dotnet build -c Release
```

- 出力先: `.\TaskManagerTrayWidget\bin\Release\net9.0-windows\TaskManagerTrayWidget.exe`

### 3. 単一実行可能ファイル (.exe 1本) として出力 (Publish)

他のPCへの配布や、スタートアップ登録に適した1本の `.exe` ファイルとして出力します。

```powershell
dotnet publish TaskManagerTrayWidget/TaskManagerTrayWidget.csproj -c Release -r win-x64 --no-self-contained -p:PublishSingleFile=true
```

- 出力先: `.\TaskManagerTrayWidget\bin\Release\net9.0-windows\win-x64\publish\TaskManagerTrayWidget.exe`

---

## 🚀 使用方法

### 起動

以下のいずれかの方法で起動します。

1. **PowerShell / コマンドプロンプトから起動**:

   ```powershell
   dotnet run --project TaskManagerTrayWidget
   ```

2. **ビルドされた exe を直接実行**:
   エクスプローラー等から `TaskManagerTrayWidget.exe` をダブルクリックして起動します。

> 起動するとメイン画面は表示されず、画面右下のタスクバー（システムトレイ通知領域）にアイコンが現れます。

### 操作方法

| 操作 | 動作 |
| :--- | :--- |
| **左クリック** | Windows タスクマネージャー（`taskmgr.exe`）を起動またはアクティブ化 |
| **マウスホバー** | `CPU: xx% \| RAM: yy% [Dual]` のツールチップを表示 |
| **右クリック** | コンテキストメニューを開く（タスクマネージャー起動、表示モード切替、終了） |
| **メニュー > Exit** | アプリケーションを終了し、トレイアイコンを削除 |

### Windows 起動時に自動起動（スタートアップ登録）したい場合

1. `Win + R` キーを押し、`shell:startup` と入力して Enter を押します（スタートアップフォルダが開きます）。
2. 出力された `TaskManagerTrayWidget.exe` の**ショートカット**を作成し、このフォルダに配置します。

---

## 🧪 動作確認用コマンドライン引数

コンソールから機能の単体・総合テストを実行できます。

```powershell
# メトリクス取得テスト (CPU / RAM が正常範囲でサンプリングできるか)
.\TaskManagerTrayWidget\bin\Debug\net9.0-windows\TaskManagerTrayWidget.exe --test-metrics

# アイコン描画テスト (全モード・閾値カラー・GDI解放のテスト)
.\TaskManagerTrayWidget\bin\Debug\net9.0-windows\TaskManagerTrayWidget.exe --test-icon

# 総合テスト (メトリクス、アイコン、コンテキスト初期化および更新サイクルの検証)
.\TaskManagerTrayWidget\bin\Debug\net9.0-windows\TaskManagerTrayWidget.exe --test-all
```

---

## 📁 プロジェクト構成

```
タスクマネージャー/
├── TaskManagerTrayWidget.sln          # ソリューションファイル
├── README.md                          # 本ドキュメント
└── TaskManagerTrayWidget/
    ├── TaskManagerTrayWidget.csproj   # プロジェクトファイル (.NET 9 WinForms)
    ├── Program.cs                     # エントリポイント・CLIテスト引数処理
    ├── TrayApplicationContext.cs      # トレイ常駐ライフサイクル・メニュー・イベント管理
    ├── SystemMetricsSampler.cs        # Win32 APIによるCPU/メモリ使用率サンプラー
    └── IconRenderer.cs                # タスクマネージャー風2連バーグラフのGDIアイコン描画
```
