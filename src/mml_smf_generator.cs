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
				SmfMessage msg;
				if (ev.Arguments.Count == 3)
					msg = new SmfMessage (ev.Arguments [0], ev.Arguments [1], ev.Arguments [2], null);
				else if (ev.Arguments [0] == 0xFF)
					msg = new SmfMessage (ev.Arguments [0], ev.Arguments [1], 0, ev.Arguments.Skip (2).ToArray ());
				else
					msg = new SmfMessage (ev.Arguments [0], 0, 0, ev.Arguments.Skip (1).ToArray ());
				var smfev = new SmfEvent (ev.Tick - cur, msg);
				rtrk.Events.Add (smfev);
				cur = ev.Tick;
			}
			rtrk.Events.Add (new SmfEvent (0, new SmfMessage (0xFF, 0x2F, 0, new byte [0])));
			return rtrk;
		}
	}
}
