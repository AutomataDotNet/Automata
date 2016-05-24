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
%token RANGE DOT UP
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
   | VAR1 vws                                      { $$ = MkVar1Decl($2, null); }    
   | VAR2 vws                                      { $$ = MkVar2Decl($2, null); }    
   | TREE vws                                      { $$ = MkTreeDecl($2, null); }       
   | MACRO NAME EQ formula                         { $$ = MkPredDecl($2, null, $4, true); } 
   | MACRO NAME LPAR parameters EQ formula         { $$ = MkPredDecl($2, $4, $6, true); } 
   | PRED NAME EQ formula                          { $$ = MkPredDecl($2, null, $4, false); } 
   | PRED NAME LPAR parameters EQ formula          { $$ = MkPredDecl($2, $4, $6, false); }
   ;

parameters 
   : RPAR                                          { $$ = MkList<Param>(); }
   | VAR0 NAME params0                             { $$ = MkList<Param>(MkVar0Param($2), $3); }
   | UNIVERSE NAME parameters                      { $$ = MkList<Param>(MkUniverseParam($2), $3); }
   | VAR1 NAME params1                             { $$ = MkList<Param>(MkVar1Param(MkVarWhere($2, null)), $3); }
   | VAR1 NAME WHERE formula params1               { $$ = MkList<Param>(MkVar1Param(MkVarWhere($2, $4)), $5); }
   | VAR2 NAME params2                             { $$ = MkList<Param>(MkVar2Param(MkVarWhere($2, null)), $3); }
   | VAR2 NAME WHERE formula params2               { $$ = MkList<Param>(MkVar2Param(MkVarWhere($2, $4)), $5); }
   ;

params0
   : RPAR                                          { $$ = MkList<Param>(); }
   | COMMA VAR0 NAME params0                       { $$ = MkList<Param>(MkVar0Param($3), $4); }
   | COMMA VAR1 NAME params1                       { $$ = MkList<Param>(MkVar1Param(MkVarWhere($3, null)), $4); }
   | COMMA VAR1 NAME WHERE formula params1         { $$ = MkList<Param>(MkVar1Param(MkVarWhere($3, $5)), $6); }
   | COMMA VAR2 NAME params2                       { $$ = MkList<Param>(MkVar2Param(MkVarWhere($3, null)), $4); }
   | COMMA VAR2 NAME WHERE formula params2         { $$ = MkList<Param>(MkVar2Param(MkVarWhere($3, $5)), $6); }
   | COMMA UNIVERSE NAME parameters                { $$ = MkList<Param>(MkUniverseParam($3), $4); }
   | COMMA NAME params0                            { $$ = MkList<Param>(MkVar0Param($2), $3); }
   ;

params1
   : RPAR                                          { $$ = MkList<Param>(); }
   | COMMA VAR0 NAME params0                       { $$ = MkList<Param>(MkVar0Param($3), $4); }
   | COMMA VAR1 NAME params1                       { $$ = MkList<Param>(MkVar1Param(MkVarWhere($3, null)), $4); }
   | COMMA VAR1 NAME WHERE formula params1         { $$ = MkList<Param>(MkVar1Param(MkVarWhere($3, $5)), $6); }
   | COMMA VAR2 NAME params2                       { $$ = MkList<Param>(MkVar2Param(MkVarWhere($3, null)), $4); }
   | COMMA VAR2 NAME WHERE formula params2         { $$ = MkList<Param>(MkVar2Param(MkVarWhere($3, $5)), $6); }
   | COMMA UNIVERSE NAME parameters                { $$ = MkList<Param>(MkUniverseParam($3), $4); }
   | COMMA NAME params1                            { $$ = MkList<Param>(MkVar1Param(MkVarWhere($2, null)), $3); }
   | COMMA NAME WHERE formula params1              { $$ = MkList<Param>(MkVar1Param(MkVarWhere($2, $4)), $5); }
   ;

params2
   : RPAR                                          { $$ = MkList<Param>(); }
   | COMMA VAR0 NAME params0                       { $$ = MkList<Param>(MkVar0Param($3), $4); }
   | COMMA VAR1 NAME params1                       { $$ = MkList<Param>(MkVar1Param(MkVarWhere($3, null)), $4); }
   | COMMA VAR1 NAME WHERE formula params1         { $$ = MkList<Param>(MkVar1Param(MkVarWhere($3, $5)), $6); }
   | COMMA VAR2 NAME params2                       { $$ = MkList<Param>(MkVar2Param(MkVarWhere($3, null)), $4); }
   | COMMA VAR2 NAME WHERE formula params2         { $$ = MkList<Param>(MkVar2Param(MkVarWhere($3, $5)), $6); }
   | COMMA UNIVERSE NAME parameters                { $$ = MkList<Param>(MkUniverseParam($3), $4); }
   | COMMA NAME params2                            { $$ = MkList<Param>(MkVar2Param(MkVarWhere($2, null)), $3); }
   | COMMA NAME WHERE formula params2              { $$ = MkList<Param>(MkVar2Param(MkVarWhere($2, $4)), $5); }
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
   : NAME COMMA vws                                { $$ = MkList<VarWhere>(MkVarWhere($1, null), $3); }
   | NAME WHERE formula COMMA vws                  { $$ = MkList<VarWhere>(MkVarWhere($1, $3), $4); }
   | NAME WHERE formula                            { $$ = MkList<VarWhere>(MkVarWhere($1, $3), MkList<VarWhere>()); }
   | NAME                                          { $$ = MkList<VarWhere>(MkVarWhere($1, null), MkList<VarWhere>()); }
   ;
   
