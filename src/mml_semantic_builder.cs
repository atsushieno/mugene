using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Commons.Music.Midi.Mml
{
	#region semantic tree structure

	public class MmlSemanticTreeSet
	{
		public MmlSemanticTreeSet ()
		{
			Tracks = new List<MmlSemanticTrack> ();
			Macros = new List<MmlSemanticMacro> ();
			Variables = new List<MmlSemanticVariable> ();
		}

		public List<MmlSemanticTrack> Tracks { get; private set; }
		public List<MmlSemanticMacro> Macros { get; private set; }
		public List<MmlSemanticVariable> Variables { get; private set; }
	}

	public partial class MmlSemanticTrack
	{
		public MmlSemanticTrack (int number)
		{
			Number = number;
			Data = new List<MmlOperationUse> ();
		}

		public int Number { get; private set; }
		public List<MmlOperationUse> Data { get; private set; }
	}

	public partial class MmlSemanticMacro
	{
		public MmlSemanticMacro (string name, IList<int> targetTracks)
		{
			Name = name;
			TargetTracks = targetTracks;
			Arguments = new List<MmlSemanticVariable> ();
			Data = new List<MmlOperationUse> ();
		}

		public string Name { get; private set; }
		public IList<int> TargetTracks { get; private set; }
		public List<MmlSemanticVariable> Arguments { get; private set; }
		public List<MmlOperationUse> Data { get; private set; }
	}

	public class MmlSemanticVariable
	{
		public MmlSemanticVariable (string name, MmlDataType type)
		{
			Name = name;
			Type = type;
		}

		public string Name { get; private set; }
		public MmlDataType Type { get; private set; }
		public MmlValueExpr DefaultValue { get; set; }

		public void FillDefaultValue ()
		{
			switch (Type) {
			case MmlDataType.Number:
			case MmlDataType.Length:
				DefaultValue = new MmlConstantExpr (Type, 0);
				break;
			case MmlDataType.String:
				DefaultValue = new MmlConstantExpr (Type, "");
				break;
			case MmlDataType.Any:
				// it happens only for macro arg definition.
				break;
			default:
				throw new NotImplementedException ("type " + Type);
			}
		}
		
		public override string ToString ()
		{
			if (DefaultValue != null)
				return String.Format ("{0}:{1}(={2})", Name, Type, DefaultValue);
			else
				return String.Format ("{0}:{1}", Name, Type);
		}
	}

	public abstract partial class MmlValueExpr
	{
		public static int ComputeLength (int baseValue, int dots)
		{
			int ret = baseValue;
			for (int add = baseValue / 2; dots > 0; add /= 2)
				ret += add;
			return ret;
		}
	}

	public partial class MmlConstantExpr : MmlValueExpr
	{
		public MmlConstantExpr (MmlDataType type, object value)
		{
			Type = type;
			Value = value;
		}

		public MmlDataType Type { get; private set; }
		public object Value { get; private set; }

		public override string ToString ()
		{
			switch (Type) {
			case MmlDataType.Number:
				return String.Format ("#{0:X}", Value);
			case MmlDataType.String:
				return String.Format ("\"{0}\"", Value);
			default:
				return String.Format ("Constant({0}:{1})", Value, Type);
			}
		}
	}

	public partial class MmlVariableReferenceExpr : MmlValueExpr
	{
		public MmlVariableReferenceExpr (string name)
			: this (name, 1)
		{
		}
		public MmlVariableReferenceExpr (string name, int scope)
		{
			Scope = scope;
			Name = name;
		}

		public int Scope { get; private set; }

		public string Name { get; private set; }

		public override string ToString ()
		{
			return String.Format ("${0}{1}", Scope > 1 ? "$$" : "", Name);
		}
	}

	public partial class MmlParenthesizedExpr : MmlValueExpr
	{
		public MmlParenthesizedExpr (MmlValueExpr content)
		{
			Content = content;
		}

		public MmlValueExpr Content { get; private set; }

		public override string ToString ()
		{
			return String.Format ("({0})", Content);
		}
	}

	public abstract partial class MmlArithmeticExpr : MmlValueExpr
	{
		protected MmlArithmeticExpr (MmlValueExpr left, MmlValueExpr right)
		{
			Left = left;
			Right = right;
		}

		public MmlValueExpr Left { get; private set; }
		public MmlValueExpr Right { get; private set; }
	}

	public partial class MmlAddExpr : MmlArithmeticExpr
	{
		public MmlAddExpr (MmlValueExpr left, MmlValueExpr right)
			: base (left, right)
		{
		}

		public override string ToString ()
		{
			return String.Format ("{0} + {1}", Left, Right);
		}
	}

	public partial class MmlSubtractExpr : MmlArithmeticExpr
	{
		public MmlSubtractExpr (MmlValueExpr left, MmlValueExpr right)
			: base (left, right)
		{
		}

		public override string ToString ()
		{
			return String.Format ("{0} - {1}", Left, Right);
		}
	}

	public partial class MmlMultiplyExpr : MmlArithmeticExpr
	{
		public MmlMultiplyExpr (MmlValueExpr left, MmlValueExpr right)
			: base (left, right)
		{
		}

		public override string ToString ()
		{
			return String.Format ("{0} * {1}", Left, Right);
		}
	}

	public partial class MmlDivideExpr : MmlArithmeticExpr
	{
		public MmlDivideExpr (MmlValueExpr left, MmlValueExpr right)
			: base (left, right)
		{
		}

		public override string ToString ()
		{
			return String.Format ("{0} / {1}", Left, Right);
		}
	}

	public partial class MmlModuloExpr : MmlArithmeticExpr
	{
		public MmlModuloExpr (MmlValueExpr left, MmlValueExpr right)
			: base (left, right)
		{
		}

		public override string ToString ()
		{
			return String.Format ("{0} % {1}", Left, Right);
		}
	}

	public partial class MmlConditionalExpr : MmlValueExpr
	{
		public MmlConditionalExpr (MmlValueExpr condition, MmlValueExpr trueExpr, MmlValueExpr falseExpr)
		{
			Condition = condition;
			TrueExpr = trueExpr;
			FalseExpr = falseExpr;
		}

		public MmlValueExpr Condition { get; set; }
		public MmlValueExpr TrueExpr { get; set; }
		public MmlValueExpr FalseExpr { get; set; }

		public override string ToString ()
		{
			return String.Format ("{0} ? {1}, {2}", Condition, TrueExpr, FalseExpr);
		}
	}

	// at this phase, we cannot determine if an invoked operation is a macro, or a primitive operation.
	public partial class MmlOperationUse
	{
		public MmlOperationUse (string name, MmlLineInfo location)
		{
			if (name == null)
				throw new ArgumentNullException ("name");
			Name = name;
			Location = location;
			Arguments = new List<MmlValueExpr> ();
		}

		public string Name { get; private set; }

		public MmlLineInfo Location { get; private set; }

		public List<MmlValueExpr> Arguments { get; private set; }

		public override string ToString ()
		{
			string [] args = new string [Arguments.Count];
			for (int i = 0; i < args.Length; i++)
				args [i] = Arguments [i].ToString ();
			return String.Format ("{0} {{ {1} }}", Name, String.Join (",", args));
		}
	}

	#endregion

	#region semantic tree builder

	public class MmlSemanticTreeBuilder
	{
		public static MmlSemanticTreeSet Compile (MmlTokenSet tokenSet)
		{
			var b = new MmlSemanticTreeBuilder (tokenSet);
			b.Compile ();
			return b.result;
		}

		public MmlSemanticTreeBuilder (MmlTokenSet tokenSet)
		{
			if (tokenSet == null)
				throw new ArgumentNullException ("tokenSet");
			token_set = tokenSet;
			result = new MmlSemanticTreeSet ();
		}

		MmlTokenSet token_set;
		MmlSemanticTreeSet result;

		void Compile ()
		{
			var metaTrack = new MmlSemanticTrack (0);
			foreach (var p in token_set.MetaTexts) {
				var use = new MmlOperationUse ("__MIDI_META", null);
				use.Arguments.Add (new MmlConstantExpr (MmlDataType.Number, p.Key));
				use.Arguments.Add (new MmlConstantExpr (MmlDataType.String, p.Value));
				metaTrack.Data.Add (use);
			}
			result.Tracks.Add (metaTrack);
			// compile variable reference tokens into expr
			foreach (var variable in token_set.Variables)
				result.Variables.Add (BuildVariableDeclaration (variable));
			// build operation list for macros
			foreach (var macro in token_set.Macros)
				result.Macros.Add (BuildMacroOperationList (macro));
			
			// build operation list for tracks
			foreach (var track in token_set.Tracks)
				result.Tracks.Add (BuildTrackOperationList (track));
		}

		MmlSemanticVariable BuildVariableDeclaration (MmlVariableDefinition src)
		{
//Util.DebugWriter.WriteLine ("Compiling variable {0}, {1} token(s)", src.Name, src.DefaultValueTokens.Count);
			var ret = new MmlSemanticVariable (src.Name, src.Type);

			if (src.DefaultValueTokens.Count == 0)
				return ret;

			var stream = new TokenStream (src.DefaultValueTokens, src.Location);
#if USE_OLD_PARSER
			ret.DefaultValue = stream.ReadDefiniteValue ();
			stream.End ();
#else
			ret.DefaultValue = new Parser.MmlParser (stream.Source).ParseExpression ();
#endif

			return ret;
		}

		MmlSemanticMacro BuildMacroOperationList (MmlMacroDefinition src)
		{
//Util.DebugWriter.WriteLine ("Compiling macro {0}, {1} args, {2} tokens", src.Name, src.Arguments.Count, src.Tokens.Count);
//foreach (var ttt in src.Tokens) Util.DebugWriter.WriteLine ("{0} {1}", ttt.TokenType, ttt.Value);
			var ret = new MmlSemanticMacro (src.Name, src.TargetTracks);

			foreach (var arg in src.Arguments)
				ret.Arguments.Add (BuildVariableDeclaration (arg));

			CompileOperationTokens (ret.Data, new TokenStream (src.Tokens, src.Location));

			return ret;
		}

		MmlSemanticTrack BuildTrackOperationList (MmlTrack src)
		{
//Util.DebugWriter.WriteLine ("Compiling track {0}", src.Number);
			var ret = new MmlSemanticTrack (src.Number);
			CompileOperationTokens (ret.Data, new TokenStream (src.Tokens, null));
			return ret;
		}

		void CompileOperationTokens (List<MmlOperationUse> data, TokenStream stream)
		{
#if USE_OLD_PARSER
			do {
				var oper = stream.ReadOperationUse ();
				if (oper == null)
					break;
				data.Add (oper);
			} while (true);
#else
			data.AddRange (new Parser.MmlParser (stream.Source).ParseOperations ());
#endif
		}
	}

	public class TokenStream
	{
		public TokenStream (IList<MmlToken> source, MmlLineInfo definitionLocation)
		{
			Source = source;
			DefinitionLocation = definitionLocation;
		}

		public MmlLineInfo DefinitionLocation { get; private set; }

		public IList<MmlToken> Source { get; private set; }

		public int Position { get; set; }

#if USE_OLD_PARSER
		public void End ()
		{
			if (Position < Source.Count)
				throw new MmlException ("Extra tokens are found", Source [Position].Location);
		}

		bool IsIdentifier (MmlToken token)
		{
			switch (token.TokenType) {
			case MmlTokenType.Identifier:
			case MmlTokenType.Colon:
			case MmlTokenType.Slash:
				return true;
			}
			return false;
		}

		public MmlOperationUse ReadOperationUse ()
		{
			var t = PeekToken (false);
			if (t == null)
				return null;
			if (!IsIdentifier (t))
				throw new MmlException (String.Format ("Identifier is expected, but got {0}", t.TokenType), t.Location);

			var name = (string) t.Value;
			bool isPrimitive = name.StartsWith ("__", StringComparison.Ordinal);
			var ret = new MmlOperationUse (name, t.Location);

			ReadToken (); // consume identifier.

			if (isPrimitive) {
				ExpectToken (MmlTokenType.OpenCurly);
				ReadToken ();
			}

			t = PeekToken (false);
			if (t == null || IsIdentifier (t)) // next token may be an operation.
				return ret;
			do {
				// for now it allows trailing comma. See key macros
				if ((t = PeekToken ()) == null)
					break;
				if (isPrimitive && t.TokenType == MmlTokenType.CloseCurly)
					break;

				var v = PeekToken ().TokenType == MmlTokenType.Comma ? null : ReadAsValue ();
				ret.Arguments.Add (v);
				t = PeekToken (false);
				if (t == null || t.TokenType != MmlTokenType.Comma)
					break;
				ReadToken ();
			} while (true);

			if (isPrimitive) {
				if (PeekToken (false) == null)
					throw new MmlException ("')' is missing at the end of primitive operation", DefinitionLocation);
				ExpectToken (MmlTokenType.CloseCurly);
				ReadToken ();
			}

			return ret;
		}

		// FIXME: consider operator precedence
		public MmlValueExpr ReadAsValue ()
		{
			// FIXME: handle unary expression
			var next = PeekToken (true);
			switch (next.TokenType) {
			case MmlTokenType.Minus:
				ReadToken ();
				return new MmlMultiplyExpr (ReadAsValue (), new MmlConstantExpr (MmlDataType.Number, -1));
			case MmlTokenType.Percent:
				ReadToken ();
				bool negative = false;
				if (PeekToken (true).TokenType == MmlTokenType.Minus) {
					negative = true;
					ReadToken ();
				}
				var lengthBody = ReadAsValue () as MmlConstantExpr;
				if (lengthBody == null)
					throw new MmlException ("Only constant number is valid as absolute step specification", DefinitionLocation);
				return new MmlConstantExpr (MmlDataType.Length, new MmlLength ((negative ? -1 : 1) * (int) (double) MmlValueExpr.GetTypedValue (lengthBody.Value, MmlDataType.Number)) { IsValueByStep = true });
			}

			// get primary token
			var primary = ReadPrimaryTokenValue ();

			next = PeekToken ();
			if (next == null)
				return primary;

			switch (next.TokenType) {
			case MmlTokenType.Question:
				ReadToken ();
				var trueExpr = ReadAsValue ();
				ExpectToken (MmlTokenType.Comma);
				ReadToken ();
				var falseExpr = ReadAsValue ();
				return new MmlConditionalExpr (primary, trueExpr, falseExpr);
			case MmlTokenType.Plus:
				ReadToken ();
				return new MmlAddExpr (primary, ReadAsValue ());
			case MmlTokenType.Minus:
				ReadToken ();
				return new MmlSubtractExpr (primary, ReadAsValue ());
			case MmlTokenType.Asterisk:
				ReadToken ();
				return new MmlMultiplyExpr (primary, ReadAsValue ());
			case MmlTokenType.Slash:
				ReadToken ();
				return new MmlDivideExpr (primary, ReadAsValue ());
			case MmlTokenType.Percent:
				ReadToken ();
				return new MmlModuloExpr (primary, ReadAsValue ());
			}

			return primary;
		}

		MmlValueExpr ReadPrimaryTokenValue ()
		{
			var t = ReadToken ();
			switch (t.TokenType) {
			default:
			case MmlTokenType.Identifier:
			case MmlTokenType.Comma:
			case MmlTokenType.Question:
			case MmlTokenType.Plus:
			case MmlTokenType.Minus:
			case MmlTokenType.Slash: // allowed as a macro name, but not valid for a value primary token anyways.
			case MmlTokenType.Asterisk:
			case MmlTokenType.KeywordString:
			case MmlTokenType.KeywordNumber:
				throw new MmlException (String.Format ("Unexpected token {0}", t.TokenType), t.Location);
			case MmlTokenType.OpenCurly:
				var pexpr = ReadAsValue ();
				ExpectToken (MmlTokenType.CloseCurly);
				ReadToken ();
				return new MmlParenthesizedExpr (pexpr);
			case MmlTokenType.Dollar:
				t = ReadToken ();
#if true
				// variable reference
				RejectNonIdentifier (t);
				return new MmlVariableReferenceExpr ((string) t.Value);
#else
				{
					int scope = 1;
					while (t.TokenType == MmlToken.Dollar) {
						t = ReadToken ();
						scope++;
						if (scope == 3)
							break;
					}
					// variable reference
					RejectNonIdentifier (t);
					return new MmlVariableReferenceExpr ((string) t.Value, scope);
				}
#endif
			case MmlTokenType.StringLiteral:
				return new MmlConstantExpr (MmlDataType.String, t.Value);
			case MmlTokenType.NumberLiteral:
				int dots = 0;
				MmlToken dot = null;
				while ((dot = PeekToken (false)) != null && dot.TokenType == MmlTokenType.Period) {
					dots++;
					ReadToken ();
				}
				if (dots > 0)
					return new MmlConstantExpr (MmlDataType.Length, new MmlLength ((int) t.Value) { Dots = dots });
				return new MmlConstantExpr (MmlDataType.Number, t.Value);
			default:
				throw new MmlException (String.Format ("Unexpected token for constant: {0}", t.TokenType), t.Location);
			}
		}

		public MmlToken PeekToken ()
		{
			return PeekToken (false);
		}

		public MmlToken PeekToken (bool required)
		{
			if (Position >= Source.Count) {
				if (required)
					throw new MmlException ("Insufficient mml token", DefinitionLocation);
				return null;
			}
			return Source [Position];
		}

		public MmlToken ReadToken ()
		{
			if (Position >= Source.Count)
				throw new MmlException ("Insufficient mml token", DefinitionLocation);
			return Source [Position++];
		}

		public void ExpectToken (MmlTokenType type)
		{
			if (Position == Source.Count)
				throw new MmlException ("Insufficient mml token", DefinitionLocation);
			if (Source [Position].TokenType != type)
				throw new MmlException (String.Format ("Expected token {0}, but got {1}", type, Source [Position].TokenType), Source [Position].Location);
		}

		void RejectNonIdentifier (MmlToken token)
		{
			switch (token.TokenType) {
			case MmlTokenType.Identifier:
			case MmlTokenType.Slash:
				return;
			default:
				throw new MmlException (String.Format ("Identifier is expected, but got {0}", token.TokenType), token.Location);
			}
		}
#endif
	}

	#endregion
}
