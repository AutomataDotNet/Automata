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
%token WHERE PREFIX MAX MIN PCONST UNION INTER
%token GUIDE UNIVERSE INCLUDE ASSERT EXECUTE CONST
%token DEFAULTWHERE1 DEFAULTWHERE2 TREE MACRO PRED ALLPOS TYPE
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
   : header SEMICOLON decls                        { $$ = MkProgram($1, $3); }
   | decls                                         { $$ = MkProgram($1); }
   ;

header
   : WS1S | WS2S | M2LSTR | M2LTREE                       
   ;

decls
   : decl SEMICOLON EOF                            { $$ = MkList<Decl>($1, MkList<Decl>()); }
   | decl SEMICOLON decls                          { $$ = MkList<Decl>($1, $3); }
   ;

decl 
   : formula                                       { $$ = MkFormulaDecl($1); }
   | UNIVERSE univargs                             { $$ = MkUnivDecl($2); }        
   | ASSERT formula                                { $$ = MkAssertDecl($2); }
   | EXECUTE formula                               { $$ = MkExecuteDecl($2); }
   | CONST NAME EQ intterm                         { $$ = MkConstDecl($2, $4); }
   | DEFAULTWHERE1 LPAR NAME RPAR EQ formula       { $$ = MkDefaultWhere1Decl($3, $6); }
   | DEFAULTWHERE2 LPAR NAME RPAR EQ formula       { $$ = MkDefaultWhere2Decl($3, $6); }
   | VAR0 names                                    { $$ = MkVar0Decl($2); }                     
   | VAR1 univs vws                                { $$ = MkVar1Decl($3, $2); }
   | VAR2 univs vws                                { $$ = MkVar2Decl($3, $2); }
   | TREE univs vws                                { $$ = MkTreeDecl($3, $2); }
   | VAR1 vws                                      { $$ = MkVar1Decl($2); }    
   | VAR2 vws                                      { $$ = MkVar2Decl($2); }    
   | TREE vws                                      { $$ = MkTreeDecl($2); }        
   ;

intterm 
   : LPAR intterm RPAR                             { $$ = $2; }
   | intterm PLUS intterm                          { $$ = MkArithmFuncApp($2, $1, $3); }
   | intterm MINUS intterm                         { $$ = MkArithmFuncApp($2, $1, $3); }
   | intterm TIMES intterm                         { $$ = MkArithmFuncApp($2, $1, $3); }
   | intterm DIV intterm                           { $$ = MkArithmFuncApp($2, $1, $3); }
   | intterm MOD intterm                           { $$ = MkArithmFuncApp($2, $1, $3); }
   | NAME                                          { $$ = MkConstRef($1); }
   | NUMBER                                        { $$ = MkInt($1); }
   ;

univargs
   : univarg COMMA univargs                        { $$ = MkList<UnivArg>($1, $3); }
   | univarg                                       { $$ = MkList<UnivArg>($1, MkList<UnivArg>()); }
   ;

univarg 
   : NAME COLON NAME                               { $$ = MkUnivArgWithType($1, $3); }
   | NAME COLON NUMBER                             { $$ = MkUnivArgWithSucc($1, $3); }
   | NAME                                          { $$ = MkUnivArg($1); }
   ;

vws
   : NAME COMMA vws                                { $$ = MkList<VarWhere>(MkVarWhere($1), $3); }
   | NAME WHERE formula COMMA vws                  { $$ = MkList<VarWhere>(MkVarWhere($1, $3), $4); }
   | NAME WHERE formula                            { $$ = MkList<VarWhere>(MkVarWhere($1, $3), MkList<VarWhere>()); }
   | NAME                                          { $$ = MkList<VarWhere>(MkVarWhere($1), MkList<VarWhere>()); }
   ;

formula 
   : TRUE                                          { $$ = MkBooleanConstant($1) ; }
   | FALSE                                         { $$ = MkBooleanConstant($1) ; }
   | LPAR formula RPAR                             { $$ = $2; }
   | NOT formula                                   { $$ = MkBooleanFormula($1, $2); }
   | RESTRICT LPAR formula RPAR                    { $$ = MkRestrict($1, $3); }
   | EMPTY LPAR term RPAR                          { $$ = MkIsEmpty($1, $3); }
   | Q0 names COLON formula                        { $$ = MkQ0Formula($1, $2, $4); }
   | Q univs vws COLON formula                     { $$ = MkQFormula($1, $3, $5, $2); }
   | Q vws COLON formula                           { $$ = MkQFormula($1, $2, $4); }
   | formula AND formula                           { $$ = MkBooleanFormula($2, $1, $3); }
   | formula OR formula                            { $$ = MkBooleanFormula($2, $1, $3); }
   | formula IMPLIES formula                       { $$ = MkBooleanFormula($2, $1, $3); }
   | formula EQUIV formula                         { $$ = MkBooleanFormula($2, $1, $3); }
   | term EQ term                                  { $$ = MkAtom2($2, $1, $3); }
   | term NE term                                  { $$ = MkAtom2($2, $1, $3); } 
   | term GT term                                  { $$ = MkAtom2($2, $1, $3); }
   | term GE term                                  { $$ = MkAtom2($2, $1, $3); }
   | term LT term                                  { $$ = MkAtom2($2, $1, $3); }
   | term LE term                                  { $$ = MkAtom2($2, $1, $3); }
   | term IN term                                  { $$ = MkAtom2($2, $1, $3); }
   | term NOTIN term                               { $$ = MkAtom2($2, $1, $3); }
   | term SUBSET term                              { $$ = MkAtom2($2, $1, $3); } 
   | NAME LPAR exps                                { $$ = MkPredApp($1, $3); }
   | NAME                                          { $$ = MkBooleanVariable($1); }
   ;

Q
   : EX1 | EX2 | ALL1 | ALL2 
   ;

Q0 
   : EX0 | ALL0
   ;

exps 
   : RPAR                                          { $$ = MkList<Expression>();}
   | COMMA exp exps                                { $$ = MkList<Expression>($2, $3);}
   ;
exp
   : term
   | formula
   ;

names
   : NAME COMMA names                              { $$ = MkList<Token>($1, $3); }
   | NAME                                          { $$ = MkList<Token>($1, MkList<Token>()); }
   ;

univs
   : LBRACKET NAME univs_                          { $$ = MkList<Token>($1, $2); }
   ;
univs_
   : RBRACKET                                      { $$ = MkList<Token>(); }
   | COMMA NAME univs_                             { $$ = MkList<Token>($2, $3); }
   ;

term 
   : NAME                                          { $$ = MkName($1); }
   | NUMBER                                        { $$ = MkInt($1); }   
   | LPAR term RPAR                                { $$ = $2; }                    
   | term PLUS term                                { $$ = MkArithmFuncApp($2, $1, $3); }
   | term MINUS term                               { $$ = MkArithmFuncApp($2, $1, $3); }
   | term TIMES term                               { $$ = MkArithmFuncApp($2, $1, $3); }
   | term DIV term                                 { $$ = MkArithmFuncApp($2, $1, $3); }
   | term MOD term                                 { $$ = MkArithmFuncApp($2, $1, $3); }
   | MIN term                                      { $$ = MkMinOrMax($1, $2); }             
   | MAX term                                      { $$ = MkMinOrMax($1, $2); }
   ;

%%
