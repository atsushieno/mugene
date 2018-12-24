// created by jay 0.7 (c) 1998 Axel.Schreiner@informatik.uni-osnabrueck.de

#line 4 "mugenelib/src/mml_parser.jay"
using System;
using System.Collections.Generic;
using System.IO;
using Commons.Music.Midi.Mml;

namespace Commons.Music.Midi.Mml.Parser
{
	public class MmlParserInput : yyParser.yyInput
	{
		IList<MmlToken> tokens;
		int idx = -1;

		public MmlParserInput (IList<MmlToken> tokens)
		{
			this.tokens = tokens;
		}
		
		public MmlLineInfo Location {
			get { return tokens.Count > 0 ? tokens [0].Location : null; }
		}
		
		public bool advance ()
		{
			if (++idx >= tokens.Count)
				return false;
			return true;
		}
		
		public object value ()
		{
			if (idx >= tokens.Count)
				return null;
			return (MmlToken) tokens [idx];
		}

		public int token ()
		{
			switch (tokens [idx].TokenType) {
			case MmlTokenType.Identifier:
				return Token.Identifier;
			case MmlTokenType.StringLiteral:
				return Token.StringLiteral;
			case MmlTokenType.NumberLiteral:
				return Token.NumberLiteral;
			case MmlTokenType.Period:
				return Token.Dot;
			case MmlTokenType.Comma:
				return Token.Comma;
			case MmlTokenType.Percent:
				return Token.Percent;
			case MmlTokenType.OpenCurly:
				return Token.OpenCurly;
			case MmlTokenType.CloseCurly:
				return Token.CloseCurly;
			case MmlTokenType.Question:
				return Token.Question;
			case MmlTokenType.Plus:
				return Token.Plus;
			case MmlTokenType.Minus:
				return Token.Minus;
			case MmlTokenType.Asterisk:
				return Token.Asterisk;
			case MmlTokenType.Slash:
				return Token.Slash;
			case MmlTokenType.Dollar:
				return Token.Dollar;
			case MmlTokenType.Colon:
				return Token.Colon;
			case MmlTokenType.Caret:
				return Token.Caret;
			case MmlTokenType.BackSlashLesser:
				return Token.BackSlashLesser;
			case MmlTokenType.BackSlashLesserEqual:
				return Token.BackSlashLesserEqual;
			case MmlTokenType.BackSlashGreater:
				return Token.BackSlashGreater;
			case MmlTokenType.BackSlashGreaterEqual:
				return Token.BackSlashGreaterEqual;
			case MmlTokenType.KeywordNumber:
				return Token.KeywordNumber;
			case MmlTokenType.KeywordLength:
				return Token.KeywordLength;
			case MmlTokenType.KeywordString:
				return Token.KeywordString;
			case MmlTokenType.KeywordBuffer:
				return Token.KeywordBuffer;
			}
			throw new Exception ("Internal error: unexpected MmlTokenType: " + tokens [idx].TokenType);
		}
	}

	public class MmlParser
	{
		static readonly MmlValueExpr skipped_argument = new MmlConstantExpr (null, MmlDataType.String, "DEFAULT ARGUMENT");
		MmlParserInput input;
		MmlLineInfo current_location;

		// 1: NOW at ...
		// 2+: jay output
		int yacc_verbose_flag;

		public MmlParser (IList<MmlToken> tokens)
		{
			try {
				yacc_verbose_flag = int.Parse (Environment.GetEnvironmentVariable ("MUGENE_MML_DEBUG"));
			} catch (Exception) {
			}
			if (yacc_verbose_flag > 1)
				debug = new yydebug.yyDebugSimple ();
			input = new MmlParserInput (tokens);
		}

        public List<MmlOperationUse> ParseOperations ()
        {
            var results = Parse ();
            var o = results as List<MmlOperationUse>;
            if (o == null) {
                Util.Report (MmlDiagnosticVerbosity.Error, input.Location, "operations are expected, but got an expression.");
                return new List<MmlOperationUse> ();
            }
            return o;
        }

        public MmlValueExpr ParseExpression ()
        {
            var o = Parse () as MmlValueExpr;
            if (o == null) {
                Util.Report (MmlDiagnosticVerbosity.Error, input.Location, "an expression is expected, but got operations.");
                return null;
            }
            return o;
        }
		
		object Parse ()
		{
			try {
				return yyparse (input);
			} catch (yyParser.yyException ex) {
				Util.Report (MmlDiagnosticVerbosity.Error, current_location, "MML parser error: {0}", ex);
                return null;
			}
		}

#line default

  /** error output stream.
      It should be changeable.
    */
  public System.IO.TextWriter ErrorOutput = System.Console.Out;

