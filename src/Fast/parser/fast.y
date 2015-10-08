%namespace Microsoft.Fast.AST
%parsertype FastPgmParser
%visibility internal
%partial
%YYSTYPE object
%YYLTYPE FastLexLocation

%start fast_pgm

%token CONTAINS IS_EMPTY_LANG IS_EMPTY_TRANS
%token TYPECHECK
%token GEN_CSHARP
%token EQ_LANG EQ_TRANS
%token COMPOSE RESTRICT_INP RESTRICT_OUT
%token INTERSECT DIFFERENCE UNION DOMAIN PRE_IMAGE COMPLEMENT MINIMIZE
%token ID
%token NUMBER
%token COMMENT
%token STRING
%token CHAR
%token PUBLIC
%token ENUM
%token CONST
%token FUN
%token ALPHABET LANG TRANS
%token DEF TREE
%token PRINT
%token ASSERT_TRUE ASSERT_FALSE
%token ASSIGN
%token RIGHT_ARROW
%token WHERE GIVEN TO
%token APPLY GET_WITNESS 
%token LBRACKET RBRACKET LBRACE RBRACE LPAR RPAR
%token COMMA COLON
%token AND NOT IMPLIES OR
%token LT GT LE GE EQ NE
%token PLUS MINUS TIMES DIV MOD SHL SHR BVAND BVNOT BVXOR BAR
%token ITE
%token TRUE FALSE

%token BADCHAR

%left OR
%left AND 
%nonassoc LT GT LE GE EQ NE
%left PLUS MINUS
%left TIMES DIV
%left MOD SHL SHR

%% 

fast_pgm 
   : fast_defs                     { $$ = MkFastPgm($1); }
   ;

fast_defs 
   : fast_def fast_defs            { $$ = MkDefList($1, $2); }
   | COMMENT  fast_defs            { $$ = $2; }
   | nothing                       { $$ = MkEmptyDefList(); }
   ;

fast_def 
   : ENUM enum_def                 { $$ = $2; }
   | CONST const_def               { $$ = $2; } 
   | FUN  fun_def                  { $$ = $2; } 
   | ALPHABET alphabet_def         { $$ = $2; } 
   | PUBLIC LANG lang_def          { $$ = $3; SetPublic($$); } 
   | LANG lang_def                 { $$ = $2; } 
   | PUBLIC TRANS trans_def        { $$ = $3; SetPublic($$); } 
   | TRANS trans_def               { $$ = $2; } 
   | DEF def_def                   { $$ = $2; } 
   | TREE tree_def                 { $$ = $2; } 
   | PRINT query_expr              { $$ = $2; } 
   | ASSERT_TRUE bool_query_expr   { $$ = $2; SetAssertTrue($$); }                      
   | ASSERT_FALSE bool_query_expr  { $$ = $2; SetAssertFalse($$); }                      
   ; 

enum_def  
   : ID LBRACE ID enum_def_elems_rest { $$ = MkEnumDef($1, MkTokenList($3, $4)); }
   ;
enum_def_elems_rest 
   : RBRACE                           { $$ = MkEmptyTokenList(); }
   | COMMA ID enum_def_elems_rest     { $$ = MkTokenList($2, $3); }
   ;


tree_def  
   : ID COLON ID ASSIGN tree_def_arg  { $$ = MkTreeDef($1, $3, $5); }
   ;
tree_def_arg 
   : LPAR APPLY trans_exp expr RPAR   { $$ = new object[]{$3, $4}; }
   | LPAR GET_WITNESS lang_exp RPAR   { $$ = $3; }
   | expr                             { $$ = $1; }
   ;

def_def 
   : ID COLON ID ASSIGN lang_exp                 { $$ = MkLangDefDef($1, $3, $5); }
   | ID COLON ID RIGHT_ARROW ID ASSIGN trans_exp { $$ = MkTransDefDef($1, $3, $5, $7); }
   ;

fun_def 
   : ID fun_vars ID ASSIGN expr        { $$ = MkFunctionDef($1, $2, $3, $5); }
   ; 
fun_vars 
   : COLON                             { $$ = MkEmptyFastTokenPairList(); }
   | LPAR ID COLON ID RPAR fun_vars    { $$ = MkFastTokenPairList($2, $4, $6); }
   ;

const_def 
   : ID COLON ID ASSIGN expr           { $$ = MkConstDef($1, $3, $5); }
   ;

alphabet_def 
   : ID LBRACE constr_defs                    { $$ = MkAlphabetDef($1, MkEmptyFastTokenPairList(), $3); }
   | ID LBRACKET attr_defs LBRACE constr_defs { $$ = MkAlphabetDef($1, $3, $5); }
   ;
