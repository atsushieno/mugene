//#define LOOP_BY_RESULT
using System;
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
		static MmlValueExpr ()
		{
			StringToBytes = (s => Encoding.UTF8.GetBytes (s));
		}

		public static Func<string,byte[]> StringToBytes { get; set; }

		public object ResolvedValue { get; set; }

		public abstract void Resolve (MmlResolveContext ctx, MmlDataType type);

		public byte ByteValue {
			get { return (byte) IntValue; }
		}

		public byte [] ByteArrayValue {
			get {
				if (ResolvedValue is string)
					return StringToBytes (StringValue);
				return new byte [] { ByteValue };
			}
		}

		public int IntValue {
			get { return ResolvedValue is int ? (int) ResolvedValue : ResolvedValue is byte ? (byte) ResolvedValue : (int) (double) ResolvedValue; }
		}

		public double DoubleValue {
//			get { return ResolvedValue is double ? (double) ResolvedValue : (double) (int) ResolvedValue; }
			get { return (double) GetTypedValue (ResolvedValue, MmlDataType.Number); }
		}

		public string StringValue {
			get { return (string) ResolvedValue; }
		}

		public static object GetTypedValue (object value, MmlDataType type)
		{
			// FIXME: use constant for "192" everywhere.
			switch (type) {
			case MmlDataType.Any:
				return value;
			case MmlDataType.String:
				if (value is string)
					return value;
				break; // error
			case MmlDataType.Number:
				if (value is double)
					return value;
				if (value is int)
					return (double) (int) value;
				if (value is byte)
					return (double) (byte) value;
				if (value is MmlLength)
					return (double) ((MmlLength) value).GetSteps (192); // FIXME: use correct number for 192
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
			ResolvedValue = GetTypedValue (Value, type);
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
				var arg = ctx.MacroArguments.LastOrDefault (v => v.Key.Name == Name);
				if (arg.Key != null) {
					ResolvedValue = GetTypedValue (arg.Value, type);
					return;
				}
			}

			var variable = ctx.SourceTree.Variables.FirstOrDefault (v => v.Name == Name);
			if (variable == null)
				// FIXME: supply location
				throw new MmlException (String.Format ("Cannot resolve variable '{0}'", Name), null);
			object val;
			if (ctx.Values.TryGetValue (variable, out val))
				ResolvedValue = val;
			else
				ResolvedValue = variable.DefaultValue.ResolvedValue;
			ResolvedValue = GetTypedValue (ResolvedValue, type);
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

			Left.Resolve (ctx, type);
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
			Tracks = new List<MmlResolvedTrack> ();
		}

		public List<MmlResolvedTrack> Tracks { get; private set; }
	}

	public class MmlResolvedTrack
	{
		public MmlResolvedTrack (int number)
		{
			Number = number;
			Events = new List<MmlResolvedEvent> ();
		}

		public int Number { get; set; }
		public List<MmlResolvedEvent> Events { get; private set; }
	}

	public class MmlResolvedEvent
	{
		public MmlResolvedEvent (MmlPrimitiveOperation operation, int tick)
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
		public MmlPrimitiveOperation Operation { get; private set; }

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
			MacroArguments = new List<KeyValuePair<MmlSemanticVariable,object>> ();
			Values = new Dictionary<MmlSemanticVariable,object> ();
			Loops = new Stack<Loop> ();
		}

		public MmlResolveContext GlobalContext { get; private set; }

		public int TimelinePosition { get; set; }

		public MmlSemanticTreeSet SourceTree { get; set; }

		public List<KeyValuePair<MmlSemanticVariable,object>> MacroArguments { get; private set; }
		public Dictionary<MmlSemanticVariable,object> Values { get; internal set; }
		public Stack<Loop> Loops { get; private set; }

		public Loop CurrentLoop {
			get { return Loops.Count > 0 ? Loops.Peek () : null; }
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
			result = new MmlResolvedMusic ();
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
				var rtrk = new MmlResolvedTrack (track.Number);
				result.Tracks.Add (rtrk);
				var tctx = new MmlResolveContext (source, global_context);
				var list = track.Expanded;
				current_output = rtrk.Events;
				ProcessOperations (rtrk, tctx, list, 0, list.Count);
				Sort (current_output);
				foreach (var ev in current_output)
					Util.DebugWriter.WriteLine ("{0} {1} {2}", ev.Tick, ev.Operation, ev.Arguments.Count);
			}
		}

		List<MmlResolvedEvent> chord = new List<MmlResolvedEvent> ();
		bool recordNextAsChord;

		Stack<MmlLineInfo> locations = new Stack<MmlLineInfo> ();

		MmlLineInfo location { get { return locations.Count > 0 ? locations.Peek () : null; } }

		void ProcessOperations (MmlResolvedTrack track, MmlResolveContext rctx, List<MmlOperationUse> list, int start, int count)
		{
//Util.DebugWriter.WriteLine ("Resolve variables in track {0}", track.Number);

			for (int i = start; i < start + count; i++) {
				var oper = list [i];

				var pop = MmlPrimitiveOperation.All.FirstOrDefault (o => o.Name == oper.Name);
				if (pop == null)
					throw new MmlException (String.Format ("INTERNAL ERROR: unresolved non-primitive operation: {0}", oper.Name), location);

				switch (oper.Name) {
				case "__LOCATE":
					locations.Push ((MmlLineInfo) ((MmlConstantExpr) oper.Arguments [0]).Value);
					continue;
				case "__UNLOCATE":
					locations.Pop ();
					continue;
				case "__PRINT": {
					oper.Arguments [0].Resolve (rctx, MmlDataType.String);
					string name = oper.Arguments [0].StringValue;
					var variable = source.Variables.FirstOrDefault (v => v.Name == name);
					if (variable == null)
						throw new MmlException (String.Format ("Target variable not found: {0}", name), location);
					DebugPrint.WriteLine (rctx.Values [variable]);
					break;
					}
				case "__LET": {
					oper.Arguments [0].Resolve (rctx, MmlDataType.String);
					string name = oper.Arguments [0].StringValue;
					var variable = source.Variables.FirstOrDefault (v => v.Name == name);
					if (variable == null)
						throw new MmlException (String.Format ("Target variable not found: {0}", name), location);
					oper.Arguments [1].Resolve (rctx, variable.Type);
					rctx.Values [variable] = oper.Arguments [1].ResolvedValue;
					if (name == "__timeline_position")
						rctx.TimelinePosition = oper.Arguments [1].IntValue;
					break;
					}
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
				case "__APPLY":
					var oa = oper.Arguments [0];
					oa.Resolve (rctx, MmlDataType.String);
					string apparg = oa.StringValue;
					var macro = source.Macros.LastOrDefault (mm => mm.Name == apparg);
					if (macro == null)
						throw new MmlException (String.Format ("Macro {0} was not found used in __APPLY operation", aname), location);

					// add macro argument definitions
					var tmplist = new List<MmlOperationUse> ();
					var tmpop = new MmlOperationUse (apparg, oper.Location);
					for (int x = 1; x < oper.Arguments.Count; x++)
						tmpop.Arguments.Add (oper.Arguments [x]);
					MmlMacroExpander.AddMacroUseToResult (tmplist, macro, tmpop);

					// then apply the use to the results.
					ProcessOperations (track, rctx, tmplist, 0, tmplist.Count);
					break;
				case "__MIDI":
					oper.ValidateArguments (rctx, oper.Arguments.Count);
					var mop = new MmlResolvedEvent (pop, rctx.TimelinePosition);
					foreach (var arg in oper.Arguments)
						mop.Arguments.Add (arg.ByteValue);
					current_output.Add (mop);
					if (recordNextAsChord)
						chord.Add (mop);
					recordNextAsChord = false;
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
					var mmop = new MmlResolvedEvent (pop, rctx.TimelinePosition);
					mmop.Arguments.Add (0xFF);
					//if (oper.Arguments.Count != 1)
					//	throw new MmlException (String.Format ("META operation argument count mismatch. Expected 1, got {0}", oper.Arguments.Count), location);
					foreach (var arg in oper.Arguments)
						mmop.Arguments.AddRange (arg.ByteArrayValue);
					current_output.Add (mmop);
					break;
				case "__LOOP_BEGIN":
					oper.ValidateArguments (rctx, 0);
					var loop = new Loop (rctx) { BeginAt= new LoopLocation (i, current_output.Count, rctx.TimelinePosition) };
					rctx.Values = new Dictionary<MmlSemanticVariable,object> (loop.SavedValues.Count);
					foreach (var p in loop.SavedValues)
						rctx.Values.Add (p.Key, p.Value);
					rctx.Loops.Push (loop);
					current_output = loop.Events;
					break;
				case "__LOOP_BREAK":
					oper.ValidateArguments (rctx, oper.Arguments.Count);
					loop = rctx.CurrentLoop;
					if (loop == null)
						throw new MmlException ("Loop break operation must be inside a pair of loop start and end", location);
					if (loop.FirstBreakAt == null)
						loop.FirstBreakAt = new LoopLocation (i, current_output.Count, rctx.TimelinePosition);
					foreach (var cl in loop.CurrentBreaks)
						loop.EndLocations [cl] = new LoopLocation (i, current_output.Count, rctx.TimelinePosition);
					loop.CurrentBreaks.Clear ();

					// FIXME: actually this logic does not make sense as now it is defined with fixed-length arguments...
					if (oper.Arguments.Count == 0) { // default loop break
						if (loop.Breaks.ContainsKey (-1))
							throw new MmlException ("Default loop break is already defined in current loop", location);
						loop.Breaks.Add (-1, new LoopLocation (i, current_output.Count, rctx.TimelinePosition));
						loop.CurrentBreaks.Add (-1);
					} else {
						for (int x = 0; x < oper.Arguments.Count; x++) {
							var numexpr = oper.Arguments [x];
							var num = numexpr.IntValue - 1;
							if (x > 0 && num < 0)
								break; // after the last argument.
							loop.CurrentBreaks.Add (num);
							if (loop.Breaks.ContainsKey (num))
								throw new MmlException (String.Format ("Loop section {0} was already defined in current loop", num), location);
							// specified loop count is for human users. Here the number is for program, hence -1.
							loop.Breaks.Add (num, new LoopLocation (i, current_output.Count, rctx.TimelinePosition));
						}
					}
					break;
				case "__LOOP_END":
					oper.ValidateArguments (rctx, 1, MmlDataType.Number);
					loop = rctx.CurrentLoop;
					if (loop == null)
						throw new MmlException ("Loop has not started", location);
					foreach (var cl in loop.CurrentBreaks)
						loop.EndLocations [cl] = new LoopLocation (i, current_output.Count, rctx.TimelinePosition);
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
							ProcessOperations (track, rctx, list, loop.BeginAt.Source + 1, i  - loop.BeginAt.Source - 2);
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
							ProcessOperations (track, rctx, list, loop.BeginAt.Source + 1, loop.FirstBreakAt.Source  - loop.BeginAt.Source - 2);
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
							ProcessOperations (track, rctx, list, lb.Source + 9, elb.Source - lb.Source - 18);
#endif
						}
					}
					break;
				default:
					throw new NotImplementedException (oper.Name);
				}
			}
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
