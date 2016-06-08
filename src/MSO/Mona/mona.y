%namespace Microsoft.Automata.MSO.Mona
%parsertype MonaParser
%visibility public
%partial
%YYSTYPE object
%YYLTYPE LexLocationInFile

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
   : header SEMICOLON decls                        { $$ = MkProgram($3, $1); }
   | decls                                         { $$ = MkProgram($1, null); }
   ;

header
   : WS1S | WS2S | M2LSTR | M2LTREE                       
   ;

decls
   : decl SEMICOLON EOF                            { $$ = MkList<MonaDecl>($1); }
   | decl SEMICOLON decls                          { $$ = MkList<MonaDecl>($1, $3); }
   ;

decl 
   : formula                                       { $$ = MkFormulaDecl($1); }
   | UNIVERSE univargs                             { $$ = MkUnivDecl($2); }      
   | ALLPOS NAME                                   { $$ = MkAllposDecl($2); }  
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
   : RPAR                                          { $$ = MkList<MonaParam>(); }
   | VAR0 NAME params0                             { $$ = MkList<MonaParam>(MkVar0Param($2), $3); }
   | UNIVERSE NAME parameters                      { $$ = MkList<MonaParam>(MkUniverseParam($2), $3); }
   | VAR1 NAME params1                             { $$ = MkList<MonaParam>(MkVar1Param(MkVarWhere($2, null)), $3); }
   | VAR1 NAME WHERE formula params1               { $$ = MkList<MonaParam>(MkVar1Param(MkVarWhere($2, $4)), $5); }
   | VAR2 NAME params2                             { $$ = MkList<MonaParam>(MkVar2Param(MkVarWhere($2, null)), $3); }
   | VAR2 NAME WHERE formula params2               { $$ = MkList<MonaParam>(MkVar2Param(MkVarWhere($2, $4)), $5); }
   ;

params0
   : RPAR                                          { $$ = MkList<MonaParam>(); }
   | COMMA VAR0 NAME params0                       { $$ = MkList<MonaParam>(MkVar0Param($3), $4); }
   | COMMA VAR1 NAME params1                       { $$ = MkList<MonaParam>(MkVar1Param(MkVarWhere($3, null)), $4); }
   | COMMA VAR1 NAME WHERE formula params1         { $$ = MkList<MonaParam>(MkVar1Param(MkVarWhere($3, $5)), $6); }
   | COMMA VAR2 NAME params2                       { $$ = MkList<MonaParam>(MkVar2Param(MkVarWhere($3, null)), $4); }
   | COMMA VAR2 NAME WHERE formula params2         { $$ = MkList<MonaParam>(MkVar2Param(MkVarWhere($3, $5)), $6); }
   | COMMA UNIVERSE NAME parameters                { $$ = MkList<MonaParam>(MkUniverseParam($3), $4); }
   | COMMA NAME params0                            { $$ = MkList<MonaParam>(MkVar0Param($2), $3); }
   ;

params1
   : RPAR                                          { $$ = MkList<MonaParam>(); }
   | COMMA VAR0 NAME params0                       { $$ = MkList<MonaParam>(MkVar0Param($3), $4); }
   | COMMA VAR1 NAME params1                       { $$ = MkList<MonaParam>(MkVar1Param(MkVarWhere($3, null)), $4); }
   | COMMA VAR1 NAME WHERE formula params1         { $$ = MkList<MonaParam>(MkVar1Param(MkVarWhere($3, $5)), $6); }
   | COMMA VAR2 NAME params2                       { $$ = MkList<MonaParam>(MkVar2Param(MkVarWhere($3, null)), $4); }
   | COMMA VAR2 NAME WHERE formula params2         { $$ = MkList<MonaParam>(MkVar2Param(MkVarWhere($3, $5)), $6); }
   | COMMA UNIVERSE NAME parameters                { $$ = MkList<MonaParam>(MkUniverseParam($3), $4); }
   | COMMA NAME params1                            { $$ = MkList<MonaParam>(MkVar1Param(MkVarWhere($2, null)), $3); }
   | COMMA NAME WHERE formula params1              { $$ = MkList<MonaParam>(MkVar1Param(MkVarWhere($2, $4)), $5); }
   ;

params2
   : RPAR                                          { $$ = MkList<MonaParam>(); }
   | COMMA VAR0 NAME params0                       { $$ = MkList<MonaParam>(MkVar0Param($3), $4); }
   | COMMA VAR1 NAME params1                       { $$ = MkList<MonaParam>(MkVar1Param(MkVarWhere($3, null)), $4); }
   | COMMA VAR1 NAME WHERE formula params1         { $$ = MkList<MonaParam>(MkVar1Param(MkVarWhere($3, $5)), $6); }
   | COMMA VAR2 NAME params2                       { $$ = MkList<MonaParam>(MkVar2Param(MkVarWhere($3, null)), $4); }
   | COMMA VAR2 NAME WHERE formula params2         { $$ = MkList<MonaParam>(MkVar2Param(MkVarWhere($3, $5)), $6); }
   | COMMA UNIVERSE NAME parameters                { $$ = MkList<MonaParam>(MkUniverseParam($3), $4); }
   | COMMA NAME params2                            { $$ = MkList<MonaParam>(MkVar2Param(MkVarWhere($2, null)), $3); }
   | COMMA NAME WHERE formula params2              { $$ = MkList<MonaParam>(MkVar2Param(MkVarWhere($2, $4)), $5); }
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
   : univarg COMMA univargs                        { $$ = MkList<MonaUnivArg>($1, $3); }
   | univarg                                       { $$ = MkList<MonaUnivArg>($1); }
   ;

