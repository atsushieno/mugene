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
		public const string VsqInclude = "vsq-support.mml";

		static List<string> default_includes = new List<string> (new string [] {
			"default-macro.mml",
			"drum-part.mml",
			"gs-sysex.mml",
			"nrpn-gs-xg.mml",
		});

		static Util ()
		{
			DebugWriter = TextWriter.Null;
		}
		public static TextWriter DebugWriter { get; set; }
		
		public static IList<string> DefaultIncludes {
			get { return default_includes; }
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
		
		const string help = @"
Usage: mugene [options] mml_files

Options:
  --output
			specify explicit output file name.
  --vsq
			Vocaloid VSQ mode on.
			Changes extension to .vsq and encoding to ShiftJIS,
			and uses VSQ metadata mode.
  --verbose
			prints debugging aid.
  --use-vsq-metadata
			uses Vocaloid VSQ metadata mode.
  --disable-running-status
			disables running status in SMF.
  --encoding
			uses specified encoding.
			Shift_JIS, euc-jp, iso-2022-jp etc.
  --nodefault
			prevents default mml files being included.
			This option is for core MML operation hackers.
";

		void CompileCore (string [] args)
		{
			if (args == null || args.Length == 0)
				throw new MmlException (help, null);

			// file names -> input sources
			var inputFilenames = new List<string> ();
			string outfilename = null, explicitfilename = null;
			bool disableRunningStatus = false;
			bool useVsqMetadata = false;
			string extension = ".mid";
			var metaWriter = SmfWriterExtension.DefaultMetaEventWriter;
			bool noDefault = false;

			foreach (string arg in args) {
				switch (arg) {
				case "--nodefault":
					noDefault = true;
					continue;
				case "--vsq": // for convenience
					useVsqMetadata = true;
					disableRunningStatus = true;
					MmlValueExpr.StringToBytes = s => Encoding.GetEncoding (932).GetBytes (s);
					extension = ".vsq";
					Util.DefaultIncludes.Add (Util.VsqInclude);
					continue;
				case "--verbose":
					verbose = true;
					continue;
				case "--use-vsq-metadata":
					useVsqMetadata = true;
					Util.DefaultIncludes.Add (Util.VsqInclude);
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
					if (arg == "--help")
						throw new MmlException (help, null);
					break;
				}
				outfilename = Path.ChangeExtension (arg, extension);
				inputFilenames.Add (arg);
			}
			if (explicitfilename != null)
				outfilename = explicitfilename;
			
			var resolver = new FileStreamResolver ();
			Resolver = resolver;
			if (!noDefault) {
				foreach (var fname in Util.DefaultIncludes) {
					resolver.DefaultFiles.Add (fname);
					inputFilenames.Add (fname);
				}
			}

			var inputs = new List<MmlInputSource> ();
			foreach (var fname in inputFilenames)
				inputs.Add (new MmlInputSource (fname, Resolver.Resolve (fname)));
			if (useVsqMetadata)
				metaWriter = SmfWriterExtension.VsqMetaTextSplitter;

			using (var output = File.Create (outfilename))
				Compile (inputs, metaWriter, output, disableRunningStatus);
			Console.WriteLine ("Written SMF file ... {0}", outfilename);
		}

		public void Compile (IList<MmlInputSource> inputs, Func<bool, SmfEvent, Stream, int> metaWriter, Stream output, bool disableRunningStatus)
		{
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
			var w = new SmfWriter (output);
			w.DisableRunningStatus = disableRunningStatus;
			if (metaWriter != null)
				w.MetaEventWriter = metaWriter;
			w.WriteMusic (smf);
		}
	}

	public class MmlPrimitiveOperation
	{
		public static IList<MmlPrimitiveOperation> All { get; private set; }

		static MmlPrimitiveOperation ()
		{
			var l = new List<MmlPrimitiveOperation> ();
			//l.Add (new MmlPrimitiveOperation () { Name = "__LOCATE"});
			//l.Add (new MmlPrimitiveOperation () { Name = "__UNLOCATE"});
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
#if !UNHACK_LOOP
			l.Add (new MmlPrimitiveOperation () { Name = "["});
			l.Add (new MmlPrimitiveOperation () { Name = ":"});
			l.Add (new MmlPrimitiveOperation () { Name = "/"});
			l.Add (new MmlPrimitiveOperation () { Name = "]"});
#endif
//			l.Add (new MmlPrimitiveOperation () { Name = "__MACRO_ARG_DEF"}); // internal use
//			l.Add (new MmlPrimitiveOperation () { Name = "__MACRO_ARG_UNDEF"}); // internal use
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
		public MmlLength (int num)
		{
			dots = 0;
			is_value_by_step = false;
			number = num;
		}

		int number, dots;
		bool is_value_by_step;
		public int Number { get { return number; } set { number = value; } }
		public int Dots { get { return dots; } set { dots = value; } }
		public bool IsValueByStep { get { return is_value_by_step; } set { is_value_by_step = value; } }

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
