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
		bool verbose;

		public StreamResolver Resolver {
			get { return resolver; }
			set {
				if (value == null)
					throw new ArgumentNullException ("value");
				resolver = value;
			}
		}

		public void Compile (string [] args)
		{
			try {
				CompileCore (args);
			} catch (MmlException ex) {
				Console.Error.WriteLine (ex.Message);
			} catch (Exception ex) {
				if (verbose)
					throw;
				Console.Error.WriteLine (ex);
			}
		}

		void CompileCore (string [] args)
		{
			// file names -> input sources
			var inputs = new List<MmlInputSource> ();
			string outfilename = null, explicitfilename = null;
			bool disableRunningStatus = false;
			bool useVsqMetadata = false;
			foreach (string arg in args) {
				switch (arg) {
				case "--verbose":
					verbose = true;
					continue;
				case "--use-vsq-metadata":
					useVsqMetadata = true;
					continue;
				case "--disable-running-status":
					disableRunningStatus = true;
					continue;
				default:
					if (arg.StartsWith ("--encoding:", StringComparison.Ordinal)) {
						var enc = Encoding.GetEncoding (arg.Substring (11));
						MmlValueExpr.StringToBytes = s => enc.GetBytes (s);
						continue;
					}
					if (arg.StartsWith ("--output:", StringComparison.Ordinal)) {
						explicitfilename = arg.Substring (9);
						continue;
					}
					break;
				}
				outfilename = Path.ChangeExtension (arg, ".mid");
				inputs.Add (new MmlInputSource (arg, Resolver.Resolve (arg)));
			}
			if (explicitfilename != null)
				outfilename = explicitfilename;

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
			using (var outfile = File.Create (outfilename)) {
				var w = new SmfWriter (outfile);
				w.DisableRunningStatus = disableRunningStatus;
				if (useVsqMetadata)
					w.MetaEventWriter = SmfWriterExtension.VsqMetaTextSplitter;
				w.WriteMusic (smf);
			}
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
			l.Add (new MmlPrimitiveOperation () { Name = "__PRINT"});
			l.Add (new MmlPrimitiveOperation () { Name = "__LET"});
			l.Add (new MmlPrimitiveOperation () { Name = "__STORE"});
			l.Add (new MmlPrimitiveOperation () { Name = "__STORE_FORMAT"});
			l.Add (new MmlPrimitiveOperation () { Name = "__APPLY"});
			l.Add (new MmlPrimitiveOperation () { Name = "__MIDI"});
			l.Add (new MmlPrimitiveOperation () { Name = "__ON_MIDI_NOTE_OFF"});
			l.Add (new MmlPrimitiveOperation () { Name = "__MIDI_META"});
			l.Add (new MmlPrimitiveOperation () { Name = "__SAVE_OPER_BEGIN"});
			l.Add (new MmlPrimitiveOperation () { Name = "__SAVE_OPER_END"});
			l.Add (new MmlPrimitiveOperation () { Name = "__RESTORE_OPER"});
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
		Buffer,
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
