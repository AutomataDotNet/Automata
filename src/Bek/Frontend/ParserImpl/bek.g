grammar bek;

options {
    language=CSharp2;
    backtrack=true;
    memoize=true; 
}

@namespace {Microsoft.Bek.Frontend.ParserImpl}
@lexer::namespace {Microsoft.Bek.Frontend.ParserImpl} 

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

internal List<BekPgm> BekPgms() { return bekPgms(); }

internal expr Comp_expr() { return comp_expr(); }

}


@rulecatch {
catch (RecognitionException e) {
  throw e;
}}

@header{ 
using Microsoft.Bek.Frontend.AST; 
using System.Collections.Generic;
}

// for use when repeatedly parsing Bek defs
bekPgms returns [List<BekPgm> result]
    @init { $result = new List<BekPgm>(); }
    : funcs=bekLocalFunctions 
	  (bekPgm { $bekPgm.result.AddLocalFunctions($funcs.result); $result.Add($bekPgm.result); } )+
      EOF
    ;

// the given bek program
bekPgm returns [BekPgm result]
	: 'program' name=ID '(' str=ID ')' '{' body=statement '}' 
    {  $result = new BekPgm(new ident($name.Text, $name.Line, $name.CharPositionInLine), new ident($str.Text, $str.Line, $str.CharPositionInLine), $body.result);
    }
	;

bekLocalFunctions returns [List<BekLocalFunction> result]
    @init { $result = new List<BekLocalFunction>(); }
	: ('function' fname=ID '(' args=ident_list ')' '=' body=comp_expr ';' 
	    { if ($result.Find(x => x.id.name.Equals($fname.Text)) != null) 
		  throw new Microsoft.Bek.Frontend.Meta.BekParseException($fname.Line, $fname.CharPositionInLine, string.Format("'{0}' duplicate definition", $fname.Text));
		  $result.Add(new BekLocalFunction(new ident($fname.Text, $fname.Line, $fname.CharPositionInLine), $args.result.ToArray(), $body.result)); })*
	;

ident_list returns [List<ident> result]
    @init { $result = new List<ident>(); }
	: (arg=ID { $result.Add(new ident($arg.Text, $arg.Line, $arg.CharPositionInLine)); } 
      (',' arg1=ID 
      { if ($result.Find(x => x.name.Equals($arg1.Text)) != null) 
		throw new Microsoft.Bek.Frontend.Meta.BekParseException($arg1.Line, $arg1.CharPositionInLine, string.Format("'{0}' duplicate parameter", $arg1.Text));
        $result.Add(new ident($arg1.Text, $arg1.Line, $arg1.CharPositionInLine)); })*)?
	;

//stmt_list returns [List<stmt> result] @init { $result = new List<stmt>(); }
//    : statement ';' { $result.Add($statement.result); }
//	;

statement returns [stmt result]
	: 'return' exp=iter_expr ';'
      {$result = new returnstmt($exp.result) ;}
    | repl='replace' '{' cases=replace_cases '}' (';')?
      {$result = new returnstmt(new replace(new ident($repl.Text, $repl.Line, $repl.CharPositionInLine), $cases.result));}
	; 

replace_cases returns [ List<replacecase> result ] @init { $result = new List<replacecase>(); } 
   : (pattern=str_const arrow='==>' replace_case=replace_rhs ';' 
     {$result.Add(new replacecase(new ident($arrow.Text, $arrow.Line, $arrow.CharPositionInLine), $pattern.result, $replace_case.result));})+
     (elsename='else' '==>' else_case=replace_rhs ';' 
     {$result.Add(new replacecase(new ident($elsename.Text, $elsename.Line, $elsename.CharPositionInLine), null, $else_case.result));})?
   ;

replace_rhs returns [expr result]
   : str_const {$result = $str_const.result;}
   | str='[' param=exp_param_list ']' { $result = new functioncall(new ident("string", $str.Line, $str.CharPositionInLine), $param.result); } 
   ;
    
iter_expr returns [iterexpr result]
	: cases=iter_expr_main 'end' '{' endcases=case_list '}' 
	{	$result = $cases.result;
	    foreach (var ecase in $endcases.result) ecase.endcase=true;
		$result.body.AddRange($endcases.result);
	} 
	| iter_expr_main {$result = $iter_expr_main.result;}
	;

iter_expr_main returns [iterexpr result]
	: 'iter' '(' binder=ID 'in' str=ID ')' 
	         init=iter_init
	         '{'   
                body=case_list
             '}' 
    {  $result = new iterexpr(new ident($binder.Text, $binder.Line, $binder.CharPositionInLine),
                              new ident($str.Text, $str.Line, $str.CharPositionInLine),	
				              init,
				              body);
    } 
	; 

