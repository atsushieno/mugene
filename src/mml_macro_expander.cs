using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Commons.Music.Midi.Mml
{
	#region semantic tree expansion structures

	public partial class MmlSemanticTrack
	{
		public List<MmlOperationUse> Expanded { get; set; }
	}

	public partial class MmlSemanticMacro
	{
		public List<MmlOperationUse> Expanded { get; set; }
	}

	#endregion

	#region semantic tree expander

	public class MmlMacroExpander
	{
		public static void Expand (MmlSemanticTreeSet source)
		{
			new MmlMacroExpander (source).Expand ();
		}

		MmlMacroExpander (MmlSemanticTreeSet source)
		{
			this.source = source;
		}

		MmlSemanticTreeSet source;
		Stack<MmlSemanticMacro> expansion_stack = new Stack<MmlSemanticMacro> ();

		void Expand ()
		{
			var ctx = new MmlResolveContext (source, null);

			// resolve variables without any context.
			foreach (var variable in source.Variables) {
				if (variable.DefaultValue == null)
					variable.FillDefaultValue ();
				if (variable.DefaultValue == null)
					throw new Exception ("INTERNAL ERROR: no default value for " + variable.Name);
				variable.DefaultValue.Resolve (ctx, variable.Type);
			}

			foreach (var macro in source.Macros)
				ExpandMacro (macro);

			foreach (var track in source.Tracks)
				ExpandTrack (track);
		}

		void ExpandContent (List<MmlOperationUse> src, List<MmlOperationUse> dst, int targetTrackNumber)
		{
			foreach (var op in src) {
				if (MmlPrimitiveOperation.All.Any (poper => poper.Name == op.Name)) {
					dst.Add (op);
					continue;
				}
				var m = source.Macros.LastOrDefault (mm => mm.Name == op.Name && (mm.TargetTracks == null || mm.TargetTracks.Contains (targetTrackNumber)));
				if (m == null)
					throw new MmlException (String.Format ("Macro {0} is not defined", op.Name), op.Location);
				ExpandMacro (m); // while expanding track, it must do nothing.
				AddMacroUseToResult (dst, m, op);
			}
		}

		void ExpandTrack (MmlSemanticTrack track)
		{
//Util.DebugWriter.Write ("Expanding track {0}, of {1} operation(s)", track.Number, track.Data.Count);
			track.Expanded = new List<MmlOperationUse> ();

			ExpandContent (track.Data, track.Expanded, track.Number);
//Util.DebugWriter.WriteLine (" ... into {0} operation(s)", track.Expanded.Count);
//foreach (var op in track.Expanded) Util.DebugWriter.WriteLine (op);
		}

		void ExpandMacro (MmlSemanticMacro macro)
		{
//Util.DebugWriter.Write ("Expanding macro {0}, from {1} operation(s)", macro.Name, macro.Data.Count);
			if (macro.Expanded != null)
				return;
			macro.Expanded = new List<MmlOperationUse> ();

			ExpandMacro (macro.Arguments, macro.Data, macro.Expanded, macro);
		}

		internal void ExpandMacro (List<MmlSemanticVariable> args, List<MmlOperationUse> src, List<MmlOperationUse> output, MmlSemanticMacro macro)
		{
			foreach (var variable in args)
				if (variable.DefaultValue == null)
						variable.FillDefaultValue ();

			if (expansion_stack.Contains (macro))
				throw new MmlException (String.Format ("Illegally recursive macro reference to {0} is found", macro.Name), null);
			expansion_stack.Push (macro);

			ExpandContent (src, output, -1);
//Util.DebugWriter.WriteLine (" ... into {0} operation(s)", output.Count);

			expansion_stack.Pop ();
		}

		internal static void AddMacroUseToResult (List<MmlOperationUse> referencing, MmlSemanticMacro used, MmlOperationUse op)
		{
			var macro = used;

			for (int i = 0; i < macro.Arguments.Count; i++) {
				var defarg = macro.Arguments [i];
				var defuse = i < op.Arguments.Count ? op.Arguments [i] : null;
				var def = new MmlOperationUse ("__MACRO_ARG_DEF", op.Location);
				def.Arguments.Add (new MmlConstantExpr (MmlDataType.Any, defarg)); // somewhat special (it takes semantic variable)
				def.Arguments.Add (defuse ?? defarg.DefaultValue);
				referencing.Add (def);
			}

			referencing.AddRange (used.Expanded);

			foreach (var arg in macro.Arguments) {
				var def = new MmlOperationUse ("__MACRO_ARG_UNDEF", op.Location);
				def.Arguments.Add (new MmlConstantExpr (MmlDataType.String, arg.Name));
				referencing.Add (def);
			}
		}
	}

	#endregion
}
