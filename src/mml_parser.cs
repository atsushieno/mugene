// created by jay 0.7 (c) 1998 Axel.Schreiner@informatik.uni-osnabrueck.de

#line 2 "mml_parser.jay"
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
		
		public bool advance ()
		{
			if (++idx > tokens.Count)
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
			if (idx >= tokens.Count)
				return Token.EOF;
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
		MmlParserInput input;
		int yacc_verbose_flag;

		public static List<MmlOperationUse> Parse (IList<MmlToken> tokens)
		{
			return new MmlParser (tokens).Parse ();
		}
		
		MmlParser (IList<MmlToken> tokens)
		{
			try {
				yacc_verbose_flag = int.Parse (Environment.GetEnvironmentVariable ("MUGENE_MML_DEBUG"));
			} catch (FormatException) {
			}
			input = new MmlParserInput (tokens);
		}

		public List<MmlOperationUse> Parse ()
		{
			debug = new yydebug.yyDebugSimple ();

			return (List<MmlOperationUse>) yyparse (input);
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

  protected static  int yyFinal = 3;
 // Put this array into a separate class so it is only initialized if debugging is actually used
 // Use MarshalByRefObject to disable inlining
 class YYRules : MarshalByRefObject {
  public static  string [] yyRule = {
    "$accept : OptOperationUses",
    "OptOperationUses : EOF",
    "OptOperationUses : OperationUses EOF",
    "OperationUses : OperationUse",
    "OperationUses : OperationUses OperationUse",
    "OperationUse : Identifier ArgumentsOptCurly",
    "ArgumentsOptCurly : OptArguments",
    "ArgumentsOptCurly : OpenCurly OptArguments CloseCurly",
    "OptArguments :",
    "OptArguments : Arguments",
    "Arguments : Argument",
    "Arguments : Arguments Comma Argument",
    "Argument : Expression",
    "Expression : ConditionalExpr",
    "ConditionalExpr : AddSubExpr",
    "ConditionalExpr : AddSubExpr Question ConditionalExpr Comma ConditionalExpr",
    "AddSubExpr : MulDivModExpr",
    "AddSubExpr : AddSubExpr Plus MulDivModExpr",
    "AddSubExpr : AddSubExpr Minus MulDivModExpr",
    "MulDivModExpr : Value",
    "MulDivModExpr : MulDivModExpr Asterisk Value",
    "MulDivModExpr : MulDivModExpr Slash Value",
    "MulDivModExpr : MulDivModExpr Percent Value",
    "Value : VariableReference",
    "Value : StringConstant",
    "Value : StepConstant",
    "Value : NumberOrLengthConstant",
    "Value : OpenCurly Expression CloseCurly",
    "VariableReference : Dollar Identifier",
    "StringConstant : StringLiteral",
    "StepConstant : Percent NumberLiteral",
    "NumberOrLengthConstant : PositiveNumberOrLengthConstant",
    "NumberOrLengthConstant : Minus PositiveNumberOrLengthConstant",
    "PositiveNumberOrLengthConstant : NumberLiteral",
    "PositiveNumberOrLengthConstant : NumberLiteral Dots",
    "Dots : Dot",
    "Dots : Dots Dot",
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
    null,null,null,null,null,null,null,"EOF","Identifier","StringLiteral",
    "NumberLiteral","Comma","OpenParen","CloseParen","OpenCurly",
    "CloseCurly","Question","Plus","Minus","Asterisk","Slash","Percent",
    "Dollar","Colon","Dot","KeywordNumber","KeywordLength",
    "KeywordString",
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
#line 134 "mml_parser.jay"
  {
		yyVal = new List<MmlOperationUse> ();
	}
  break;
case 2:
#line 138 "mml_parser.jay"
  {
		yyVal = yyVals[-1+yyTop];
	}
  break;
case 3:
#line 144 "mml_parser.jay"
  {
		var l = new List<MmlOperationUse> ();
		l.Add ((MmlOperationUse) yyVals[0+yyTop]);
		yyVal = l;
	}
  break;
case 4:
#line 150 "mml_parser.jay"
  {
		var l = (List<MmlOperationUse>) yyVals[-1+yyTop];
		l.Add ((MmlOperationUse) yyVals[0+yyTop]);
		yyVal = l;
	}
  break;
case 5:
#line 158 "mml_parser.jay"
  {
		var i = (MmlToken) yyVals[-1+yyTop];
		Console.WriteLine ("NOW at : " + i.Location);
		var o = new MmlOperationUse ((string) i.Value, i.Location);
		foreach (MmlValueExpr a in (IEnumerable<MmlValueExpr>) yyVals[0+yyTop])
			o.Arguments.Add (a);
		yyVal = o;
	}
  break;
case 7:
#line 170 "mml_parser.jay"
  {
		yyVal = yyVals[-1+yyTop];
	}
  break;
case 8:
#line 176 "mml_parser.jay"
  {
		yyVal = new List<MmlValueExpr> ();
	}
  break;
case 10:
#line 183 "mml_parser.jay"
  {
		var l = new List<MmlValueExpr> ();
		l.Add ((MmlValueExpr) yyVals[0+yyTop]);
		yyVal = l;
	}
  break;
case 11:
#line 189 "mml_parser.jay"
  {
		var l = (List<MmlValueExpr>) yyVals[-2+yyTop];
		l.Add ((MmlValueExpr) yyVals[0+yyTop]);
		yyVal = l;
	}
  break;
case 15:
#line 204 "mml_parser.jay"
  {
		yyVal = new MmlConditionalExpr ((MmlValueExpr) yyVals[-4+yyTop], (MmlValueExpr) yyVals[-2+yyTop], (MmlValueExpr) yyVals[0+yyTop]);
	}
  break;
case 17:
#line 211 "mml_parser.jay"
  {
		yyVal = new MmlAddExpr ((MmlValueExpr) yyVals[-2+yyTop], (MmlValueExpr) yyVals[0+yyTop]);
	}
  break;
case 18:
#line 215 "mml_parser.jay"
  {
		yyVal = new MmlSubtractExpr ((MmlValueExpr) yyVals[-2+yyTop], (MmlValueExpr) yyVals[0+yyTop]);
	}
  break;
case 20:
#line 222 "mml_parser.jay"
  {
		yyVal = new MmlMultiplyExpr ((MmlValueExpr) yyVals[-2+yyTop], (MmlValueExpr) yyVals[0+yyTop]);
	}
  break;
case 21:
#line 226 "mml_parser.jay"
  {
		yyVal = new MmlDivideExpr ((MmlValueExpr) yyVals[-2+yyTop], (MmlValueExpr) yyVals[0+yyTop]);
	}
  break;
case 22:
#line 230 "mml_parser.jay"
  {
		yyVal = new MmlModuloExpr ((MmlValueExpr) yyVals[-2+yyTop], (MmlValueExpr) yyVals[0+yyTop]);
	}
  break;
case 27:
#line 240 "mml_parser.jay"
  {
		yyVal = new MmlParenthesizedExpr ((MmlValueExpr) yyVals[-1+yyTop]);
	}
  break;
case 28:
#line 246 "mml_parser.jay"
  {
		var i = (MmlToken) yyVals[0+yyTop];
		yyVal = new MmlVariableReferenceExpr ((string) i.Value);
	}
  break;
case 29:
#line 253 "mml_parser.jay"
  {
		var t = (MmlToken) yyVals[0+yyTop];
		yyVal = new MmlConstantExpr (MmlDataType.String, (string) t.Value);
	}
  break;
case 30:
#line 260 "mml_parser.jay"
  {
		var n = (MmlToken) yyVals[0+yyTop];
		var l = new MmlLength ((int) (double) MmlValueExpr.GetTypedValue (n.Value, MmlDataType.Number)) { IsValueByStep = true };
		yyVal = new MmlConstantExpr (MmlDataType.Length, l);
	}
  break;
case 32:
#line 269 "mml_parser.jay"
  {
		yyVal = new MmlMultiplyExpr (new MmlConstantExpr (MmlDataType.Number, -1), (MmlValueExpr) yyVals[0+yyTop]);
	}
  break;
case 33:
#line 275 "mml_parser.jay"
  {
		var t = (MmlToken) yyVals[0+yyTop];
		yyVal = new MmlConstantExpr (MmlDataType.Number, t.Value);
	}
  break;
case 34:
#line 280 "mml_parser.jay"
  {
		var t = (MmlToken) yyVals[-1+yyTop];
		var d = (int) yyVals[0+yyTop];
		yyVal = new MmlConstantExpr (MmlDataType.Length, new MmlLength ((int) t.Value) { Dots = d });
	}
  break;
case 35:
#line 288 "mml_parser.jay"
  {
		yyVal = 1;
	}
  break;
case 36:
#line 292 "mml_parser.jay"
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
    0,    0,    1,    1,    2,    3,    3,    4,    4,    5,
    5,    6,    7,    8,    8,    9,    9,    9,   10,   10,
   10,   10,   11,   11,   11,   11,   11,   12,   13,   14,
   15,   15,   16,   16,   17,   17,
  };
   static  short [] yyLen = {           2,
    1,    2,    1,    2,    2,    1,    3,    0,    1,    1,
    3,    1,    1,    1,    5,    1,    3,    3,    1,    3,
    3,    3,    1,    1,    1,    1,    3,    2,    1,    2,
    1,    2,    1,    2,    1,    2,
  };
   static  short [] yyDefRed = {            0,
    1,    0,    0,    0,    3,   29,    0,    0,    0,    0,
    0,    5,    6,    0,   10,   12,   13,    0,    0,   19,
   23,   24,   25,   26,   31,    2,    4,   35,    0,    0,
    0,    0,   32,   30,   28,    0,    0,    0,    0,    0,
    0,    0,   36,    0,    7,   27,   11,    0,    0,    0,
   20,   21,   22,    0,   15,
  };
  protected static  short [] yyDgoto  = {             3,
    4,    5,   12,   13,   14,   15,   16,   17,   18,   19,
   20,   21,   22,   23,   24,   25,   29,
  };
  protected static  short [] yySindex = {         -253,
    0, -225,    0, -205,    0,    0, -257, -223, -239, -222,
 -218,    0,    0, -219,    0,    0,    0, -259, -245,    0,
    0,    0,    0,    0,    0,    0,    0,    0, -230, -223,
 -210, -200,    0,    0,    0, -223, -223, -223, -223, -223,
 -223, -223,    0, -200,    0,    0,    0, -180, -245, -245,
    0,    0,    0, -223,    0,
  };
  protected static  short [] yyRindex = {            0,
    0, -181,    0,    0,    0,    0, -255, -177,    0,    0,
    0,    0,    0, -201,    0,    0,    0, -171, -207,    0,
    0,    0,    0,    0,    0,    0,    0,    0, -238,    0,
    0, -172,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0, -195, -183,
    0,    0,    0,    0,    0,
  };
  protected static  short [] yyGindex = {            0,
    0,   87,    0,   84,    0,   57,   -8,  -36,    0,   41,
   27,    0,    0,    0,    0,   86,    0,
  };
  protected static  short [] yyTable = {            32,
   48,   33,   33,    1,    2,   33,   37,   38,   39,   33,
   33,   33,   33,   33,   33,   33,   28,   55,   34,   34,
    7,   44,   34,   40,   41,   42,   34,   34,   34,   34,
   34,   34,   34,    6,    7,    6,    7,   34,    8,   35,
   30,   36,    9,   43,    9,   10,   11,   10,   11,   16,
   16,   26,    2,   16,   45,    9,    9,   16,   16,   16,
   16,   17,   17,    9,   46,   17,   51,   52,   53,   17,
   17,   17,   17,   18,   18,    8,    8,   18,   49,   50,
   54,   18,   18,   18,   18,   14,   14,    8,   12,   14,
   27,   31,   47,   14,   33,
  };
  protected static  short [] yyCheck = {             8,
   37,  257,  258,  257,  258,  261,  266,  267,  268,  265,
  266,  267,  268,  269,  270,  271,  274,   54,  257,  258,
  260,   30,  261,  269,  270,  271,  265,  266,  267,  268,
  269,  270,  271,  259,  260,  259,  260,  260,  264,  258,
  264,  261,  268,  274,  268,  271,  272,  271,  272,  257,
  258,  257,  258,  261,  265,  257,  258,  265,  266,  267,
  268,  257,  258,  265,  265,  261,   40,   41,   42,  265,
  266,  267,  268,  257,  258,  257,  258,  261,   38,   39,
  261,  265,  266,  267,  268,  257,  258,  265,  261,  261,
    4,    8,   36,  265,    9,
  };

#line 297 "mml_parser.jay"

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
  public const int EOF = 257;
  public const int Identifier = 258;
  public const int StringLiteral = 259;
  public const int NumberLiteral = 260;
  public const int Comma = 261;
  public const int OpenParen = 262;
  public const int CloseParen = 263;
  public const int OpenCurly = 264;
  public const int CloseCurly = 265;
  public const int Question = 266;
  public const int Plus = 267;
  public const int Minus = 268;
  public const int Asterisk = 269;
  public const int Slash = 270;
  public const int Percent = 271;
  public const int Dollar = 272;
  public const int Colon = 273;
  public const int Dot = 274;
  public const int KeywordNumber = 275;
  public const int KeywordLength = 276;
  public const int KeywordString = 277;
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
