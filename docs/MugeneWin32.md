mugene-win.exe is a winforms-based mugene MML authoring environment that uses primitive TextBox (read only mode so far; editing MML in simple TextBox is too lame, so edit it in your favorite text editor.) It should run on .NET 3.5+ or Mono as long as windows forms are supported.

So far it opens the default MIDI output device to play the compiled SMF.

So far it can open only one MML file at a time.

To (re)build mugene-win.exe, copy this directory under mugene, and run make.

external libraries:
        porttime.dll, portmidi.dll : from portmidi project, DLL version.
        others are from my project (managed-midi).

screenshot:
<img src="http://img.f.hatena.ne.jp/images/fotolife/a/atsushieno/20100627/20100627032626.png" />

