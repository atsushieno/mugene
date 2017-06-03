# en

mugene is a music macro language (MML) compiler to generate standard midi format files (SMFs). It has been somewhat extended to also generate Vocaloid song files.

The MML syntax is somewhat formalized in MMLSpec.txt included in dists, and also briefly explained at Introduction to mugene MML.

The source package is tailored for GNU/Linux or Mac platform, but also available as Windows runnable (depends on .NET Framework). On GNU/Linux platform, configure/make/make install and run “mugene” [yourmmlfilename.mml]. On Windows, download binary archive, expand it and run mugene.exe ..\mml\default-macro.mml ..\mml\gs-sysex.mml ..\mml\drum-part.mml ..\nrpn-gs-xg.mml [yourmmlfilename.mml]. You might want to create a .bat file to shorten this lengthy command line.

If you want to use it as vocaloid “VSQ” generator, also add “—vsq” and “path/to/mml/vsq-support.mml” (replace / with \ on Windows) as arguments before your mml filename. mugene always takes the final non-option argument file name as the output filename basis, so if you don’t specify output files, it will try to create like “/usr/local/lib/mugene/mml/nrpn-gs-xg.mid” (and will fail depending on the permission).

VSQ support is almost only for Japanese, but if you are interested, a brief syntax is available on “vsq-support.mml” (included). I also wrote some research entry for VSQ lyrics macro.

# ja

mugeneは標準MIDIフォーマット(SMF)のファイルを生成するmusic macro language (MML)コンパイラです。また、"Vocaloid"の歌唱ファイルを作成できるよう、多少機能拡張されています。

MML文法は、配布アーカイブ内の MMLSpec.txt で多少formal syntaxとして定義されており、また mugene-users-guide-ja でも簡単に説明されています。

ソースパッケージはGNU/LinuxあるいはMacのプラットフォーム用に作られていますが、Windows上でも実行可能です（.NET Frameworkに依存）。GNU/Linuxプラットフォームでは、configure/make/make install を行って、"mugene" [mmlファイル名] を実行します。Windows上では、バイナリアーカイブをダウンロードして展開し、mugene.exe ..\mml\default-macro.mml ..\mml\gs-sysex.mml ..\mml\drum-part.mml ..\nrpn-gs-xg.mml [mmlファイル名] を実行します。長ったらしいコマンドラインを省略するバッチファイルを作った方がいいかもしれません。

vocaloidの"VSQ"ジェネレータとして使用したい場合は、引数に"—vsq"と"path/to/mml/vsq-support.mml" （Windowsの場合は\で区切る）をあなたのMMLファイル名の前に指定して下さい。mugeneでは常に最後の非オプション引数であるファイル名を出力ファイル名の本体部分に使います。なのでもしファイル名を指定しなければ、"/usr/local/lib/mugene/mml/nrpn-gs-xg.mid"のようなファイルを生成しようとします（そしてアクセス許可が無ければ失敗するでしょう）。

VSQサポートはほぼ日本語専用ですが、その簡単な説明は英語で(!) vsq-support.mml の冒頭に書いてあります。また、簡単な研究エントリ ( here )を書いて、そこにサンプルを載せておきました。
