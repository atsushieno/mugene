using System;
using Irony.Parsing;
using Irony.Ast;

namespace Commons.Music.Midi.Mml
{
	static class Extensions {
		public static T WhichCreates<T> (this T nt, Func<AstContext,ParseTreeNode,object> creator) where T : BnfTerm
		{
			nt.AstConfig.NodeCreator = (ctx, node) => node.AstNode = creator (ctx, node);
			return nt;
		}

		public static T WhichCreates<T> (this T term, object value) where T : BnfTerm
		{
			term.AstConfig.NodeCreator = (ctx, node) => node.AstNode = value;
			return term;
		}

		public static T At<T> (this T nt, int index) where T : BnfTerm
		{
			nt.AstConfig.NodeCreator = (ctx, node) => node.AstNode = node.ChildNodes [index].AstNode;
			return nt;
		}

		public static T ValueAt<T> (this ParseTreeNode node, int index) => (T) node.ChildNodes [index].AstNode;
	}

	[Language ("mugene MML")]
	public class MmlGrammar : Grammar
	{
		static NonTerminal NT (string label) => new NonTerminal (label);

		public MmlGrammar ()
		{
			CommentTerminal single_line_comment = new CommentTerminal ("SingleLineComment", "//", "\r", "\n");
			NonGrammarTerminals.Add (single_line_comment);

			var compile_unit = NT ("compile_unit");
			var operation_uses = NT ("operation_uses");
			var operation_use = NT ("operation_use");
			var can_be_identifier = NT ("can_be_identifier");
			var arguments_opt_curly = NT ("arguments_opt_curly");
			var opt_arguments = NT ("_opt_arguments");
			var opt_argument = NT ("opt_argument");
			var argument = NT ("argument");

			var expression = NT ("expression");
			var conditional_expr = NT ("conditional_expr");
			var comparison_expr = NT ("comparison_expr");
			var comparison_oper = NT ("comparison_oper");
			var add_sub_expr = NT ("add_sub_expr");
			var add_sub_oper = NT ("add_sub_oper");
			var mul_div_expr = NT ("mul_div_expr");
			var mul_div_oper = NT ("mul_div_oper");

			var primary_expr = NT ("primary_expr");
			var variable_reference = NT ("variable_reference");
			var number_or_length_constant = NT ("number_or_length_constant");
			var string_constant = NT ("string_constant");
			var step_constant = NT ("step_constant");
			var unary_expr = NT ("unary_expr");
			var string_literal = new StringLiteral ("string_literal", "\"");
			var number_literal = new NumberLiteral ("number_literal", NumberOptions.None);
			var identifier = new IdentifierTerminal ("identifier", IdOptions.None);
			var dots = new RegexLiteral ("(\\.)+");

			compile_unit.Rule = expression | operation_uses;
			operation_uses.Rule = MakePlusRule (operation_uses, operation_use);
			operation_use.Rule = can_be_identifier + arguments_opt_curly;
			arguments_opt_curly.Rule = opt_arguments | ("{" + opt_arguments + "}").At (1);
			opt_arguments.Rule = MakeStarRule (opt_arguments, ToTerm (","), opt_argument);
			opt_argument.Rule = Empty | expression;

			expression.Rule = conditional_expr;
			conditional_expr.Rule = comparison_expr
				| (conditional_expr + "?" + conditional_expr + "," + conditional_expr).WhichCreates (
					(ctx, node) => new MmlConditionalExpr (node.ValueAt<MmlValueExpr> (0), node.ValueAt<MmlValueExpr> (2), node.ValueAt<MmlValueExpr> (4)));
			comparison_expr.Rule = add_sub_expr
				| (comparison_expr + comparison_oper + add_sub_expr).WhichCreates (
					(ctx, node) => new MmlComparisonExpr (node.ValueAt<MmlValueExpr> (0), node.ValueAt<MmlValueExpr> (2), node.ValueAt<ComparisonType> (1)));
			comparison_oper.Rule =
				               ToTerm ("\\<").WhichCreates (ComparisonType.Lesser)
				               | ToTerm ("\\<=").WhichCreates (ComparisonType.LesserEqual)
				               | ToTerm ("\\>").WhichCreates (ComparisonType.Greater)
				               | ToTerm ("\\>=").WhichCreates (ComparisonType.GreaterEqual);
			add_sub_expr.Rule = mul_div_expr
				| (mul_div_expr + (ToTerm ("+") | "^") + add_sub_expr).WhichCreates (
					(ctx, node) => new MmlAddExpr (node.ValueAt<MmlValueExpr> (0), node.ValueAt<MmlValueExpr> (2))) // ^ is for tie = length addition.
				| (mul_div_expr + "-" + add_sub_expr).WhichCreates (
					(ctx, node) => new MmlSubtractExpr (node.ValueAt<MmlValueExpr> (0), node.ValueAt<MmlValueExpr> (2)));
			mul_div_expr.Rule = primary_expr
				| (mul_div_expr + "*" + primary_expr).WhichCreates (
						(ctx, node) => new MmlMultiplyExpr (node.ValueAt<MmlValueExpr> (0), node.ValueAt<MmlValueExpr> (2)))
				| (mul_div_expr + "-" + primary_expr).WhichCreates (
						(ctx, node) => new MmlDivideExpr (node.ValueAt<MmlValueExpr> (0), node.ValueAt<MmlValueExpr> (2)))
				| (mul_div_expr + "%" + primary_expr).WhichCreates (
						(ctx, node) => new MmlModuloExpr (node.ValueAt<MmlValueExpr> (0), node.ValueAt<MmlValueExpr> (2)));

			primary_expr.Rule = variable_reference
				| string_literal
				| ("{" + expression + "}").WhichCreates (
					(ctx, node) => new MmlParenthesizedExpr (node.ValueAt<MmlValueExpr> (1)))
				| step_constant
				| unary_expr;

			unary_expr.Rule = number_or_length_constant
				| ("-" + number_or_length_constant).WhichCreates (
					(ctx, node) => new MmlMultiplyExpr (new MmlConstantExpr (MmlDataType.Number, -1), node.ValueAt<MmlValueExpr> (1)))
				| ("^" + number_or_length_constant).WhichCreates (
					(ctx, node) => new MmlAddExpr (new MmlVariableReferenceExpr ("__length"), node.ValueAt<MmlValueExpr> (1)));
			variable_reference.Rule = ("$" + can_be_identifier).WhichCreates (
				(ctx, node) => new MmlVariableReferenceExpr ((string) node.ValueAt<MmlToken> (1).Value));
			step_constant.Rule =
				("%" + number_literal).WhichCreates (
					(ctx, node) => {
						var n = node.ValueAt<MmlToken> (1);
						var l = new MmlLength ((int) (double) MmlValueExpr.GetTypedValue (n.Value, MmlDataType.Number)) { IsValueByStep = true };
						return new MmlConstantExpr (MmlDataType.Length, l);
					})
				| ("%-" + number_literal).WhichCreates (
					(ctx, node) => {
						var n = node.ValueAt<MmlToken> (2);
						var l = new MmlLength (-1 * (int) (double) MmlValueExpr.GetTypedValue (n.Value, MmlDataType.Number)) { IsValueByStep = true };
						return new MmlConstantExpr (MmlDataType.Length, l);
					});
			number_or_length_constant.Rule = 
				number_literal.WhichCreates (
					(ctx, node) => new MmlConstantExpr (MmlDataType.Number, node.ValueAt<MmlToken> (0).Value))
				| (number_literal + dots).WhichCreates (
					(ctx, node) => new MmlConstantExpr (MmlDataType.Length, new MmlLength ((int) node.ValueAt<MmlToken> (0).Value) { Dots = node.ValueAt<int> (1) }))
				| dots.WhichCreates (
					(ctx, node) => new MmlMultiplyExpr (new MmlConstantExpr (MmlDataType.Number, MmlValueExpr.LengthDotsToMultiplier (node.ValueAt<int> (0))), new MmlVariableReferenceExpr ("__length")));
			can_be_identifier.Rule = identifier | ":" | "/";

			this.Root = compile_unit;
		}
	}
}
