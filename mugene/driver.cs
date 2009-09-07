using System;

namespace Commons.Music.Midi.Mml
{
	public class MmlCompilerDriver
	{
		public static void Main (string [] args)
		{
			var p = new MmlCompiler ();
			p.Compile (args);
		}
	}
}
