using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Commons.Music.Midi.Mml
{
	#region mml token sequence structure

	public enum MmlTokenType
	{
		Identifier,
		StringLiteral,
		NumberLiteral,
		Period,
		Comma,
		Percent,
		OpenParen,
		CloseParen,
		OpenCurly,
		CloseCurly,
		Question,
		Plus,
		Minus,
		Asterisk,
		Slash,
		Dollar,
		Colon,
		Caret,
		BackSlashLesser,
		BackSlashLesserEqual,
		BackSlashGreater,
		BackSlashGreaterEqual,
		KeywordNumber,
		KeywordLength,
		KeywordString,
		KeywordBuffer,
	}

	public class MmlTokenSet
	{
		public MmlTokenSet ()
		{
			BaseCount = 192;
			Conditional = new MmlCompilationCondition ();
			Macros = new List<MmlMacroDefinition> ();
			Variables = new List<MmlVariableDefinition> ();
			Tracks = new List<MmlTrack> ();
			MetaTexts = new List<KeyValuePair<byte, string>> ();
		}

		public int BaseCount { get; set; }

		public MmlCompilationCondition Conditional { get; private set; }
		public List<MmlMacroDefinition> Macros { get; private set; }
		public List<MmlVariableDefinition> Variables { get; private set; }
		public List<MmlTrack> Tracks { get; private set; }
		public List<KeyValuePair<byte,string>> MetaTexts { get; private set; }
		
		public MmlTrack GetTrack (int number)
		{
			MmlTrack t = Tracks.FirstOrDefault (tr => tr.Number == number);
			if (t == null) {
				t = new MmlTrack (number);
				Tracks.Add (t);
			}
			return t;
		}
	}

	public class MmlCompilationCondition
	{
		public MmlCompilationCondition ()
		{
			Blocks = new List<string> ();
			Tracks = new List<int> ();
		}

		public List<int> Tracks { get; private set; }
		public IList<string> Blocks { get; private set; }
		
		public bool ShouldCompileBlock (string name)
		{
			return name == null || Blocks.Count == 0 || Blocks.Contains (name);
		}
		
		public bool ShouldCompileTrack (int track)
		{
			return Tracks.Count == 0 || Tracks.Contains (track);
		}
	}

	public class MmlToken
	{
		public MmlTokenType TokenType { get; set; }
		public object Value { get; set; }
		public MmlLineInfo Location { get; set; }
	}

	public abstract class MmlOperationDefinition
	{
		protected MmlOperationDefinition ()
		{
			Arguments = new List<MmlVariableDefinition> ();
		}

		public List<MmlVariableDefinition> Arguments { get; private set; }
		public string Name { get; set; }
	}

	public class MmlMacroDefinition : MmlOperationDefinition
	{
		public MmlMacroDefinition (string name, IList<int> targetTracks, MmlLineInfo location)
		{
			Name = name;
			TargetTracks = targetTracks;
			Location = location;
			Tokens = new List<MmlToken> ();
		}

		public IList<int> TargetTracks { get; private set; }
		public MmlLineInfo Location { get; private set; }
		public List<MmlToken> Tokens { get; private set; }
	}
	
	public class MmlVariableDefinition
	{
		public MmlVariableDefinition (string name, MmlLineInfo location)
		{
			Name = name;
			Location = location;
			DefaultValueTokens = new List<MmlToken> ();
		}
		
		public string Name { get; set; }
		public MmlLineInfo Location { get; private set; }
		public MmlDataType Type { get; set; }
		public List<MmlToken> DefaultValueTokens { get; private set; }
	}
	
	public class MmlTrack
	{
		public MmlTrack (int number)
		{
			Number = number;
			Tokens = new List<MmlToken> ();
		}

		public int Number { get; private set; }
		public List<MmlToken> Tokens { get; private set; }
	}

	#endregion

	#region input sources to tokenizer sources

	public abstract class StreamResolver
	{
		public virtual TextReader Resolve (string file)
		{
			var ret = OnResolve (file);
			if (ret == null)
				throw new IOException ($"MML stream {file} could not be resolved.");
			return ret;
		}

		protected internal abstract TextReader OnResolve (string file);
		
		Stack<string> includes = new Stack<string> ();

		public virtual void PushInclude (string file)
		{
			if (includes.Contains (file))
				throw new InvalidOperationException (string.Format ("File {0} is already being processed. Recursive inclusion is prohibited.", file));
			includes.Push (file);
		}

		public virtual void PopInclude ()
		{
			includes.Pop ();
		}
	}

	public class MergeStreamResolver : StreamResolver
	{
		StreamResolver [] resolvers;

		public MergeStreamResolver (params StreamResolver [] resolvers)
		{
			this.resolvers = resolvers;
		}

		protected internal override TextReader OnResolve (string file)
		{
			foreach (var r in resolvers) {
				var ret = r.OnResolve (file);
				if (ret != null)
					return ret;
			}
			return null;
		}
	}

	public class ManifestResourceStreamResolver : StreamResolver
	{
		protected internal override TextReader OnResolve (string file)
		{
			var res = GetType ().Assembly.GetManifestResourceStream (file);
			return res == null ? null : new StreamReader (res);
		}
	}

	public class LocalFileStreamResolver : StreamResolver
	{
		protected internal override TextReader OnResolve (string file)
		{
			if (File.Exists (file))
				return File.OpenText (file);
			string commonPath = Path.Combine (Path.GetDirectoryName (new Uri (GetType ().Assembly.CodeBase).LocalPath), "mml", file);
			if (File.Exists (commonPath))
				return File.OpenText (commonPath);
			else
				return null;
		}
	}

	// file sources to be parsed into MmlSourceLineSet, for each track
	// and macro.
	public class MmlInputSource
	{
		public MmlInputSource (string file, TextReader reader)
		{
			File = file;
			Reader = reader;
		}

		public string File { get; private set; }
		public TextReader Reader { get; private set; }
	}

	public class MmlLineInfo
	{
		public MmlLineInfo (string file, int line)
		{
			File = file;
			LineNumber = line;
			LinePosition = 0;
		}

		public string File { get; private set; }
		public int LineNumber { get; private set; }
		public int LinePosition { get; set; }

		public MmlLineInfo Clone ()
		{
			return new MmlLineInfo (File, LineNumber) { LinePosition = this.LinePosition };
		}

		public override string ToString ()
		{
			return String.Format ("Location: {0} ({1}, {2})", File, LineNumber, LinePosition);
		}
	}

	public class MmlLine
	{
		public static MmlLine Create (string file, int lineNumber, string text)
		{
			return new MmlLine (new MmlLineInfo (file, lineNumber), text);
		}

		public MmlLine (MmlLineInfo location, string text)
		{
			Location = location;
			Text = text;
		}

		public MmlLineInfo Location { get; set; }
		public string Text { get; set; }

		public bool TryMatch (string target)
		{
			if (Location.LinePosition + target.Length > Text.Length)
				return false;
			if (String.CompareOrdinal (Text, Location.LinePosition, target, 0, target.Length) != 0)
				return false;
			Location.LinePosition += target.Length;
			return true;
		}

		public int PeekChar ()
		{
			if (Location.LinePosition == Text.Length)
				return -1;
			return Text [Location.LinePosition];
		}

		public int ReadChar ()
		{
			if (Location.LinePosition == Text.Length)
				return -1;
			return Text [Location.LinePosition++];
		}

		public MmlLine Clone ()
		{
			var loc = new MmlLineInfo (Location.File, Location.LineNumber);
			loc.LinePosition = Location.LinePosition;
			return new MmlLine (loc, Text);
		}
	}

	public class MmlTokenizerSource
	{
		MmlLexer match_longest;

		public MmlLexer MatchLongest {
			get {
				if (match_longest == null)
					match_longest = new MmlMatchLongestLexer (this);
				return match_longest;
			}
		}

		// can be switched
		public MmlLexer Lexer { get; set; }

		// It holds ongoing definition of a macro. Used for argument name lookup.
		public MmlMacroDefinition CurrentMacroDefinition { get; set; }

		// It does not differentiate tracks, but contains all of the mml track lines.
		public List<MmlTrackSource> Tracks { get; private set; }
		// contains macros.
		public List<MmlMacroSource> Macros { get; private set; }
		// contains variables.
		public List<MmlVariableSource> Variables { get; private set; }
		// contains any other pragma directives.
		public List<MmlPragmaSource> Pragmas { get; private set; }

		public List<string> PrimitiveOperations { get; private set; }

		public MmlTokenizerSource ()
		{
			Lexer = MatchLongest;

			Tracks = new List<MmlTrackSource> ();
			Macros = new List<MmlMacroSource> ();
			Pragmas = new List<MmlPragmaSource> ();
			Variables = new List<MmlVariableSource> ();

			PrimitiveOperations = new List<string> ();
			foreach (var primitive in MmlPrimitiveOperation.All)
				PrimitiveOperations.Add (primitive.Name);
		}
	}

	public class MmlInputSourceReader
	{
		public static MmlTokenizerSource Parse (MmlCompiler compiler, IList<MmlInputSource> inputs)
		{
			var r = new MmlInputSourceReader (compiler);
			r.Process (inputs);
			return r.result;
		}

		MmlCompiler compiler;
		MmlTokenizerSource result;

		public MmlInputSourceReader (MmlCompiler compiler)
		{
			this.compiler = compiler;
		}

		bool in_comment_mode;

		static string TrimComments (string s, int start)
		{
			int idx2 = s.IndexOf ("//", start, s.Length - start, StringComparison.Ordinal);
			if (idx2 < 0)
				return s;
			int idx1 = s.IndexOf ('"', start);
			if (idx1 < 0 || idx2 < idx1)
				return s.Substring (0, idx2);
			int idx3 = s.IndexOf ('"', idx1 + 1);
			if (idx3 < 0) // it is invalid, but I don't care here
				return s.Substring (0, idx2);
			if (idx3 > idx2)
				return TrimComments (s, idx3 + 1); // skip this "//" inside literal
			else
				return TrimComments (s, idx3 + 1); // skip this literal. There still may be another literal to care.
		}

		public void Process (IList<MmlInputSource> inputs)
		{
			result = new MmlTokenizerSource ();
			DoProcess (inputs);
		}

		void DoProcess (IList<MmlInputSource> inputs)
		{
			for (int i = 0; i < inputs.Count; i++) {
				int line = 0;
				string s = String.Empty;
				MmlSourceLineSet ls = null;
				// inputs could grow up.
				var input = inputs [i];
				bool continued = false;
				while (true) {
					s = input.Reader.ReadLine ();
					line++;
					bool wasContinued = continued;
					if (s == null) {
						if (wasContinued)
							throw MmlError (input, line, "Unexpected end of consecutive line by '\\' at the end of file");
						break;
					}
					s = TrimComments (s, 0);
					if (s.Length == 0) // comment line is allowed inside multi-line MML.
						continue;

					continued = (s [s.Length - 1] == '\\');
					if (continued)
						s = s.Substring (0, s.Length - 1);
					if (wasContinued) {
						if (!in_comment_mode)
							ls.AddConsecutiveLine (s);
						continue;
					}
					if (s [0] == '#')
						ls = ProcessPragmaLine (MmlLine.Create (input.File, line, s));
					else
						ls = ProcessTrackLine (MmlLine.Create (input.File, line, s));
				}
			}
		}

		MmlSourceLineSet ProcessPragmaLine (MmlLine line)
		{
			result.Lexer.SetCurrentInput (line);
			line.Location.LinePosition++;
			// get identifier
			var identifier = result.Lexer.ReadNewIdentifier ();
			switch (identifier) {
			case "include":
				result.Lexer.SkipWhitespaces (true);
				return ProcessIncludeLine (line);
			case "variable":
				result.Lexer.SkipWhitespaces (true);
				return ProcessVariableLine (line);
			case "macro":
				result.Lexer.SkipWhitespaces (true);
				return ProcessMacroLine (line);
			case "comment":
				in_comment_mode = true;
				return null;
			case "endcomment":
				in_comment_mode = false;
				return null;
			case "define":
			case "conditional":
			case "meta":
			case "basecount":
				break;
			default:
				throw MmlError (line.Location, String.Format ("Unexpected preprocessor directive: {0}", identifier));
			}

			result.Lexer.SkipWhitespaces (true);
			var ps = new MmlPragmaSource (identifier);
			ps.Lines.Add (line);
			result.Pragmas.Add (ps);
			return ps;
		}
		
		MmlSourceLineSet ProcessIncludeLine (MmlLine line)
		{
			string file = line.Text.Substring (line.Location.LinePosition).Trim ();
			compiler.Resolver.PushInclude (file);
			this.DoProcess (new MmlInputSource [] {new MmlInputSource (file, compiler.Resolver.Resolve (file))});
			compiler.Resolver.PopInclude ();
			return new MmlUntypedSource (line);
		}

		MmlSourceLineSet ProcessMacroLine (MmlLine line)
		{
			if (in_comment_mode)
				return null;
			var mms = new MmlMacroSource ();
			mms.Lines.Add (line);
			result.Macros.Add (mms);
			return mms;
		}

		MmlSourceLineSet ProcessVariableLine (MmlLine line)
		{
			if (in_comment_mode)
				return null;
			var vs = new MmlVariableSource ();
			vs.Lines.Add (line);
			result.Variables.Add (vs);
			return vs;
		}

		string previous_section;
		int [] previous_range;

		MmlSourceLineSet ProcessTrackLine (MmlLine line)
		{
			if (in_comment_mode)
				return null;
			result.Lexer.SetCurrentInput (line);

			string section = previous_section;
			int [] range = previous_range;
			if (result.Lexer.IsWhitespace (line.PeekChar ()))
				result.Lexer.SkipWhitespaces (true);
			else {
				if (result.Lexer.IsIdentifier (line.PeekChar (), true)) {
					section = result.Lexer.ReadNewIdentifier ();
					result.Lexer.SkipWhitespaces (false);
				}
				if (result.Lexer.IsNumber (line.PeekChar ())) {
					range = result.Lexer.ReadRange ().ToArray ();
					result.Lexer.SkipWhitespaces (true);
				}
			}
			if (range == null)
				throw new MmlException ("Current line indicates no track number, and there was no indicated tracks previously.", line.Location);

			previous_section = section;
			previous_range = range;
			result.Lexer.SkipWhitespaces (false);
			var ts = new MmlTrackSource (section, range);
			ts.Lines.Add (line);
			result.Tracks.Add (ts);
			return ts;
		}

		public Exception MmlError (MmlLineInfo location, string msg)
		{
			return new MmlException (msg, location);
		}

		public Exception MmlError (MmlInputSource input, int line, string msg)
		{
			return new MmlException (msg, new MmlLineInfo (input.File, line));
		}
	}

	#endregion

	#region tokenizer sources to parsed mml tokens

	// represents a set of lines for either a macro or a track lines.

	public abstract class MmlSourceLineSet
	{
		public List<MmlLine> Lines { get; private set; }

		protected MmlSourceLineSet ()
		{
			Lines = new List<MmlLine> ();
		}

		public virtual void AddConsecutiveLine (string text)
		{
			if (Lines.Count == 0)
				throw new InvalidOperationException ("Unexpected addition to previous line while there was no registered line.");
			var prev = Lines [Lines.Count - 1];
			MmlLine line = MmlLine.Create (prev.Location.File, prev.Location.LineNumber + 1, text);
			Lines.Add (line);
		}
	}
		
	class MmlUntypedSource : MmlSourceLineSet
	{
		public MmlUntypedSource (MmlLine singleLine)
		{
			Lines.Add (singleLine);
		}
	}

	public class MmlTrackSource : MmlSourceLineSet
	{
		public MmlTrackSource (string blockName, IEnumerable<int> tracks)
		{
			BlockName = blockName;
			Tracks = new List<int> ();
			foreach (var i in tracks)
				Tracks.Add (i);
		}

		public string BlockName { get; private set; }
		public IList<int> Tracks { get; private set; }
	}

	public class MmlMacroSource : MmlSourceLineSet
	{
		public MmlMacroSource ()
		{
		}

		public string ParsedName { get; set; }
	}

	public class MmlVariableSource : MmlSourceLineSet
	{
		public MmlVariableSource ()
		{
			ParsedNames = new List<string> ();
		}

		public List<string> ParsedNames { get; private set; }
	}

	public class MmlPragmaSource : MmlSourceLineSet
	{
		public MmlPragmaSource (string name)
		{
			Name = name;
		}

		public string Name { get; set; }
	}

	public enum MmlSourceLineSetKind
	{
		Track,
		Macro,
		Pragma,
	}

	public abstract class MmlLexer
	{
		protected MmlLexer (MmlTokenizerSource source)
		{
			TokenizerSource = source;
		}

		MmlSourceLineSet input;
		int current_line;

		// It contains all macro definitions.
		public MmlTokenizerSource TokenizerSource { get; private set; }

		public MmlLine Line {
			get {
				var l = input.Lines [current_line];
				if (l.Location.LinePosition == l.Text.Length && current_line + 1 < input.Lines.Count) {
					current_line++;
					return Line;
				}
				return input.Lines [current_line];
			}
		}

		public MmlTokenType CurrentToken { get; set; }
		public object Value { get; set; }

		public bool NewIdentifierMode { get; set; }

		public void SetCurrentInput (MmlLine line)
		{
			SetCurrentInput (new MmlUntypedSource (line));
		}

		public void SetCurrentInput (MmlSourceLineSet input)
		{
			this.input = input;
			current_line = 0;
		}

		internal Exception LexerError (string msg)
		{
			if (Line == null)
				return new Exception (msg);
			return new MmlException (msg, Line.Location);
		}

		public MmlToken CreateParsedToken ()
		{
			return new MmlToken () { TokenType = CurrentToken, Value = this.Value, Location = Line.Location.Clone () };
		}
		public virtual bool IsWhitespace (int ch)
		{
			return ch == ' ' || ch == '\t';
		}

		public virtual void SkipWhitespaces ()
		{
			SkipWhitespaces (false);
		}

		public virtual void SkipWhitespaces (bool mandatory)
		{
			if (mandatory && !IsWhitespace (Line.PeekChar ()))
				throw LexerError ("Whitespaces are expected");

			while (IsWhitespace (Line.PeekChar ()))
				Line.ReadChar ();
		}

		public virtual bool IsNumber (int c)
		{
			return '0' <= c && c <= '9';
		}

		public virtual int ReadNumber ()
		{
			var line = Line;
			int ch_ = line.PeekChar ();
			if (ch_ < 0)
				throw LexerError ("Expected a number, but reached the end of input");
			char ch = (char) ch_;
			if (ch != '#' && !IsNumber (ch))
				throw LexerError (String.Format ("Expected a number, but got '{0}'", ch));
			if (ch == '#') {
				line.ReadChar ();
				int val = 0;
				bool passed = false;
				while (true) {
					ch = (char) line.PeekChar ();
					bool isnum = IsNumber (ch);
					bool isupper = 'A' <= ch && ch <= 'F';
					bool islower = 'a' <= ch && ch <= 'f';
					if (!isnum && !isupper && !islower) {
						if (!passed)
							throw LexerError ("Invalid hexadecimal digits");
						break;
					}
					passed = true;
					int h = 
						isnum ? line.ReadChar () - '0' :
						isupper ? line.ReadChar () - 'A' + 10 :
						line.ReadChar () - 'a' + 10;
					val = val * 16 + h;
				}
				return val;
			} else {
				int val = 0;
				while (true) {
					val = val * 10 + (line.ReadChar () - '0');
					if (!IsNumber ((char) line.PeekChar ()))
						break;
				} 
				return val;
			}
		}

		StringBuilder string_literal_buffer = new StringBuilder ();

		public virtual string ReadStringLiteral ()
		{
			var sb = string_literal_buffer;
			sb.Length = 0;
			Line.ReadChar (); // ' or "
			var startLoc = Line.Location;
			while (true) {
				int ch = Line.ReadChar ();
				switch (ch) {
				case -1:
					throw LexerError (String.Format ("Incomplete string literal starting from {0}", startLoc));
				case '"':
					return sb.ToString ();
				case '\\':
					ch = Line.ReadChar ();
					switch (ch) {
					case '/': // This is a quick workaround to avoid "//" being treated as comment, even within a string literal
						sb.Append ('/');
						break;
					case '"':
						sb.Append ('"');
						break;
					case '\\':
						sb.Append ('\\');
						break;
					case 'r':
						sb.Append ('\r');
						break;
					case 'n':
						sb.Append ('\n');
						break;
					default:
						Line.Location.LinePosition--;
						if (ch == '#' || '0' <= ch && ch <= '9') {
							sb.Append ((char) ReadNumber ());
							ch = Line.ReadChar ();
							if (ch != ';')
								throw LexerError ("Unexpected string escape sequence: ';' is expected after number escape sequence");
						}
						else
							throw LexerError (String.Format ("Unexpected string escape sequence: \\{0}", (char) ch));
						break;
					}
					break;
				default:
					sb.Append ((char) ch);
					break;
				}
			}
		}

		public virtual IEnumerable<int> ReadRange ()
		{
			int i = ReadNumber ();
			switch (Line.PeekChar ()) {
			case '-':
				Line.ReadChar ();
				int j = ReadNumber ();
				if (j < i)
					throw LexerError ("Invalid range specification: larger number must appear later");
				while (i <= j)
					yield return i++;
				break;
			case ',':
				yield return i;
				Line.ReadChar ();
				// recursion
				foreach (int ii in ReadRange ())
					yield return ii;
				break;
			default:
				yield return i;
				break;
			}
		}

		public virtual bool IsIdentifier (int c, bool isStartChar)
		{
			if (c < 0)
				return false;
			if (IsWhitespace (c))
				return false;

			if (IsNumber (c))
				return false;

			switch (c) {
			case '\r':
			case '\n':
				throw LexerError ("INTERNAL ERROR: this should not accept EOLs");
			case '?': // conditional
			case '+': // addition
			case '-': // subtraction
			case '^': // length-addition
			case '#': // hex number prefix / preprocessor directive at line head
				return !isStartChar; // could be part of identifier
			case ':': // variable argument-type separator / loop break
			case '/': // division / loop break
			case '%': // modulo / length by step
			case '(': // parenthesized expr / velocity down
			case ')': // parenthesized expr / velocity up
				return isStartChar; // valid only as head character
			case '*': // multiplication
			case '$': // variable reference
			case ',': // identifier separator
			case '"': // string quotation
			case '{': // macro body start
			case '}': // macro body end
			case '\\': // escape sequence marker
				return false;
			}

			// everything else is regarded as a valid identifier
			return true;
		}

		public string ReadNewIdentifier ()
		{
			int start = Line.Location.LinePosition;
			if (!IsIdentifier (Line.ReadChar (), true))
				throw LexerError ("Identifier character is expected");
			while (IsIdentifier (Line.PeekChar (), false))
				Line.ReadChar ();
//Util.DebugWriter.WriteLine ("NEW Identifier: " + Line.Text.Substring (start, Line.Location.LinePosition - start));
			return Line.Text.Substring (start, Line.Location.LinePosition - start);
		}

		public void ExpectNext (MmlTokenType tokenType)
		{
			if (!Advance ())
				throw LexerError (String.Format ("Expected token {0}, but reached end of the input", CurrentToken));
			ExpectCurrent (tokenType);
		}
		
		public void ExpectCurrent (MmlTokenType tokenType)
		{
			if (CurrentToken != tokenType)
				throw LexerError (String.Format ("Expected token {0} but found {1}", tokenType, CurrentToken));
		}

		public virtual bool Advance ()
		{
			var ret = _advance ();
//Util.DebugWriter.WriteLine ("TOKEN: {0} : Value: {1}", CurrentToken, Value);
			return ret;
		}
		
		bool _advance ()
		{
			SkipWhitespaces ();
			int ch_ = Line.PeekChar ();
			if (ch_ < 0)
				return false;
			char ch = (char) ch_;
			switch (ch) {
			case '.':
				ConsumeAsToken (MmlTokenType.Period);
				return true;
			case ',':
				ConsumeAsToken (MmlTokenType.Comma);
				return true;
			case '%':
				ConsumeAsToken (MmlTokenType.Percent);
				return true;
			case '{':
				ConsumeAsToken (MmlTokenType.OpenCurly);
				return true;
			case '}':
				ConsumeAsToken (MmlTokenType.CloseCurly);
				return true;
			case '?':
				ConsumeAsToken (MmlTokenType.Question);
				return true;
			case '^':
				ConsumeAsToken (MmlTokenType.Caret);
				return true;
			case '+':
				ConsumeAsToken (MmlTokenType.Plus);
				return true;
			case '-':
				ConsumeAsToken (MmlTokenType.Minus);
				return true;
			case '*':
				ConsumeAsToken (MmlTokenType.Asterisk);
				return true;
			case ':':
				ConsumeAsTokenOrIdentifier (MmlTokenType.Colon, ":");
				return true;
			case '/':
				ConsumeAsTokenOrIdentifier (MmlTokenType.Slash, "/");
				return true;
			case '\\':
				Line.ReadChar ();
				ch_ = Line.PeekChar ();
				switch (ch_) {
				case '<':
					Line.ReadChar ();
					if (Line.PeekChar () == '=')
						ConsumeAsToken (MmlTokenType.BackSlashLesserEqual);
					else {
						CurrentToken = MmlTokenType.BackSlashLesser;
						Value = null;
					}
					return true;
				case '>':
					Line.ReadChar ();
					if (Line.PeekChar () == '=')
						ConsumeAsToken (MmlTokenType.BackSlashGreaterEqual);
					else {
						CurrentToken = MmlTokenType.BackSlashGreater;
						Value = null;
					}
					return true;
				default:
					throw new MmlException (String.Format ("Unexpected escaped token: '\\{0}'", (char) ch_), Line.Location);
				}
			case '$':
				ConsumeAsToken (MmlTokenType.Dollar);
				return true;
			case '"':
				Value = ReadStringLiteral ();
				CurrentToken = MmlTokenType.StringLiteral;
				return true;
			}
			if (ch == '#' || IsNumber (ch)) {
				Value = ReadNumber ();
				CurrentToken = MmlTokenType.NumberLiteral;
				return true;
			}
			if (TryParseTypeName ())
				return true;
			if (NewIdentifierMode) {
				Value = ReadNewIdentifier ();
				CurrentToken = MmlTokenType.Identifier;
				return true;
			}
			var ident = TryReadIdentifier ();
			if (ident != null) {
				Value = ident;
				CurrentToken = MmlTokenType.Identifier;
				return true;
			}

			throw LexerError (String.Format ("The lexer could not read a valid token: '{0}'", ch));
		}

		bool TryParseTypeName ()
		{
			if (Line.TryMatch ("number")) {
				Value = MmlDataType.Number;
				CurrentToken = MmlTokenType.KeywordNumber;
			} else if (Line.TryMatch ("length")) {
				Value = MmlDataType.Length;
				CurrentToken = MmlTokenType.KeywordLength;
			} else if (Line.TryMatch ("string")) {
				Value = MmlDataType.String;
				CurrentToken = MmlTokenType.KeywordString;
			} else if (Line.TryMatch ("buffer")) {
				Value = MmlDataType.Buffer;
				CurrentToken = MmlTokenType.KeywordBuffer;
			}
			else
				return false;
			return true;
		}

		void ConsumeAsToken (MmlTokenType token)
		{
			Line.ReadChar ();
			CurrentToken = token;
			Value = null;
		}

		void ConsumeAsTokenOrIdentifier (MmlTokenType token, string value)
		{
			ConsumeAsToken (token);
			Value = value;
		}

		public abstract string TryReadIdentifier ();

		public virtual IEnumerable<string> GetValidIdentifiers ()
		{
			if (TokenizerSource.CurrentMacroDefinition != null)
				foreach (var a in TokenizerSource.CurrentMacroDefinition.Arguments)
					yield return a.Name;
			foreach (var v in TokenizerSource.Variables)
				foreach (var s in v.ParsedNames)
					yield return s;
			foreach (var m in TokenizerSource.Macros)
				if (m.ParsedName != null)
					yield return m.ParsedName;
			foreach (var pname in TokenizerSource.PrimitiveOperations)
				yield return pname;
		}
	}

	public class MmlMatchLongestLexer : MmlLexer
	{
		public MmlMatchLongestLexer (MmlTokenizerSource source)
			: base (source)
		{
		}

		int [] matchpos;
		char [] buffer = new char[256];
		int buffer_pos;

		public override string TryReadIdentifier ()
		{
			if (matchpos == null)
				matchpos = new int [TokenizerSource.Macros.Count];
			if (matchpos.Length < TokenizerSource.Macros.Count)
				throw new InvalidOperationException ("Macro definition is added somewhere after the first macro search is invoked.");
			string matched = null;

			buffer_pos = 0; // reset

			foreach (var name in GetValidIdentifiers ()) {
				if (matched != null && matched.Length >= name.Length)
					continue; // no hope it could match.
//Util.DebugWriter.WriteLine ("!!! {0} / {1} / {2}", name, matched, buffer_pos);
				if (Matches (name))
					matched = name;
			}
			if (matched != null)
				return matched;

//			/*
			// then it could be a new identifier.
			// In such case, read up until the input comes to non-identifier.
			// If it is not a valid identifier input, then return null.
			if (buffer_pos == 0) {
				if (!IsIdentifier (Line.PeekChar (), true))
					return null; // not an identifier
				buffer [buffer_pos++] = (char) Line.ReadChar ();
			}

			while (true) {
				int ch = Line.PeekChar ();
				if (!IsIdentifier (ch, false))
					break;
				if (buffer.Length == buffer_pos) {
					var newbuf = new char [buffer.Length << 1];
					Array.Copy (buffer, 0, newbuf, 0, buffer.Length);
					buffer = newbuf;
				}
				buffer [buffer_pos++] = (char) ch;
				Line.ReadChar ();
			}
			return new string (buffer, 0, buffer_pos);
//			*/
//			return null;
		}

		// examines if current token matches the argument identifier,
		// proceeding the MmlLine.
		bool Matches (string name)
		{
			bool ret = false;
			int savedPos = Line.Location.LinePosition;
			int savedBufferPos = buffer_pos;
			ret = MatchesProceed (name);
			if (!ret) {
				buffer_pos = savedBufferPos;
				Line.Location.LinePosition = savedPos;
			}
			return ret;
		}

		bool MatchesProceed (string name)
		{
			for (int i = 0; i < buffer_pos; i++) {
				if (i == name.Length)
					return true; // matched within the buffer
				if (buffer [i] != name [i])
					return false;
			}
			while (buffer_pos < name.Length) {
				if (buffer.Length == buffer_pos) {
					var newbuf = new char [buffer.Length << 1];
					Array.Copy (buffer, 0, newbuf, 0, buffer.Length);
					buffer = newbuf;
				}
				int ch_ = Line.PeekChar ();
				if (ch_ < 0)
					return false;
				buffer [buffer_pos] = (char) ch_;
//Util.DebugWriter.WriteLine ("$$$ {0} {1} ({2})", buffer [buffer_pos], name [buffer_pos], buffer_pos);
				if (buffer [buffer_pos] != name [buffer_pos])
					return false;
				buffer_pos++;
				Line.ReadChar ();
			}
			return true;
		}
	}

	public class MmlTokenizer
	{
		static readonly Dictionary<string,byte> meta_map = new Dictionary<string, byte> ();
		static MmlTokenizer ()
		{
			meta_map.Add ("text", 1);
			meta_map.Add ("copyright", 2);
			meta_map.Add ("title", 3);
		}
		
		public static MmlTokenSet Tokenize (MmlTokenizerSource source)
		{
			var tokenizer = new MmlTokenizer (source);
			tokenizer.Process ();
			return tokenizer.result;
		}

		public MmlTokenizer (MmlTokenizerSource source)
		{
			this.source = source;
			this.result = new MmlTokenSet ();
		}

		MmlTokenizerSource source;
		Dictionary<string,string> aliases = new Dictionary<string, string> ();
		MmlTokenSet result;

		public void Process ()
		{
			// process pragmas
			foreach (var ps in source.Pragmas)
				ParsePragmaLines (ps);

			// add built-in variables
			result.Variables.Add (new MmlVariableDefinition ("__timeline_position", null) { Type = MmlDataType.Number });
			var bc = new MmlVariableDefinition ("__base_count", null) { Type = MmlDataType.Number };
			bc.DefaultValueTokens.Add (new MmlToken () { TokenType = MmlTokenType.NumberLiteral, Value = result.BaseCount });
			result.Variables.Add (bc);

			// process variables
			foreach (var vs in source.Variables)
				ParseVariableLines (vs);

			// process macros, recursively
			foreach (var ms in source.Macros)
				ParseMacroLines (ms);

			// process tracks
			foreach (var ts in source.Tracks)
				ParseTrackLines (ts);
		}

		void ParsePragmaLines (MmlPragmaSource src)
		{
			source.Lexer.SetCurrentInput (src);
			switch (src.Name) {
			default:
				throw new NotImplementedException ();
			case "basecount":
				source.Lexer.ExpectNext (MmlTokenType.NumberLiteral);
				result.BaseCount = (int) source.Lexer.Value;
				MmlValueExpr.BaseCount = result.BaseCount;
				break;
			case "conditional":
				var category = source.Lexer.ReadNewIdentifier ();
				switch (category) {
				case "block":
					source.Lexer.SkipWhitespaces (true);
					while (true) {
						source.Lexer.NewIdentifierMode = true;
						source.Lexer.ExpectNext (MmlTokenType.Identifier);
						string s = (string) source.Lexer.Value;
						result.Conditional.Blocks.Add (s);
						source.Lexer.SkipWhitespaces ();
						if (!source.Lexer.Advance () || source.Lexer.CurrentToken != MmlTokenType.Comma)
							break;
						source.Lexer.SkipWhitespaces ();
					}
					if (source.Lexer.Advance ())
						throw new MmlException ("Extra conditional tokens", source.Lexer.Line.Location);
					source.Lexer.NewIdentifierMode = false;
					break;
				case "track":
					source.Lexer.SkipWhitespaces (true);
					var tracks = source.Lexer.ReadRange ().ToArray ();
					result.Conditional.Tracks.AddRange (tracks);
					source.Lexer.SkipWhitespaces ();
					if (source.Lexer.Advance ())
						throw new MmlException ("Extra conditional tokens", source.Lexer.Line.Location);
					break;
				default:
					throw new MmlException (String.Format ("Unexpected compilation condition type '{0}'", category), source.Lexer.Line.Location);
				}
				break;
			case "meta":
				source.Lexer.NewIdentifierMode = true;
				var identifier = source.Lexer.ReadNewIdentifier ();
				source.Lexer.SkipWhitespaces (true);
				var text = source.Lexer.ReadStringLiteral ();
				switch (identifier) {
				case "title":
				case "copyright":
				case "text":
					break;
				default:
					throw new MmlException (String.Format ("Invalid #meta directive argument: {0}", identifier), source.Lexer.Line.Location);
				}
				result.MetaTexts.Add (new KeyValuePair<byte,string> (meta_map [identifier], text));
				source.Lexer.NewIdentifierMode = false;
				break;
			case "define":
				source.Lexer.NewIdentifierMode = true;
				identifier = source.Lexer.ReadNewIdentifier ();
				source.Lexer.SkipWhitespaces (true);
				if (aliases.ContainsKey (identifier))
					Console.WriteLine ("Warning: overwriting definition {0}, redefined at {1}", identifier, source.Lexer.Line.Location);
				aliases [identifier] = source.Lexer.Line.Text.Substring (source.Lexer.Line.Location.LinePosition);
				source.Lexer.NewIdentifierMode = false;
				break;
			}
		}

		void ParseVariableLines (MmlVariableSource src)
		{
			foreach (var line in src.Lines)
				foreach (var entry in aliases)
					line.Text = line.Text.Replace (entry.Key, entry.Value);
			source.Lexer.SetCurrentInput (src);

			source.Lexer.NewIdentifierMode = true;
			source.Lexer.Advance ();
			int idx = result.Variables.Count;
			ParseVariableList (result.Variables, true);
			for (int i = idx; i < result.Variables.Count; i++)
				src.ParsedNames.Add (result.Variables [i].Name);
			source.Lexer.NewIdentifierMode = false;
		}

		void ParseMacroLines (MmlMacroSource src)
		{
//Util.DebugWriter.WriteLine ("Parsing Macro: " + src.Name);
			foreach (var line in src.Lines)
				foreach (var entry in aliases)
					line.Text = line.Text.Replace (entry.Key, entry.Value);
			source.Lexer.SetCurrentInput (src);

			int [] range = null;
			var ch = source.Lexer.Line.PeekChar ();
			if (ch == '#' || source.Lexer.IsNumber ((char) ch)) {
				range = source.Lexer.ReadRange ().ToArray ();
				source.Lexer.SkipWhitespaces (true);
			}

			// get identifier
			var identifier = source.Lexer.ReadNewIdentifier ();
			source.Lexer.SkipWhitespaces (true);

			src.ParsedName = identifier;

			var m = new MmlMacroDefinition (identifier, range, src.Lines [0].Location);
			source.CurrentMacroDefinition = m;
			if (m.Tokens.Count == 0) {
				// get args
				source.Lexer.NewIdentifierMode = true;
				source.Lexer.Advance ();
				ParseVariableList (m.Arguments, false);
			}
			source.Lexer.NewIdentifierMode = false;
			while (source.Lexer.Advance ())
				m.Tokens.Add (source.Lexer.CreateParsedToken ());
			if (m.Tokens.Count == 0 || m.Tokens [m.Tokens.Count - 1].TokenType != MmlTokenType.CloseCurly)
				source.Lexer.LexerError (String.Format ("'{{' is expected at the end of macro definition for '{0}'", identifier));
			m.Tokens.RemoveAt (m.Tokens.Count - 1);
			result.Macros.Add (m);
			source.CurrentMacroDefinition = null;
		}

		void ParseVariableList (List<MmlVariableDefinition> vars, bool isVariable)
		{
			int count = 0;
			while (true) {
				if (source.Lexer.CurrentToken == MmlTokenType.OpenCurly)
					break; // go to parse body
				if (count > 0) {
					source.Lexer.ExpectCurrent (MmlTokenType.Comma);
					source.Lexer.NewIdentifierMode = true;
					source.Lexer.Advance ();
				}
				source.Lexer.ExpectCurrent (MmlTokenType.Identifier);
				var arg = new MmlVariableDefinition ((string) source.Lexer.Value, source.Lexer.Line.Location);
				vars.Add (arg);
				count++;

				// FIXME: possibly use MmlToken.Colon?
				source.Lexer.SkipWhitespaces ();
				if (source.Lexer.Line.PeekChar () != ':') {
					arg.Type = MmlDataType.Number;
					if (!source.Lexer.Advance () && isVariable)
						return;
					continue;
				}
				source.Lexer.Line.ReadChar ();

				source.Lexer.NewIdentifierMode = false;
				if (!source.Lexer.Advance ())
					throw source.Lexer.LexerError ("type name is expected after ':' in macro argument definition");
				switch (source.Lexer.CurrentToken) {
				case MmlTokenType.KeywordNumber:
				case MmlTokenType.KeywordString:
				case MmlTokenType.KeywordLength:
				case MmlTokenType.KeywordBuffer:
					break;
				default:
					throw new MmlException (String.Format ("Data type name is expected, but got {0}", source.Lexer.CurrentToken), source.Lexer.Line.Location);
				}
				arg.Type = (MmlDataType) source.Lexer.Value;
				source.Lexer.SkipWhitespaces ();
				if (source.Lexer.Line.PeekChar () != '=') {
					if (!source.Lexer.Advance () && isVariable)
						return;
					continue;
				}
				source.Lexer.Line.ReadChar ();

				bool loop = true;
				while (loop) {
					if (!source.Lexer.Advance ()) {
						if (isVariable)
							return;
						throw source.Lexer.LexerError ("Incomplete argument default value definition");
					}
					switch (source.Lexer.CurrentToken) {
					case MmlTokenType.Comma:
					case MmlTokenType.OpenCurly:
						loop = false;
						continue;
					}

					arg.DefaultValueTokens.Add (source.Lexer.CreateParsedToken ());
				}
			}
		}

		void ParseTrackLines (MmlTrackSource src)
		{
			var tokens = new List<MmlToken> ();
			foreach (var line in src.Lines)
				foreach (var entry in aliases)
					line.Text = line.Text.Replace (entry.Key, entry.Value);
			source.Lexer.SetCurrentInput (src);
			while (source.Lexer.Advance ())
					tokens.Add (source.Lexer.CreateParsedToken ());
			// Compilation conditionals are actually handled here.
			if (!result.Conditional.ShouldCompileBlock (src.BlockName))
				return;
			foreach (var t in src.Tracks) {
				if (result.Conditional.ShouldCompileTrack (t))
					result.GetTrack (t).Tokens.AddRange (tokens);
			}
		}
	}
	#endregion
}
