using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Commons.Music.Midi;

namespace Commons.Music.Midi.Mml.Decompiler
{
	class Decompiler
	{
		public static void Main (string [] args)
		{
			Encoding enc = Encoding.UTF8;
			foreach (var arg in args) {
				if (arg.StartsWith ("--encoding:", StringComparison.Ordinal)) {
					enc = Encoding.GetEncoding (arg.Substring ("--encoding:".Length));
					continue;
				}
				using (var stream = File.OpenRead (arg)) {
					var smfr = new SmfReader ();
					smfr.Read (stream);
					Decompiler.Decompile (smfr.Music, Console.Out, enc);
				}
			}
		}
		
		class ChannelContext
		{
			public ChannelContext ()
			{
				Channel = -1;
				Octave = 5;
				Velocity = 100;
			}
			
			public int Channel { get; set; }
			public int Octave { get; set; }
			public int Velocity { get; set; }
		}

		public static void Decompile (SmfMusic music, TextWriter output, Encoding encoding)
		{
			new Decompiler () { Music = music, Out = output, Encoding = encoding }.Decompile ();
		}

		SmfMusic Music { get; set; }
		Encoding Encoding { get; set; }
		TextWriter Out { get; set; }

		void OutputPreprocessing (string s, params object [] args)
		{
			Out.WriteLine (string.Format (s, args));
		}

		void Decompile ()
		{
			if (Music.DeltaTimeSpec < 0)
				throw new NotSupportedException (string.Format ("Timebase is {0}. Absolute timebase is not supported.", Music.DeltaTimeSpec));
			OutputPreprocessing ("#basecount {0}", Music.DeltaTimeSpec);

			for (int i = 0; i < Music.Tracks.Count; i++)
				Decompile (i, Music.Tracks [i]);
		}

		void Decompile (int trackNo, SmfTrack track)
		{
			var msgBlockByTime = new List<List<SmfMessage>> ();
			int m = 0;
			while (m < track.Messages.Count) {
				var l = new List<SmfMessage> ();
				msgBlockByTime.Add (l);
				for (; m < track.Messages.Count; m++) {
					l.Add (track.Messages [m]);
					if (track.Messages [m].DeltaTime > 0)
						break;
				}
				m++;
			}
			var context = new ChannelContext ();
			Out.WriteLine ("// -------- track {0} --------", trackNo);
			Out.WriteLine ("{0}\t o{1} v{2}", trackNo, context.Octave, context.Velocity);
			Out.Write ("{0}\t", trackNo);
			m = 0;
			foreach (var g in msgBlockByTime) {
				OutputTrackPart (trackNo, context, g);
				m += g.Count;
				if (m > 6) {
					m = 0;
					Out.WriteLine ();
					Out.Write ("{0}\t", trackNo);
				}
			}
			Out.WriteLine ();
		}

