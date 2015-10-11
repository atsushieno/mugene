//#define LOOP_BY_RESULT
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
				if (Arguments.Count < minParams || minParams < 0)
					throw new MmlException ("Insufficient argument(s)", Location);
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

		public double DoubleValue {
//			get { return ResolvedValue is double ? (double) ResolvedValue : (double) (int) ResolvedValue; }
			get { return (double) GetTypedValue (ResolvedValue, MmlDataType.Number); }
		}

		public string StringValue {
			get { return ResolvedValue.ToString (); }
		}

		public static object GetTypedValue (object value, MmlDataType type)
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
			// FIXME: supply location
			throw new MmlException (String.Format ("Invalid value {0} for the expected data type {1}", value, type), null);
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
					ResolvedValue = GetTypedValue (Value, type);
			}
		}
	}

	public partial class MmlVariableReferenceExpr : MmlValueExpr
	{
		public override void Resolve (MmlResolveContext ctx, MmlDataType type)
		{
			if (Scope == 3) {
				if (ctx.GlobalContext == null)
					throw new MmlException (String.Format ("Global variable '{0}' cannot be resolved at this compilation phase", Name), null);
				ResolveCore (ctx.GlobalContext, type, true);
			}
			else
				ResolveCore (ctx, type, Scope > 1);
		}
		void ResolveCore (MmlResolveContext ctx, MmlDataType type, bool excludeMacroArgs)
		{
//Util.DebugWriter.WriteLine ("Find {0} from: ", Name);
//foreach (var arga in ctx.MacroArguments) Util.DebugWriter.Write ("{0} ", arga.Key.Name);
//foreach (var variablea in ctx.SourceTree.Variables) Util.DebugWriter.Write ("{0} ", variablea.Name);
//Util.DebugWriter.WriteLine ();

			if (!excludeMacroArgs) { // reference to macro argument takes precedence
				object _arg = ctx.MacroArguments [Name];
				if (_arg != null) {
					var arg = (KeyValuePair<MmlSemanticVariable,object>) _arg;
					ResolvedValue = GetTypedValue (arg.Value, type);
					return;
				}
			}

			var variable = (MmlSemanticVariable) ctx.SourceTree.Variables [Name];
			if (variable == null)
				// FIXME: supply location
				throw new MmlException (String.Format ("Cannot resolve variable '{0}'", Name), null);
			var val = ctx.EnsureDefaultResolvedVariable (variable);
			ResolvedValue = GetTypedValue (val, type);
//Util.DebugWriter.WriteLine ("**** resolved value for {0} is {1}, of type {2}", this, ResolvedValue, type);
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
				ResolvedValue = new MmlLength ((int) (Left.DoubleValue + Right.DoubleValue)) { IsValueByStep = true };
			else
				ResolvedValue = Left.DoubleValue + Right.DoubleValue;
		}
	}

	public partial class MmlSubtractExpr : MmlArithmeticExpr
	{
		public override void Resolve (MmlResolveContext ctx, MmlDataType type)
		{
			Left.Resolve (ctx, type);
			Right.Resolve (ctx, type);
			if (type == MmlDataType.Length)
				ResolvedValue = new MmlLength ((int) (Left.DoubleValue - Right.DoubleValue)) { IsValueByStep = true };
			else
				ResolvedValue = Left.DoubleValue - Right.DoubleValue;
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
				ResolvedValue = new MmlLength ((int) (Left.DoubleValue * Right.DoubleValue)) { IsValueByStep = true };
			else
				ResolvedValue = Left.DoubleValue * Right.DoubleValue;
		}
	}

	public partial class MmlDivideExpr : MmlArithmeticExpr
	{
		public override void Resolve (MmlResolveContext ctx, MmlDataType type)
		{
			Left.Resolve (ctx, type);
			Right.Resolve (ctx, type);
			if (type == MmlDataType.Length)
				ResolvedValue = new MmlLength ((int) (Left.DoubleValue / Right.DoubleValue)) { IsValueByStep = true };
			else
				ResolvedValue = Left.DoubleValue / Right.DoubleValue;
		}
	}

	public partial class MmlModuloExpr : MmlArithmeticExpr
	{
		public override void Resolve (MmlResolveContext ctx, MmlDataType type)
		{
			Left.Resolve (ctx, type);
			Right.Resolve (ctx, type);
			if (type == MmlDataType.Length)
				ResolvedValue = new MmlLength ((int) (Left.DoubleValue % Right.DoubleValue)) { IsValueByStep = true };
			else
				ResolvedValue = (int) Left.DoubleValue % (int) Right.DoubleValue;
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
		public MmlResolvedTrack (int number, MmlSemanticTreeSet source)
		{
			Number = number;
			Events = new List<MmlResolvedEvent> ();
			Macros = new Hashtable ();
			foreach (var m in source.Macros)
				if (m.TargetTracks == null || m.TargetTracks.Contains (number))
					Macros [m.Name] = m; // possibly overwrite.
		}

		public int Number { get; set; }
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

	/*
	public class LoopBreak
	{
		public LoopBreak ()
		{
			Events = new List<MmlResolvedEvent> ();
		}

		public LoopLocation Location { get; set; }
		public List<MmlResolvedEvent> Events { get; private set; }
	}
	*/

	public partial class MmlResolveContext
	{
		public MmlResolveContext (MmlSemanticTreeSet song, MmlResolveContext globalContext)
		{
			GlobalContext = globalContext;
			SourceTree = song;
			MacroArguments = new Hashtable ();
			Values = new Dictionary<MmlSemanticVariable,object> ();
			Loops = new Stack<Loop> ();
		}

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
		public static MmlResolvedMusic Generate (MmlSemanticTreeSet source)
		{
			var gen = new MmlEventStreamGenerator (source);
			gen.Generate ();
			return gen.result;
		}

		MmlEventStreamGenerator (MmlSemanticTreeSet source)
		{
			this.source = source;
			result = new MmlResolvedMusic () { BaseCount = source.BaseCount };
		}

		MmlSemanticTreeSet source;
		MmlResolveContext global_context;
		MmlResolvedMusic result;
		List<MmlResolvedEvent> current_output;
		TextWriter DebugPrint = Console.Out;

		void Generate ()
		{
			global_context = new MmlResolveContext (source, null);

			foreach (var track in source.Tracks) {
				var rtrk = new MmlResolvedTrack (track.Number, source);
				result.Tracks.Add (rtrk);
				var tctx = new MmlResolveContext (source, global_context);
				var list = track.Data;
				current_output = rtrk.Events;
				ProcessOperations (rtrk, tctx, list, 0, list.Count);
				Sort (current_output);
				foreach (var ev in current_output)
					Util.DebugWriter.WriteLine ("{0} {1} {2}", ev.Tick, ev.Operation, ev.Arguments.Count);
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

		Stack<MmlLineInfo> locations = new Stack<MmlLineInfo> ();

		MmlLineInfo location { get { return locations.Count > 0 ? locations.Peek () : null; } }

		void ProcessOperations (MmlResolvedTrack track, MmlResolveContext rctx, List<MmlOperationUse> list, int start, int count)
		{
//Util.DebugWriter.WriteLine ("Resolve variables in track {0}", track.Number);

			int storeIndex = -1;
			List<MmlResolvedEvent> storeCurrentOutput = null, storeDummy = new List<MmlResolvedEvent> ();
			StoredOperations currentStoredOperations = null;
			bool is_string_format = false;

			for (int listIndex = start; listIndex < start + count; listIndex++) {
				var oper = list [listIndex];

//				var pop = MmlPrimitiveOperation.All.FirstOrDefault (o => o.Name == oper.Name);
//				if (pop == null)
//					throw new MmlException (String.Format ("INTERNAL ERROR: unresolved non-primitive operation: {0}", oper.Name), location);

				switch (oper.Name) {
					/*
				case "__LOCATE":
					locations.Push ((MmlLineInfo) ((MmlConstantExpr) oper.Arguments [0]).Value);
					continue;
				case "__UNLOCATE":
					locations.Pop ();
					continue;
					*/
				case "__PRINT": {
					oper.Arguments [0].Resolve (rctx, MmlDataType.String);
					DebugPrint.WriteLine (oper.Arguments [0].StringValue);
					break;
					}
				case "__LET": {
					oper.Arguments [0].Resolve (rctx, MmlDataType.String);
					string name = oper.Arguments [0].StringValue;
					var variable = (MmlSemanticVariable) source.Variables [name];
					if (variable == null)
						throw new MmlException (String.Format ("Target variable not found: {0}", name), location);
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
					var variable = (MmlSemanticVariable) source.Variables [name];
					if (variable == null)
						throw new MmlException (String.Format ("Target variable not found: {0}", name), location);
					if (variable.Type != MmlDataType.Buffer)
						throw new MmlException (String.Format ("Target variable is not a buffer: {0}", name), location);
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
					var variable = (MmlSemanticVariable) source.Variables [name];
					if (variable == null)
						throw new MmlException (String.Format ("Target variable not found: {0}", name), location);
					if (is_string_format) {
						if (variable.Type != MmlDataType.String)
							throw new MmlException (String.Format ("Target variable is not a string: {0}", name), location);
					} else {
						if (variable.Type != MmlDataType.Buffer)
							throw new MmlException (String.Format ("Target variable is not a buffer: {0}", name), location);
					}
					try {
						string v = string.Format (format, (object []) (from x in oper.Arguments.Skip (2) select (object) x.StringValue).ToArray ());
						if (is_string_format)
							rctx.Values [variable] = v;
						else
							((StringBuilder) rctx.EnsureDefaultResolvedVariable (variable)).Append (v);
					} catch (FormatException ex) {
						throw new MmlException (String.Format ("Format error while applying '{0}' to '{1}': {2}", format, name, ex.Message), location);
					}
					break;
					}
					/*
				case "__MACRO_ARG_DEF":
					oper.Arguments [0].Resolve (rctx, MmlDataType.Any);
					var argdef = (MmlSemanticVariable) oper.Arguments [0].ResolvedValue;
					oper.ValidateArguments (rctx, 2, MmlDataType.Any, argdef.Type); // it is rather to resolve argument value.
					var argval = oper.Arguments [1];
					rctx.MacroArguments.Add (new KeyValuePair<MmlSemanticVariable, object> (argdef,  argval.ResolvedValue));
					break;
				case "__MACRO_ARG_UNDEF":
					oper.ValidateArguments (rctx, 1, MmlDataType.String);
					var aname = oper.Arguments [0].StringValue;
					// This could actually be Last(), but since there is broken loop expansion
					// that involves macro arg definitions extraneously, Last() causes IOE
					// as it tries to undefine "undeclared" arguments.
					var argpair = rctx.MacroArguments.LastOrDefault (a => a.Key.Name == aname);
					rctx.MacroArguments.Remove (argpair);
					break;
					*/
				case "__APPLY":
					var oa = oper.Arguments [0];
					oa.Resolve (rctx, MmlDataType.String);
					string apparg = oa.StringValue;

					// add macro argument definitions
					var tmpop = new MmlOperationUse (apparg, oper.Location);
					for (int x = 1; x < oper.Arguments.Count; x++)
						tmpop.Arguments.Add (oper.Arguments [x]);
					ProcessMacroCall (track, rctx, tmpop);

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
					//if (oper.Arguments.Count != 1)
					//	throw new MmlException (String.Format ("META operation argument count mismatch. Expected 1, got {0}", oper.Arguments.Count), location);
					foreach (var arg in oper.Arguments)
						mmop.Arguments.AddRange (arg.ByteArrayValue);
					current_output.Add (mmop);
					break;
				case "__SAVE_OPER_BEGIN":
					oper.ValidateArguments (rctx, 0);
					if (storeIndex >= 0)
						throw new MmlException ("__SAVE_OPER_BEGIN works only within a simple list", oper.Location);
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
					rctx.Values [(MmlSemanticVariable) source.Variables ["__timeline_position"]] = rctx.TimelinePosition;
					ProcessOperations (track, rctx, ss.Operations, 0, ss.Operations.Count);
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
						throw new MmlException ("Loop break operation must be inside a pair of loop start and end", location);
					if (loop.FirstBreakAt == null)
						loop.FirstBreakAt = new LoopLocation (listIndex, current_output.Count, rctx.TimelinePosition);
					foreach (var cl in loop.CurrentBreaks)
						loop.EndLocations [cl] = new LoopLocation (listIndex, current_output.Count, rctx.TimelinePosition);
					loop.CurrentBreaks.Clear ();

					// FIXME: actually this logic does not make sense as now it is defined with fixed-length arguments...
					if (oper.Arguments.Count == 0) { // default loop break
						if (loop.Breaks.ContainsKey (-1) && loop.Breaks.Values.All (b => b.Source != listIndex))
							throw new MmlException ("Default loop break is already defined in current loop", location);
						loop.Breaks.Add (-1, new LoopLocation (listIndex, current_output.Count, rctx.TimelinePosition));
						loop.CurrentBreaks.Add (-1);
					} else {
						for (int x = 0; x < oper.Arguments.Count; x++) {
							var numexpr = oper.Arguments [x];
							var num = numexpr.IntValue - 1;
							if (x > 0 && num < 0)
								break; // after the last argument.
							loop.CurrentBreaks.Add (num);
							if (loop.Breaks.ContainsKey (num) && loop.Breaks.Values.All (b => b.Source != listIndex))
								throw new MmlException (String.Format ("Loop section {0} was already defined in current loop", num), location);
							// specified loop count is for human users. Here the number is for program, hence -1.
							loop.Breaks.Add (num, new LoopLocation (listIndex, current_output.Count, rctx.TimelinePosition));
						}
					}
					break;
				case "__LOOP_END":
#if !UNHACK_LOOP
				case "]":
#endif
					oper.ValidateArguments (rctx, 1, MmlDataType.Number);
					loop = rctx.CurrentLoop;
					if (loop == null)
						throw new MmlException ("Loop has not started", location);
					foreach (var cl in loop.CurrentBreaks)
						loop.EndLocations [cl] = new LoopLocation (listIndex, current_output.Count, rctx.TimelinePosition);
					int loopCount;
					switch (oper.Arguments.Count) {
					case 0:
						loopCount = 2;
						break;
					case 1:
						loopCount = oper.Arguments [0].IntValue;
						break;
					default:
						throw new MmlException ("Arguments at loop end exceeded", location);
					}
					
					rctx.Loops.Pop ();
					var outside = rctx.CurrentLoop;
					current_output = outside != null ? outside.Events : track.Events;
					// now expand loop.
					// - verify that every loop break does not exceed the loop count
					// - add sequence before the first break
					// - add sequence for each break. If no explicit break, then use default.

					foreach (var p in loop.Breaks) {
						if (p.Key > loopCount)
							throw new MmlException ("Loop break specified beyond the loop count", list [p.Value.Source].Location);
					}

#if !LOOP_BY_RESULT
					rctx.Values = loop.SavedValues;
#endif
					int baseTicks;
					int baseOutputEnd;
					int tickOffset = 0;
					if (loop.FirstBreakAt == null) { // w/o break
						baseTicks = rctx.TimelinePosition - loop.BeginAt.Tick;
						baseOutputEnd = loop.Events.Count;
#if LOOP_BY_RESULT
						for (int l = 0; l < loopCount; l++) {
							foreach (var rop in loop.Events.Take (baseOutputEnd))
								current_output.Add (new MmlResolvedEvent (rop, rop.Tick + tickOffset));
							tickOffset += baseTicks;
						}
#else
						rctx.TimelinePosition = loop.BeginAt.Tick;

						// This range of commands actually adds extra argument definitions for loop operation, but it won't hurt.
						for (int l = 0; l < loopCount; l++)
							ProcessOperations (track, rctx, list, loop.BeginAt.Source + 1, listIndex  - loop.BeginAt.Source - 1);
#endif
					} else { // w/ breaks
						baseTicks = loop.FirstBreakAt.Tick - loop.BeginAt.Tick;
						baseOutputEnd = loop.FirstBreakAt.Output;

#if !LOOP_BY_RESULT
						rctx.TimelinePosition = loop.BeginAt.Tick;
#endif
						for (int l = 0; l < loopCount; l++) {
#if LOOP_BY_RESULT
							foreach (var rop in loop.Events.Take (baseOutputEnd))
								current_output.Add (new MmlResolvedEvent (rop, rop.Tick + tickOffset));
#else
							ProcessOperations (track, rctx, list, loop.BeginAt.Source + 1, loop.FirstBreakAt.Source  - loop.BeginAt.Source - 1);
#endif
							tickOffset += baseTicks;
							LoopLocation lb = null;
							if (!loop.Breaks.TryGetValue (l, out lb)) {
								if (l + 1 == loopCount)
									break; // this is to break the loop at the last iteration.
								if (!loop.Breaks.TryGetValue (-1, out lb))
									throw new MmlException (String.Format ("No corresponding loop break specification for iteration at {0} from the innermost loop", l + 1), list [loop.BeginAt.Source].Location);
							}
							if (lb == null) // final break
								break;
							LoopLocation elb;
							if (!loop.EndLocations.TryGetValue (l, out elb))
								elb = loop.EndLocations [-1];
							int breakOffset = lb.Tick - loop.BeginAt.Tick + baseTicks;
#if LOOP_BY_RESULT
							foreach (var rop in loop.Events.Skip (lb.Output).Take (elb.Output - lb.Output))
								current_output.Add (new MmlResolvedEvent (rop, loop.FirstBreakAt.Tick + rop.Tick - breakOffset + loop.BeginAt.Tick + tickOffset));
							tickOffset += elb.Tick - lb.Tick;
#else
							ProcessOperations (track, rctx, list, lb.Source + 1, elb.Source - lb.Source - 1);
#endif
						}
					}
					break;
				default:
					ProcessMacroCall (track, rctx, oper);
					break;
				}
			}
		}
		
		Stack<MmlSemanticMacro> expansion_stack = new  Stack<MmlSemanticMacro> ();

		List<Hashtable> arg_caches = new List<Hashtable> ();
		int cache_stack_num;

		void ProcessMacroCall (MmlResolvedTrack track, MmlResolveContext ctx, MmlOperationUse oper)
		{
			var macro = (MmlSemanticMacro) track.Macros [oper.Name];
			if (macro == null)
				throw new MmlException (String.Format ("Macro {0} was not found. {1}", oper.Name, oper.Location), location);
			if (expansion_stack.Contains (macro))
				throw new MmlException (String.Format ("Illegally recursive macro reference to {0} is found", macro.Name), null);
			expansion_stack.Push (macro);
			//foreach (var variable in macro.Arguments)
			//	if (variable.DefaultValue == null)
			//			variable.FillDefaultValue ();

			if (cache_stack_num == arg_caches.Count)
				arg_caches.Add (new Hashtable ());
			var args = arg_caches [cache_stack_num++];
			for (int i = 0; i < macro.Arguments.Count; i++) {
				MmlSemanticVariable argdef = macro.Arguments [i];
				MmlValueExpr arg = i < oper.Arguments.Count ? oper.Arguments [i] : null;
				if (arg == null)
					arg = argdef.DefaultValue;
				arg.Resolve (ctx, argdef.Type);
				if (args.Contains (argdef.Name))
					throw new MmlException (String.Format ("Argument name must be identical to all other argument names. Argument '{0}' in '{1}' macro", argdef.Name, oper.Name), oper.Location);
				args.Add (argdef.Name, new KeyValuePair<MmlSemanticVariable, object> (argdef,  arg.ResolvedValue));
			}
			var argsBak = ctx.MacroArguments;
			ctx.MacroArguments = args;
			ProcessOperations (track, ctx, macro.Data, 0, macro.Data.Count);
			ctx.MacroArguments = argsBak;
			
			expansion_stack.Pop ();
			args.Clear ();
			--cache_stack_num;
		}

		void Sort (List<MmlResolvedEvent> l)
		{
			var idxl = new List<int> (l.Count);
			idxl.Add (0);
			int prev = 0;
			for (int i = 0; i < l.Count; i++) {
				if (l [i].Tick != prev) {
					idxl.Add (i);
					prev = l [i].Tick;
				}
			}
			if (idxl.Count == 1)
				return; // no need to sort.

			idxl.Sort (delegate (int i1, int i2) {
				return l [i1].Tick - l [i2].Tick;
				});

			// now build a new event list based on the sorted blocks.
			var l2 = new List<MmlResolvedEvent> (l.Count);
			int idx;
			for (int i = 0; i < idxl.Count; i++)
				for (idx = idxl [i], prev = l [idx].Tick; idx < l.Count && l [idx].Tick == prev; idx++)
					l2.Add (l [idx]);
			l.Clear ();
			l.AddRange (l2);
		}
	}

	#endregion
}
