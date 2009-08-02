using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Commons.Music.Midi;

namespace Commons.Music.Midi.Mml
{
	internal class Util
	{
		static Util ()
		{
			DebugWriter = TextWriter.Null;
		}
		public static TextWriter DebugWriter { get; set; }
	}

	public class MmlCompilerDriver
	{
		public static void Main (string [] args)
		{
			var p = new MmlCompiler ();
			p.Compile (args);
		}
	}

	public class MmlCompiler
	{
		public MmlCompiler ()
		{
			resolver = new FileStreamResolver ();
		}

		StreamResolver resolver;

		public StreamResolver Resolver {
			get { return resolver; }
			set {
				if (value == null)
					throw new ArgumentNullException ("value");
				resolver = value;
			}
		}

		public void Compile (string [] files)
		{
			// file names -> input sources
			var inputs = new List<MmlInputSource> ();
			string outfilename = null;
			foreach (string file in files) {
				if (Path.GetFileName (file) != "default-macro.mml" && outfilename == null)
					outfilename = Path.ChangeExtension (file, ".mid");
				inputs.Add (new MmlInputSource (file, Resolver.Resolve (file)));
			}

			// input sources -> tokenizer sources
			var tokenizerSources = MmlInputSourceReader.Parse (inputs);

			// tokenizer sources -> token streams
			var tokens = MmlTokenizer.Tokenize (tokenizerSources);

			// token streams -> semantic trees
			var tree = MmlSemanticTreeBuilder.Compile (tokens);

			// semantic trees -> simplified streams
			MmlMacroExpander.Expand (tree);

			// simplified streams -> raw events
			var resolved = MmlEventStreamGenerator.Generate (tree);

			// raw events -> SMF
			var smf = MmlSmfGenerator.Generate (resolved);

			// output
			if (outfilename == null) {
				Console.WriteLine ("Give me input file other than default macro");
				return;
			}
			using (var outfile = File.Create (outfilename))
				new SmfWriter (outfile).WriteMusic (smf);
			Console.WriteLine ("Written SMF file ... {0}", outfilename);
		}
	}

	public class MmlPrimitiveOperation
	{
		public static IList<MmlPrimitiveOperation> All { get; private set; }

		static MmlPrimitiveOperation ()
		{
			var l = new List<MmlPrimitiveOperation> ();
			l.Add (new MmlPrimitiveOperation () { Name = "__LOCATE"});
			l.Add (new MmlPrimitiveOperation () { Name = "__UNLOCATE"});
			l.Add (new MmlPrimitiveOperation () { Name = "__LET"});
			l.Add (new MmlPrimitiveOperation () { Name = "__APPLY"});
			l.Add (new MmlPrimitiveOperation () { Name = "__MIDI"});
			l.Add (new MmlPrimitiveOperation () { Name = "__ON_MIDI_NOTE_OFF"});
			l.Add (new MmlPrimitiveOperation () { Name = "__MIDI_META"});
			l.Add (new MmlPrimitiveOperation () { Name = "__LOOP_BEGIN"});
			l.Add (new MmlPrimitiveOperation () { Name = "__LOOP_BREAK"});
			l.Add (new MmlPrimitiveOperation () { Name = "__LOOP_END"});
			l.Add (new MmlPrimitiveOperation () { Name = "__MACRO_ARG_DEF"}); // internal use
			l.Add (new MmlPrimitiveOperation () { Name = "__MACRO_ARG_UNDEF"}); // internal use
			All = l;
		}

		public string Name { get; set; }
	}

	public enum MmlDataType
	{
		Any,
		Number,
		Length,
		String,
	}

	public struct MmlLength
	{
		public MmlLength (int number)
		{
			Number = number;
			Dots = 0;
			IsValueByStep = false;
		}

		public int Number { get; set; }
		public int Dots { get; set; }
		public bool IsValueByStep { get; set; }

		public int GetSteps (int numerator)
		{
			if (IsValueByStep)
				return Number;
			if (Number == 0)
				return 0;
			int basis = numerator / Number;
			int ret = basis;
			for (int i = 0; i < Dots; i++)
				ret += (basis /= 2);
			return ret;
		}

		public override string ToString ()
		{
			return String.Format ("[{2}{0}{1}]", Number, new string ('.', Dots), IsValueByStep ? "%" : String.Empty);
		}
	}

	public class MmlException : Exception
	{
		public MmlException ()
			: this ("MML error", null)
		{
		}

		public MmlException (string message, MmlLineInfo location)
			: this (message, location, null)
		{
		}

		static string FormatMessage (string message, MmlLineInfo location)
		{
			if (location == null)
				return message;
			return String.Format ("{0} ({1} line {2} column {3})",
				message,
				location.File,
				location.LineNumber,
				location.LinePosition);
		}

		public MmlException (string message, MmlLineInfo location, Exception innerException)
			: base (FormatMessage (message, location), innerException)
		{
		}
	}
}