  /** simplified error message.
      @see <a href="#yyerror(java.lang.String, java.lang.String[])">yyerror</a>
    */
  public void yyerror (string message) {
    yyerror(message, null);
  }
#pragma warning disable 649
  /* An EOF token */
  public int eof_token;
#pragma warning restore 649
  /** (syntax) error message.
      Can be overwritten to control message format.
      @param message text to be displayed.
      @param expected vector of acceptable tokens, if available.
    */
  public void yyerror (string message, string[] expected) {
    if ((yacc_verbose_flag > 0) && (expected != null) && (expected.Length  > 0)) {
      ErrorOutput.Write (message+", expecting");
      for (int n = 0; n < expected.Length; ++ n)
        ErrorOutput.Write (" "+expected[n]);
        ErrorOutput.WriteLine ();
    } else
      ErrorOutput.WriteLine (message);
  }

  /** debugging support, requires the package jay.yydebug.
      Set to null to suppress debugging messages.
    */
  internal yydebug.yyDebug debug;

  protected const int yyFinal = 12;
 // Put this array into a separate class so it is only initialized if debugging is actually used
 // Use MarshalByRefObject to disable inlining
 class YYRules : MarshalByRefObject {
  public static readonly string [] yyRule = {
    "$accept : ExpressionOrOptOperationUses",
    "ExpressionOrOptOperationUses :",
    "ExpressionOrOptOperationUses : OperationUses",
    "ExpressionOrOptOperationUses : Expression",
    "OperationUses : OperationUse",
    "OperationUses : OperationUses OperationUse",
    "$$1 :",
    "OperationUse : CanBeIdentifier $$1 ArgumentsOptCurly",
    "ArgumentsOptCurly : OptArguments",
    "ArgumentsOptCurly : OpenCurly OptArguments CloseCurly",
    "OptArguments :",
    "OptArguments : Arguments",
    "Arguments : Argument",
    "Arguments : OptArgument Comma Arguments",
    "OptArgument :",
    "OptArgument : Argument",
    "Argument : Expression",
    "Expression : ConditionalExpr",
    "ConditionalExpr : ComparisonExpr",
    "ConditionalExpr : ComparisonExpr Question ConditionalExpr Comma ConditionalExpr",
    "ComparisonExpr : AddSubExpr",
    "ComparisonExpr : AddSubExpr ComparisonOperator ComparisonExpr",
    "ComparisonOperator : BackSlashLesser",
    "ComparisonOperator : BackSlashLesserEqual",
    "ComparisonOperator : BackSlashGreater",
    "ComparisonOperator : BackSlashGreaterEqual",
    "AddSubExpr : MulDivModExpr",
    "AddSubExpr : AddSubExpr Plus MulDivModExpr",
    "AddSubExpr : AddSubExpr Caret MulDivModExpr",
    "AddSubExpr : AddSubExpr Minus MulDivModExpr",
    "MulDivModExpr : PrimaryExpr",
    "MulDivModExpr : MulDivModExpr Asterisk PrimaryExpr",
    "MulDivModExpr : MulDivModExpr Slash PrimaryExpr",
    "MulDivModExpr : MulDivModExpr Percent PrimaryExpr",
    "PrimaryExpr : VariableReference",
    "PrimaryExpr : StringConstant",
    "PrimaryExpr : OpenCurly Expression CloseCurly",
    "PrimaryExpr : StepConstant",
    "PrimaryExpr : UnaryExpr",
    "UnaryExpr : NumberOrLengthConstant",
    "UnaryExpr : Minus NumberOrLengthConstant",
    "UnaryExpr : Caret NumberOrLengthConstant",
    "VariableReference : Dollar CanBeIdentifier",
    "StringConstant : StringLiteral",
    "StepConstant : Percent NumberLiteral",
    "StepConstant : Percent Minus NumberLiteral",
    "NumberOrLengthConstant : NumberLiteral",
    "NumberOrLengthConstant : NumberLiteral Dots",
    "NumberOrLengthConstant : Dots",
    "Dots : Dot",
    "Dots : Dots Dot",
    "CanBeIdentifier : Identifier",
    "CanBeIdentifier : Colon",
    "CanBeIdentifier : Slash",
  };
 public static string getRule (int index) {
    return yyRule [index];
 }
}
  protected static readonly string [] yyNames = {    
    "end-of-file",null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,"Identifier","StringLiteral",
    "NumberLiteral","Comma","OpenParen","CloseParen","OpenCurly",
    "CloseCurly","Question","Caret","Plus","Minus","Asterisk","Slash",
    "Percent","Dollar","Colon","Dot","BackSlashLesser",
    "BackSlashLesserEqual","BackSlashGreater","BackSlashGreaterEqual",
    "KeywordNumber","KeywordLength","KeywordString","KeywordBuffer",
  };

