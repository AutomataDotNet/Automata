grammar query;

options {
    language=CSharp2;
    backtrack=false;
    memoize=false;
}

@namespace {Microsoft.Bek.Query} 
@lexer::namespace {Microsoft.Bek.Query} 
 
// fail (very) early on errors

@lexer::members { 
public override void ReportError(RecognitionException e)  
{
    throw e; // just rethrow everything... 
}
} 

@parser::members {
protected override object RecoverFromMismatchedToken(IIntStream input, int ttype, BitSet follow)
{
    // just rethrow  
    throw new MismatchedTokenException(ttype, input);
}

public override object RecoverFromMismatchedSet(IIntStream input, RecognitionException e, BitSet follow)
{
    // rethrow...
    throw e;
}

internal System.Tuple<List<Expression>,List<string>> Queries() {
  var res = queries();
  return new System.Tuple<List<Expression>,List<string>>(res.result,res.querystrings);
}

internal Expression Query() {
  var res = query();
  return res.result;
}

}

@rulecatch {
catch (RecognitionException e) {
  throw e;
}}

@header{ 
using System.Collections.Generic;
}

queries returns [List<Expression> result, List<string> querystrings]
    @init { $result = new List<Expression>(); $querystrings = new List<string>(); } 
    : (query ';' { $result.Add($query.result); $querystrings.Add($query.text); })* EOF
	;

query returns [Expression result]
    : 'eq1' '(' st1=exprT ',' st2=exprT ')' {$result = new PartialEquivalenceExpression($st1.result, $st2.result);}
    | 'eqD' '(' st1=exprT ',' st2=exprT ')' {$result = new DomainEquivalenceExpression($st1.result, $st2.result);}
	| 'eqB' '(' k=INT ',' st1=exprT ',' st2=exprT ')' {$result = new BoundedEquivalenceExpression($k.Text, $st1.result, $st2.result);}
	| 'eq'  '(' st1=exprT ',' st2=exprT ')' {$result = new FullEquivalenceExpression($st1.result, $st2.result);} 
	| ('image' | 'eval') '(' st1=exprT ',' arg=exprS ')' {$result = new ImageExpression($st1.result, $arg.result);}
    | 'display' '(' st=expr {$result = new DisplayExpression($st.result);} (',' elemcnt=INT 
	                        {$result = new DisplayExpression($st.result, new Identifier($elemcnt.Text,$elemcnt.Line,$elemcnt.CharPositionInLine));})? ')'
    | 'dot' '(' st=expr {$result = new DotExpression($st.result);} (',' elemcnt=INT 
	                        {$result = new DotExpression($st.result, new Identifier($elemcnt.Text,$elemcnt.Line,$elemcnt.CharPositionInLine));})? ')'
	| 'cs' '(' st1=exprT ')' {$result = new CsExpression($st1.result);}
	| 'js' '(' st1=exprT ')' {$result = new JsExpression($st1.result);}
	| ('member' | 'in') '(' elem=exprS ',' aut=exprA ')' {$result = new MembershipExpression($elem.result, $aut.result);} 
	| 'subset'  '(' sa1=exprA ',' sa2=exprA ')' {$result = new SubsetExpression($sa1.result, $sa2.result);} 
    | 'isempty'  '(' sa1=exprA ')' {$result = new IsEmptyExpression($sa1.result);} 
	| symb=ID '=' defn=expr {$result = new LetExpression($symb.Text,$defn.result);} 
	;

exprS returns [Expression result]
    : str=exprString {$result = $str.result;}
	| ('image' | 'eval') '(' st=exprT ',' arg=exprS ')'  {$result = new ImageExpression($st.result, $arg.result);}
	;
	 
exprString returns [Expression result]
    :  str=STRING {$result = StringExpression.Mk(new Identifier($str.Text, $str.Line, $str.CharPositionInLine));}
	| lstr=LSTRING {$result = StringExpression.Mk(new Identifier($lstr.Text, $lstr.Line, $lstr.CharPositionInLine));}
	;

exprT returns [Expression result]
    : 'join' '(' st1=exprT ',' st2=exprT ')' {$result = new JoinExpression($st1.result, $st2.result);}
	| ('explore' | 'ex') '(' st1=exprT ')' {$result = new ExploreExpression($st1.result);}
	| ('exploreB' | 'exB') '(' st1=exprT ')' {$result = new ExploreBoolsExpression($st1.result);}
	| 'restrict' '(' st1=exprT ',' st2=exprA ')' {$result = new RestrictExpression($st1.result, $st2.result);}
	| symb=ID {$result = new VariableExpression(new Identifier($symb.Text, $symb.Line, $symb.CharPositionInLine));}
	;

exprA returns [Expression result]
    : str=exprS {$result = $str.result;}
	| ('re' | 'regex') '(' pat=exprString ')' {$result = new RegexExpression($pat.result);}
    | ('intersect' | 'prod') '(' sa1=exprA ',' sa2=exprA ')' {$result = new IntersectExpression($sa1.result, $sa2.result);}
	| ('invimage' | 'inv') '(' st=exprT ',' sa=exprA ')' {$result = new InvimageExpression($st.result, $sa.result);}
	| 'minus' '(' sa1=exprA ',' sa2=exprA ')' {$result = new MinusExpression($sa1.result, $sa2.result);}
	| ('complement' | '~') '(' sa=exprA ')' {$result = new ComplementExpression($sa.result);}
    | 'minimize' '(' sa=exprA ')' {$result = new MinimizeExpression($sa.result);}
	| 'determinize' '(' sa=exprA ')' {$result = new DeterminizeExpression($sa.result);}
	| 'eliminateEpsilons' '(' sa=exprA ')' {$result = new EliminateEpsilonsExpression($sa.result);} 
	| ('domain' | 'dom') '(' st=exprT ')' {$result = new DomainExpression($st.result);}
    | symb=ID {$result = new VariableExpression(new Identifier($symb.Text,$symb.Line,$symb.CharPositionInLine));} 
	;

expr returns [Expression result]
    : st=exprT {$result = $st.result;} 
	| sa=exprA {$result = $sa.result;}
	;

// Lexer 

ID  :	('a'..'z'|'A'..'Z'|'_') ('a'..'z'|'A'..'Z'|'0'..'9'|'_')*
    ;

INT :	'0' (('x' (HEX_DIGIT)+) | ('0'..'9')*)
	|	('1'..'9') ('0'..'9')*
    ;

COMMENT
    :   '//' ~('\n'|'\r')* '\r'? '\n' {$channel=Hidden;}
    |   '/*' ( options {greedy=false;} : . )* '*/' {$channel=Hidden;}
    ;

WS  :   ( ' '
        | '\t'
        | '\r'
        | '\n'
        ) {$channel=Hidden;}
    ;

STRING
    :  '"' ( ESC_SEQ | ~('\\'|'"') )* '"'
    ;

LSTRING 
    :  '@' '"' (~('"'))* '"'
	;
    
CHAR
	:  '\'' ( ESC_SEQ | ~ ('\''|'\\') ) '\''
    ;

fragment
ESC_SEQ
    :   '\\' ('b'|'t'|'n'|'f'|'r'|'\"'|'\''|'\\')
    |   UNICODE_SEQ 
	|   HEX_SEQ
    ;
    
fragment  
HEX_DIGIT : ('0'..'9'|'a'..'f'|'A'..'F') ;  
   

fragment
UNICODE_SEQ
    :   '\\' 'u' HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT
	;

fragment
HEX_SEQ
	: '\\' 'x' HEX_DIGIT HEX_DIGIT
	;