		void OutputTrackPart (int trackNo, ChannelContext context, List<SmfMessage> messages)
		{
			int m = 0;
			while (m < messages.Count) {
				var evt = messages [m++].Event;
				if (evt.EventType < SmfEvent.SysEx1 && context.Channel != evt.Channel) {
					Out.Write ("CH");
					Out.Write (evt.Channel);
					context.Channel = evt.Channel;
					Out.Write (' ');
				};
				switch (evt.EventType) {
				case SmfEvent.PAf:
					Out.Write ("PAF");
					Out.Write (evt.Msb);
					Out.Write (',');
					Out.Write (evt.Msb);
					Out.Write (' ');
					break;
				case SmfEvent.CC:
					switch (evt.Msb) {
					case SmfCC.BankSelect:
						if (m < messages.Count && messages [m].Event.EventType == SmfEvent.Program) {
							Out.Write ("@");
							Out.Write (messages [m++].Event.Msb);
							Out.Write (',');
							Out.Write (evt.Lsb);
							break;
						}
						goto default;
					case SmfCC.DteMsb:
						if (m < messages.Count && messages [m].Event.EventType == SmfEvent.CC && messages [m].Event.Msb == SmfCC.DteLsb) {
							Out.Write ("DTE#");
							Out.Write (evt.Lsb.ToString ("X02"));
							Out.Write (",#");
							Out.Write (messages [m++].Event.Lsb.ToString ("X02"));
							break;
						} else {
							Out.Write ("DTEM#");
							Out.Write (evt.Lsb.ToString ("X02"));
						}
						break;
					case SmfCC.NrpnMsb:
						if (m < messages.Count && messages [m].Event.EventType == SmfEvent.CC && messages [m].Event.Msb == SmfCC.NrpnLsb) {
							Out.Write ("NRPN#");
							Out.Write (evt.Lsb.ToString ("X02"));
							Out.Write (",#");
							Out.Write (messages [m++].Event.Lsb.ToString ("X02"));
							break;
						} else {
							Out.Write ("NRPNM#");
							Out.Write (evt.Lsb.ToString ("X02"));
						}
						goto default;
					case SmfCC.RpnMsb:
						if (m < messages.Count && messages [m].Event.EventType == SmfEvent.CC && messages [m].Event.Msb == SmfCC.RpnLsb) {
							Out.Write ("RPN#");
							Out.Write (evt.Lsb.ToString ("X02"));
							Out.Write (",#");
							Out.Write (messages [m++].Event.Lsb.ToString ("X02"));
							break;
						} else {
							Out.Write ("RPNM#");
							Out.Write (evt.Lsb.ToString ("X02"));
						}
						goto default;
					case SmfCC.DteLsb:
						Out.Write ("DTEL#");
						Out.Write (evt.Lsb);
						break;
					case SmfCC.NrpnLsb:
						Out.Write ("NRPNL#");
						Out.Write (evt.Lsb);
						break;
					case SmfCC.RpnLsb:
						Out.Write ("RPNL#");
						Out.Write (evt.Lsb);
						break;
					case SmfCC.Csd:
						Out.Write ("CSD");
						Out.Write (evt.Lsb);
						break;
					case SmfCC.Celeste: // so far...
						Out.Write ("DSD");
						Out.Write (evt.Lsb);
						break;
					case SmfCC.Expression:
						Out.Write ("E");
						Out.Write (evt.Lsb);
						break;
					case SmfCC.Legato:
						Out.Write ("LEGATO");
						Out.Write (evt.Lsb.ToString ("X02"));
						break;
					case SmfCC.Modulation:
						Out.Write ("M");
						Out.Write (evt.Lsb);
						break;
					case SmfCC.Pan:
						Out.Write ("P");
						Out.Write (evt.Lsb);
						break;
					case SmfCC.Rsd:
						Out.Write ("RSD");
						Out.Write (evt.Lsb);
						break;
					case SmfCC.SoftPedal:
						Out.Write ("SOFT");
						Out.Write (evt.Lsb);
						break;
					case SmfCC.Sostenuto:
						Out.Write ("SOS");
						Out.Write (evt.Lsb);
						break;
					case SmfCC.Volume:
						Out.Write ("V");
						Out.Write (evt.Lsb);
						break;
					default:
						Out.Write ("CC#");
						Out.Write (evt.Msb.ToString ("X02"));
						Out.Write (",#");
						Out.Write (evt.Msb.ToString ("X02"));
						break;
					}
					Out.Write (' ');
					break;
				case SmfEvent.Program:
					Out.Write ("@");
					Out.Write (evt.Msb);
					Out.Write (' ');
					break;
				case SmfEvent.CAf:
					Out.Write ("CAF");
					Out.Write (evt.Msb);
					Out.Write (' ');
					break;
				case SmfEvent.Pitch:
					Out.Write ("PITCH");
					Out.Write ((evt.Msb * 0x80 + evt.Lsb) + 8192);
					Out.Write (' ');
					break;
				case SmfEvent.SysEx1:
					Out.Write ("__MIDI {");
					WriteBinary (evt.Data, 0, evt.Data.Length);
					Out.Write ("} ");
					break;
				case SmfEvent.Meta:
					var ttype = GetMetaStringType (evt.MetaType);
					if (ttype != null) {
						Out.Write (ttype);
						Out.Write (" \"");
						Out.Write (Encoding.GetString (evt.Data, 0, evt.Data.Length).Replace ("\"", "\"\""));
						Out.Write ('"');
					} else {
						switch (evt.MetaType) {
						case SmfMetaType.Tempo:
							Out.Write ("t");
							Out.Write ((int) (60 * 1000000 / (evt.Data [0] * 0x10000 + evt.Data [1] * 0x100 + evt.Data [2])));
							break;
						case SmfMetaType.TimeSignature:
							Out.Write ("BEAT");
							Out.Write (evt.Data [0]);
							Out.Write (",");
							Out.Write (Math.Pow (2, evt.Data [1]));
							break;
						default:
							Out.Write ("__MIDI_META { #");
							Out.Write (evt.MetaType.ToString ("X02"));
							if (evt.Data.Length > 0)
								Out.Write (", ");
							WriteBinary (evt.Data, 0, evt.Data.Length);
							Out.Write ("} ");
							break;
						}
					}
					Out.Write (' ');
					break;
				case SmfEvent.NoteOn:
					if (evt.Lsb == 0)
						goto case SmfEvent.NoteOff;
					Out.Write ("NON{0},0,{1} ", evt.Msb, evt.Lsb);
					break;
				case SmfEvent.NoteOff:
					if (evt.Lsb == 0) // usually
						Out.Write ("NOFF{0} ", evt.Msb);
					else
						Out.Write ("NOFF{0},0,{1} ", evt.Msb, evt.Lsb);
					break;
				}
			}
			int r = messages.Last ().DeltaTime;
			for (; r > Music.DeltaTimeSpec; r -= Music.DeltaTimeSpec)
				Out.Write ("r1");
			if (r > 0) {
				int b = Music.DeltaTimeSpec / r;
				if (b * r == Music.DeltaTimeSpec) // i.e. no remainder
					Out.Write ("r" + b);
				else
					Out.Write ("r#" + r);
				Out.Write (' ');
			}
		}
		
		string GetMetaStringType (int type)
		{
			switch (type) {
			case SmfMetaType.Copyright:
				return "COPYRIGHT";
			case SmfMetaType.Cue:
				return "CUE";
			case SmfMetaType.InstrumentName:
				return "INSTRUMENTNAME";
			case SmfMetaType.Lyric:
				return "LYRIC";
			case SmfMetaType.Marker:
				return "MARKER";
			case SmfMetaType.Text:
				return "TEXT";
			case SmfMetaType.TrackName:
				return "TRACKNAME";
			}
			return null;
		}
		
		void WriteBinary (byte [] data, int start, int length)
		{
			for (int x = start; x < start + length; x++) {
				if (x > 0)
					Out.Write (',');
				Out.Write ('#');
				Out.Write (x.ToString ("X02"));
			}
		}
	}
}
