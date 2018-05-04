using System;
using System.Collections;
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
			BaseCount = 192;
			Tracks = new List<MmlSemanticTrack> ();
			Macros = new List<MmlSemanticMacro> ();
			Variables = new Hashtable ();
		}

		public int BaseCount { get; set; }
		public List<MmlSemanticTrack> Tracks { get; private set; }
		public List<MmlSemanticMacro> Macros { get; private set; }
		public Hashtable Variables { get; private set; }
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
			case MmlDataType.Buffer:
				// Note that it never fills a specific StringBuilder object
				// It should be instantiated in each Resolve() evaluation instead.
				DefaultValue = new MmlConstantExpr (Type, null);
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

	public partial class MmlComparisonExpr : MmlValueExpr
	{
		public MmlComparisonExpr (MmlValueExpr left, MmlValueExpr right, ComparisonType type)
		{
			Left = left;
			Right = right;
			ComparisonType = type;
		}

		public MmlValueExpr Left { get; set; }
		public MmlValueExpr Right { get; set; }
		public ComparisonType ComparisonType { get; set; }

		public override string ToString ()
		{
			return String.Format ("{0} {2} {1}", Left, Right, ToString (ComparisonType));
		}
		
		string ToString (ComparisonType type)
		{
			switch (type) {
			case ComparisonType.Lesser:
				return "<";
			case ComparisonType.LesserEqual:
				return "<=";
			case ComparisonType.Greater:
				return ">";
			case ComparisonType.GreaterEqual:
				return ">";
			}
			throw new Exception ();
		}
	}

	public enum ComparisonType
	{
		Lesser,
		LesserEqual,
		Greater,
		GreaterEqual,
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
			result = new MmlSemanticTreeSet () { BaseCount = tokenSet.BaseCount };
		}

		MmlTokenSet token_set;
		MmlSemanticTreeSet result;

		void Compile ()
		{
			var metaTrack = new MmlSemanticTrack (0);
			foreach (var p in token_set.MetaTexts) {
				var use = new MmlOperationUse (MmlPrimitiveOperation.MidiMeta.Name, null);
				use.Arguments.Add (new MmlConstantExpr (MmlDataType.Number, p.Key));
				use.Arguments.Add (new MmlConstantExpr (MmlDataType.String, p.Value));
				metaTrack.Data.Add (use);
			}
			if (metaTrack.Data.Count > 0)
				result.Tracks.Add (metaTrack);
			// compile variable reference tokens into expr
			foreach (var variable in token_set.Variables)
				result.Variables.Add (variable.Name,BuildVariableDeclaration (variable));
			// build operation list for macros
			foreach (var macro in token_set.Macros)
				result.Macros.Add (BuildMacroOperationList (macro));
			
			// build operation list for tracks
			foreach (var track in token_set.Tracks)
				result.Tracks.Add (BuildTrackOperationList (track));
		}

		MmlSemanticVariable BuildVariableDeclaration (MmlVariableDefinition src)
		{
			var ret = new MmlSemanticVariable (src.Name, src.Type);

			if (src.DefaultValueTokens.Count == 0)
				return ret;

			var stream = new TokenStream (src.DefaultValueTokens, src.Location);
			ret.DefaultValue = new Parser.MmlParser (stream.Source).ParseExpression ();

			return ret;
		}

		MmlSemanticMacro BuildMacroOperationList (MmlMacroDefinition src)
		{
			var ret = new MmlSemanticMacro (src.Name, src.TargetTracks);

			foreach (var arg in src.Arguments)
				ret.Arguments.Add (BuildVariableDeclaration (arg));

			CompileOperationTokens (ret.Data, new TokenStream (src.Tokens, src.Location));

			return ret;
		}

		MmlSemanticTrack BuildTrackOperationList (MmlTrack src)
		{
			var ret = new MmlSemanticTrack (src.Number);
			CompileOperationTokens (ret.Data, new TokenStream (src.Tokens, null));
			return ret;
		}

		void CompileOperationTokens (List<MmlOperationUse> data, TokenStream stream)
		{
			data.AddRange (new Parser.MmlParser (stream.Source).ParseOperations ());
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
	}

	#endregion
}