  /** index-checked interface to yyNames[].
      @param token single character or %token value.
      @return token name or [illegal] or [unknown].
    */
  public static string yyname (int token) {
    if ((token < 0) || (token > yyNames.Length)) return "[illegal]";
    string name;
    if ((name = yyNames[token]) != null) return name;
    return "[unknown]";
  }

#pragma warning disable 414
  int yyExpectingState;
#pragma warning restore 414
  /** computes list of expected tokens on error by tracing the tables.
      @param state for which to compute the list.
      @return list of token names.
    */
  protected int [] yyExpectingTokens (int state){
    int token, n, len = 0;
    bool[] ok = new bool[yyNames.Length];
    if ((n = yySindex[state]) != 0)
      for (token = n < 0 ? -n : 0;
           (token < yyNames.Length) && (n+token < yyTable.Length); ++ token)
        if (yyCheck[n+token] == token && !ok[token] && yyNames[token] != null) {
          ++ len;
          ok[token] = true;
        }
    if ((n = yyRindex[state]) != 0)
      for (token = n < 0 ? -n : 0;
           (token < yyNames.Length) && (n+token < yyTable.Length); ++ token)
        if (yyCheck[n+token] == token && !ok[token] && yyNames[token] != null) {
          ++ len;
          ok[token] = true;
        }
    int [] result = new int [len];
    for (n = token = 0; n < len;  ++ token)
      if (ok[token]) result[n++] = token;
    return result;
  }
  protected string[] yyExpecting (int state) {
    int [] tokens = yyExpectingTokens (state);
    string [] result = new string[tokens.Length];
    for (int n = 0; n < tokens.Length;  n++)
      result[n++] = yyNames[tokens [n]];
    return result;
  }

  /** the generated parser, with debugging messages.
      Maintains a state and a value stack, currently with fixed maximum size.
      @param yyLex scanner.
      @param yydebug debug message writer implementing yyDebug, or null.
      @return result of the last reduction, if any.
      @throws yyException on irrecoverable parse error.
    */
  internal Object yyparse (yyParser.yyInput yyLex, Object yyd)
				 {
    this.debug = (yydebug.yyDebug)yyd;
    return yyparse(yyLex);
  }

  /** initial size and increment of the state/value stack [default 256].
      This is not final so that it can be overwritten outside of invocations
      of yyparse().
    */
  protected int yyMax;

  /** executed at the beginning of a reduce action.
      Used as $$ = yyDefault($1), prior to the user-specified action, if any.
      Can be overwritten to provide deep copy, etc.
      @param first value for $1, or null.
      @return first.
    */
  protected Object yyDefault (Object first) {
    return first;
  }

	static int[] global_yyStates;
	static object[] global_yyVals;
#pragma warning disable 649
	protected bool use_global_stacks;
#pragma warning restore 649
	object[] yyVals;					// value stack
	object yyVal;						// value stack ptr
	int yyToken;						// current input
	int yyTop;