iter_init returns [iterinit result]
    @init{ $result = new iterinit(); }
	: 
	| '[' (assgn=iter_const_assgn ';' { $result.assgns.Add($assgn.result); })* ']'
	;

iter_const_assgn returns [iterassgn result] 
	: lhs=ID ':=' rhschar=char_const  {$result = new iterassgn(new ident($lhs.Text, $lhs.Line, $lhs.CharPositionInLine), $rhschar.result);}	
	| lhs=ID ':=' rhsbool=bool_const  {$result = new iterassgn(new ident($lhs.Text, $lhs.Line, $lhs.CharPositionInLine), $rhsbool.result);}
	;

case_list returns [List<itercase> result] @init { $result = new List<itercase>(); }
    : (case_stmt { $result.Add($case_stmt.result); })+
	;
	
case_stmt returns [itercase result]
	: 'case' '('  comp_expr ')' ':'
	     body=iterstmt_list_or_ite
	{ $result = new itercase($comp_expr.result,
	                         $body.result);
    }
	| 'end' 'case' '(' comp_expr ')' ':'
	     body=iterstmt_list_or_ite
    { $result = new itercase($comp_expr.result, 
	                        $body.result);
      $result.endcase = true;
    }
	;

iterstmt_list_or_ite returns [List<iterstmt> result]
    : ifthenelse_pairs {$result = new List<iterstmt>(); $result.Add(ifthenelse.Mk($ifthenelse_pairs.result));}
	| iterstmt_list {$result = $iterstmt_list.result;}
	;

ifthenelse_pairs returns [List<KeyValuePair<expr,List<iterstmt>>> result] @init { $result = new List<KeyValuePair<expr,List<iterstmt>>>(); }
    : 	'if' '(' cond=comp_expr ')' '{' tcase=iterstmt_list_or_ite '}' 
	     {$result.Add(new KeyValuePair<expr,List<iterstmt>>($cond.result, $tcase.result)); }
	  ('else' 'if' '(' elseif=comp_expr ')' '{' elseifcase=iterstmt_list_or_ite '}'
	    {$result.Add(new KeyValuePair<expr,List<iterstmt>>($elseif.result, $elseifcase.result)); })* 
	  'else' '{' fcase=iterstmt_list_or_ite '}' 
	    {$result.Add(new KeyValuePair<expr,List<iterstmt>>(null, $fcase.result)); }
	;

iterstmt_list returns [List<iterstmt> result] @init { $result = new List<iterstmt>(); }
    : (iter_stmt ';' { $result.Add($iter_stmt.result); })+
	;

iter_stmt returns [iterstmt result]
	: 'skip' 
	| name=ID ':=' rhs=comp_expr { $result = new iterassgn(new ident($name.Text, $name.Line, $name.CharPositionInLine), $rhs.result); }
	| yield_stmt {$result = $yield_stmt.result;}
	| raise_stmt {$result = $raise_stmt.result;}
    ;

yield_stmt returns [iterstmt result]
	:	'yield' '(' args=yield_expr_list ')' {$result = new yieldstmt($args.result); }
	|   'yield' '(' ')' { $result = new yieldstmt( new List<expr>() ); }
	;

raise_stmt returns [iterstmt result]
	:	'raise' name=ID {$result = new raisestmt($name.Text); }
	;

yield_expr_list returns [ List<expr> result] @init { $result = new List<expr>(); }
    : arg1=comp_expr { $result.Add($arg1.result);} 
	(',' arg=comp_expr { $result.Add($arg.result);} )* 
	;

//**COMPOUND EXPRESSIONS**

comp_expr returns [expr result]	
	: lhs=ID inId='in' rhs=str_const  {$result = new functioncall(new ident($inId.Text, $inId.Line, $inId.CharPositionInLine), 
                                                                  new ident($lhs.Text, $lhs.Line, $lhs.CharPositionInLine), $rhs.result); }
	| exp1=or_term op=INFIX_ARITHM_OP exp2=or_term 
      {$result = new functioncall(new ident($op.Text, $op.Line, $op.CharPositionInLine), $exp1.result, $exp2.result); }
	| exp=or_term { $result = $exp.result; }
	;	

or_term returns [expr result] 
    : exp1=and_term { $result = $exp1.result; }
	  (op='||' nexp=and_term { $result = new functioncall(new ident($op.Text, $op.Line, $op.CharPositionInLine), $result, $nexp.result); })*
	;

