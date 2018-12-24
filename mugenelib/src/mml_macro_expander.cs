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
		public static void Expand (MmlSemanticTreeSet source, MmlCompiler contextCompiler)
		{
			new MmlMacroExpander (source, contextCompiler).Expand ();
		}

		MmlMacroExpander (MmlSemanticTreeSet source, MmlCompiler contextCompiler)
		{
			this.compiler = contextCompiler;
			this.source = source;
		}

		MmlCompiler compiler;
		MmlSemanticTreeSet source;
		Stack<MmlSemanticMacro> expansion_stack = new Stack<MmlSemanticMacro> ();

		void Expand ()
		{
			var ctx = new MmlResolveContext (source, null, compiler);

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
			foreach (var variable in macro.Arguments)
				if (variable.DefaultValue == null)
						variable.FillDefaultValue ();
		}
	}

	#endregion
}
