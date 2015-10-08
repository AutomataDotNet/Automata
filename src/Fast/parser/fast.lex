%namespace Microsoft.Fast.AST
%visibility internal

%{
 
  public string sourcefile = null;
  public int MakeFastToken(Tokens kind)
  {
     int dummy = yytext.Length; //force evaluation of the string yytext
	 string text = yytext;
	 yylloc = new FastLexLocation(tokLin, tokCol, tokELin, tokECol, sourcefile);
	 if (kind == Tokens.BADCHAR)
	   throw new FastParseException(yylloc, string.Format("Syntax error, unexpected character '{0}'",text));

     yylval = new FastToken(text, yylloc, kind); 
     return (int)kind;  
  }

  override public void yyerror(string message, params object[] args)
  {
	  var token = (FastToken)yylval;
	  throw new FastParseException(token.Location, message);
  }

%}

LT              <
GT              >
LE              <=
GE              >=
EQ              ==
NE              !=
PLUS            +
MINUS           -
TIMES           *
DIV             [\/]
MOD             [\%]|mod
SHL             <<
SHR             >>

TRUE            true
FALSE           false
NOT             !|not
AND             &&|and
OR              \|\||or
BVXOR           ^
BVAND           &
BVNOT           ~ 
IMPLIES         =>

ITE             ?

HEX_DIGIT       [0-9a-fA-F]
CONTAINS        contains
IS_EMPTY_LANG   is_empty_lang
IS_EMPTY_TRANS  is_empty_trans 
TYPECHECK       typecheck
GEN_CSHARP      gen_csharp
EQ_LANG         eq_lang
EQ_TRANS        eq_trans
COMPOSE         compose
RESTRICT_INP    restrict_inp
RESTRICT_OUT    restrict_out
INTERSECT       intersect
DIFFERENCE      difference
UNION           union
DOMAIN          domain
PRE_IMAGE       pre_image
COMPLEMENT      complement
MINIMIZE        minimize

PUBLIC          Public|Active|Visible

ENUM            Enum
CONST           Const
FUN             Fun
ALPHABET        Alphabet
LANG            Lang
TRANS           Trans
DEF             Def
TREE            Tree
PRINT           Print
ASSERT_TRUE     AssertTrue                    
ASSERT_FALSE    AssertFalse

ASSIGN          :=
RIGHT_ARROW     ->

WHERE           where
GIVEN           given
TO              to

APPLY           apply
GET_WITNESS     get_witness


