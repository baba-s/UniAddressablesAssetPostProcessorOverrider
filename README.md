# UniAddressablesAssetPostProcessorOverrider

アセットが削除された時に Addressable Asset System のグループを更新しないようにするエディタ拡張

## メモ

* Addressable Asset System では、アセットが削除された時に  
そのアセットが格納されているグループの情報が更新される  
* Jenkins などの CI ツールで Addressable のグループを自動更新するようにしている場合、  
各作業者の環境でアセットを削除した時にグループの情報が更新されないようにしたいことがある  
    * グループの変更漏れや競合などを防ぐため  
* このリポジトリを Unity プロジェクトに追加すると  
アセットが削除された時に Addressable のグループが更新されないようになるため  
各作業者は Addressable のグループのコミットを意識する必要がなくなり、  
Jenkins などの CI ツールで更新を一元管理できるようになる  
