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
		public static SmfMusic Generate (MmlResolvedMusic source)
		{
			var gen = new MmlSmfGenerator (source);
			gen.GenerateSong ();
			return gen.result;
		}

		MmlSmfGenerator (MmlResolvedMusic source)
		{
			this.source = source;
			result = new SmfMusic () { DeltaTimeSpec = (short) (source.BaseCount / 4) };
		}

		MmlResolvedMusic source;
		SmfMusic result;

		void GenerateSong ()
		{
			foreach (var strk in source.Tracks)
				result.Tracks.Add (GenerateTrack (strk));
		}

		SmfTrack GenerateTrack (MmlResolvedTrack source)
		{
			var rtrk = new SmfTrack ();
			int cur = 0;
			foreach (var ev in source.Events) {
				SmfEvent evt;
				if (ev.Arguments.Count == 3)
					evt = new SmfEvent (ev.Arguments [0], ev.Arguments [1], ev.Arguments [2], null);
				else if (ev.Arguments [0] == 0xFF)
					evt = new SmfEvent (ev.Arguments [0], ev.Arguments [1], 0, ev.Arguments.Skip (2).ToArray ());
				else
					evt = new SmfEvent (ev.Arguments [0], 0, 0, ev.Arguments.Skip (1).ToArray ());
				var msg = new SmfMessage (ev.Tick - cur, evt);
				rtrk.Messages.Add (msg);
				cur = ev.Tick;
			}
			rtrk.Messages.Add (new SmfMessage (0, new SmfEvent (0xFF, 0x2F, 0, new byte [0])));
			return rtrk;
		}
	}
}
