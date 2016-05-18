%namespace Microsoft.Automata.MSO.Mona
%parsertype MonaParser
%visibility public
%partial
%YYSTYPE object
%YYLTYPE LexLocation

%start program

%token WS1S WS2S M2LSTR M2LTREE
%token LT GT LE GE EQ NE 
%token PLUS MINUS TIMES DIV MOD SETMINUS
%token TRUE FALSE NOT AND OR IMPLIES EQUIV
%token IN NOTIN SUBSET EMPTY RESTRICT
%token EX0 EX1 EX2 ALL0 ALL1 ALL2 LET0 LET1 LET2 VAR0 VAR1 VAR2
%token UNIVERSE WHERE PREFIX MAX MIN PCONST UNION INTER
%token ARROW NAME NUMBER
%token LBRACKET RBRACKET LBRACE RBRACE LPAR RPAR COMMA COLON SEMICOLON
%token DOT UP
%token BADCHAR
%token maxParseToken COMMENT

%nonassoc COLON
%right EQUIV
%right IMPLIES
%left OR
%left AND
%nonassoc NOT
%nonassoc IN NOTIN SUBSET
%nonassoc EQ NE GT GE LT LE
%nonassoc MAX MIN
%left UNION
%left INTER
%left SETMINUS
%left PLUS MINUS
%left TIMES DIV MOD
%nonassoc DOT UP

%% 

program 
   : header SEMICOLON decls        { $$ = MkProgram($1, $3); }
   | decls                         { $$ = MkProgram($1); }
   ;

header
   : WS1S | WS2S | M2LSTR | M2LTREE                       
   ;

decls
   : decl SEMICOLON decls_         { $$ = MkDeclarations($1, $3); }
   ;

decls_
   : EOF                           { $$ = MkDeclarations(); }
   | decl SEMICOLON decls_         { $$ = MkDeclarations($1, $3); }
   ;

decl 
   : var_decl 
   | formula                       
   ;

formula 
   : TRUE                          { $$ = MkBooleanFormula($1) ; }
   | FALSE                         { $$ = MkBooleanFormula($1) ; }
   | LPAR formula RPAR             { $$ = $2; }
   | NOT formula                   { $$ = MkBooleanFormula($1, $2); }
   | RESTRICT LPAR formula RPAR    { $$ = MkBooleanFormula($1, $3); }
   | EMPTY LPAR term RPAR          { $$ = MkAtom($1, $3); }
   | q0 names formula              { $$ = MkQ0Formula($1, $2, $3); }
   | q RBRACKET univs vars COLON formula { $$ = MkQFormula($1, $4, $5, $3); }
   | q vars COLON formula          { $$ = MkQFormula($1, $2, $3); }
   | formula AND formula           { $$ = MkBooleanFormula($2, $1, $3); }
   | formula OR formula            { $$ = MkBooleanFormula($2, $1, $3); }
   | formula IMPLIES formula       { $$ = MkBooleanFormula($2, $1, $3); }
   | formula EQUIV formula         { $$ = MkBooleanFormula($2, $1, $3); }
   | term EQ term                  { $$ = MkAtom($2, $1, $3); }
   | term NE term                  { $$ = MkAtom($2, $1, $3); } 
   | term GT term                  { $$ = MkAtom($2, $1, $3); }
   | term GE term                  { $$ = MkAtom($2, $1, $3); }
   | term LT term                  { $$ = MkAtom($2, $1, $3); }
   | term LE term                  { $$ = MkAtom($2, $1, $3); }
   | term IN term                  { $$ = MkAtom($2, $1, $3); }
   | term NOTIN term               { $$ = MkAtom($2, $1, $3); }
   | term SUBSET term              { $$ = MkAtom($2, $1, $3); } 
   | NAME LPAR exps                { $$ = MkPredApp($1, $3); }
   | NAME                          { $$ = MkBooleanFormula($1); }
   ;

q
   : EX1 | EX2 | ALL1 | ALL2 
   ;

q0 
   : EX0 | ALL0
   ;

exps 
   : RPAR                          { $$ = MkList<Expression>();}
   | COMMA exp exps                { $$ = MkList<Expression>($2, $3);}
   ;

names
   : NAME names_                   { $$ = MkList<Token>($1, $2); }
   ;
names_
   : COLON                         { $$ = MkList<Token>(); }
   | COMMA NAME names_             { $$ = MkList<Token>($1, $2); }
   ;

exp
   : term
   | formula
   ;

univs
   : NAME univs_                   { $$ = MkList<Token>($1, $2); }
   ;
univs_
   : RBRACKET                      { $$ = MkList<Token>(); }
   | COMMA NAME univs_             { $$ = MkList<Token>($2, $3); }
   ;

vars
   : NAME COMMA vars               { $$ = MkList<VarWhere>(MkVar($1), $3); }
   | NAME WHERE formula COMMA vars { $$ = MkList<VarWhere>(MkVar($1, $3), $4); }
   | NAME WHERE formula            { $$ = MkList<VarWhere>(MkVar($1, $3), MkList<VarWhere>()); }
   | NAME                          { $$ = MkList<VarWhere>(MkVar($1), MkList<VarWhere>()); }
   ;

term 
   : NAME                          { $$ = MkName($1); }
   | NUMBER                        { $$ = MkInt($1); }   
   | LPAR term RPAR                { $$ = $2; }                    
   | term PLUS term                { $$ = MkFuncApp($2, $1, $3); }
   | term MINUS term               { $$ = MkFuncApp($2, $1, $3); }
   | term TIMES term               { $$ = MkFuncApp($2, $1, $3); }
   | term DIV term                 { $$ = MkFuncApp($2, $1, $3); }
   | term MOD term                 { $$ = MkFuncApp($2, $1, $3); }
   | MIN term                      { $$ = MkFuncApp($1, $2); }             
   | MAX term                      { $$ = MkFuncApp($1, $2); }
   ;

var_decl 
   : var_kind LBRACKET univs vars  { $$ = MkVarDecl($1, $4, $3); }
   | var_kind vars                 { $$ = MkVarDecl($1, $2); }
   ;
   
var_kind 
   : VAR1 | VAR2
   ;

%%