formula 
   : TRUE                                          { $$ = MkBooleanConstant($1) ; }
   | FALSE                                         { $$ = MkBooleanConstant($1) ; }
   | LPAR formula RPAR                             { $$ = $2; }
   | NOT formula                                   { $$ = MkBooleanFormula($1, $2); }
   | RESTRICT LPAR formula RPAR                    { $$ = MkRestrict($1, $3); }
   | EMPTY LPAR term2 RPAR                         { $$ = MkIsEmpty($1, $3); }
   | Q0 names COLON formula                        { $$ = MkQ0Formula($1, $2, $4); }
   | Q univs vws COLON formula                     { $$ = MkQFormula($1, $3, $5, $2); }
   | Q vws COLON formula                           { $$ = MkQFormula($1, $2, $4); }
   | NAME LPAR exprs RPAR                          { $$ = MkPredApp($1, $3); }
   | formula AND formula                           { $$ = MkBooleanFormula($2, $1, $3); }
   | formula OR formula                            { $$ = MkBooleanFormula($2, $1, $3); }
   | formula IMPLIES formula                       { $$ = MkBooleanFormula($2, $1, $3); }
   | formula EQUIV formula                         { $$ = MkBooleanFormula($2, $1, $3); }
   | term EQ term                                  { $$ = MkAtom2($2, $1, $3); }
   | term NE term                                  { $$ = MkAtom2($2, $1, $3); } 
   | term1 GT term1                                { $$ = MkAtom2($2, $1, $3); }
   | term1 GE term1                                { $$ = MkAtom2($2, $1, $3); }
   | term1 LT term1                                { $$ = MkAtom2($2, $1, $3); }
   | term1 LE term1                                { $$ = MkAtom2($2, $1, $3); }
   | term1 IN term2                                { $$ = MkAtom2($2, $1, $3); }
   | term1 NOTIN term2                             { $$ = MkAtom2($2, $1, $3); }
   | term2 SUBSET term2                            { $$ = MkAtom2($2, $1, $3); } 
   | NAME                                          { $$ = MkName($1); }
   ;

Q
   : EX1 | EX2 | ALL1 | ALL2 
   ;

Q0 
   : EX0 | ALL0
   ;

exprs 
   : expr COMMA exprs                              { $$ = MkList<Expr>($1, $3); }
   | expr                                          { $$ = MkList<Expr>($1,  MkList<Expr>()); }
   ;

expr
   : term  
   | formula
   ;

term 
   : term1 
   | term2
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

term1 
   : NUMBER                                        { $$ = MkInt($1); }  
   | LPAR term1 RPAR                               { $$ = $2; }                   
   | term1 PLUS term1                              { $$ = MkArithmFuncApp($2, $1, $3); }
   | term1 MINUS term1                             { $$ = MkArithmFuncApp($2, $1, $3); }
   | term1 TIMES term1                             { $$ = MkArithmFuncApp($2, $1, $3); }
   | term1 DIV term1                               { $$ = MkArithmFuncApp($2, $1, $3); }
   | term1 MOD term1                               { $$ = MkArithmFuncApp($2, $1, $3); }
   | MIN term2                                     { $$ = MkMinOrMax($1, $2); }             
   | MAX term2                                     { $$ = MkMinOrMax($1, $2); }
   | NAME                                          { $$ = MkName($1); }
   ;

term2 
   : LPAR term2 RPAR                               { $$ = $2; }  
   | LBRACE elemslist                              { $$ = MkSet($1, $2); }
   | EMPTY                                         { $$ = MkSet($1); }
   | PCONST LPAR intterm RPAR                      { $$ = MkPconst($1, $3); }
   | term2 UNION term2                             { $$ = MkSetOp($2, $1, $3); }
   | term2 INTER term2                             { $$ = MkSetOp($2, $1, $3); }
   | term2 SETMINUS term2                          { $$ = MkSetOp($2, $1, $3); }
   | term2 PLUS intterm                            { $$ = MkSetOp($2, $1, $3); }
   | term2 MINUS intterm                           { $$ = MkSetOp($2, $1, $3); }
   | NAME                                          { $$ = MkName($1); }
   ;

elemslist 
   : RBRACE                                        { $$ = MkList<Expr>(); }
   | elems RBRACE                                  { $$ = MkList<Expr>($1, MkList<Expr>()); }
   | elems COMMA elemslist                         { $$ = MkList<Expr>($1, $3); }
   ;

elems 
   : term1 RANGE term1                             { $$ = MkRange($2, $1, $3); }
   | term1    
   ;                                     

%%