univarg 
   : NAME COLON NAME                               { $$ = MkUnivArgWithType($1, $3); }
   | NAME COLON NUMBER                             { $$ = MkUnivArgWithSucc($1, $3); }
   | NAME                                          { $$ = MkUnivArg($1); }
   ;

vws
   : NAME COMMA vws                                { $$ = MkList<MonaVarWhere>(MkVarWhere($1, null), $3); }
   | NAME WHERE formula COMMA vws                  { $$ = MkList<MonaVarWhere>(MkVarWhere($1, $3), $4); }
   | NAME WHERE formula                            { $$ = MkList<MonaVarWhere>(MkVarWhere($1, $3)); }
   | NAME                                          { $$ = MkList<MonaVarWhere>(MkVarWhere($1, null)); }
   ;
   
formula 
   : TRUE                                          { $$ = MkBooleanConstant($1) ; }
   | FALSE                                         { $$ = MkBooleanConstant($1) ; }
   | LPAR formula RPAR                             { $$ = $2; }
   | NOT formula                                   { $$ = MkNegatedFormula($1, $2); }
   | RESTRICT LPAR formula RPAR                    { $$ = MkRestrict($1, $3); }
   | EMPTY LPAR term2 RPAR                         { $$ = MkIsEmpty($1, $3); }
   | Q0 names COLON formula                        { $$ = MkQ0Formula($1, $2, $4); }
   | Q univs vws COLON formula                     { $$ = MkQFormula($1, $3, $5, $2); }
   | Q vws COLON formula                           { $$ = MkQFormula($1, $2, $4, null); }
   | NAME LPAR RPAR                                { $$ = MkPredApp($1, MkList<MonaExpr>()); }
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
   | letexpr
   | NAME                                          { $$ = MkName($1); }
   ;

letexpr
   : LET0 letexprs0 IN formula                     { $$ = MkLet($1, $2, $4); }
   | LET1 letexprs1 IN formula                     { $$ = MkLet($1, $2, $4); }
   | LET2 letexprs2 IN formula                     { $$ = MkLet($1, $2, $4); }
   ;

Q
   : EX1 | EX2 | ALL1 | ALL2 
   ;

Q0 
   : EX0 | ALL0
   ;

exprs 
   : expr COMMA exprs                              { $$ = MkList<MonaExpr>($1, $3); }
   | expr                                          { $$ = MkList<MonaExpr>($1); }
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
   | NAME                                          { $$ = MkList<Token>($1); }
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
   | LBRACE RBRACE                                 { $$ = MkSet($1, MkList<MonaExpr>()); }
   | LBRACE elemslist RBRACE                       { $$ = MkSet($1, $2); }
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
   : elems COMMA elemslist                         { $$ = MkList<MonaExpr>($1, $3); }
   | elems                                         { $$ = MkList<MonaExpr>($1); }
   ;

elems 
   : term1 RANGE term1                             { $$ = MkRange($2, $1, $3); }
   | term1    
   ;      
   
letexprs0
   : NAME EQ formula0 COMMA letexprs0              { $$ = MkList<Tuple<Token,MonaExpr>>(new Tuple<Token,MonaExpr>((Token)$1,(MonaExpr)$3), $5); }
   | NAME EQ formula0                              { $$ = MkList<Tuple<Token,MonaExpr>>(new Tuple<Token,MonaExpr>((Token)$1,(MonaExpr)$3)); }  
   ;    
   
letexprs1
   : NAME EQ term1 COMMA letexprs1                 { $$ = MkList<Tuple<Token,MonaExpr>>(new Tuple<Token,MonaExpr>((Token)$1,(MonaExpr)$3), $5); }
   | NAME EQ term1                                 { $$ = MkList<Tuple<Token,MonaExpr>>(new Tuple<Token,MonaExpr>((Token)$1,(MonaExpr)$3)); }  
   ;     
   
letexprs2
   : NAME EQ term2 COMMA letexprs2                 { $$ = MkList<Tuple<Token,MonaExpr>>(new Tuple<Token,MonaExpr>((Token)$1,(MonaExpr)$3), $5); }
   | NAME EQ term2                                 { $$ = MkList<Tuple<Token,MonaExpr>>(new Tuple<Token,MonaExpr>((Token)$1,(MonaExpr)$3)); }  
   ; 
   
formula0 
   : LPAR formula RPAR                             { $$ = $2; }
   ;
              
%%