ID              ([a-zA-Z_][A-Za-z_0-9]*)([\.]([a-zA-Z_][A-Za-z_0-9]*))?
NUMBER          [0-9]+((\.|\/)[0-9]+)?|0x{HEX_DIGIT}+
COMMENT         \/\/.*|\/\*(.|\n)*\*\/
WS              [ \t\r\f\v\n]
HEX_SEQ         \\x{HEX_DIGIT}{HEX_DIGIT}
UNICODE_SEQ     \\u{HEX_DIGIT}{HEX_DIGIT}{HEX_DIGIT}{HEX_DIGIT}
ESC_SEQ         \\(b|t|n|f|r|\"|\'|\\)|{UNICODE_SEQ}|{HEX_SEQ}
STRING          \"({ESC_SEQ}|[^\\\"])*\"|@\"[^\"]*\"
CHAR            \'({ESC_SEQ}|[^\'\\])\'
LBRACKET        [\[]
RBRACKET        [\]]
LBRACE          [\{]
RBRACE          [\}]
LPAR            [\(]
RPAR            [\)]
COMMA           [,]
COLON           [:]
BAR             [\|]

BADCHAR         [^\t\r\f\v\n ]

%%

{CONTAINS}        {return MakeFastToken(Tokens.CONTAINS);}
{IS_EMPTY_LANG}   {return MakeFastToken(Tokens.IS_EMPTY_LANG);}
{IS_EMPTY_TRANS}  {return MakeFastToken(Tokens.IS_EMPTY_TRANS);}
{TYPECHECK}       {return MakeFastToken(Tokens.TYPECHECK);}
{GEN_CSHARP}      {return MakeFastToken(Tokens.GEN_CSHARP);}
{EQ_LANG}         {return MakeFastToken(Tokens.EQ_LANG);}
{EQ_TRANS}        {return MakeFastToken(Tokens.EQ_TRANS);}
{COMPOSE}         {return MakeFastToken(Tokens.COMPOSE);}
{RESTRICT_INP}    {return MakeFastToken(Tokens.RESTRICT_INP);}
{RESTRICT_OUT}    {return MakeFastToken(Tokens.RESTRICT_OUT);}
{INTERSECT}       {return MakeFastToken(Tokens.INTERSECT);}
{DIFFERENCE}      {return MakeFastToken(Tokens.DIFFERENCE);}
{UNION}           {return MakeFastToken(Tokens.UNION);}
{DOMAIN}          {return MakeFastToken(Tokens.DOMAIN);}
{PRE_IMAGE}       {return MakeFastToken(Tokens.PRE_IMAGE);}
{COMPLEMENT}      {return MakeFastToken(Tokens.COMPLEMENT);}
{MINIMIZE}        {return MakeFastToken(Tokens.MINIMIZE);}

{NUMBER}          {return MakeFastToken(Tokens.NUMBER);}
{COMMENT}         {return MakeFastToken(Tokens.COMMENT);}
{STRING}          {return MakeFastToken(Tokens.STRING);}
{CHAR}            {return MakeFastToken(Tokens.CHAR);} 
{LBRACKET}        {return MakeFastToken(Tokens.LBRACKET);} 
{RBRACKET}        {return MakeFastToken(Tokens.RBRACKET);}  
{PUBLIC}          {return MakeFastToken(Tokens.PUBLIC);} 

{ENUM}            {return MakeFastToken(Tokens.ENUM);} 
{CONST}           {return MakeFastToken(Tokens.CONST);} 
{FUN}             {return MakeFastToken(Tokens.FUN);} 
{ALPHABET}        {return MakeFastToken(Tokens.ALPHABET);} 
{LANG}            {return MakeFastToken(Tokens.LANG);} 
{TRANS}           {return MakeFastToken(Tokens.TRANS);} 
{DEF}             {return MakeFastToken(Tokens.DEF);} 
{TREE}            {return MakeFastToken(Tokens.TREE);} 
{PRINT}           {return MakeFastToken(Tokens.PRINT);} 
{ASSERT_TRUE}     {return MakeFastToken(Tokens.ASSERT_TRUE);} 
{ASSERT_FALSE}    {return MakeFastToken(Tokens.ASSERT_FALSE);} 

{ASSIGN}          {return MakeFastToken(Tokens.ASSIGN);}
{RIGHT_ARROW}     {return MakeFastToken(Tokens.RIGHT_ARROW);}
{WHERE}           {return MakeFastToken(Tokens.WHERE);}
{GIVEN}           {return MakeFastToken(Tokens.GIVEN);}
{TO}              {return MakeFastToken(Tokens.TO);}

{APPLY}           {return MakeFastToken(Tokens.APPLY);}
{GET_WITNESS}     {return MakeFastToken(Tokens.GET_WITNESS);}

{LBRACE}          {return MakeFastToken(Tokens.LBRACE);}
{RBRACE}          {return MakeFastToken(Tokens.RBRACE);}        
{LPAR}            {return MakeFastToken(Tokens.LPAR);}
{RPAR}            {return MakeFastToken(Tokens.RPAR);}
{COMMA}           {return MakeFastToken(Tokens.COMMA);}       
{COLON}           {return MakeFastToken(Tokens.COLON);}        
{BAR}             {return MakeFastToken(Tokens.BAR);}    

{LT}              {return MakeFastToken(Tokens.LT);}  
{GT}              {return MakeFastToken(Tokens.GT);} 
{LE}              {return MakeFastToken(Tokens.LE);} 
{GE}              {return MakeFastToken(Tokens.GE);} 
{EQ}              {return MakeFastToken(Tokens.EQ);} 
{PLUS}            {return MakeFastToken(Tokens.PLUS);} 
{MINUS}           {return MakeFastToken(Tokens.MINUS);} 
{TIMES}           {return MakeFastToken(Tokens.TIMES);} 
{DIV}             {return MakeFastToken(Tokens.DIV);} 
{MOD}             {return MakeFastToken(Tokens.MOD);} 
{SHL}             {return MakeFastToken(Tokens.SHL);} 
{SHR}             {return MakeFastToken(Tokens.SHR);} 
{NE}              {return MakeFastToken(Tokens.NE);} 
{NOT}             {return MakeFastToken(Tokens.NOT);} 
{AND}             {return MakeFastToken(Tokens.AND);} 
{BVNOT}           {return MakeFastToken(Tokens.BVNOT);} 
{BVXOR}           {return MakeFastToken(Tokens.BVXOR);} 
{BVAND}           {return MakeFastToken(Tokens.BVAND);} 
{IMPLIES}         {return MakeFastToken(Tokens.IMPLIES);} 
{OR}              {return MakeFastToken(Tokens.OR);} 

{TRUE}            {return MakeFastToken(Tokens.TRUE);} 
{FALSE}           {return MakeFastToken(Tokens.FALSE);} 

{ITE}             {return MakeFastToken(Tokens.ITE);} 

{ID}              {return MakeFastToken(Tokens.ID);}

{BADCHAR}         {return MakeFastToken(Tokens.BADCHAR);}

%%
