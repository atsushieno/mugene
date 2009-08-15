// created by jay 0.7 (c) 1998 Axel.Schreiner@informatik.uni-osnabrueck.de

#line 4 "mml_parser.jay"
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
			case MmlTokenType.BackSlashLesser:
				return Token.BackSlashLesser;
			case MmlTokenType.BackSlashGreater:
				return Token.BackSlashGreater;
			case MmlTokenType.KeywordNumber:
				return Token.KeywordNumber;
			case MmlTokenType.KeywordLength:
				return Token.KeywordLength;
			case MmlTokenType.KeywordString:
				return Token.KeywordString;
			}
			throw new Exception ("Internal error: unexpected MmlTokenType: " + tokens [idx].TokenType);
		}
	}

	public class MmlParser
	{
		static readonly MmlValueExpr skipped_argument = new MmlConstantExpr (MmlDataType.String, "DEFAULT ARGUMENT");
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
			var o = Parse () as List<MmlOperationUse>;
			if (o == null)
				throw new MmlException ("operations are expected, but got an expression.", input.Location);
			return o;
		}

		public MmlValueExpr ParseExpression ()
		{
			var o = Parse () as MmlValueExpr;
			if (o == null)
				throw new MmlException ("an expression is expected, but got operations.", input.Location);
			return o;
		}
		
		object Parse ()
		{
			try {
				return yyparse (input);
			} catch (yyParser.yyException ex) {
				throw new MmlException ("MML parser error", current_location);
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

  /* An EOF token */
  public int eof_token;

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

  protected static  int yyFinal = 10;
 // Put this array into a separate class so it is only initialized if debugging is actually used
 // Use MarshalByRefObject to disable inlining
 class YYRules : MarshalByRefObject {
  public static  string [] yyRule = {
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
    "VariableReference : Dollar CanBeIdentifier",
    "StringConstant : StringLiteral",
    "StepConstant : Percent NumberLiteral",
    "StepConstant : Percent Minus NumberLiteral",
    "NumberOrLengthConstant : NumberLiteral",
    "NumberOrLengthConstant : NumberLiteral Dots",
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
  protected static  string [] yyNames = {    
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
    "CloseCurly","Question","Plus","Minus","Asterisk","Slash","Percent",
    "Dollar","Colon","Dot","BackSlashLesser","BackSlashLesserEqual",
    "BackSlashGreater","BackSlashGreaterEqual","KeywordNumber",
    "KeywordLength","KeywordString",
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

  int yyExpectingState;
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

  /** the generated parser.
      Maintains a state and a value stack, currently with fixed maximum size.
      @param yyLex scanner.
      @return result of the last reduction, if any.
      @throws yyException on irrecoverable parse error.
    */
  internal Object yyparse (yyParser.yyInput yyLex)
  {
    if (yyMax <= 0) yyMax = 256;			// initial size
    int yyState = 0;                                   // state stack ptr
    int [] yyStates = new int[yyMax];	                // state stack 
    Object yyVal = null;                               // value stack ptr
    Object [] yyVals = new Object[yyMax];	        // value stack
    int yyToken = -1;					// current input
    int yyErrorFlag = 0;				// #tks to shift

    /*yyLoop:*/ for (int yyTop = 0;; ++ yyTop) {
      if (yyTop >= yyStates.Length) {			// dynamically increase
        int[] i = new int[yyStates.Length+yyMax];
        yyStates.CopyTo (i, 0);
        yyStates = i;
        Object[] o = new Object[yyVals.Length+yyMax];
        yyVals.CopyTo (o, 0);
        yyVals = o;
      }
      yyStates[yyTop] = yyState;
      yyVals[yyTop] = yyVal;
      if (debug != null) debug.push(yyState, yyVal);

      /*yyDiscarded:*/ for (;;) {	// discarding a token does not change stack
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
        yyVal = yyDefault(yyV > yyTop ? null : yyVals[yyV]);
        switch (yyN) {
case 1:
#line 165 "mml_parser.jay"
  {
		yyVal = new List<MmlOperationUse> ();
	}
  break;
case 4:
#line 173 "mml_parser.jay"
  {
		var l = new List<MmlOperationUse> ();
		l.Add ((MmlOperationUse) yyVals[0+yyTop]);
		yyVal = l;
	}
  break;
case 5:
#line 179 "mml_parser.jay"
  {
		var l = (List<MmlOperationUse>) yyVals[-1+yyTop];
		l.Add ((MmlOperationUse) yyVals[0+yyTop]);
		yyVal = l;
	}
  break;
case 6:
#line 187 "mml_parser.jay"
  {
		current_location = ((MmlToken) yyVals[0+yyTop]).Location;
	}
  break;
case 7:
#line 191 "mml_parser.jay"
  {
		var i = (MmlToken) yyVals[-2+yyTop];
		if (yacc_verbose_flag > 0)
			Console.WriteLine ("NOW at : " + i.Location);
		var o = new MmlOperationUse ((string) i.Value, i.Location);
		foreach (MmlValueExpr a in (IEnumerable<MmlValueExpr>) yyVals[0+yyTop])
			o.Arguments.Add (a == skipped_argument ? null : a);
		yyVal = o;
	}
  break;
case 9:
#line 204 "mml_parser.jay"
  {
		yyVal = yyVals[-1+yyTop];
	}
  break;
case 10:
#line 210 "mml_parser.jay"
  {
		yyVal = new List<MmlValueExpr> ();
	}
  break;
case 12:
#line 217 "mml_parser.jay"
  {
		var l = new List<MmlValueExpr> ();
		l.Add ((MmlValueExpr) yyVals[0+yyTop]);
		yyVal = l;
	}
  break;
case 13:
#line 223 "mml_parser.jay"
  {
		var a = (MmlValueExpr) yyVals[-2+yyTop];
		var l = (List<MmlValueExpr>) yyVals[0+yyTop];
		l.Insert (0, a);
		yyVal = l;
	}
  break;
case 14:
#line 232 "mml_parser.jay"
  {
		yyVal = skipped_argument;
	}
  break;
case 19:
#line 246 "mml_parser.jay"
  {
		yyVal = new MmlConditionalExpr ((MmlValueExpr) yyVals[-4+yyTop], (MmlValueExpr) yyVals[-2+yyTop], (MmlValueExpr) yyVals[0+yyTop]);
	}
  break;
case 21:
#line 253 "mml_parser.jay"
  {
		yyVal = new MmlComparisonExpr ((MmlValueExpr) yyVals[-2+yyTop], (MmlValueExpr) yyVals[0+yyTop], (ComparisonType) yyVals[-1+yyTop]);
	}
  break;
case 22:
#line 259 "mml_parser.jay"
  {
		yyVal = ComparisonType.Lesser;
	}
  break;
case 23:
#line 263 "mml_parser.jay"
  {
		yyVal = ComparisonType.LesserEqual;
	}
  break;
case 24:
#line 267 "mml_parser.jay"
  {
		yyVal = ComparisonType.Greater;
	}
  break;
case 25:
#line 271 "mml_parser.jay"
  {
		yyVal = ComparisonType.GreaterEqual;
	}
  break;
case 27:
#line 278 "mml_parser.jay"
  {
		yyVal = new MmlAddExpr ((MmlValueExpr) yyVals[-2+yyTop], (MmlValueExpr) yyVals[0+yyTop]);
	}
  break;
case 28:
#line 282 "mml_parser.jay"
  {
		yyVal = new MmlSubtractExpr ((MmlValueExpr) yyVals[-2+yyTop], (MmlValueExpr) yyVals[0+yyTop]);
	}
  break;
case 30:
#line 289 "mml_parser.jay"
  {
		yyVal = new MmlMultiplyExpr ((MmlValueExpr) yyVals[-2+yyTop], (MmlValueExpr) yyVals[0+yyTop]);
	}
  break;
case 31:
#line 293 "mml_parser.jay"
  {
		yyVal = new MmlDivideExpr ((MmlValueExpr) yyVals[-2+yyTop], (MmlValueExpr) yyVals[0+yyTop]);
	}
  break;
case 32:
#line 297 "mml_parser.jay"
  {
		yyVal = new MmlModuloExpr ((MmlValueExpr) yyVals[-2+yyTop], (MmlValueExpr) yyVals[0+yyTop]);
	}
  break;
case 35:
#line 305 "mml_parser.jay"
  {
		yyVal = new MmlParenthesizedExpr ((MmlValueExpr) yyVals[-1+yyTop]);
	}
  break;
case 39:
#line 314 "mml_parser.jay"
  {
		yyVal = new MmlMultiplyExpr (new MmlConstantExpr (MmlDataType.Number, -1), (MmlValueExpr) yyVals[0+yyTop]);
	}
  break;
case 40:
#line 320 "mml_parser.jay"
  {
		var i = (MmlToken) yyVals[0+yyTop];
		yyVal = new MmlVariableReferenceExpr ((string) i.Value);
	}
  break;
case 41:
#line 327 "mml_parser.jay"
  {
		var t = (MmlToken) yyVals[0+yyTop];
		yyVal = new MmlConstantExpr (MmlDataType.String, (string) t.Value);
	}
  break;
case 42:
#line 334 "mml_parser.jay"
  {
		var n = (MmlToken) yyVals[0+yyTop];
		var l = new MmlLength ((int) (double) MmlValueExpr.GetTypedValue (n.Value, MmlDataType.Number)) { IsValueByStep = true };
		yyVal = new MmlConstantExpr (MmlDataType.Length, l);
	}
  break;
case 43:
#line 340 "mml_parser.jay"
  {
		var n = (MmlToken) yyVals[0+yyTop];
		var l = new MmlLength (-1 * (int) (double) MmlValueExpr.GetTypedValue (n.Value, MmlDataType.Number)) { IsValueByStep = true };
		yyVal = new MmlConstantExpr (MmlDataType.Length, l);
	}
  break;
case 44:
#line 348 "mml_parser.jay"
  {
		var t = (MmlToken) yyVals[0+yyTop];
		yyVal = new MmlConstantExpr (MmlDataType.Number, t.Value);
	}
  break;
case 45:
#line 353 "mml_parser.jay"
  {
		var t = (MmlToken) yyVals[-1+yyTop];
		var d = (int) yyVals[0+yyTop];
		yyVal = new MmlConstantExpr (MmlDataType.Length, new MmlLength ((int) t.Value) { Dots = d });
	}
  break;
case 46:
#line 361 "mml_parser.jay"
  {
		yyVal = 1;
	}
  break;
case 47:
#line 365 "mml_parser.jay"
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
      continue_yyDiscarded: continue;	// implements the named-loop continue: 'continue yyDiscarded'
      }
    continue_yyLoop: continue;		// implements the named-loop continue: 'continue yyLoop'
    }
  }

   static  short [] yyLhs  = {              -1,
    0,    0,    0,    1,    1,    6,    3,    5,    5,    7,
    7,    8,    8,   10,   10,    9,    2,   11,   11,   12,
   12,   14,   14,   14,   14,   13,   13,   13,   15,   15,
   15,   15,   16,   16,   16,   16,   16,   20,   20,   17,
   18,   19,   19,   21,   21,   22,   22,    4,    4,    4,
  };
   static  short [] yyLen = {           2,
    0,    1,    1,    1,    2,    0,    3,    1,    3,    0,
    1,    1,    3,    0,    1,    1,    1,    1,    5,    1,
    3,    1,    1,    1,    1,    1,    3,    3,    1,    3,
    3,    3,    1,    1,    3,    1,    1,    1,    2,    2,
    1,    2,    3,    1,    2,    1,    2,    1,    1,    1,
  };
   static  short [] yyDefRed = {            0,
   48,   41,    0,    0,    0,   50,    0,    0,   49,    0,
    0,    3,    4,    6,   17,    0,    0,    0,   29,   33,
   34,   36,   37,   38,   46,    0,    0,   39,   42,    0,
   40,    5,    0,    0,    0,    0,   22,   23,   24,   25,
    0,    0,    0,    0,   47,   35,   43,    0,   16,    7,
    8,   11,    0,    0,    0,    0,    0,   21,   30,   31,
   32,    0,    0,    0,    0,    9,   13,   19,
  };
  protected static  short [] yyDgoto  = {            10,
   11,   49,   13,   14,   50,   33,   51,   52,   53,   54,
   15,   16,   17,   41,   18,   19,   20,   21,   22,   23,
   24,   26,
  };
  protected static  short [] yySindex = {         -254,
    0,    0, -266, -234, -247,    0, -248, -249,    0,    0,
 -249,    0,    0,    0,    0, -244, -220, -242,    0,    0,
    0,    0,    0,    0,    0, -243, -232,    0,    0, -221,
    0,    0, -218, -234, -234, -234,    0,    0,    0,    0,
 -234, -234, -234, -234,    0,    0,    0, -234,    0,    0,
    0,    0,    0, -212, -209, -242, -242,    0,    0,    0,
    0, -232, -225, -234, -234,    0,    0,    0,
  };
  protected static  short [] yyRindex = {           42,
    0,    0,    1,    0,    0,    0,    0,    0,    0,    0,
   61,    0,    0,    0,    0,  108,  106,   43,    0,    0,
    0,    0,    0,    0,    0,   22,    0,    0,    0,    0,
    0,    0,   44,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0, -250,    0,    0,
    0,    0,  119,    0,    0,   64,   85,    0,    0,    0,
    0, -198,    0, -197,    0,    0,    0,    0,
  };
  protected static  short [] yyGindex = {            0,
    0,    2,   54,   58,    0,    0,   19,    4,    0,    0,
  -34,   28,    0,    0,   -1,   16,    0,    0,    0,    0,
   65,    0,
  };
  protected static  short [] yyTable = {            55,
   44,   12,    1,    2,    3,   27,   25,    1,    4,   14,
   29,    3,    5,   10,    6,    7,    8,    9,   30,    6,
   34,   45,    9,    2,    3,   42,   43,   44,    4,   45,
   68,   46,    5,   56,   57,    7,    8,   47,   66,    2,
    3,    1,   26,   10,   48,   35,   36,   64,    5,   62,
   65,    7,    8,   37,   38,   39,   40,   59,   60,   61,
    2,   16,   14,   27,   32,   31,   63,   67,   58,   28,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,   28,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,   20,    0,   18,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,   12,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,   44,    0,    0,
   44,    0,    0,    0,   44,   44,   44,   44,   44,   44,
   44,    0,   44,    0,   44,   44,   44,   44,   45,    0,
    0,   45,    0,    0,    0,   45,   45,   45,   45,   45,
   45,   45,    0,   45,    0,   45,   45,   45,   45,   26,
   10,    0,   26,   14,    0,    0,   26,   26,   26,   26,
    0,    0,   10,    0,   26,   10,   26,   26,   26,   26,
   27,    0,    0,   27,    0,    0,    0,   27,   27,   27,
   27,    0,    0,    0,    0,   27,    0,   27,   27,   27,
   27,   28,    0,    0,   28,    0,    0,    0,   28,   28,
   28,   28,    0,    0,    0,    0,   28,    0,   28,   28,
   28,   28,   20,    0,   18,   20,    0,   18,    0,   20,
   20,   18,    0,    0,   20,   12,   18,   20,   15,   18,
    0,    0,   12,    0,    0,    0,    0,   12,    0,    0,
   12,
  };
  protected static  short [] yyCheck = {            34,
    0,    0,  257,  258,  259,    4,  273,  257,  263,  260,
  259,  259,  267,  264,  269,  270,  271,  272,  267,  269,
  265,    0,  272,  258,  259,  268,  269,  270,  263,  273,
   65,  264,  267,   35,   36,  270,  271,  259,  264,  258,
  259,    0,    0,    0,  263,  266,  267,  260,  267,   48,
  260,  270,  271,  274,  275,  276,  277,   42,   43,   44,
    0,  260,  260,    0,   11,    8,   48,   64,   41,    5,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,    0,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,    0,   -1,    0,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,    0,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
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
  270,   -1,  272,   -1,  274,  275,  276,  277,  257,   -1,
   -1,  260,   -1,   -1,   -1,  264,  265,  266,  267,  268,
  269,  270,   -1,  272,   -1,  274,  275,  276,  277,  257,
  257,   -1,  260,  260,   -1,   -1,  264,  265,  266,  267,
   -1,   -1,  269,   -1,  272,  272,  274,  275,  276,  277,
  257,   -1,   -1,  260,   -1,   -1,   -1,  264,  265,  266,
  267,   -1,   -1,   -1,   -1,  272,   -1,  274,  275,  276,
  277,  257,   -1,   -1,  260,   -1,   -1,   -1,  264,  265,
  266,  267,   -1,   -1,   -1,   -1,  272,   -1,  274,  275,
  276,  277,  257,   -1,  257,  260,   -1,  260,   -1,  264,
  265,  264,   -1,   -1,  269,  257,  269,  272,  260,  272,
   -1,   -1,  264,   -1,   -1,   -1,   -1,  269,   -1,   -1,
  272,
  };

#line 376 "mml_parser.jay"

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
  public const int Plus = 266;
  public const int Minus = 267;
  public const int Asterisk = 268;
  public const int Slash = 269;
  public const int Percent = 270;
  public const int Dollar = 271;
  public const int Colon = 272;
  public const int Dot = 273;
  public const int BackSlashLesser = 274;
  public const int BackSlashLesserEqual = 275;
  public const int BackSlashGreater = 276;
  public const int BackSlashGreaterEqual = 277;
  public const int KeywordNumber = 278;
  public const int KeywordLength = 279;
  public const int KeywordString = 280;
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