  /** the generated parser.
      Maintains a state and a value stack, currently with fixed maximum size.
      @param yyLex scanner.
      @return result of the last reduction, if any.
      @throws yyException on irrecoverable parse error.
    */
  internal Object yyparse (yyParser.yyInput yyLex)
  {
    if (yyMax <= 0) yyMax = 256;		// initial size
    int yyState = 0;                   // state stack ptr
    int [] yyStates;               	// state stack 
    yyVal = null;
    yyToken = -1;
    int yyErrorFlag = 0;				// #tks to shift
	if (use_global_stacks && global_yyStates != null) {
		yyVals = global_yyVals;
		yyStates = global_yyStates;
   } else {
		yyVals = new object [yyMax];
		yyStates = new int [yyMax];
		if (use_global_stacks) {
			global_yyVals = yyVals;
			global_yyStates = yyStates;
		}
	}

    /*yyLoop:*/ for (yyTop = 0;; ++ yyTop) {
      if (yyTop >= yyStates.Length) {			// dynamically increase
        global::System.Array.Resize (ref yyStates, yyStates.Length+yyMax);
        global::System.Array.Resize (ref yyVals, yyVals.Length+yyMax);
      }
      yyStates[yyTop] = yyState;
      yyVals[yyTop] = yyVal;
      if (debug != null) debug.push(yyState, yyVal);

      /*yyDiscarded:*/ while (true) {	// discarding a token does not change stack
        int yyN;
        if ((yyN = yyDefRed[yyState]) == 0) {	// else [default] reduce (yyN)
          if (yyToken < 0) {
            yyToken = yyLex.advance() ? yyLex.token() : 0;
            if (debug != null)
              debug.lex(yyState, yyToken, yyname(yyToken), yyLex.value());
          }
          if ((yyN = yySindex[yyState]) != 0 && ((yyN += yyToken) >= 0)
              && (yyN < yyTable.Length) && (yyCheck[yyN] == yyToken)) {
            if (debug != null)
              debug.shift(yyState, yyTable[yyN], yyErrorFlag-1);
            yyState = yyTable[yyN];		// shift to yyN
            yyVal = yyLex.value();
            yyToken = -1;
            if (yyErrorFlag > 0) -- yyErrorFlag;
            goto continue_yyLoop;
          }
          if ((yyN = yyRindex[yyState]) != 0 && (yyN += yyToken) >= 0
              && yyN < yyTable.Length && yyCheck[yyN] == yyToken)
            yyN = yyTable[yyN];			// reduce (yyN)
          else
            switch (yyErrorFlag) {
  
            case 0:
              yyExpectingState = yyState;
              // yyerror(String.Format ("syntax error, got token `{0}'", yyname (yyToken)), yyExpecting(yyState));
              if (debug != null) debug.error("syntax error");
              if (yyToken == 0 /*eof*/ || yyToken == eof_token) throw new yyParser.yyUnexpectedEof ();
              goto case 1;
            case 1: case 2:
              yyErrorFlag = 3;
              do {
                if ((yyN = yySindex[yyStates[yyTop]]) != 0
                    && (yyN += Token.yyErrorCode) >= 0 && yyN < yyTable.Length
                    && yyCheck[yyN] == Token.yyErrorCode) {
                  if (debug != null)
                    debug.shift(yyStates[yyTop], yyTable[yyN], 3);
                  yyState = yyTable[yyN];
                  yyVal = yyLex.value();
                  goto continue_yyLoop;
                }
                if (debug != null) debug.pop(yyStates[yyTop]);
              } while (-- yyTop >= 0);
              if (debug != null) debug.reject();
              throw new yyParser.yyException("irrecoverable syntax error");
  
            case 3:
              if (yyToken == 0) {
                if (debug != null) debug.reject();
                throw new yyParser.yyException("irrecoverable syntax error at end-of-file");
              }
              if (debug != null)
                debug.discard(yyState, yyToken, yyname(yyToken),
  							yyLex.value());
              yyToken = -1;
              goto continue_yyDiscarded;		// leave stack alone
            }
        }
        int yyV = yyTop + 1-yyLen[yyN];
        if (debug != null)
          debug.reduce(yyState, yyStates[yyV-1], yyN, YYRules.getRule (yyN), yyLen[yyN]);
        yyVal = yyV > yyTop ? null : yyVals[yyV]; // yyVal = yyDefault(yyV > yyTop ? null : yyVals[yyV]);
        switch (yyN) {
case 1:
#line 183 "mugenelib/src/mml_parser.jay"
  {
		yyVal = new List<MmlOperationUse> ();
	}
  break;
case 4:
  case_4();
  break;
case 5:
  case_5();
  break;
case 6:
#line 205 "mugenelib/src/mml_parser.jay"
  {
		current_location = ((MmlToken) yyVals[0+yyTop]).Location;
	}
  break;
case 7:
  case_7();
  break;
case 9:
#line 222 "mugenelib/src/mml_parser.jay"
  {
		yyVal = yyVals[-1+yyTop];
	}
  break;
case 10:
#line 228 "mugenelib/src/mml_parser.jay"
  {
		yyVal = new List<MmlValueExpr> ();
	}
  break;
case 12:
  case_12();
  break;
case 13:
  case_13();
  break;
case 14:
#line 250 "mugenelib/src/mml_parser.jay"
  {
		yyVal = skipped_argument;
	}
  break;
case 19:
#line 264 "mugenelib/src/mml_parser.jay"
  {
		yyVal = new MmlConditionalExpr ((MmlValueExpr) yyVals[-4+yyTop], (MmlValueExpr) yyVals[-2+yyTop], (MmlValueExpr) yyVals[0+yyTop]);
	}
  break;
case 21:
#line 271 "mugenelib/src/mml_parser.jay"
  {
		yyVal = new MmlComparisonExpr ((MmlValueExpr) yyVals[-2+yyTop], (MmlValueExpr) yyVals[0+yyTop], (ComparisonType) yyVals[-1+yyTop]);
	}
  break;
case 22:
#line 277 "mugenelib/src/mml_parser.jay"
  {
		yyVal = ComparisonType.Lesser;
	}
  break;
case 23:
#line 281 "mugenelib/src/mml_parser.jay"
  {
		yyVal = ComparisonType.LesserEqual;
	}
  break;
case 24:
#line 285 "mugenelib/src/mml_parser.jay"
  {
		yyVal = ComparisonType.Greater;
	}
  break;
case 25:
#line 289 "mugenelib/src/mml_parser.jay"
  {
		yyVal = ComparisonType.GreaterEqual;
	}
  break;
case 27:
#line 296 "mugenelib/src/mml_parser.jay"
  {
		yyVal = new MmlAddExpr ((MmlValueExpr) yyVals[-2+yyTop], (MmlValueExpr) yyVals[0+yyTop]);
	}
  break;
case 28:
#line 300 "mugenelib/src/mml_parser.jay"
  {
		yyVal = new MmlAddExpr ((MmlValueExpr) yyVals[-2+yyTop], (MmlValueExpr) yyVals[0+yyTop]);
	}
  break;
case 29:
#line 304 "mugenelib/src/mml_parser.jay"
  {
		yyVal = new MmlSubtractExpr ((MmlValueExpr) yyVals[-2+yyTop], (MmlValueExpr) yyVals[0+yyTop]);
	}
  break;
case 31:
#line 311 "mugenelib/src/mml_parser.jay"
  {
		yyVal = new MmlMultiplyExpr ((MmlValueExpr) yyVals[-2+yyTop], (MmlValueExpr) yyVals[0+yyTop]);
	}
  break;
case 32:
#line 315 "mugenelib/src/mml_parser.jay"
  {
		yyVal = new MmlDivideExpr ((MmlValueExpr) yyVals[-2+yyTop], (MmlValueExpr) yyVals[0+yyTop]);
	}
  break;
case 33:
#line 319 "mugenelib/src/mml_parser.jay"
  {
		yyVal = new MmlModuloExpr ((MmlValueExpr) yyVals[-2+yyTop], (MmlValueExpr) yyVals[0+yyTop]);
	}
  break;
case 36:
#line 327 "mugenelib/src/mml_parser.jay"
  {
		yyVal = new MmlParenthesizedExpr ((MmlValueExpr) yyVals[-1+yyTop]);
	}
  break;
case 40:
#line 336 "mugenelib/src/mml_parser.jay"
  {
		yyVal = new MmlMultiplyExpr (new MmlConstantExpr (input.Location, MmlDataType.Number, -1), (MmlValueExpr) yyVals[0+yyTop]);
	}
  break;
case 41:
#line 340 "mugenelib/src/mml_parser.jay"
  {
		yyVal = new MmlAddExpr (new MmlVariableReferenceExpr (input.Location, "__length"), (MmlValueExpr) yyVals[0+yyTop]);
	}
  break;
case 42:
  case_42();
  break;
case 43:
  case_43();
  break;
case 44:
  case_44();
  break;
case 45:
  case_45();
  break;
case 46:
  case_46();
  break;
case 47:
  case_47();
  break;
case 48:
  case_48();
  break;
case 49:
#line 393 "mugenelib/src/mml_parser.jay"
  {
		yyVal = 1;
	}
  break;
case 50:
#line 397 "mugenelib/src/mml_parser.jay"
  {
		yyVal = ((int) yyVals[-1+yyTop]) + 1;
	}
  break;
#line default
        }
        yyTop -= yyLen[yyN];
        yyState = yyStates[yyTop];
        int yyM = yyLhs[yyN];
        if (yyState == 0 && yyM == 0) {
          if (debug != null) debug.shift(0, yyFinal);
          yyState = yyFinal;
          if (yyToken < 0) {
            yyToken = yyLex.advance() ? yyLex.token() : 0;
            if (debug != null)
               debug.lex(yyState, yyToken,yyname(yyToken), yyLex.value());
          }
          if (yyToken == 0) {
            if (debug != null) debug.accept(yyVal);
            return yyVal;
          }
          goto continue_yyLoop;
        }
        if (((yyN = yyGindex[yyM]) != 0) && ((yyN += yyState) >= 0)
            && (yyN < yyTable.Length) && (yyCheck[yyN] == yyState))
          yyState = yyTable[yyN];
        else
          yyState = yyDgoto[yyM];
        if (debug != null) debug.shift(yyStates[yyTop], yyState);
	 goto continue_yyLoop;
      continue_yyDiscarded: ;	// implements the named-loop continue: 'continue yyDiscarded'
      }
    continue_yyLoop: ;		// implements the named-loop continue: 'continue yyLoop'
    }
  }

/*
 All more than 3 lines long rules are wrapped into a method
*/
void case_4()
#line 189 "mugenelib/src/mml_parser.jay"
{
		var l = new List<MmlOperationUse> ();
		l.Add ((MmlOperationUse) yyVals[0+yyTop]);
		yyVal = l;
	}

void case_5()
#line 195 "mugenelib/src/mml_parser.jay"
{
		var l = (List<MmlOperationUse>) yyVals[-1+yyTop];
		l.Add ((MmlOperationUse) yyVals[0+yyTop]);
		yyVal = l;
	}

void case_7()
#line 207 "mugenelib/src/mml_parser.jay"
{
		var i = (MmlToken) yyVals[-2+yyTop];
		if (yacc_verbose_flag > 0)
			Console.WriteLine ("NOW at : " + i.Location);
		var o = new MmlOperationUse ((string) i.Value, i.Location);
		foreach (MmlValueExpr a in (IEnumerable<MmlValueExpr>) yyVals[0+yyTop])
			o.Arguments.Add (a == skipped_argument ? null : a);
		yyVal = o;
	}

void case_12()
#line 233 "mugenelib/src/mml_parser.jay"
{
		var l = new List<MmlValueExpr> ();
		l.Add ((MmlValueExpr) yyVals[0+yyTop]);
		yyVal = l;
	}

void case_13()
#line 239 "mugenelib/src/mml_parser.jay"
{
		var a = (MmlValueExpr) yyVals[-2+yyTop];
		var l = (List<MmlValueExpr>) yyVals[0+yyTop];
		l.Insert (0, a);
		yyVal = l;
	}

void case_42()
#line 344 "mugenelib/src/mml_parser.jay"
{
		var i = (MmlToken) yyVals[0+yyTop];
		yyVal = new MmlVariableReferenceExpr (input.Location, (string) i.Value);
	}

void case_43()
#line 351 "mugenelib/src/mml_parser.jay"
{
		var t = (MmlToken) yyVals[0+yyTop];
		yyVal = new MmlConstantExpr (input.Location, MmlDataType.String, (string) t.Value);
	}

void case_44()
#line 358 "mugenelib/src/mml_parser.jay"
{
		var n = (MmlToken) yyVals[0+yyTop];
		var l = new MmlLength ((int) (double) MmlValueExpr.GetTypedValue (n.Value, MmlDataType.Number, n.Location)) { IsValueByStep = true };
		yyVal = new MmlConstantExpr (input.Location, MmlDataType.Length, l);
	}

void case_45()
#line 364 "mugenelib/src/mml_parser.jay"
{
		var n = (MmlToken) yyVals[0+yyTop];
		var l = new MmlLength (-1 * (int) (double) MmlValueExpr.GetTypedValue (n.Value, MmlDataType.Number, n.Location)) { IsValueByStep = true };
		yyVal = new MmlConstantExpr (input.Location, MmlDataType.Length, l);
	}

void case_46()
#line 372 "mugenelib/src/mml_parser.jay"
{
		var t = (MmlToken) yyVals[0+yyTop];
		yyVal = new MmlConstantExpr (input.Location, MmlDataType.Number, t.Value);
	}

void case_47()
#line 377 "mugenelib/src/mml_parser.jay"
{
		var t = (MmlToken) yyVals[-1+yyTop];
		var d = (int) yyVals[0+yyTop];
		yyVal = new MmlConstantExpr (input.Location, MmlDataType.Length, new MmlLength ((int) t.Value) { Dots = d });
	}

void case_48()
#line 383 "mugenelib/src/mml_parser.jay"
{
		var d = (int) yyVals[0+yyTop];
		yyVal = new MmlMultiplyExpr (new MmlConstantExpr (input.Location, MmlDataType.Number, MmlValueExpr.LengthDotsToMultiplier (d)), new MmlVariableReferenceExpr (input.Location, "__length"));
	}

#line default
   static readonly short [] yyLhs  = {              -1,
    0,    0,    0,    1,    1,    6,    3,    5,    5,    7,
    7,    8,    8,   10,   10,    9,    2,   11,   11,   12,
   12,   14,   14,   14,   14,   13,   13,   13,   13,   15,
   15,   15,   15,   16,   16,   16,   16,   16,   20,   20,
   20,   17,   18,   19,   19,   21,   21,   21,   22,   22,
    4,    4,    4,
  };
   static readonly short [] yyLen = {           2,
    0,    1,    1,    1,    2,    0,    3,    1,    3,    0,
    1,    1,    3,    0,    1,    1,    1,    1,    5,    1,
    3,    1,    1,    1,    1,    1,    3,    3,    3,    1,
    3,    3,    3,    1,    1,    3,    1,    1,    1,    2,
    2,    2,    1,    2,    3,    1,    2,    1,    1,    2,
    1,    1,    1,
  };
   static readonly short [] yyDefRed = {            0,
   51,   43,    0,    0,    0,    0,   53,    0,    0,   52,
   49,    0,    0,    3,    4,    6,   17,    0,    0,    0,
   30,   34,   35,   37,   38,   39,    0,    0,    0,   41,
   40,   44,    0,   42,    5,    0,    0,    0,    0,    0,
   22,   23,   24,   25,    0,    0,    0,    0,   50,   36,
   45,    0,   16,    7,    8,   11,    0,    0,    0,    0,
    0,    0,   21,   31,   32,   33,    0,    0,    0,    0,
    9,   13,   19,
  };
  protected static readonly short [] yyDgoto  = {            12,
   13,   53,   15,   16,   54,   36,   55,   56,   57,   58,
   17,   18,   19,   45,   20,   21,   22,   23,   24,   25,
   26,   27,
  };
  protected static readonly short [] yySindex = {         -254,
    0,    0, -267, -237, -249, -249,    0, -227, -246,    0,
    0,    0, -246,    0,    0,    0,    0, -257, -206, -213,
    0,    0,    0,    0,    0,    0, -261, -261, -236,    0,
    0,    0, -244,    0,    0, -219, -237, -237, -237, -237,
    0,    0,    0,    0, -237, -237, -237, -237,    0,    0,
    0, -237,    0,    0,    0,    0,    0, -230, -224, -213,
 -213, -213,    0,    0,    0,    0, -236, -221, -237, -237,
    0,    0,    0,
  };
  protected static readonly short [] yyRindex = {           46,
    0,    0,    1,    0,    0,    0,    0,    0,    0,    0,
    0,    0,   48,    0,    0,    0,    0,  157,  155,   67,
    0,    0,    0,    0,    0,    0,   23,   45,    0,    0,
    0,    0,    0,    0,    0,   68,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0, -222,    0,    0,    0,    0,  169,    0,    0,   89,
  111,  133,    0,    0,    0,    0, -210,    0, -209,    0,
    0,    0,    0,
  };
  protected static readonly short [] yyGindex = {            0,
    0,    2,   53,   50,    0,    0,   26,   10,    0,    0,
  -37,   35,    0,    0,   25,   27,    0,    0,    0,    0,
   71,   78,
  };
  protected static readonly short [] yyTable = {            59,
   46,   14,    1,    2,    3,   29,   11,   37,    4,    3,
    1,    5,   49,    6,   51,    7,    8,    9,   10,   11,
    2,    3,   48,    7,   11,    4,   10,   50,    5,   69,
    6,   32,   73,    8,    9,   70,   11,   14,    2,    3,
   33,   10,   71,   52,   47,    1,    5,    2,    6,   16,
   14,    8,    9,   67,   11,   46,   47,   48,   34,   38,
   39,   40,   60,   61,   62,   35,   26,   10,   41,   42,
   43,   44,   64,   65,   66,   30,   31,   68,   72,   63,
   28,    0,    0,    0,    0,    0,    0,    0,   28,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
   27,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,   29,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,   20,    0,   18,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,   12,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,   46,    0,    0,
   46,    0,    0,    0,   46,   46,   46,   46,   46,   46,
   46,   46,    0,   46,    0,   46,   46,   46,   46,   48,
    0,    0,   48,    0,    0,    0,   48,   48,   48,   48,
   48,   48,   48,   48,    0,   48,    0,   48,   48,   48,
   48,   47,    0,    0,   47,    0,    0,    0,   47,   47,
   47,   47,   47,   47,   47,   47,    0,   47,    0,   47,
   47,   47,   47,   26,   10,    0,   26,   14,    0,    0,
   26,   26,   26,   26,   26,    0,    0,   10,    0,   26,
   10,   26,   26,   26,   26,   28,    0,    0,   28,    0,
    0,    0,   28,   28,   28,   28,   28,    0,    0,    0,
    0,   28,    0,   28,   28,   28,   28,   27,    0,    0,
   27,    0,    0,    0,   27,   27,   27,   27,   27,    0,
    0,    0,    0,   27,    0,   27,   27,   27,   27,   29,
    0,    0,   29,    0,    0,    0,   29,   29,   29,   29,
   29,    0,    0,    0,    0,   29,    0,   29,   29,   29,
   29,   20,    0,   18,   20,    0,   18,    0,   20,   20,
   18,    0,    0,    0,   20,   12,   18,   20,   15,   18,
    0,    0,   12,    0,    0,    0,    0,    0,   12,    0,
    0,   12,
  };
  protected static readonly short [] yyCheck = {            37,
    0,    0,  257,  258,  259,    4,  274,  265,  263,  259,
  257,  266,  274,  268,  259,  270,  271,  272,  273,  274,
  258,  259,    0,  270,  274,  263,  273,  264,  266,  260,
  268,  259,   70,  271,  272,  260,  274,  260,  258,  259,
  268,  264,  264,  263,    0,    0,  266,    0,  268,  260,
  260,  271,  272,   52,  274,  269,  270,  271,    9,  266,
  267,  268,   38,   39,   40,   13,    0,    0,  275,  276,
  277,  278,   46,   47,   48,    5,    6,   52,   69,   45,
    3,   -1,   -1,   -1,   -1,   -1,   -1,   -1,    0,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
    0,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,    0,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,    0,   -1,    0,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,    0,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  257,   -1,   -1,
  260,   -1,   -1,   -1,  264,  265,  266,  267,  268,  269,
  270,  271,   -1,  273,   -1,  275,  276,  277,  278,  257,
   -1,   -1,  260,   -1,   -1,   -1,  264,  265,  266,  267,
  268,  269,  270,  271,   -1,  273,   -1,  275,  276,  277,
  278,  257,   -1,   -1,  260,   -1,   -1,   -1,  264,  265,
  266,  267,  268,  269,  270,  271,   -1,  273,   -1,  275,
  276,  277,  278,  257,  257,   -1,  260,  260,   -1,   -1,
  264,  265,  266,  267,  268,   -1,   -1,  270,   -1,  273,
  273,  275,  276,  277,  278,  257,   -1,   -1,  260,   -1,
   -1,   -1,  264,  265,  266,  267,  268,   -1,   -1,   -1,
   -1,  273,   -1,  275,  276,  277,  278,  257,   -1,   -1,
  260,   -1,   -1,   -1,  264,  265,  266,  267,  268,   -1,
   -1,   -1,   -1,  273,   -1,  275,  276,  277,  278,  257,
   -1,   -1,  260,   -1,   -1,   -1,  264,  265,  266,  267,
  268,   -1,   -1,   -1,   -1,  273,   -1,  275,  276,  277,
  278,  257,   -1,  257,  260,   -1,  260,   -1,  264,  265,
  264,   -1,   -1,   -1,  270,  257,  270,  273,  260,  273,
   -1,   -1,  264,   -1,   -1,   -1,   -1,   -1,  270,   -1,
   -1,  273,
  };

#line 406 "mugenelib/src/mml_parser.jay"

	}
#line default
namespace yydebug {
        using System;
	 internal interface yyDebug {
		 void push (int state, Object value);
		 void lex (int state, int token, string name, Object value);
		 void shift (int from, int to, int errorFlag);
		 void pop (int state);
		 void discard (int state, int token, string name, Object value);
		 void reduce (int from, int to, int rule, string text, int len);
		 void shift (int from, int to);
		 void accept (Object value);
		 void error (string message);
		 void reject ();
	 }
	 
