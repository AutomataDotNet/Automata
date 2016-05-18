%namespace Microsoft.Automata.MSO.Mona
%visibility internal

%{
 
  public string sourcefile = null;
  public int MakeMonaToken(Tokens kind)
  {
     int dummy = yytext.Length; //force evaluation of the string yytext
	 string text = yytext;
	 yylloc = new QUT.Gppg.LexLocation(tokLin, tokCol, tokELin, tokECol);
	 if (kind == Tokens.BADCHAR)
	   throw new MonaParseException(yylloc, string.Format("Syntax error, unexpected '{0}'",text));

     yylval = new Token(text, yylloc, kind, sourcefile); 
     return (int)kind;  
  }

  override public void yyerror(string message, params object[] args)
  {
	  var token = (Token)yylval;
	  throw new MonaParseException(token.Location, message);
  }

%}
        
WS              [ \t\r\f\v\n]
COMMENT         [\/]\*(.|\n)*\*[\/]|\x23.*


WS1S            ws1s
WS2S            ws2s
M2LSTR          m2l-str
M2LTREE         m2l-tree

LE              <=
LT              <
GE              >=
GT              >
EQ              =
NE              ~=

PLUS            +
MINUS           -
TIMES           *
DIV             [\/]
MOD             [\%]
SETMINUS        [\\]

TRUE            true
FALSE           false
NOT             ~
AND             &
OR              \|
IMPLIES         =>
EQUIV           <=>

IN              in
NOTIN           notin
SUBSET          sub
EMPTY           empty
RESTRICT        restrict

EX0             ex0
EX1             ex1
EX2             ex2
ALL0            all0
ALL1            all1
ALL2            all2
LET0            let0
LET1            let1
LET2            let2
VAR0            var0
VAR1            var1
VAR2            var2

UNIVERSE        universe
WHERE           where
PREFIX          prefix
MAX             max
MIN             min
PCONST          pconst
UNION           union
INTER           inter

LBRACKET        [\[]
RBRACKET        [\]]
LBRACE          [\{]
RBRACE          [\}]
LPAR            [\(]
RPAR            [\)]
COMMA           [,]
COLON           [:]
SEMICOLON       [;]

DOT             [\.]
UP              [\^]

ARROW           ->
NAME            ([A-Za-z_\$'][A-Za-z_0-9\$']*)
NUMBER          [0-9]+|0x[0-9A-Fa-f]+

BADCHAR         [^ \t\r\f\v\n]

%%

{WS1S}            {return MakeMonaToken(Tokens.WS1S);}
{WS2S}            {return MakeMonaToken(Tokens.WS2S);}
{M2LSTR}          {return MakeMonaToken(Tokens.M2LSTR);}
{M2LTREE}         {return MakeMonaToken(Tokens.M2LTREE);}

{LT}              {return MakeMonaToken(Tokens.LT);}
{GT}              {return MakeMonaToken(Tokens.GT);}
{LE}              {return MakeMonaToken(Tokens.LE);}
{GE}              {return MakeMonaToken(Tokens.GE);}
{EQ}              {return MakeMonaToken(Tokens.EQ);}
{NE}              {return MakeMonaToken(Tokens.NE);}
{PLUS}            {return MakeMonaToken(Tokens.PLUS);}
{MINUS}           {return MakeMonaToken(Tokens.MINUS);}
{TIMES}           {return MakeMonaToken(Tokens.TIMES);}
{DIV}             {return MakeMonaToken(Tokens.DIV);}
{MOD}             {return MakeMonaToken(Tokens.MOD);}
{SETMINUS}        {return MakeMonaToken(Tokens.SETMINUS);}

{TRUE}            {return MakeMonaToken(Tokens.TRUE);}
{FALSE}           {return MakeMonaToken(Tokens.FALSE);}
{NOT}             {return MakeMonaToken(Tokens.NOT);}
{AND}             {return MakeMonaToken(Tokens.AND);}
{OR}              {return MakeMonaToken(Tokens.OR);}
{IMPLIES}         {return MakeMonaToken(Tokens.IMPLIES);}
{EQUIV}           {return MakeMonaToken(Tokens.EQUIV);}
{IN}              {return MakeMonaToken(Tokens.IN);}
{NOTIN}           {return MakeMonaToken(Tokens.NOTIN);}
{SUBSET}          {return MakeMonaToken(Tokens.SUBSET);}
{EMPTY}           {return MakeMonaToken(Tokens.EMPTY);}
{RESTRICT}        {return MakeMonaToken(Tokens.RESTRICT);}

{EX0}             {return MakeMonaToken(Tokens.EX0);}
{EX1}             {return MakeMonaToken(Tokens.EX1);}
{EX2}             {return MakeMonaToken(Tokens.EX2);}
{ALL0}            {return MakeMonaToken(Tokens.ALL0);}
{ALL1}            {return MakeMonaToken(Tokens.ALL1);}
{ALL2}            {return MakeMonaToken(Tokens.ALL2);}
{LET0}            {return MakeMonaToken(Tokens.LET0);}
{LET1}            {return MakeMonaToken(Tokens.LET1);}
{LET2}            {return MakeMonaToken(Tokens.LET2);}
{VAR0}            {return MakeMonaToken(Tokens.VAR0);}
{VAR1}            {return MakeMonaToken(Tokens.VAR1);}
{VAR2}            {return MakeMonaToken(Tokens.VAR2);}

{UNIVERSE}        {return MakeMonaToken(Tokens.UNIVERSE);}
{WHERE}           {return MakeMonaToken(Tokens.WHERE);}
{PREFIX}          {return MakeMonaToken(Tokens.PREFIX);}
{MAX}             {return MakeMonaToken(Tokens.MAX);}
{MIN}             {return MakeMonaToken(Tokens.MIN);}
{PCONST}          {return MakeMonaToken(Tokens.PCONST);}
{UNION}           {return MakeMonaToken(Tokens.UNION);}
{INTER}           {return MakeMonaToken(Tokens.INTER);}

{ARROW}           {return MakeMonaToken(Tokens.ARROW);}

{NAME}            {return MakeMonaToken(Tokens.NAME);}
{NUMBER}          {return MakeMonaToken(Tokens.NUMBER);}

{LBRACKET}        {return MakeMonaToken(Tokens.LBRACKET);}
{RBRACKET}        {return MakeMonaToken(Tokens.RBRACKET);}
{LBRACE}          {return MakeMonaToken(Tokens.LBRACE);}
{RBRACE}          {return MakeMonaToken(Tokens.RBRACE);}
{LPAR}            {return MakeMonaToken(Tokens.LPAR);}
{RPAR}            {return MakeMonaToken(Tokens.RPAR);}
{COMMA}           {return MakeMonaToken(Tokens.COMMA);}
{COLON}           {return MakeMonaToken(Tokens.COLON);}
{SEMICOLON}       {return MakeMonaToken(Tokens.SEMICOLON);}

{DOT}             {return MakeMonaToken(Tokens.DOT);}
{UP}              {return MakeMonaToken(Tokens.UP);}

{COMMENT}         {return MakeMonaToken(Tokens.COMMENT);}

{BADCHAR}         {return MakeMonaToken(Tokens.BADCHAR);}

%%
