# dtenv
Simple Dart version management

# 1. Installation
レポジトリのソリューションのビルドを行い、[リリースフォルダーに作成されたファイル](https://github.com/enumori/dtenv/releases/download/2021.05.20/dtenv.zip)を任意のフォルダーに配置します。配置したフォルダーにパスを通します。

もしくは、powershellを起動して以下のコマンドを入力します。

```
Set-ExecutionPolicy RemoteSigned -scope Process
Invoke-WebRequest -Uri "https://github.com/enumori/dtenv/releases/download/2021.05.20/dtenv.zip" -OutFile .\dtenv.zip
Expand-Archive -Path .\dtenv.zip -DestinationPath $env:USERPROFILE
Remove-Item .\dtenv.zip
Rename-Item  $env:USERPROFILE\dtenv  $env:USERPROFILE\.dtenv
$path = [Environment]::GetEnvironmentVariable("PATH", "User")
$path = "$env:USERPROFILE\.dtenv;" + $path
[Environment]::SetEnvironmentVariable("PATH", $path, "User")
```
powershellやコマンドプロンプトを起動するとdtenvが使用できます。

# 2. Command Reference
| 実行内容 | コマンド|
| --- | --- |
| インストール可能なDartバージョンのリスト | dtenv install --list |
| インストール可能なDartバージョンのリストの更新 | dtenv update |
| インストール | dtenv install バージョン |
| インストール済みバージョンのリスト | dtenv versions |
| 全体のバージョンの切り替え | dtenv global バージョン |
| ローカルフォルダーのバージョンの切り替え | dtenv local バージョン |

# 3. 使い方
## 1. Dartをダウンロードする
```
PS > dtenv install 2.12.4
```
## 2. 使用するバージョンに設定する
```
PS > dtenv global 2.12.4
```
## 3. 指定したバージョンが使用できるかを確認
```
PS > dart --version
2で設定したバージョンが表示される
```