using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Commons.Music.Midi.Mml
{
	#region variable resolver structures

	public partial class MmlOperationUse
	{
		public void ValidateArguments (MmlResolveContext ctx, int minParams, params MmlDataType [] types)
		{
			if (Arguments.Count != types.Length) {
				if (Arguments.Count < minParams || minParams < 0) {
					ctx.Compiler.Report (MmlDiagnosticVerbosity.Error, Location, "Insufficient argument(s)");
					return;
				}
			}
			for (int i = 0; i < Arguments.Count; i++) {
				var arg = Arguments [i];
				var type = i < types.Length ? types [i] : MmlDataType.Any;
				arg.Resolve (ctx, type);
			}
		}
	}

	public abstract partial class MmlValueExpr
	{
		internal static int BaseCount = 192;

		static MmlValueExpr ()
		{
			StringToBytes = (s => Encoding.UTF8.GetBytes (s));
		}

		public static Func<string,byte[]> StringToBytes { get; set; }

		public static double LengthDotsToMultiplier (int dots)
		{
			return 2.0d - Math.Pow (0.5, dots);
		}

		public object ResolvedValue { get; set; }

		public abstract void Resolve (MmlResolveContext ctx, MmlDataType type);

		public byte ByteValue {
			get { return (byte) IntValue; }
		}

		public byte [] ByteArrayValue {
			get {
				if (ResolvedValue is string || ResolvedValue is StringBuilder)
					return StringToBytes (StringValue);
				return new byte [] { ByteValue };
			}
		}

		public int IntValue {
			get {
				return ResolvedValue is int ? (int) ResolvedValue :
					ResolvedValue is byte ? (byte) ResolvedValue :
					ResolvedValue is MmlLength ? ((MmlLength) ResolvedValue).GetSteps (BaseCount) :
					(int) (double) ResolvedValue;
			}
		}

		public double GetDoubleValue (MmlResolveContext ctx) => (double) GetTypedValue (ctx, ResolvedValue, MmlDataType.Number, Location);

		public string StringValue => ResolvedValue.ToString ();

		public static object GetTypedValue (MmlResolveContext ctx, object value, MmlDataType type, MmlLineInfo location, bool throwException = false)
			=> GetTypedValue (ctx.Compiler, value, type, location, throwException);

		public static object GetTypedValue (MmlCompiler compiler, object value, MmlDataType type, MmlLineInfo location, bool throwException = false)
		{
			switch (type) {
			case MmlDataType.Any:
				return value;
			case MmlDataType.String:
				return value.ToString ();
			case MmlDataType.Number:
				if (value is double)
					return value;
				if (value is int)
					return (double) (int) value;
				if (value is byte)
					return (double) (byte) value;
				if (value is MmlLength)
					return (double) ((MmlLength) value).GetSteps (MmlValueExpr.BaseCount);
				break; // error
			case MmlDataType.Length:
				if (value is MmlLength)
					return value;
				int denom = 0;
				if (value is double)
					denom = (int) (double) value;
				else if (value is int)
					denom = (int) value;
				else if (value is byte)
					denom = (byte) value;
				else
					break; // error
				return new MmlLength (denom);
			}
			compiler.Report ( MmlDiagnosticVerbosity.Error, location, "Invalid value {0} for the expected data type {1}", value, type);
			return null;
		}
	}

	public partial class MmlConstantExpr
	{
		public override void Resolve (MmlResolveContext ctx, MmlDataType type)
		{
			if (ResolvedValue == null) {
				if (type == MmlDataType.Buffer)
					ResolvedValue = new StringBuilder ();
				else
					ResolvedValue = GetTypedValue (ctx, Value, type, Location);
			}
		}
	}

	public partial class MmlVariableReferenceExpr : MmlValueExpr
	{
		public override void Resolve (MmlResolveContext ctx, MmlDataType type)
		{
			if (Scope == 3) {
				if (ctx.GlobalContext == null)
					ctx.Compiler.Report ( MmlDiagnosticVerbosity.Error, null, "Global variable '{0}' cannot be resolved at this compilation phase", Name);
				else
					ResolveCore (ctx.GlobalContext, type, true);
			}
			else
				ResolveCore (ctx, type, Scope > 1);
		}
		void ResolveCore (MmlResolveContext ctx, MmlDataType type, bool excludeMacroArgs)
		{
			if (!excludeMacroArgs) { // reference to macro argument takes precedence
				object _arg = ctx.MacroArguments [Name];
				if (_arg != null) {
					var arg = (KeyValuePair<MmlSemanticVariable,object>) _arg;
					ResolvedValue = GetTypedValue (ctx, arg.Value, type, Location);
					return;
				}
			}

			var variable = (MmlSemanticVariable) ctx.SourceTree.Variables.Get (Name);
			if (variable == null)
				ctx.Compiler.Report (MmlDiagnosticVerbosity.Error, Location, "Cannot resolve variable '{0}'", Name);
			else {
				var val = ctx.EnsureDefaultResolvedVariable (variable);
				ResolvedValue = GetTypedValue (ctx, val, type, Location);
			}
		}
	}

	public partial class MmlParenthesizedExpr : MmlValueExpr
	{
		public override void Resolve (MmlResolveContext ctx, MmlDataType type)
		{
			Content.Resolve (ctx, type);
			ResolvedValue = Content.ResolvedValue;
		}
	}

	public partial class MmlAddExpr : MmlArithmeticExpr
	{
		public override void Resolve (MmlResolveContext ctx, MmlDataType type)
		{
			Left.Resolve (ctx, type);
			Right.Resolve (ctx, type);
			if (type == MmlDataType.Length)
				ResolvedValue = new MmlLength ((int) (Left.GetDoubleValue (ctx) + Right.GetDoubleValue (ctx))) { IsValueByStep = true };
			else
				ResolvedValue = Left.GetDoubleValue (ctx) + Right.GetDoubleValue (ctx);
		}
	}

	public partial class MmlSubtractExpr : MmlArithmeticExpr
	{
		public override void Resolve (MmlResolveContext ctx, MmlDataType type)
		{
			Left.Resolve (ctx, type);
			Right.Resolve (ctx, type);
			if (type == MmlDataType.Length)
				ResolvedValue = new MmlLength ((int) (Left.GetDoubleValue (ctx) - Right.GetDoubleValue (ctx))) { IsValueByStep = true };
			else
				ResolvedValue = Left.GetDoubleValue (ctx) - Right.GetDoubleValue (ctx);
		}
	}

	public partial class MmlMultiplyExpr : MmlArithmeticExpr
	{
		public override void Resolve (MmlResolveContext ctx, MmlDataType type)
		{
			// multiplication cannot be straightforward. Number * Length must be Length,
			// but the number must not be converted to a length e.g. "1" must not be
			// interpreted as %{$__base_count}. Actually Length * Length must be invalid.

			Left.Resolve (ctx, MmlDataType.Number);
			Right.Resolve (ctx, type);
			if (type == MmlDataType.Length)
				ResolvedValue = new MmlLength ((int) (Left.GetDoubleValue (ctx) * Right.GetDoubleValue (ctx))) { IsValueByStep = true };
			else
				ResolvedValue = Left.GetDoubleValue (ctx) * Right.GetDoubleValue (ctx);
		}
	}

	public partial class MmlDivideExpr : MmlArithmeticExpr
	{
		public override void Resolve (MmlResolveContext ctx, MmlDataType type)
		{
			Left.Resolve (ctx, type);
			Right.Resolve (ctx, type);
			if (type == MmlDataType.Length)
				ResolvedValue = new MmlLength ((int) (Left.GetDoubleValue (ctx) / Right.GetDoubleValue (ctx))) { IsValueByStep = true };
			else
				ResolvedValue = Left.GetDoubleValue (ctx) / Right.GetDoubleValue (ctx);
		}
	}

	public partial class MmlModuloExpr : MmlArithmeticExpr
	{
		public override void Resolve (MmlResolveContext ctx, MmlDataType type)
		{
			Left.Resolve (ctx, type);
			Right.Resolve (ctx, type);
			if (type == MmlDataType.Length)
				ResolvedValue = new MmlLength ((int) (Left.GetDoubleValue (ctx) % Right.GetDoubleValue (ctx))) { IsValueByStep = true };
			else
				ResolvedValue = (int) Left.GetDoubleValue (ctx) % (int) Right.GetDoubleValue (ctx);
		}
	}

	public partial class MmlConditionalExpr : MmlValueExpr
	{
		public override void Resolve (MmlResolveContext ctx, MmlDataType type)
		{
			Condition.Resolve (ctx, MmlDataType.Number);
			TrueExpr.Resolve (ctx, type);
			FalseExpr.Resolve (ctx, type);
			ResolvedValue = Condition.IntValue != 0 ? TrueExpr.ResolvedValue : FalseExpr.ResolvedValue;
		}
	}

	public partial class MmlComparisonExpr : MmlValueExpr
	{
		public override void Resolve (MmlResolveContext ctx, MmlDataType type)
		{
			Left.Resolve (ctx, type);
			Right.Resolve (ctx, type);
			switch (ComparisonType) {
			case ComparisonType.Lesser:
				ResolvedValue = ((IComparable) Left.ResolvedValue).CompareTo (Right.ResolvedValue) < 0 ? 1 : 0;
				break;
			case ComparisonType.LesserEqual:
				ResolvedValue = ((IComparable) Left.ResolvedValue).CompareTo (Right.ResolvedValue) <= 0 ? 1 : 0;
				break;
			case ComparisonType.Greater:
				ResolvedValue = ((IComparable) Left.ResolvedValue).CompareTo (Right.ResolvedValue) > 0 ? 1 : 0;
				break;
			case ComparisonType.GreaterEqual:
				ResolvedValue = ((IComparable) Left.ResolvedValue).CompareTo (Right.ResolvedValue) >= 0 ? 1 : 0;
				break;
			}
		}
	}

	#endregion

	#region primitive event stream structures

	public partial class MmlResolvedMusic
	{
		public MmlResolvedMusic ()
		{
			BaseCount = 192;
			Tracks = new List<MmlResolvedTrack> ();
		}

		public int BaseCount { get; set; }
		public List<MmlResolvedTrack> Tracks { get; private set; }
	}

	public class MmlResolvedTrack
	{
		public MmlResolvedTrack (double number, MmlSemanticTreeSet source)
		{
			Number = number;
			Events = new List<MmlResolvedEvent> ();
			Macros = new Hashtable ();
			foreach (var m in source.Macros)
				if (m.TargetTracks == null || m.TargetTracks.Contains (number))
					Macros [m.Name] = m; // possibly overwrite.
		}

		public double Number { get; set; }
		public List<MmlResolvedEvent> Events { get; private set; }
		public Hashtable Macros { get; private set; }
	}

	public class MmlResolvedEvent
	{
		public MmlResolvedEvent (string operation, int tick)
		{
			Operation = operation;
			Tick = tick;
			Arguments = new List<byte> ();
		}

		// copy constructor
		public MmlResolvedEvent (MmlResolvedEvent other, int tick)
		{
			Operation = other.Operation;
			Arguments = other.Arguments;
			Tick = tick;
		}

		public int Tick { get; set; }
		public string Operation { get; private set; }

		public List<byte> Arguments { get; private set; }
	}

	#endregion

	#region primitive resolver

	public class Loop
	{
		public Loop (MmlResolveContext ctx)
		{
			Breaks = new Dictionary<int,LoopLocation> ();
			EndLocations = new Dictionary<int,LoopLocation> ();
			Events = new List<MmlResolvedEvent> ();
			CurrentBreaks = new List<int> ();
			SavedValues = ctx.Values;
		}

		public LoopLocation BeginAt { get; set; }
		public LoopLocation FirstBreakAt { get; set; }
		public Dictionary<int,LoopLocation> Breaks { get; private set; } // count -> indexes
		public List<MmlResolvedEvent> Events { get; private set; }
		public Dictionary<MmlSemanticVariable,object> SavedValues { get; private set; }
		public Dictionary<int,LoopLocation> EndLocations { get; private set; } // count -> indexes for end
		public List<int> CurrentBreaks { get; private set; }
	}

	public class LoopLocation
	{
		public LoopLocation (int source, int output, int tick)
		{
			Source = source;
			Output = output;
			Tick = tick;
		}
		public readonly int Source;
		public readonly int Output;
		public readonly int Tick;
	}

	public partial class MmlResolveContext
	{
		public MmlResolveContext (MmlSemanticTreeSet song, MmlResolveContext globalContext, MmlCompiler contextCompiler)
		{
			GlobalContext = globalContext;
			SourceTree = song;
			MacroArguments = new Hashtable ();
			Values = new Dictionary<MmlSemanticVariable,object> ();
			Loops = new Stack<Loop> ();
			this.compiler = contextCompiler ?? globalContext.compiler;
		}

		MmlCompiler compiler;

		internal MmlCompiler Compiler => compiler;

		public MmlResolveContext GlobalContext { get; private set; }

		public int TimelinePosition { get; set; }

		public MmlSemanticTreeSet SourceTree { get; set; }

		public Hashtable MacroArguments { get; internal set; }
		public Dictionary<MmlSemanticVariable,object> Values { get; internal set; }
		public Stack<Loop> Loops { get; private set; }

		public Loop CurrentLoop {
			get { return Loops.Count > 0 ? Loops.Peek () : null; }
		}
		
		public object EnsureDefaultResolvedVariable (MmlSemanticVariable variable)
		{
			object val;
			if (!Values.TryGetValue (variable, out val)) {
				variable.DefaultValue.Resolve (this, variable.Type);
				val = variable.DefaultValue.ResolvedValue;
				Values [variable] = val;
			}
			return val;
		}
	}

	public class MmlEventStreamGenerator
	{
		public static MmlResolvedMusic Generate (MmlSemanticTreeSet source, MmlCompiler contextCompiler)
		{
			var gen = new MmlEventStreamGenerator (source, contextCompiler);
			gen.Generate ();
			return gen.result;
		}

		MmlEventStreamGenerator (MmlSemanticTreeSet source, MmlCompiler contextCompiler)
		{
			this.source = source;
			this.compiler = contextCompiler;
			result = new MmlResolvedMusic () { BaseCount = source.BaseCount };
		}

		MmlCompiler compiler;
		MmlSemanticTreeSet source;
		MmlResolveContext global_context;
		MmlResolvedMusic result;
		List<MmlResolvedEvent> current_output;

		void Generate ()
		{
			global_context = new MmlResolveContext (source, null, compiler);

			foreach (var track in source.Tracks) {
				var rtrk = new MmlResolvedTrack (track.Number, source);
				result.Tracks.Add (rtrk);
				var tctx = new MmlResolveContext (source, global_context, compiler);
				var list = track.Data;
				current_output = rtrk.Events;
				ProcessOperations (rtrk, tctx, list, 0, list.Count, null);
				Sort (current_output);
			}
		}

		List<MmlResolvedEvent> chord = new List<MmlResolvedEvent> ();
		bool recordNextAsChord;
		Dictionary<int,StoredOperations> stored_operations = new Dictionary<int,StoredOperations> ();

		class StoredOperations
		{
			public StoredOperations ()
			{
			}
			
			public List<MmlOperationUse> Operations { get; set; }
			public Dictionary<MmlSemanticVariable,object> Values { get; set; }
			public Hashtable MacroArguments { get; set; }
		}

		// extraTailArgs is a list of arguments that are passed to the context macro call e.g.
		//   #macro CHORD_A c0e0g
		//   1   CHORD_A4 CHORD_A8 CHORD_A8
		// In this case, 4, 8 and 8 are NOT passed to CHORD_A unless these extraTailArgs are passed
		// and causes unexpected outputs.
		// We can still define macros to take full arguments to the defined sequence of operators
		// (in this case, to 'g'), but that is super annoying and basically impossible unless
		// you know the macro definition details (which almost no one would know).
		void ProcessOperations (MmlResolvedTrack track, MmlResolveContext rctx, List<MmlOperationUse> list, int start, int count, IEnumerable<MmlValueExpr> extraTailArgs)
		{
			int storeIndex = -1;
			List<MmlResolvedEvent> storeCurrentOutput = null, storeDummy = new List<MmlResolvedEvent> ();
			StoredOperations currentStoredOperations = null;
			bool is_string_format = false;

			for (int listIndex = start; listIndex < start + count; listIndex++) {
				var oper = list [listIndex];
				var extraTailArgsIfApplied = listIndex == start + count - 1 ? extraTailArgs : null;

				switch (oper.Name) {
				case "__PRINT": {
					oper.Arguments [0].Resolve (rctx, MmlDataType.String);
					compiler.Report (MmlDiagnosticVerbosity.Information, oper.Location, oper.Arguments [0].StringValue, extraTailArgs);
					break;
					}
				case "__LET": {
					oper.Arguments [0].Resolve (rctx, MmlDataType.String);
					string name = oper.Arguments [0].StringValue;
					var variable = (MmlSemanticVariable) source.Variables.Get (name);
					if (variable == null) {
						compiler.Report (MmlDiagnosticVerbosity.Error, oper.Location, "Target variable not found: {0}", name);
						break;
					}
					oper.Arguments [1].Resolve (rctx, variable.Type);
					rctx.Values [variable] = oper.Arguments [1].ResolvedValue;
					if (name == "__timeline_position")
						rctx.TimelinePosition = oper.Arguments [1].IntValue;
					break;
					}
				case "__STORE": {
					oper.Arguments [0].Resolve (rctx, MmlDataType.String);
					oper.ValidateArguments (rctx, oper.Arguments.Count);
					string name = oper.Arguments [0].StringValue;
					var variable = (MmlSemanticVariable) source.Variables.Get (name);
					if (variable == null) {
						compiler.Report (MmlDiagnosticVerbosity.Error, oper.Location, "Target variable not found: {0}", name);
						break;
					}
					if (variable.Type != MmlDataType.Buffer) {
						compiler.Report (MmlDiagnosticVerbosity.Error, oper.Location, "Target variable is not a buffer: {0}", name);
						break;
					}
					var sb = (StringBuilder) rctx.EnsureDefaultResolvedVariable (variable);
					for (int i = 1; i < oper.Arguments.Count; i++)
						sb.Append (oper.Arguments [i].StringValue);
					break;
					}
				case "__FORMAT":
					is_string_format = true;
					goto case "__STORE_FORMAT";
				case "__STORE_FORMAT": {
					oper.Arguments [0].Resolve (rctx, MmlDataType.String);
					oper.Arguments [1].Resolve (rctx, MmlDataType.String);
					oper.ValidateArguments (rctx, oper.Arguments.Count);
					string name = oper.Arguments [0].StringValue;
					string format = oper.Arguments [1].StringValue;
					var variable = (MmlSemanticVariable)source.Variables.Get (name);
					if (variable == null) {
						compiler.Report (MmlDiagnosticVerbosity.Error, oper.Location, "Target variable not found: {0}", name);
						break;
					}
					if (is_string_format) {
						if (variable.Type != MmlDataType.String) {
							compiler.Report (MmlDiagnosticVerbosity.Error, oper.Location, "Target variable is not a string: {0}", name);
							break;
						}
					} else {
						if (variable.Type != MmlDataType.Buffer) {
							compiler.Report (MmlDiagnosticVerbosity.Error, oper.Location, "Target variable is not a buffer: {0}", name);
							break;
						}
					}
					try {
						string v = string.Format (format, (object []) (from x in oper.Arguments.Skip (2) select (object) x.StringValue).ToArray ());
						if (is_string_format)
							rctx.Values [variable] = v;
						else
							((StringBuilder) rctx.EnsureDefaultResolvedVariable (variable)).Append (v);
					} catch (FormatException ex) {
						compiler.Report ( MmlDiagnosticVerbosity.Error, oper.Location, "Format error while applying '{0}' to '{1}': {2}", format, name, ex.Message);
						break;
					}
					break;
					}
				case "__APPLY":
					var oa = oper.Arguments [0];
					oa.Resolve (rctx, MmlDataType.String);
					string apparg = oa.StringValue;

					// add macro argument definitions
					var tmpop = new MmlOperationUse (apparg, oper.Location);
					for (int x = 1; x < oper.Arguments.Count; x++)
						tmpop.Arguments.Add (oper.Arguments [x]);
					ProcessMacroCall (track, rctx, tmpop, extraTailArgsIfApplied);

					break;
				case "__MIDI":
					oper.ValidateArguments (rctx, oper.Arguments.Count);
					var mop = new MmlResolvedEvent ("MIDI", rctx.TimelinePosition);
					foreach (var arg in oper.Arguments)
						mop.Arguments.Add (arg.ByteValue);
					current_output.Add (mop);
					if (recordNextAsChord)
						chord.Add (mop);
					recordNextAsChord = false;
					break;
				case "__SYNC_NOFF_WITH_NEXT":
					recordNextAsChord = true;
					break;
				case "__ON_MIDI_NOTE_OFF":
					// handle zero-length note
					oper.ValidateArguments (rctx, 3, MmlDataType.Number, MmlDataType.Number, MmlDataType.Number);
					if (oper.Arguments [0].IntValue == 0)
						// record next note as part of chord
						recordNextAsChord = true;
					else {
						foreach (MmlResolvedEvent cop in chord)
							cop.Tick += oper.Arguments [0].IntValue;
						chord.Clear ();
					}
					break;
				case "__MIDI_META":
					oper.ValidateArguments (rctx, oper.Arguments.Count);
					var mmop = new MmlResolvedEvent ("META", rctx.TimelinePosition);
					mmop.Arguments.Add (0xFF);
					foreach (var arg in oper.Arguments)
						mmop.Arguments.AddRange (arg.ByteArrayValue);
					current_output.Add (mmop);
					break;
				case "__SAVE_OPER_BEGIN":
					oper.ValidateArguments (rctx, 0);
					if (storeIndex >= 0) {
						compiler.Report (MmlDiagnosticVerbosity.Error, oper.Location, "__SAVE_OPER_BEGIN works only within a simple list without nested uses");
						break;
					}
					storeIndex = listIndex + 1;
					storeCurrentOutput = current_output;
					current_output = storeDummy;
					currentStoredOperations = new StoredOperations ();
					currentStoredOperations.Values = new Dictionary<MmlSemanticVariable, object> (rctx.Values);
					currentStoredOperations.MacroArguments = (Hashtable) rctx.MacroArguments.Clone ();
					break;
				case "__SAVE_OPER_END": {
					oper.ValidateArguments (rctx, 1, MmlDataType.Number);
					int bufIdx = oper.Arguments [0].IntValue;
					stored_operations [bufIdx] = currentStoredOperations;
					currentStoredOperations.Operations =
						new List<MmlOperationUse> (list.Skip (storeIndex).Take (listIndex - storeIndex - 1));
					current_output = storeCurrentOutput;
					storeDummy.Clear ();
					storeIndex = -1;
					currentStoredOperations = null;
					// FIXME: might be better to restore variables
					break;
					}
				case "__RESTORE_OPER": {
					oper.ValidateArguments (rctx, 1, MmlDataType.Number);
					int bufIdx = oper.Arguments [0].IntValue;
					var ss = stored_operations [bufIdx];
					var valuesBak = rctx.Values;
					var macroArgsBak = rctx.MacroArguments;
					rctx.Values = ss.Values;
					rctx.MacroArguments = ss.MacroArguments;
					// adjust timeline_position (no need to update rctx.TimelinePosition here).
					rctx.Values [(MmlSemanticVariable) source.Variables.Get ("__timeline_position")] = rctx.TimelinePosition;
					ProcessOperations (track, rctx, ss.Operations, 0, ss.Operations.Count, extraTailArgsIfApplied);
					rctx.Values = valuesBak;
					rctx.MacroArguments = macroArgsBak;
					break;
					}
				case "__LOOP_BEGIN":
#if !UNHACK_LOOP
				case "[":
#endif
					oper.ValidateArguments (rctx, 0);
					var loop = new Loop (rctx) { BeginAt= new LoopLocation (listIndex, current_output.Count, rctx.TimelinePosition) };
					rctx.Values = new Dictionary<MmlSemanticVariable,object> (loop.SavedValues.Count);
					foreach (var p in loop.SavedValues)
						rctx.Values.Add (p.Key, p.Value);
					rctx.Loops.Push (loop);
					current_output = loop.Events;
					break;
				case "__LOOP_BREAK":
#if !UNHACK_LOOP
				case "/":
				case ":":
#endif
					oper.ValidateArguments (rctx, oper.Arguments.Count);
					loop = rctx.CurrentLoop;
					if (loop == null)
						compiler.Report (MmlDiagnosticVerbosity.Error, oper.Location, "Loop break operation must be inside a pair of loop start and end");
					if (loop.FirstBreakAt == null)
						loop.FirstBreakAt = new LoopLocation (listIndex, current_output.Count, rctx.TimelinePosition);
					foreach (var cl in loop.CurrentBreaks)
						loop.EndLocations [cl] = new LoopLocation (listIndex, current_output.Count, rctx.TimelinePosition);
					loop.CurrentBreaks.Clear ();

					// FIXME: actually this logic does not make sense as now it is defined with fixed-length arguments...
					if (oper.Arguments.Count == 0) { // default loop break
						if (loop.Breaks.ContainsKey (-1) && loop.Breaks.Values.All (b => b.Source != listIndex))
							compiler.Report (MmlDiagnosticVerbosity.Error, oper.Location, "Default loop break is already defined in current loop");
						loop.Breaks.Add (-1, new LoopLocation (listIndex, current_output.Count, rctx.TimelinePosition));
						loop.CurrentBreaks.Add (-1);
					} else {
						for (int x = 0; x < oper.Arguments.Count; x++) {
							var numexpr = oper.Arguments [x];
							var num = numexpr.IntValue - 1; // "1st. loop" for musicians == 0th iteration in code.
							if (x > 0 && num < 0)
								break; // after the last argument.
							loop.CurrentBreaks.Add (num);
							if (loop.Breaks.ContainsKey (num) && loop.Breaks.Values.All (b => b.Source != listIndex)) {
								compiler.Report (MmlDiagnosticVerbosity.Error, oper.Location, "Loop section {0} was already defined in current loop", num);
								break;
							}
							// specified loop count is for human users. Here the number is for program, hence -1.
							loop.Breaks.Add (num, new LoopLocation (listIndex, current_output.Count, rctx.TimelinePosition));
						}
					}
					break;
				case "__LOOP_END":
#if !UNHACK_LOOP
				case "]":
#endif
					oper.ValidateArguments (rctx, 0, MmlDataType.Number);
					loop = rctx.CurrentLoop;
					if (loop == null) {
						compiler.Report (MmlDiagnosticVerbosity.Error, oper.Location, "Loop has not started");
						break;
					}
					foreach (var cl in loop.CurrentBreaks)
						loop.EndLocations [cl] = new LoopLocation (listIndex, current_output.Count, rctx.TimelinePosition);
					int loopCount = 0;
					switch (oper.Arguments.Count) {
					case 0:
						loopCount = 2;
						break;
					case 1:
						loopCount = oper.Arguments [0].IntValue;
						break;
					default:
						compiler.Report (MmlDiagnosticVerbosity.Error, oper.Location, "Arguments at loop end exceeded");
						break;
					}
					
					rctx.Loops.Pop ();
					var outside = rctx.CurrentLoop;
					current_output = outside != null ? outside.Events : track.Events;

					// now expand loop.
					// - verify that every loop break does not exceed the loop count
					// - add sequence before the first break
					// - add sequence for each break. If no explicit break, then use default.
					foreach (var p in loop.Breaks) {
						if (p.Key > loopCount) {
							compiler.Report (MmlDiagnosticVerbosity.Error, list [p.Value.Source].Location, "Loop break specified beyond the loop count");
							loop.Breaks.Clear ();
						}
					}

					rctx.Values = loop.SavedValues;

					int baseTicks;
					int baseOutputEnd;
					int tickOffset = 0;
					if (loop.FirstBreakAt == null) { // w/o break
						baseTicks = rctx.TimelinePosition - loop.BeginAt.Tick;
						baseOutputEnd = loop.Events.Count;
						rctx.TimelinePosition = loop.BeginAt.Tick;

						// This range of commands actually adds extra argument definitions for loop operation, but it won't hurt.
						for (int l = 0; l < loopCount; l++)
							ProcessOperations (track, rctx, list, loop.BeginAt.Source + 1, listIndex  - loop.BeginAt.Source - 1, extraTailArgsIfApplied);
					} else { // w/ breaks
						baseTicks = loop.FirstBreakAt.Tick - loop.BeginAt.Tick;
						baseOutputEnd = loop.FirstBreakAt.Output;

						rctx.TimelinePosition = loop.BeginAt.Tick;

						for (int l = 0; l < loopCount; l++) {
							ProcessOperations (track, rctx, list, loop.BeginAt.Source + 1, loop.FirstBreakAt.Source  - loop.BeginAt.Source - 1, extraTailArgsIfApplied);
							tickOffset += baseTicks;
							LoopLocation lb = null;
							if (!loop.Breaks.TryGetValue (l, out lb)) {
								if (l + 1 == loopCount)
									break; // this is to break the loop at the last iteration.
								if (!loop.Breaks.TryGetValue (-1, out lb)) {
									compiler.Report (MmlDiagnosticVerbosity.Error, list [loop.BeginAt.Source].Location, "No corresponding loop break specification for iteration at {0} from the innermost loop", l + 1);
									loop.Breaks.Clear ();
								}
							}
							if (lb == null) // final break
								break;
							LoopLocation elb;
							if (!loop.EndLocations.TryGetValue (l, out elb))
								elb = loop.EndLocations [-1];
							int breakOffset = lb.Tick - loop.BeginAt.Tick + baseTicks;
							ProcessOperations (track, rctx, list, lb.Source + 1, elb.Source - lb.Source - 1, extraTailArgsIfApplied);
						}
					}
					break;
				default:
					ProcessMacroCall (track, rctx, oper, extraTailArgsIfApplied);
					break;
				}
			}
		}
		
		Stack<MmlSemanticMacro> expansion_stack = new  Stack<MmlSemanticMacro> ();

		List<Hashtable> arg_caches = new List<Hashtable> ();
		int cache_stack_num;

		void ProcessMacroCall (MmlResolvedTrack track, MmlResolveContext ctx, MmlOperationUse oper, IEnumerable<MmlValueExpr> extraTailArgs)
		{
			var macro = (MmlSemanticMacro) track.Macros [oper.Name];
			if (macro == null) {
				compiler.Report (MmlDiagnosticVerbosity.Error, oper.Location, "Macro {0} was not found", oper.Name);
				return;
			}
			if (expansion_stack.Contains (macro)) {
				compiler.Report (MmlDiagnosticVerbosity.Error, oper.Location, "Illegally recursive macro reference to {0} is found", macro.Name);
				return;
			}
			expansion_stack.Push (macro);

			if (cache_stack_num == arg_caches.Count)
				arg_caches.Add (new Hashtable ());
			var args = arg_caches [cache_stack_num++];
			var operUseArgs = extraTailArgs != null ? oper.Arguments.Concat (extraTailArgs).ToList () : oper.Arguments;
			for (int i = 0; i < macro.Arguments.Count; i++) {
				MmlSemanticVariable argdef = macro.Arguments [i];
				MmlValueExpr arg = i < operUseArgs.Count ? operUseArgs [i] : null;
				if (arg == null)
					arg = argdef.DefaultValue;
				arg.Resolve (ctx, argdef.Type);
				if (args.Contains (argdef.Name))
					compiler.Report (MmlDiagnosticVerbosity.Error, oper.Location, "Argument name must be identical to all other argument names. Argument '{0}' in '{1}' macro", argdef.Name, oper.Name);
				args.Add (argdef.Name, new KeyValuePair<MmlSemanticVariable, object> (argdef,  arg.ResolvedValue));
			}
			var argsBak = ctx.MacroArguments;
			ctx.MacroArguments = args;
			var extraTailArgsToCall = macro.Arguments.Count < operUseArgs.Count ? operUseArgs.Skip (macro.Arguments.Count) : null;
			ProcessOperations (track, ctx, macro.Data, 0, macro.Data.Count, extraTailArgsToCall);
			ctx.MacroArguments = argsBak;
			
			expansion_stack.Pop ();
			args.Clear ();
			--cache_stack_num;
		}

		void Sort (List<MmlResolvedEvent> l)
		{
			var msgBlockByTime = new Dictionary<int,List<MmlResolvedEvent>> ();
			int m = 0;
			int prev = 0;

			while (m < l.Count) {
				var e = l [m];
				List<MmlResolvedEvent> pl;
				if (!msgBlockByTime.TryGetValue (l [m].Tick, out pl)) {
					pl = new List<MmlResolvedEvent> ();
					msgBlockByTime.Add (e.Tick, pl);
				}
				prev = l [m].Tick;
				pl.Add (l [m]);
				for (m++; m < l.Count && l [m].Tick == prev; m++)
					pl.Add (l [m]);
			}
			
			l.Clear ();
			foreach (var sl in msgBlockByTime.OrderBy (kvp => kvp.Key).Select (kvp => kvp.Value))
				l.AddRange (sl);
		}
	}

	#endregion
}
