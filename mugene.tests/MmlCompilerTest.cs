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
	}
}