attr_defs 
   : RBRACKET                            { $$ = MkEmptyFastTokenPairList(); }
   | ID COLON ID attr_defs_rest          { $$ = MkFastTokenPairList($1, $3, $4); }
   ;
attr_defs_rest 
   : RBRACKET                            { $$ = MkEmptyFastTokenPairList(); }
   | COMMA ID COLON ID attr_defs_rest    { $$ = MkFastTokenPairList($2, $4, $5); }
   ;
constr_defs 
   : RBRACE                                 { $$ = MkEmptyFastTokenPairList(); }
   | ID RBRACE                              { $$ = MkFastTokenPairList($1, MkZero(), MkEmptyFastTokenPairList()); }
   | ID COMMA constr_defs                   { $$ = MkFastTokenPairList($1, MkZero(), $3); }
   | ID LPAR NUMBER RPAR RBRACE             { $$ = MkFastTokenPairList($1, $3, MkEmptyFastTokenPairList()); }
   | ID LPAR NUMBER RPAR COMMA constr_defs  { $$ = MkFastTokenPairList($1, $3, $6); }
   ;

lang_def 
   : ID LPAR ID COLON ID RPAR LBRACE guarded_expr guarded_exprs { $$ = MkLangDef($1, $5, $3, MkGuardedExpList($8, $9)); }
   | ID COLON ID LBRACE guarded_expr guarded_exprs              { $$ = MkLangDef($1, $3, MkGuardedExpList($5, $6)); }
   ;

guarded_exprs 
   : RBRACE                         { $$ = MkEmptyGuardedExpList(); }
   | BAR guarded_expr guarded_exprs { $$ = MkGuardedExpList($2, $3); }
   ;

trans_def 
   : ID LPAR ID COLON ID RPAR COLON ID LBRACE guarded_trans_expr guarded_trans_exprs { $$ = MkTransDef($1, $5, $8, $3, MkGuardedExpList($10, $11)); }
   | ID COLON ID RIGHT_ARROW ID LBRACE guarded_trans_expr guarded_trans_exprs        { $$ = MkTransDef($1, $3, $5, MkGuardedExpList($7, $8)); }
   ;

guarded_expr 
   : pattern guarded_expr1             { $$ = MkGuardedExp($1, ((object[])$2)[0], ((object[])$2)[1]); }
   ;
guarded_expr1 
   : WHERE expr guarded_expr2          { $$ = new object[]{$2, $3}; } 
   | guarded_expr2                     { $$ = new object[]{null, $1}; }
   ;
guarded_expr2 
   : GIVEN LPAR ID ID RPAR given_exprs { $$ = MkFExpList(MkAppExp($3, MkFExpList(MkId($4),MkEmptyFExpList())), $6);  }
   | nothing                           { $$ = null;}
   ;
given_exprs 
   : LPAR ID ID RPAR given_exprs       { $$ = MkFExpList(MkAppExp($2, MkFExpList(MkId($3),MkEmptyFExpList())), $5); }
   | nothing                           { $$ = MkEmptyFExpList(); }
   ;
nothing 
   :
   ;

guarded_trans_exprs 
   : RBRACE                                     { $$ = MkEmptyGuardedExpList(); }
   | BAR guarded_trans_expr guarded_trans_exprs { $$ = MkGuardedExpList($2, $3); }
   ;
guarded_trans_expr 
   : pattern guarded_expr1 TO expr { $$ = MkGuardedExp($1, ((object[])$2)[0], ((object[])$2)[1], $4); }
   ;

pattern 
   : ID LPAR pattern_attrs         { $$ = MkPattern($1, $3); }
   ;
pattern_attrs 
   : RPAR                          { $$ = MkEmptyTokenList(); }
   | ID pattern_attrs_rest         { $$ = MkTokenList($1, $2); }
   ;
pattern_attrs_rest  
   : RPAR                          { $$ = MkEmptyTokenList(); }
   | COMMA ID pattern_attrs_rest   { $$ = MkTokenList($2, $3); }
   ;
  
