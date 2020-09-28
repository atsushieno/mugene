using NUnit.Framework;
using System;
using Commons.Music.Midi.Mml;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Commons.Music.Midi.Mml.Tests
{
	[TestFixture]
	public class MmlCompilerTest
	{
		[Test]
		public void SimpleCompilation ()
		{
			MmlTestUtility.TestCompile ("SimpleCompilation", "1   o5cde");
		}
		
		[Test]
		public void BlockSpecification ()
		{
			MmlTestUtility.TestCompile ("BlockSpecification", @"
A 1	o5cde
	cdefg
B	a1	
");
		}

		[Test]
		public void SyncNoteOffWithNextAkaArpeggio ()
		{
			MmlTestUtility.TestCompile ("SyncNoteOffWithNextAkaArpeggio", "A 1 o5c&d&e");
		}

		[Test]
		public void Issue11 ()
		{
			MmlTestUtility.TestCompile ("Issue11", @"#macro 5 CRDA len:length=$__length { f0,,60>c0f$len< }
5 CRDA8");
		}

		[Test]
		public void UnexpectedLoopBreak ()
		{
			Assert.Throws<MmlException> (delegate {
				MmlTestUtility.TestCompile("UnexpectedLoopBreak", @"1  c4 :1 d");
			});
		}

		[Test]
		public void UnexpectedLoopClose ()
		{
			Assert.Throws<MmlException>(delegate {
				MmlTestUtility.TestCompile("UnexpectedLoopClose", @"1  c4 ]2");
			});
		}

		[Test]
		public void MissingLoopClose ()
		{
			Assert.Throws<MmlException> (delegate {
				MmlTestUtility.TestCompile("MissingLoopClose", @"1  [ c4 ");
			});
		}

		[Test]
		public void LoopBreaksBeyondLoopCount ()
		{
			Assert.Throws<MmlException> (delegate {
				MmlTestUtility.TestCompile("LoopBreaksBeyondLoopCount", @"1  [ c4 :1 d :2 e :3 f :4 g  ]2");
			});
		}

		[Test]
		public void LoopBreaksInAnotherLoop () // issue #13
		{
			MmlTestUtility.TestCompile ("LoopBreaksInAnotherLoop", @"1  [[ c4 :1 d :2 e :3 f :4 g  ]4 ]2");
		}

		[Test]
		public void DotWithoutLength () // issue #9
		{
			MmlTestUtility.TestCompile ("DotWithoutLength", @"1 c4.");
		}

		[Test]
		public void TimingOperator ()
		{
			MmlTestUtility.TestCompile ("TimingOperator", @"1 c4,,,2 ASSERT_STEP48");
		}

		[Test]
		public void ChordExtraMacroArgs ()
		{
			MmlTestUtility.TestCompile ("ChordExtraMacroArgs", @"
#macro CHORD_A { c0e0g }
1  o5 l4 CHORD_A8 CHORD_A8 CHORD_A ASSERT_STEP96");
		}

		[Test]
		public void EvenSimplerCompilation ()
		{
			var bytes = new MmlCompiler ().Compile (false, "1   v120 o4 @0 c4e4g4>c4").ToBytes ();
			var result = string.Concat (bytes.Select (b => b.ToString ("X")));
			Console.WriteLine(result);
			Assert.AreEqual ("00C000903078308030009034783080340090377830803700903C7830803C00FF2F0", result.Substring (40), "MIDI bytes");
		}

		[Test]
		public void MultipleConditionalTrackRanges ()
		{
			MmlTestUtility.TestCompile (nameof (MultipleConditionalTrackRanges)
				, "#conditional track 1-10,21-30");
			MmlTestUtility.TestCompile (nameof (MultipleConditionalTrackRanges)
				, "1-100 l4");
		}
		
		[Test]
		public void MetaTextInVariousLengths ()
		{
			MmlTestUtility.TestCompile (nameof (MultipleConditionalTrackRanges)
				, "0 MARKER \"A\"");
			MmlTestUtility.TestCompile (nameof (MultipleConditionalTrackRanges)
				, "0 MARKER \"Section A\"");
		}

		[Test]
		public void DoubleTrackNumber ()
		{
			MmlTestUtility.TestCompile (nameof (DoubleTrackNumber)
				, "1.1 c2d4\n11 e2f4\n0.11 gab");
			MmlTestUtility.TestCompile (nameof (DoubleTrackNumber) + " - 2", "0-100,2.1	r1");
		}

		[Test]
		public void DrumTrackNoteVelocityIssue35 ()
		{
			string mml = @"
#define DRUMTRACKS 10

9	t120

9	V120 l16
	v127 n36,1  n36,1,1,64

#macro DRUMTRACKS Ba { n36,0,127 r }
#macro DRUMTRACKS Bb { n36,0,112 r }
#macro DRUMTRACKS Bc { n36,0,96 r }
#macro DRUMTRACKS Bd { n36,0,80 r }
#macro DRUMTRACKS Be { n36,0,64 r }
#macro DRUMTRACKS Sa { n38,0,127 r }
#macro DRUMTRACKS Sb { n38,0,112 r }
#macro DRUMTRACKS Sc { n38,0,96 r }
#macro DRUMTRACKS Sd { n38,0,80 r }
#macro DRUMTRACKS Se { n38,0,64 r }

10	V120 l16
	v127 n36,1  n36,1,1,64
";
			var music = new MmlCompiler ().Compile (false, mml);
			var messages1 = music.Tracks [0].Messages.Where (m => m.Event.EventType == MidiEvent.NoteOn).ToArray ();
			Assert.AreEqual (127, messages1 [0].Event.Lsb, "Messages1.Velocity1");
			Assert.AreEqual (64, messages1 [1].Event.Lsb, "Messages1.Velocity2");

			var messages2 = music.Tracks [1].Messages.Where (m => m.Event.EventType == MidiEvent.NoteOn).ToArray ();
			Assert.AreEqual (127, messages2 [0].Event.Lsb, "Messages2.Velocity1");
			Assert.AreEqual (64, messages2 [1].Event.Lsb, "Messages2.Velocity2");
		}
	}
}