and_term returns [expr result] 
    : exp1=sum_term { $result = $exp1.result; }
	  (op='&&' nexp=sum_term { $result = new functioncall(new ident($op.Text, $op.Line, $op.CharPositionInLine), $result, $nexp.result); })*
	;

sum_term returns [expr result] 
    : exp1=mul_term { $result = $exp1.result; }
	  (op='+' nexp=mul_term { $result = new functioncall(new ident($op.Text, $op.Line, $op.CharPositionInLine), $result, $nexp.result); })*
	;

mul_term returns [expr result] 
    : exp1=expr_factor { $result = $exp1.result; }
	  (op='*' nexp=expr_factor { $result = new functioncall(new ident($op.Text, $op.Line, $op.CharPositionInLine), $result, $nexp.result); })*
	;

expr_factor returns [expr result]
   : op=UNARY_OP exp1=expr_atom { $result = new functioncall(new ident($op.Text, $op.Line, $op.CharPositionInLine), $exp1.result); }
   | expr_atom { $result = $expr_atom.result; }
   ;

expr_atom returns [expr result]
   : bool_const { $result = $bool_const.result; }
   | char_const { $result = $char_const.result; }
   | str_const  { $result = $str_const.result;  }
   | ID         { $result = new ident($ID.Text, $ID.Line, $ID.CharPositionInLine); }
   | functionapp   { $result = $functionapp.result; }
   | '(' exp=comp_expr ')' { $result = $exp.result; }
   ;

bool_const returns [boolconst result]
	: t='true' {$result = new boolconst($t.Line, $t.CharPositionInLine, true);}
	| f='false'{$result = new boolconst($f.Line, $f.CharPositionInLine, false);}     
	;

char_const returns [charconst result]
   : CHAR { $result = new charconst($CHAR.Line, $CHAR.CharPositionInLine, $CHAR.Text); }
   | INT { $result = new charconst($INT.Line, $INT.CharPositionInLine, $INT.Text); }
   ;	
  
str_const returns [strconst result]
	: STRING {$result =  new strconst($STRING.Line, $STRING.CharPositionInLine, $STRING.Text);}
    | LSTRING {$result =  new strconst($LSTRING.Line, $LSTRING.CharPositionInLine, $LSTRING.Text);}
	;

functionapp returns [functioncall result] 
   : fname=ID '(' param=exp_param_list ')' { $result = new functioncall(new ident($fname.Text, $fname.Line, $fname.CharPositionInLine), $param.result); } 
   | str='[' param=exp_param_list ']' { $result = new functioncall(new ident("string", $str.Line, $str.CharPositionInLine), $param.result); } 
   ;

exp_param_list returns [ List<expr> result ] @init { $result = new List<expr>(); } 
   : 
   | exp=comp_expr { $result.Add($exp.result); }
     (',' nexp=comp_expr { $result.Add($nexp.result); } )*
   ;
	
//////////////////////////////////////////////////////////

INFIX_ARITHM_OP 
    : '/' 
    | '-' 
    | '%' 
    | '&'
    | '|'
    | '<'
    | '>'
    | '<='
    | '>='
    | '<<'
    | '>>'
    | '^'
    | '=='
    | '!='
    ;

UNARY_OP
    : '!'
    | '~' 
    ;

ID  :	('a'..'z'|'A'..'Z'|'_') ('a'..'z'|'A'..'Z'|'0'..'9'|'_')*
    |  '#' ('0'..'9')+
    ;

INT :	'0' (('x' (HEX_DIGIT)+) | ('0'..'9')*)
	|	('1'..'9') ('0'..'9')*
    ;
        

toEof : .* EOF;


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
	: '\'' ( ESC_SEQ | ~ ('\''|'\\') ) '\''
    ;
	 
fragment
ESC_SEQ
    :   '\\' ('b'|'t'|'n'|'f'|'r'|'\"'|'\''|'\\')
    |   UNICODE_ESC
	|   HEX_SEQ
    ;
    
fragment  
HEX_DIGIT : ('0'..'9'|'a'..'f'|'A'..'F') ;  
   

fragment
OCTAL_ESC
    :   '\\' ('0'..'3') ('0'..'7') ('0'..'7')
    |   '\\' ('0'..'7') ('0'..'7')
    |   '\\' ('0'..'7')
    ;

fragment
UNICODE_ESC
    :   '\\' 'u' HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT
	;

fragment
BIG_UNICODE_ESC
    :   '\\' 'U' HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT
    ;

fragment
HEX_SEQ
	: '\\' 'x' HEX_DIGIT HEX_DIGIT
	;
