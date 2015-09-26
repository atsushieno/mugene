h2. Introduction

h3. the MIDI device basics

A MIDI device (a MIDI instrument) is either an external hardware, or a software that synthesizes voices on a computer.  There are 16 channels on a MIDI device. You can set a tone program from 128 preset tones (such as piano, guitar, bass, violin and synth reed) and several parameters on each channel. A channel can give voices within, in piano context, the range of 128 keys (the number of the maximum key-on notes depends on the MIDI device). It should also be noted that the 10th channel is assigned to constitute a drum part by default.

MIDI operations are, in general, saved in the Standard MIDI file format (SMF, with .mid extension). In a SMF, there are tracks, and each track contains a set of MIDI operations, in timeline order.

h3. What is a MML compiler

Generally speaking, a DTM (desktop music) software, typically a DAW (digital audio workstation), covers all of MIDI devices, PCM channels, and other external modules. External modules speaking here depend on the platforms. They typically proprietary softwares such as VSTi or DXi. For example, "Vocaloid" Hatsune-Miku (初音ミク) is designed as a VSTi that works only on Windows (some people use it on patched Wine), and they can be controlled remotely using ReWire protocol. VSTi receives controll operations as MIDI messages. To send operations over VSTi, you need to depend on and conform to VST library interfaces.

"mugene" is not a DAW software. Instead, it is a "MML compiler" that compiles a set of operations described in text representation into a SMF file. Only MIDI is supported. mugene itself does not provide player functionality. MIDI player functionality is supported in platform-dependent ways (there are platforms that do not support MIDI functionality). I also publish MIDI players and virtual MIDI keyboard on the same location that I publish mugene sources.

There are several variants of MML syntaxes. Since it has been evolved specifically in Japan, there is no global standard (there is a standardized specification named Music Markup Language, abbreviated as MML, which is based on XML and designed for data exchange, but it is totally different one). I'll describe mugene MML syntax in depth later.

There are several MML syntaxes, but there is a couple of common (typical) operations. For example, the most frequently used operations are c/d/e/f/g/a/b that for each matches do/re/mi/fah/so/la/ti note. A rest is 'r'. A note (and a rest) takes a length argument. A quarter do note is represented as "c4". An eighth re note is "d8".　Dotted notes are represented like "c4." . To indicate octave, use 'o' operation. Specify the height after 'o'. In any MML syntax, the default octave is typically set around the middle point. In mugene, it is 4 (between 0 to 11). o5 is higher than o4.

h3. What is mugene

mugene is a MML compiler that processes an MML that is written in mugene's own syntax (mugene MML). The mugene syntax is designed to import the flavor of classical MML syntaxes and convenient. It is though primarily designed for the developers' purpose and need. It should be also noted that to achieve "shorthand" music authoring it costs some flexibility and customizability (it is much more flexible than class syntaxes though).

In mugene MML, uppercase and lowercase are discriminated (at this point, it differs from some classic syntaxes such as N88-BASIC PLAY statement). Also, the text file encoding is always expected as UTF-8.

MML compiler mugene (the program) can be used in several platforms such as Windows, Mac OS X, GNU/Linux (it requires ECMA CLI runtime such as Mono or .NET Framework).

h2. Explaining mugene MML

From here, I describe MML as mugene MML.

h3. MML sample #1: first MML example

The following is a simple MML example within 1 channel:

// - - two slashes are used to indicate a comment line - -
#meta title “Rhapsody in Blue (introduction)”
#meta copyright “George Gershwin (1898-1937)”
1 t140 @71 V110 o6 l4 a2.g8a8gagfedc+c2dd+e2c+<ag2fea1^1
// - - - - -

It describes the popular introduction from George Gershwin's Rhapsody in Blue in MML. It is short but contains a handful of basic operations. I describe them in order.

In a SMF you can set a title for a song. The #meta title line specifies the title of the song. Surround it with double quotes.

The following #meta copyright line describes copyright notices (Gershwin's copyright has already expired, so it might not be appropriate to put it here).

The final line, finally is a track line, which describes play operations. The first "1" means the track number. The MML is interpreted per-line. Its interpretation varies what is put on the head of the line. The first two lines begin with number sign (#) which is to indicate non-track line. A line that does not put track number is regarded to indicate the same track number as the previous track line. A track number can become very large (I don't explain it; you don't have to worry in general).

The next t140 is tempo operation for the song. I don't explain what the tempo number means here, but it is the larger, the faster.

The next @71 indicates the tone by number. In MIDI, tone indicator is called Program Change. Program number 71 indicates Clarinet in the standard MIDI. In the Standard MIDI, there is a predefined mapping from number to instrument (see Wikipedia[*] etc.).

Note that in mugene MML @ operation, the number range is from 0 to 127, not 1 to 128.

[*] http://en.wikipedia.org/wiki/General_MIDI#Melodic_sounds

The next V110 is channel volume operation. The value range is 0 to 127. There is a couple of other "volume" indicators. I'll explain some later.

The next o6 indicates octave. It is the larger, the higher too. For reference, an octave consists of 12 notes, and MIDI key range is from 0 to 127. In mugene MML, the corresponding key for a note operation is defined as: oValue * 12 + keySpecificValue .

The next l4 indicates the default length. In the MML above, note operations follow, and they can omit the length argument. The l value indicates the length for them.

Then, note operations follow in the sample above. 'a' is a note operation for "la". The seven letters, c, d, e, f, g, a, and b, foreach corresponds to do, re, mi, fah, so, la, and ti. Note that 'a' is not "do", 'b' is not "re" ... the notation almost matches those in Germany.

To indicate a sharp, use '+'. Also use '-' for flat. In the sample above, there is "c+", which represents do sharp.

For reference, there is no occurence of r in the sample above, but r means a rest.

A note operation accompanies a length argument. A length consists of a number and optionally dots (represented literally). "a2." means dotted half la. A length can be omitted (I have explained about it when I explained 'l' operation).

Regarding the note length, '^' indicates "tie". a1^1 means twice of a full la note.

Finally, '>' indicates a relative increase of octave, and '<' indicates a relative decrease too. (Note that they can be exactly opposite in some other MML syntaxes.)

Here I explained those basic operations above. I haven't explained detailed operations that gives expressive flavors of a MIDI song, but you can now create a simple melody with it.

