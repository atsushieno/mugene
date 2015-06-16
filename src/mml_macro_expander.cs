using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Commons.Music.Midi.Mml
{
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
			foreach (MmlSemanticVariable variable in source.Variables.Values) {
				if (variable.DefaultValue == null)
					variable.FillDefaultValue ();
				if (variable.DefaultValue == null)
					throw new Exception ("INTERNAL ERROR: no default value for " + variable.Name);
				variable.DefaultValue.Resolve (ctx, variable.Type);
			}

			foreach (var macro in source.Macros)
				ExpandMacro (macro);
		}

		void ExpandMacro (MmlSemanticMacro macro)
		{
			ExpandMacro (macro.Arguments, macro.Data, null, macro);
		}

		internal void ExpandMacro (List<MmlSemanticVariable> args, List<MmlOperationUse> src, List<MmlOperationUse> output, MmlSemanticMacro macro)
		{
			foreach (var variable in args)
				if (variable.DefaultValue == null)
						variable.FillDefaultValue ();
		}
	}

	#endregion
}
