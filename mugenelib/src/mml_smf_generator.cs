using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Commons.Music.Midi;

namespace Commons.Music.Midi.Mml
{
	public class MmlSmfGenerator
	{
		public static MidiMusic Generate (MmlResolvedMusic source)
		{
			var gen = new MmlSmfGenerator (source);
			gen.GenerateSong ();
			return gen.result;
		}

		MmlSmfGenerator (MmlResolvedMusic source)
		{
			this.source = source;
			result = new MidiMusic () { DeltaTimeSpec = (short) (source.BaseCount / 4) };
		}

		MmlResolvedMusic source;
		MidiMusic result;

		void GenerateSong ()
		{
			foreach (var strk in source.Tracks)
				result.Tracks.Add (GenerateTrack (strk));
		}

		MidiTrack GenerateTrack (MmlResolvedTrack source)
		{
			var rtrk = new MidiTrack ();
			int cur = 0;
			foreach (var ev in source.Events) {
				MidiEvent evt;
				if (ev.Arguments.Count == 3)
					evt = new MidiEvent (ev.Arguments [0], ev.Arguments [1], ev.Arguments [2], null);
				else if (ev.Arguments [0] == 0xFF)
					evt = new MidiEvent (ev.Arguments [0], ev.Arguments [1], 0, ev.Arguments.Skip (2).ToArray ());
				else
					evt = new MidiEvent (ev.Arguments [0], 0, 0, ev.Arguments.Skip (1).ToArray ());
				var msg = new MidiMessage (ev.Tick - cur, evt);
				rtrk.Messages.Add (msg);
				cur = ev.Tick;
			}
			rtrk.Messages.Add (new MidiMessage (0, new MidiEvent (0xFF, 0x2F, 0, new byte [0])));
			return rtrk;
		}
	}
}
