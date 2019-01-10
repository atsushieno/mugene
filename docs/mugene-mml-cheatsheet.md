# MML Compiler Mugene Cheat Sheet

## Basic Structure

```
// comment begins with two-slashes.
#basecount 192
#meta title "(Song title)"
#meta copyright "(copyright lines)"
#meta text "arranged by (your name)"
//#conditional block A,B,C
//#conditional track 1,2,5-10

//---- track 1 / channel 1: Piano ----
1	CH1 @73 V110 //  `@`: program/bank `V`: channel volume
	l8 o5 v100 //`o`: octave, `l`: default length, `v`: default velocity
A	c4d4e2  c4d4e2 // begain with spaces or tabs: the same track continues.
// Blocks (`A` above) can be specified before the track number (or the actual MML content if the track number is skipped)

// assign track 10-12 as drum part (MML commmands switch to drum mode)
#define DRUMTRACKS 10,11,12

//---- track 10 / channel 10: Standard Kick & Snare ----
10	CH10 l8 v100
	[
		b4s4bbs4
	] 4 // repeats 4 times
```

## General MIDI cheat sheet

### Program numbers

```
- AcousticGrandPiano = 0
- BrightAcousticPiano = 1
- ElectricGrandPiano = 2
- HonkytonkPiano = 3
- ElectricPiano1 = 4
- ElectricPiano2 = 5
- Harpsichord = 6
- Clavi = 7
- Celesta = 8
- Glockenspiel = 9
- MusicBox = 10
- Vibraphone = 11
- Marimba = 12
- Xylophone = 13
- TubularBells = 14
- Dulcimer = 15
- DrawbarOrgan = 16
- PercussiveOrgan = 17
- RockOrgan = 18
- ChurchOrgan = 29
- ReedOrgan = 20
- Accordion = 21
- Harmonica = 22
- TangoAccordion = 23
- AcousticGuitarNylon = 24
- AcousticGuitarSteel = 25
- ElectricGuitarJazz = 26
- ElectricGuitarClean = 27
- ElectricGuitarMuted = 28
- OverdrivenGuitar = 29
- DistortionGuitar = 30
- Guitarharmonics = 31
- AcousticBass = 32
- ElectricBassFinger = 33
- ElectricBassPick = 34
- FretlessBass = 35
- SlapBass1 = 36
- SlapBass2 = 37
- SynthBass1 = 38
- SynthBass2 = 39
- Violin = 40
- Viola = 41
- Cello = 42
- Contrabass = 43
- TremoloStrings = 44
- PizzicatoStrings = 45
- OrchestralHarp = 46
- Timpani = 47
- StringEnsemble1 = 48
- StringEnsemble2 = 49
- SynthStrings1 = 50
- SynthStrings2 = 51
- ChoirAahs = 52
- VoiceOohs = 53
- SynthVoice = 54
- OrchestraHit = 55
- Trumpet = 56
- Trombone = 57
- Tuba = 58
- MutedTrumpet = 59
- FrenchHorn = 60
- BrassSection = 61
- SynthBrass1 = 62
- SynthBrass2 = 63
- SopranoSax = 64
- AltoSax = 65
- TenorSax = 66
- BaritoneSax = 67
- Oboe = 68
- EnglishHorn = 69
- Bassoon = 70
- Clarinet = 71
- Piccolo = 72
- Flute = 73
- Recorder = 74
- PanFlute = 75
- BlownBottle = 76
- Shakuhachi = 77
- Whistle = 78
- Ocarina = 79
- LeadSquare = 80
- LeadSawtooth = 81
- LeadCalliope = 82
- LeadChiff = 83
- LeadCharang = 84
- LeadVoice = 85
- LeadFifths = 86
- LeadBassAndLead = 87
- PadNewage = 88
- PadWarm = 89
- PadPolysynth = 90
- PadChoir = 91
- PadBowed = 92
- PadMetallic = 93
- PadHalo = 94
- PadSweep = 95
- FXRain = 96
- FXSoundtrack = 97
- FXCrystal = 98
- FXAtmosphere = 99
- FXBrightness = 100
- FXGoblins = 101
- FXEchoes = 102
- FXScifi = 103
- Sitar = 104
- Banjo = 105
- Shamisen = 106
- Koto = 107
- Kalimba = 108
- Bagpipe = 109
- Fiddle = 110
- Shanai = 111
- TinkleBell = 112
- Agogo = 113
- SteelDrums = 114
- Woodblock = 115
- TaikoDrum = 116
- MelodicTom = 117
- SynthDrum = 118
- ReverseCymbal = 119
- GuitarFretNoise = 120
- BreathNoise = 121
- Seashore = 122
- BirdTweet = 123
- TelephoneRing = 124
- Helicopter = 125
- Applause = 126
- Gunshot = 127
```

### Drum notes

```
- AcousticBassDrum = 34
- BassDrum1 = 35
- SideStick = 36
- AcousticSnare = 37
- HandClap = 38
- ElectricSnare = 39
- LowFloorTom = 40
- ClosedHiHat = 41
- HighFloorTom = 42
- PedalHiHat = 43
- LowTom = 44
- OpenHiHat = 45
- LowMidTom = 46
- HiMidTom = 47
- CrashCymbal1 = 48
- HighTom = 49
- RideCymbal1 = 50
- ChineseCymbal = 51
- RideBell = 52
- Tambourine = 53
- SplashCymbal = 54
- Cowbell = 55
- CrashCymbal2 = 56
- Vibraslap = 57
- RideCymbal2 = 58
- HiBongo = 59
- LowBongo = 60
- MuteHiConga = 61
- OpenHiConga = 62
- LowConga = 63
- HighTimbale = 64
- LowTimbale = 65
- HighAgogo = 66
- LowAgogo = 67
- Cabasa = 68
- Maracas = 69
- ShortWhistle = 70
- LongWhistle = 71
- ShortGuiro = 72
- LongGuiro = 73
- Claves = 74
- HiWoodBlock = 75
- LowWoodBlock = 76
- MuteCuica = 77
- OpenCuica = 78
- MuteTriangle = 79
- OpenTriangle = 80
```
