AzureStorageDiagImporter のセットアップ手順はコチラ。

# この Function の目的、用途
Azure Storage 診断ログを Log Analytics ワークスペースに送信する Azure Function です。

## これを作ろうと思った動機
Azure Storage の診断ログはログファイル内はセミコロン区切りの非構造データ形式の為、これらのログを分析しようと思っても生ログのままでは分析はしづらいし、診断ログファイルを手元にダウンロードしたりする手間も必要なので、かなり扱いづらいです。
なので、診断ログファイルを Log Analytics を通して分析できるように、診断ログを自動的に Log Analytics に送信する仕組みを試したかった。

# 概要図、主な動き
![概要図.png](https://github.com/JunAbe/AzureStorageDiagImporter/blob/master/resource/readme_1.png)

## ① Timer Trigger で毎時で Function を起動する
### Timer Trigger にした理由
本当は Event Grid によるトリガーにしたかったが、Azure 側の仕組みの都合上できなかったため。
詳細については後述。

### [補足] 毎時起動にした理由
今回はやりたい目的が達成できるかどうかを確かめるのが目的だったので楽な実装を選んだだけ。
Storage 診断ログファイルはは1時間単位のディレクトリ配下に生成されるため、とりあえず1時間に1回取得しにいくような実装にしてみた。
けれど、数分おきに Function を起動して新しいログファイルが見つかれば取り込む、といった作りにした方がよい。

## ②直近の診断ログを取得する
Function は診断ログファイルが生成しきった状態のディレクトリが対象にしてログを収集する。
Function は毎時5分に起動するような Timer Trigger でスケジューリングされている。
hh00 ディレクトリ配下のログファイルが生成しきった状態でログを収集するようにして収集漏れが起きないようにしたい。
そのため、起動した時を1時間前のディレクトリをログファイル収集対象となる。

### [補足] 診断ログコンテナの構成について
診断ログは `$logs` というコンテナ配下に生成される。
`$logs` コンテナ配下のディレクトリ構成は下記にとおりで、~hh00/ ディレクトリ配下に `.log` ファイルがぽつぽつと生成されているような状態になっている。

`$logs/[storage type]/yyyy/MM/dd/hh00/`

なお、このコンテナは Azure ポータルでは表示されないため、中を確認したい場合は Storage Explorer を使用する。
- Storage Explorer でのキャプチャ
![Storage Explorer.png](https://github.com/JunAbe/AzureStorageDiagImporter/blob/master/resource/readme_2.png)

## ③ JSON 形式に加工して Log Analytics に送信する
Log Analytics への送信方法として [HTTP データ コレクター API](https://docs.microsoft.com/ja-jp/azure/azure-monitor/platform/data-collector-api) を使用した。（現時点ではパブリックプレビュー）
基本的は上記ドキュメントを参考に実装をした。（サンプルコードがあるので比較的ラクだった）

診断ログはセミコロン区切りの非構造データ形式となっているが、Log Analytics には JSON 形式で送信する必要がある。
そのため、Function 内部でデータ形式を変換してあげてから、Log Analytics に送信するような流れとなる。

正常に Log Analytics に送信されると、少し時間がたった後に Custom Logs スキーマに反映される。

- 実際に試してみて作成された Custom Logs
![LogAanalytics_workspace.png](https://github.com/JunAbe/AzureStorageDiagImporter/blob/master/resource/readme_3.png)

これにて Storage 診断ログを Log Analytics で確認出来るようになった。
実際、これらのログをどのように使えるのか、使う頻度が高そうなクエリは今後検討したいところ。

## 今回の構成の懸念
この構成では 1時間に1度しか Function は処理をしない。つまり Log Analytics から直近の診断ログを分析出来るのは最大1時間＋α 程度のタイムラグが発生する。

実際の運用を考えると、サービス障害を検知した際はなどはすぐさまログ分析を行いたい。
なので、即時（出来るだけ早く） Log Analytics に診断ログが取り込まれるような構成が望ましい。

# 余談
## 本当は EventGrid を使って Function をトリガーしたかったが、EventGrid では無理だった
### 当初考えていた構成図
![EventGridを使った構想図.png](https://github.com/JunAbe/AzureStorageDiagImporter/blob/master/resource/readme_4.png)
### EventGrid  で構成出来た場合のメリット
log ファイルが生成されるたびに Function の処理させることが出来る。
つまり、***診断ログを即時に LogAnalytics に送信されるため、すぐに分析作業が可能になる。***
Function 内の処理も1ファイル単位で扱えばよいので、実装もさらにシンプルになる。と思って当初はこのパターンを試していたんだけど。。

### 診断ログは Azure.Storage サービスによる操作のため、EventGrid では検知できないらしい。
結果から言うと、$logs コンテナのイベントは EventGrid で拾えません。
$logs コンテナは Azure 側で管理しているコンテナであり、そのコンテナ内のイベントはユーザのイベントとして扱われない＝Event Grid の対象外、ということらしい。

通常の Blob イベント（手動でファイルをアップロードなど）については問題なく検知できるものの、$logs コンテナ配下のイベントはうまく検知できなかった。

具体的には、登録したイベントサブスクリプションのフィルタは `'subjectEndsWith': '.log'` のみ（subjectBeginsWith は未設定）の状態で検証していて、別のコンテナ配下に作成した .log ファイルは検知できるものの、$logs コンテナの方は検知できなかったという状況。

同じような質問が [Github の issue ](https://github.com/Azure/azure-functions-eventgrid-extension/issues/40)に上がっていて、その中の[回答](https://github.com/Azure/azure-functions-eventgrid-extension/issues/40#issuecomment-396686066)によると、診断ログは Azure.Storage サービスによる Blob イベントであり、私たちが EventGrid で登録できるイベントサブスクリプションでは検知することができないイベントとのこと。。

## Azure Feedback に投げてみた
[Support to event the create log file of $logs container by Event Grid.](https://feedback.azure.com/forums/909172-azure-maps/suggestions/37373437-support-to-event-the-create-log-file-of-logs-cont)

Event Grid で $logs のイベントをキャッチできるようにサポートして欲しい、という内容。
Google 翻訳を使って作成した謎文章が伝わるのか不安。。

# セットアップ手順
`local.settings.json` に下記を設定する
- Storage Account の接続文字列（`AzureWebJobsStorage`）
- Log Analytics ワークスペースID（`LOG_ANALYTICS_CUSTOMER_ID`）
- Log Analytics ワークスペースのキー（`LOG_ANALYTICS_SHARE_KEY`）

```json:local.settings.json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "",
    "CUSTOMLOG_NAME": "CustomLogsStorageDiag",
    "LOG_ANALYTICS_CUSTOMER_ID": "",
    "LOG_ANALYTICS_SHARE_KEY": "",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet"
  }
}
```

## Storage Account の接続文字列
ポータル上からマウスポチポチでもいいですが、Cloud Shell からコマンドのコピペがラクだと思います。
```bash:Azure CLI
az storage account show-connection-string -n {storage-account-name} -o tsv
```

## Log Analytics ワークスペースCustomerID と ShareKey
Azure ポータル上から確認するのがラク。（2019/06 時点）
- Log Analytics ワークスペースを開く
- 詳細設定を開く
- Connected Source → Windows Servers にて下記をコピー
  - ワークスペースID
  - 主キー（セカンダリでも良い）

![LogAnalyticsワークスペースの詳細情報.png](https://github.com/JunAbe/AzureStorageDiagImporter/blob/master/resource/readme_5.png)

この３つを `local.settings.json` にセットしたら F5 で実行できる状態になります。