	 class yyDebugSimple : yyDebug {
		 void println (string s){
			 Console.Error.WriteLine (s);
		 }
		 
		 public void push (int state, Object value) {
			 println ("push\tstate "+state+"\tvalue "+value);
		 }
		 
		 public void lex (int state, int token, string name, Object value) {
			 println("lex\tstate "+state+"\treading "+name+"\tvalue "+value);
		 }
		 
		 public void shift (int from, int to, int errorFlag) {
			 switch (errorFlag) {
			 default:				// normally
				 println("shift\tfrom state "+from+" to "+to);
				 break;
			 case 0: case 1: case 2:		// in error recovery
				 println("shift\tfrom state "+from+" to "+to
					     +"\t"+errorFlag+" left to recover");
				 break;
			 case 3:				// normally
				 println("shift\tfrom state "+from+" to "+to+"\ton error");
				 break;
			 }
		 }
		 
		 public void pop (int state) {
			 println("pop\tstate "+state+"\ton error");
		 }
		 
		 public void discard (int state, int token, string name, Object value) {
			 println("discard\tstate "+state+"\ttoken "+name+"\tvalue "+value);
		 }
		 
		 public void reduce (int from, int to, int rule, string text, int len) {
			 println("reduce\tstate "+from+"\tuncover "+to
				     +"\trule ("+rule+") "+text);
		 }
		 