expr 
   : STRING                  { $$ = MkStringValue($1); }
   | NUMBER                  { $$ = MkNumericValue($1); }
   | CHAR                    { $$ = MkNumericValue($1); }
   | TRUE                    { $$ = MkBoolValue($1); }
   | FALSE                   { $$ = MkBoolValue($1); }
   | ID                      { $$ = MkId($1); }
   | LBRACKET expr_list      { $$ = MkRecordExp($1, $2); }
   | expr log_conn expr      { $$ = MkAppExp($2, MkFExpListFromElems($1, $3)); }
   | expr arithm_rel expr    { $$ = MkAppExp($2, MkFExpListFromElems($1, $3)); }
   | expr arithm_op_pm expr  { $$ = MkAppExp($2, MkFExpListFromElems($1, $3)); }
   | expr arithm_op_td expr  { $$ = MkAppExp($2, MkFExpListFromElems($1, $3)); }
   | expr arithm_op expr     { $$ = MkAppExp($2, MkFExpListFromElems($1, $3)); }
   | uop expr                { $$ = MkAppExp($1, MkFExpListFromElems($2)); }
   | expr ITE expr COLON expr{ $$ = MkAppExp($2, MkFExpListFromElems($1, $3, $5)); }
   | LPAR expr RPAR          { $$ = $2; }
   | LPAR ID expr_args       { $$ = MkAppExp($2, $3); }
   | LPAR expr BAR expr RPAR { $$ = MkAppExp($3, MkFExpListFromElems($2, $4)); } //BVOR
   ;

expr_args 
   : RPAR            { $$ = MkEmptyFExpList(); }
   | expr expr_args  { $$ = MkFExpList($1, $2); }
   ;

log_conn 
   : OR | AND | IMPLIES
   ;

arithm_rel 
   : LT | GT | LE | GE | EQ | NE
   ;

arithm_op_pm
   : PLUS | MINUS
   ; 

arithm_op_td
   : TIMES | DIV
   ;

arithm_op
   :  MOD | SHL | SHR
   ;

uop 
   :  NOT | BVNOT
   ;

expr_list 
   : RBRACKET             { $$ = MkEmptyFExpList(); }
   | expr COMMA expr_list { $$ = MkFExpList($1, $3); }
   | expr RBRACKET        { $$ = MkFExpList($1, MkEmptyFExpList()); }
   ;

query_expr 
   : STRING               { $$ = MkStringQueryDef($1); }
   | ID                   { $$ = MkDisplayQueryDef($1); }
   | LPAR GEN_CSHARP RPAR { $$ = MkGenCodeQueryDef($2); }
   | bool_query_expr      { $$ = $1; }
   ;

bool_query_expr 
   : LPAR CONTAINS lang_exp expr RPAR                { $$ = MkContainsQueryDef($2, $3, $4); }
   | LPAR IS_EMPTY_LANG lang_exp RPAR                { $$ = MkIsEmptyLangQueryDef($2, $3); }
   | LPAR IS_EMPTY_TRANS trans_exp RPAR              { $$ = MkIsEmptyTransQueryDef($2, $3); }
   | LPAR TYPECHECK lang_exp trans_exp lang_exp RPAR { $$ = MkTypecheckQueryDef($2, $3, $4, $5); }
   | LPAR EQ_LANG lang_exp lang_exp RPAR             { $$ = MkLangEquivQueryDef($2, $3, $4); }
   | LPAR EQ_TRANS trans_exp trans_exp RPAR          { $$ = MkTransEquivQueryDef($2, $3, $4); }
   ;
  
trans_exp 
   : LPAR COMPOSE trans_exp trans_exp RPAR           { $$ = MkCompositionExp($2, $3, $4); }
   | LPAR RESTRICT_INP trans_exp lang_exp RPAR       { $$ = MkRestrictionInpExp($2, $3, $4); }
   | LPAR RESTRICT_OUT trans_exp lang_exp RPAR       { $$ = MkRestrictionOutExp($2, $3, $4); }
   | ID                                              { $$ = MkTransNameExp($1); }
   ;
  
lang_exp 
   : LPAR INTERSECT lang_exp lang_exp RPAR           { $$ = MkIntersectionExp($2, $3, $4); }
   | LPAR DIFFERENCE lang_exp lang_exp RPAR          { $$ = MkDifferenceExp($2, $3, $4); }
   | LPAR UNION lang_exp lang_exp RPAR               { $$ = MkUnionExp($2, $3, $4); }
   | LPAR DOMAIN trans_exp RPAR                      { $$ = MkDomainExp($2, $3); } 
   | LPAR PRE_IMAGE trans_exp lang_exp RPAR          { $$ = MkPreimageExp($2, $3, $4); }
   | LPAR COMPLEMENT lang_exp RPAR                   { $$ = MkComplementExp($2, $3); }
   | LPAR MINIMIZE lang_exp RPAR                     { $$ = MkMinimizeExp($2, $3); }
   | ID                                              { $$ = MkLangNameExp($1); }
   ; 

%%
