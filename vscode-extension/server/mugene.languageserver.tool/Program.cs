using System;
using Commons.Music.Midi.Mml;

namespace Commons.Music.Midi.Mml
{
	public class MugeneLanguageService {
		public static void Main ()
		{
			new MugeneServiceConnection (Console.OpenStandardInput (), Console.OpenStandardOutput ()).Listen ().Wait ();
		}
	}
}