		 public void shift (int from, int to) {
			 println("goto\tfrom state "+from+" to "+to);
		 }
		 
		 public void accept (Object value) {
			 println("accept\tvalue "+value);
		 }
		 
		 public void error (string message) {
			 println("error\t"+message);
		 }
		 
		 public void reject () {
			 println("reject");
		 }
		 
	 }
}
// %token constants
 class Token {
  public const int Identifier = 257;
  public const int StringLiteral = 258;
  public const int NumberLiteral = 259;
  public const int Comma = 260;
  public const int OpenParen = 261;
  public const int CloseParen = 262;
  public const int OpenCurly = 263;
  public const int CloseCurly = 264;
  public const int Question = 265;
  public const int Caret = 266;
  public const int Plus = 267;
  public const int Minus = 268;
  public const int Asterisk = 269;
  public const int Slash = 270;
  public const int Percent = 271;
  public const int Dollar = 272;
  public const int Colon = 273;
  public const int Dot = 274;
  public const int BackSlashLesser = 275;
  public const int BackSlashLesserEqual = 276;
  public const int BackSlashGreater = 277;
  public const int BackSlashGreaterEqual = 278;
  public const int KeywordNumber = 279;
  public const int KeywordLength = 280;
  public const int KeywordString = 281;
  public const int KeywordBuffer = 282;
  public const int yyErrorCode = 256;
 }
 namespace yyParser {
  using System;
  /** thrown for irrecoverable syntax errors and stack overflow.
    */
  internal class yyException : System.Exception {
    public yyException (string message) : base (message) {
    }
  }
  internal class yyUnexpectedEof : yyException {
    public yyUnexpectedEof (string message) : base (message) {
    }
    public yyUnexpectedEof () : base ("") {
    }
  }

  /** must be implemented by a scanner object to supply input to the parser.
    */
  internal interface yyInput {
    /** move on to next token.
        @return false if positioned beyond tokens.
        @throws IOException on input error.
      */
    bool advance (); // throws java.io.IOException;
    /** classifies current token.
        Should not be called if advance() returned false.
        @return current %token or single character.
      */
    int token ();
    /** associated with current token.
        Should not be called if advance() returned false.
        @return value for token().
      */
    Object value ();
  }
 }
} // close outermost namespace, that MUST HAVE BEEN opened in the prolog
