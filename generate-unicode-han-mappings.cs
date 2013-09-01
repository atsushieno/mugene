using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Mugene
{
	public class ChineseUtility
	{
		public static void Main (string [] args)
		{
			switch (args [0]) {
			case "bopomofo":
				PrintBopomofo (args);
				break;
			case "pinyin":
				PrintPinyin (args);
				break;
			case "readings-bopomofo":
				DumpBopomofoReadings (args);
				break;
			case "traditional":
				GenerateTraditionalVariantsMap (args);
				break;
			case "readings":
				GenerateReadingMap (args);
				break;
			case "help":
			default:
				Console.Error.WriteLine (@"
Usage: generate-unicode-han-mappings.exe [command] [args]

Commands and args:
	traditional Unicode_Variants.txt - generate Simplified-Traditional mappings
	readings Unicode_Readings.txt - generate Hanzi-to-reading mappings
	readings-bopomofo Unicode_Readings.txt - generate Hanzi-to-reading mappings in Zhuyin
	pinyin Unicode_Readings.txt textfile - translate Chinese to Pinyin
	bopomofo Unicode_Readings.txt textfile - translate Chinese to Bopomofo
");
				break;
			}
		}

		static void HanziToPinyin (string [] args)
		{
			var s = GetPinyin (args);
			Console.WriteLine (s);
		}
		
		static void PrintBopomofo (string [] args)
		{
			var bpmf = GetBopomofo (args);
			Console.WriteLine (bpmf);
		}
		
		static void PrintPinyin (string [] args)
		{
			var bpmf = GetPinyin (args);
			Console.WriteLine (bpmf);
		}

		static void DumpBopomofoReadings (string [] args)
		{
			var s = GetReadingMap (args);
			var bpmf = PinyinToBopomofo (s);
			Console.WriteLine (bpmf);
		}
		
		static string GetPinyin (string [] args)
		{
			var map = GetReadingMap (args).Replace ('\n', ' ').Split (' ');
			var ret = File.ReadAllText (args [2]);
			return GetPinyin (map, ret);
		}
		
		static string GetPinyin (string [] map, string text)
		{
			var sb = new StringBuilder ();
			return string.Join ("", GetPinyinTokenized (map, text).Select (p => p.Key).ToArray ());
		}
		
		static IEnumerable<KeyValuePair<string,bool>> GetPinyinTokenized (string [] map, string text)
		{
			foreach (var c in text) {
				bool done = false;
				for (int i = 0; i < map.Length; i++) {
					if (map [i].Length == 0)
						continue; // how come does this happen?
					if (map [i] [0] == c) {
						yield return new KeyValuePair<string,bool> (map [i].Substring (1), true);
						done = true;
						break;
					}
					else if (map [i] [0] > c)
						break;
				}
				if (!done)
					// FIXME: super duper inefficient!
					yield return new KeyValuePair<string,bool> (c.ToString (), false);
			}
		}
		
		static string GetBopomofo (string [] args)
		{
			var map = GetReadingMap (args).Replace ('\n', ' ').Split (' ');
			var text = File.ReadAllText (args [2]);
			
			var sb = new StringBuilder ();
			var ret = GetPinyinTokenized (map, text);
			foreach (var i in ret) {
				if (i.Value)
					sb.Append (PinyinToBopomofo (i.Key));
				else
					sb.Append (i.Key);
			}
			return sb.ToString ();
		}

		static string PinyinToBopomofo (string pinyin)
		{
			var sb = new StringBuilder ();
			var item = pinyin;
//			foreach (var item in ret.Replace ("\n", "").Split (' ')) {
//				for (int i = 1; i < item.Length; i++) {
				for (int i = 0; i < item.Length; i++) {
					char c = item [i];
				{
					switch (c) {
					// consonants
					case 'b': sb.Append ('\u3105'); break;
					case 'p': sb.Append ('\u3106'); break;
					case 'm': sb.Append ('\u3107'); break;
					case 'f': sb.Append ('\u3108'); break;
					case 'd': sb.Append ('\u3109'); break;
					case 't': sb.Append ('\u310A'); break;
					case 'n': sb.Append ('\u310B'); break;
					case 'l': sb.Append ('\u310C'); break;
					case 'g': sb.Append ('\u310D'); break;
					case 'k': sb.Append ('\u310E'); break;
					case 'h': sb.Append ('\u310F'); break;
					case 'j': sb.Append ('\u3110'); break;
					case 'q': sb.Append ('\u3111'); break;
					case 'x': sb.Append ('\u3112'); break;
					case 'r':
						if (i < item.Length - 1 && item [i + 1] == 'i')
							i++;
						sb.Append ('\u3116');
						break;
					case 'z':
					case 'c':
					case 's':
						if (i < item.Length - 1) {
							switch (item [i + 1]) {
							case 'h':
								switch (item [i]) {
								case 'z': sb.Append ('\u3113'); break;
								case 'c': sb.Append ('\u3114'); break;
								case 's': sb.Append ('\u3115'); break;
								}
								break;
							case 'i':
								switch (item [i]) {
								case 'z': sb.Append ('\u3117'); break;
								case 'c': sb.Append ('\u3118'); break;
								case 's': sb.Append ('\u3119'); break;
								}
								break;
							}
							i++;
						} else {
							switch (item [i]) {
							case 'z': sb.Append ('\u3117'); break;
							case 'c': sb.Append ('\u3118'); break;
							case 's': sb.Append ('\u3119'); break;
							}
						}
						break;
					// vowels
					case 'a':
						if (i < item.Length - 1) {
							switch (item [i + 1]) {
							case 'i': sb.Append ('\u311E'); break;
							case 'o': sb.Append ('\u3120'); break;
							case 'n':
								if (i < item.Length - 2 && item [i + 2] == 'g') {
									sb.Append ('\u3124');
									i++;
								}
								else
									sb.Append ('\u3122');
								break;
							default:
								sb.Append ('\u311A');
								i--;
								break;
							}
							i++;
						}
						else
							sb.Append ('\u311A');
						break;
					case 'i':
						sb.Append ('\u3127');
						if (i < item.Length - 1 && item [i + 1] == 'e') {
							sb.Append ('\u311D');
							i++;
						}
						if (i < item.Length - 2 && item [i + 1] == 'n' && item [i + 2] == 'g') {
							sb.Append ('\u3125');
							i++;
							i++;
						}
						break;
					// FIXME: it needs to handle 'u' + U+0308 (to represent \u3129)
					case 'u': sb.Append ('\u3128'); break;
					case 'e':
						if (i < item.Length - 1) {
							switch (item [i + 1]) {
							case 'i': sb.Append ('\u311F'); break;
							case 'n':
								if (i < item.Length - 2 && item [i + 2] == 'g') {
									sb.Append ('\u3125');
									i++;
								}
								else
									sb.Append ('\u3123');
								break;
							case 'r': sb.Append ('\u3126'); break;
							case 'h': sb.Append ('\u311D'); break;
							default:
								sb.Append ('\u311C');
								i--;
								break;
							}
							i++;
						}
						else
							sb.Append ('\u311C');
						break;
					case 'o':
						if (i < item.Length - 2 && item [i + 1] == 'n' && item [i + 2] == 'g') {
							sb.Append ('\u3128');
							sb.Append ('\u3125');
							i++;
							i++;
						} else if (i < item.Length - 1 && item [i + 1] == 'u') {
							sb.Append ('\u3121');
							i++;
						}
						else
							sb.Append ('\u311B');
						break;
					case 'w':
						if (i < item.Length - 1 && item [i + 1] == 'u')
							i++;
						sb.Append ('\u3128');
						break;
					case 'y':
						if (i < item.Length - 1) {
							switch (item [i + 1]) {
							case 'u': sb.Append ('\u3129'); i++; break;
							case 'i':
								if (i < item.Length - 3 && item [i + 2] == 'n' && item [i + 3] == 'g')
									break; // deal with them at 'i'.
								sb.Append ('\u3127'); i++; break;
							case 'e':
								sb.Append ('\u3127');
								sb.Append ('\u311D');
								i++;
								break;
							case 'a':
							case 'o':
								// does not consume those, but valid sequence.
								sb.Append ('\u3127');
								break;
							default: // anything else is unexpected.
								throw new InvalidOperationException (item [i + 1].ToString ());
							}
						}
						else
							throw new InvalidOperationException ();
						break;
					default:
						sb.Append (c);
						break;
					}
				}
			}
			return sb.ToString ();
		}

		static void GenerateReadingMap (string [] args)
		{
			var ret = GetReadingMap (args);
			Console.WriteLine (ret);
			Console.WriteLine ("Database size: " + ret.Length);
		}
		
		static string GetReadingMap (string [] args)
		{
			StringBuilder sb = new StringBuilder ();
			int n = 0;
			foreach (var line in File.ReadAllLines (args [1])) {
				var ents = line.Split (null);
				if (ents.Length < 3 || ents [1] != "kMandarin")
					continue;
				AppendHexAsChar (sb, ents [0]);
				var nfd = ents [2].Normalize (NormalizationForm.FormD);
				int tone = 0, existing = 0;
				foreach (var c in nfd) {
					switch (c) {
					case '\u0300': tone += 4; goto case '\0';
					case '\u0301': tone += 2; goto case '\0';
					case '\u0304': tone += 1; goto case '\0';
					case '\u030C': tone += 3; goto case '\0';
					case '\u0308': tone = 8; goto case '\0';
					default:
						if (c < 'a' || 'z' < c)
							Console.Error.WriteLine ("{0:X04} [{1}] [{2}] [{3}]", (int) c, c, ents [0], nfd);
						else
							sb.Append (c);
							break;
						tone = -1;
						goto case '\0';
					case '\0':
						if (existing != 0 && existing != 8) // 8 is the only valid combination
							Console.Error.WriteLine ("More than one tone: {0}: [{1}]", ents [0], string.Join (" ", nfd.Select (i => ((int) i).ToString ("X04")).ToArray ()));
						else if (tone != 0)
							existing = tone;
						break;
					}
				}
				sb.Append (tone.ToString ());
				sb.Append (' ');
				if (++n % 16 == 0)
					sb.Append ('\n');
			}
			return sb.ToString ();
		}

		static void GenerateTraditionalVariantsMap (string [] args)
		{
			StringBuilder sb = new StringBuilder ();
			int n = 0;
			foreach (var line in File.ReadAllLines (args [1])) {
				var ents = line.Split (null);
				if (ents.Length < 3 || ents [1] != "kTraditionalVariant")
					continue;
				AppendHexAsChar (sb, ents [0]);
				AppendHexAsChar (sb, ents [2]);
				if (++n % 16 == 0)
					sb.Append ('\n');
			}
			Console.WriteLine (sb);
			Console.WriteLine ("Database size: " + sb.Length);
		}

		static void AppendHexAsChar (StringBuilder sb, string s)
		{
			int x = int.Parse (s.Substring (2), NumberStyles.HexNumber);
			if (x < 0x10000)
				sb.Append ((char) x);
			else
				sb.Append ((char) ((x - 0x10000) / 0x400 + 0xD800)).Append ((char) ((x - 0x10000) % 0x400 + 0xDC00));
		}
	}
}
