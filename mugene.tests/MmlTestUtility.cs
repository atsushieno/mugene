using System;
using System.Collections.Generic;
using System.IO;
using Commons.Music.Midi.Mml;

namespace Commons.Music.Midi.Mml.Tests
{
	public static class MmlTestUtility
	{
		public static byte [] TestCompile (string testLabel, string mml)
		{
			var sources = new List<MmlInputSource> ();
			sources.Add (new MmlInputSource ("fakefilename.mml", new StringReader (mml)));
			using (var outs = new MemoryStream ()) {
				new MmlCompiler ().Compile (false, sources, null, outs, false);
				return outs.ToArray ();
			}
		}
	}
}

