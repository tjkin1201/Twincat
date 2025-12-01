grammar StructuredText;

// ============================================================================
// IEC 61131-3 Structured Text 문법 정의
// ============================================================================
// 목적: TwinCAT ST 코드 파싱 및 AST 생성
// 참조: IEC 61131-3 표준, TwinCAT 3 확장
// 작성일: 2025-11-20
// ============================================================================

// ----------------------------------------------------------------------------
// 최상위 규칙
// ----------------------------------------------------------------------------

// 프로그램 진입점 - 선언부와 구현부로 구성
program
    : programUnit* EOF
    ;

programUnit
    : functionBlockDeclaration
    | functionDeclaration
    | programDeclaration
    | varDeclaration
    | dataTypeDeclaration
    ;

// ----------------------------------------------------------------------------
// 선언부 (Declaration Section)
// ----------------------------------------------------------------------------


// 변수 선언
varDeclaration
    : VAR varDeclList END_VAR
    | VAR_INPUT varDeclList END_VAR
    | VAR_OUTPUT varDeclList END_VAR
    | VAR_IN_OUT varDeclList END_VAR
    | VAR_TEMP varDeclList END_VAR
    | VAR CONSTANT varDeclList END_VAR
    | VAR_GLOBAL varDeclList END_VAR
    ;

varDeclList
    : varDecl*
    ;

varDecl
    : IDENTIFIER (',' IDENTIFIER)* ':' dataType (':=' expression)? ';'
    ;

// Function Block 선언
functionBlockDeclaration
    : FUNCTION_BLOCK IDENTIFIER
      varDeclaration*
      statement*
      END_FUNCTION_BLOCK
    ;

// Function 선언
functionDeclaration
    : FUNCTION IDENTIFIER ':' dataType
      varDeclaration*
      statement*
      END_FUNCTION
    ;

// Program 선언
programDeclaration
    : PROGRAM IDENTIFIER
      varDeclaration*
      statement*
      END_PROGRAM
    ;

// 데이터 타입 선언
dataTypeDeclaration
    : TYPE IDENTIFIER ':' structType END_TYPE
    ;

structType
    : STRUCT varDeclList END_STRUCT
    | '(' enumValue (',' enumValue)* ')'  // ENUM
    | UNION varDeclList END_UNION
    | dataType  // Alias
    ;

enumValue
    : IDENTIFIER (':=' INTEGER_LITERAL)?
    ;

// ----------------------------------------------------------------------------
// 구현부 (Implementation Section)
// ----------------------------------------------------------------------------

statement
    : assignmentStatement
    | functionCallStatement     // 함수/FB 호출 구문 (예: fbMotor(); TON_Instance(IN := TRUE);)
    | ifStatement
    | caseStatement
    | forStatement
    | whileStatement
    | repeatStatement
    | exitStatement
    | returnStatement
    | emptyStatement
    ;

// 함수/Function Block 호출 구문
functionCallStatement
    : functionCall ';'
    ;

// 빈 구문 (세미콜론만)
emptyStatement
    : ';'
    ;

// 할당문
assignmentStatement
    : variable ':=' expression ';'
    ;

// IF 문 (사이클로매틱 복잡도 +1)
ifStatement
    : IF expression THEN
      statement*
      (ELSIF expression THEN statement*)*  // 각 ELSIF마다 +1
      (ELSE statement*)?
      END_IF
    ;

// CASE 문 (사이클로매틱 복잡도 = 분기 수)
caseStatement
    : CASE expression OF
      caseElement+
      (ELSE statement*)?
      END_CASE
    ;

caseElement
    : constantExpression (',' constantExpression)* ':' statement*
    ;

// FOR 문 (사이클로매틱 복잡도 +1)
forStatement
    : FOR IDENTIFIER ':=' expression TO expression (BY expression)? DO
      statement*
      END_FOR
    ;

// WHILE 문 (사이클로매틱 복잡도 +1)
whileStatement
    : WHILE expression DO
      statement*
      END_WHILE
    ;

// REPEAT 문 (사이클로매틱 복잡도 +1)
repeatStatement
    : REPEAT
      statement*
      UNTIL expression
      END_REPEAT
    ;

// EXIT 문
exitStatement
    : EXIT ';'
    ;

// RETURN 문
returnStatement
    : RETURN ';'
    ;

// ----------------------------------------------------------------------------
// 표현식 (Expressions)
// ----------------------------------------------------------------------------

expression
    : literal                                           // 리터럴
    | variable                                          // 변수
    | functionCall                                      // 함수 호출
    | '(' expression ')'                                // 괄호
    | NOT expression                                    // 논리 NOT
    | '-' expression                                    // 단항 마이너스 (음수)
    | '+' expression                                    // 단항 플러스
    | expression op=('*'|'/'|MOD) expression            // 곱셈, 나눗셈
    | expression op=('+'|'-') expression                // 덧셈, 뺄셈
    | expression op=('<'|'<='|'>'|'>='|'='|'<>') expression  // 비교
    | expression op=(AND|'&') expression                // 논리 AND
    | expression op=OR expression                       // 논리 OR
    | expression op=XOR expression                      // 논리 XOR
    ;

constantExpression
    : literal
    | IDENTIFIER
    ;

// 변수 (구조체 필드, 배열 인덱스, 절대 어드레싱 지원)
variable
    : IDENTIFIER ('.' IDENTIFIER)* ('[' expression (',' expression)* ']')*
    | DIRECT_ADDRESS                // 절대 어드레싱 (%I, %Q, %M)
    ;

// 함수 호출
functionCall
    : IDENTIFIER '(' (argumentList)? ')'
    ;

argumentList
    : argument (',' argument)*
    ;

argument
    : (IDENTIFIER ':=')? expression  // Named parameter 지원
    ;

// ----------------------------------------------------------------------------
// 데이터 타입
// ----------------------------------------------------------------------------

dataType
    : primitiveType
    | IDENTIFIER  // 사용자 정의 타입
    | ARRAY '[' arrayRange (',' arrayRange)* ']' OF dataType
    | POINTER TO dataType
    | REFERENCE TO dataType
    ;

arrayRange
    : signedInteger '..' signedInteger
    ;

signedInteger
    : '-'? INTEGER_LITERAL
    ;

primitiveType
    : BOOL | BYTE | WORD | DWORD | LWORD
    | SINT | USINT | INT | UINT | DINT | UDINT | LINT | ULINT
    | REAL | LREAL
    | STRING ('(' INTEGER_LITERAL ')')?  // STRING(80)
    | WSTRING ('(' INTEGER_LITERAL ')')?
    | TIME | DATE | TIME_OF_DAY | TOD | DATE_AND_TIME | DT
    ;

// ----------------------------------------------------------------------------
// 리터럴
// ----------------------------------------------------------------------------

literal
    : INTEGER_LITERAL
    | REAL_LITERAL
    | STRING_LITERAL
    | WSTRING_LITERAL
    | BOOLEAN_LITERAL
    | TIME_LITERAL
    ;

// ----------------------------------------------------------------------------
// 키워드 토큰
// ----------------------------------------------------------------------------

// 프로그램 구조 키워드
FUNCTION_BLOCK  : 'FUNCTION_BLOCK' ;
END_FUNCTION_BLOCK : 'END_FUNCTION_BLOCK' ;
FUNCTION        : 'FUNCTION' ;
END_FUNCTION    : 'END_FUNCTION' ;
PROGRAM         : 'PROGRAM' ;
END_PROGRAM     : 'END_PROGRAM' ;

// 변수 선언 키워드
VAR             : 'VAR' ;
VAR_INPUT       : 'VAR_INPUT' ;
VAR_OUTPUT      : 'VAR_OUTPUT' ;
VAR_IN_OUT      : 'VAR_IN_OUT' ;
VAR_TEMP        : 'VAR_TEMP' ;
VAR_GLOBAL      : 'VAR_GLOBAL' ;
END_VAR         : 'END_VAR' ;
CONSTANT        : 'CONSTANT' ;

// 데이터 타입 키워드
TYPE            : 'TYPE' ;
END_TYPE        : 'END_TYPE' ;
STRUCT          : 'STRUCT' ;
END_STRUCT      : 'END_STRUCT' ;
UNION           : 'UNION' ;
END_UNION       : 'END_UNION' ;
ARRAY           : 'ARRAY' ;
OF              : 'OF' ;
POINTER         : 'POINTER' ;
REFERENCE       : 'REFERENCE' ;
TO              : 'TO' ;

// 제어 흐름 키워드
IF              : 'IF' ;
THEN            : 'THEN' ;
ELSIF           : 'ELSIF' ;
ELSE            : 'ELSE' ;
END_IF          : 'END_IF' ;
CASE            : 'CASE' ;
END_CASE        : 'END_CASE' ;
FOR             : 'FOR' ;
DO              : 'DO' ;
END_FOR         : 'END_FOR' ;
WHILE           : 'WHILE' ;
END_WHILE       : 'END_WHILE' ;
REPEAT          : 'REPEAT' ;
UNTIL           : 'UNTIL' ;
END_REPEAT      : 'END_REPEAT' ;
EXIT            : 'EXIT' ;
RETURN          : 'RETURN' ;
BY              : 'BY' ;

// 연산자 키워드
AND             : 'AND' ;
OR              : 'OR' ;
XOR             : 'XOR' ;
NOT             : 'NOT' ;
MOD             : 'MOD' ;

// 원시 타입 키워드
BOOL            : 'BOOL' ;
BYTE            : 'BYTE' ;
WORD            : 'WORD' ;
DWORD           : 'DWORD' ;
LWORD           : 'LWORD' ;
SINT            : 'SINT' ;
USINT           : 'USINT' ;
INT             : 'INT' ;
UINT            : 'UINT' ;
DINT            : 'DINT' ;
UDINT           : 'UDINT' ;
LINT            : 'LINT' ;
ULINT           : 'ULINT' ;
REAL            : 'REAL' ;
LREAL           : 'LREAL' ;
STRING          : 'STRING' ;
WSTRING         : 'WSTRING' ;
TIME            : 'TIME' ;
DATE            : 'DATE' ;
TIME_OF_DAY     : 'TIME_OF_DAY' ;
TOD             : 'TOD' ;
DATE_AND_TIME   : 'DATE_AND_TIME' ;
DT              : 'DT' ;

// 불리언 리터럴
BOOLEAN_LITERAL : TRUE | FALSE ;
TRUE            : 'TRUE' ;
FALSE           : 'FALSE' ;

// ----------------------------------------------------------------------------
// 리터럴 토큰
// ----------------------------------------------------------------------------

// 식별자 (변수명, 함수명 등)
IDENTIFIER
    : [a-zA-Z_][a-zA-Z0-9_]*
    ;

// 정수 리터럴 (10진수, 16진수, 8진수, 2진수)
INTEGER_LITERAL
    : DECIMAL_LITERAL
    | HEX_LITERAL
    | OCTAL_LITERAL
    | BINARY_LITERAL
    ;

fragment DECIMAL_LITERAL : [0-9]+ ;
fragment HEX_LITERAL     : '16#' [0-9A-Fa-f]+ ;
fragment OCTAL_LITERAL   : '8#' [0-7]+ ;
fragment BINARY_LITERAL  : '2#' [01]+ ;

// 실수 리터럴
REAL_LITERAL
    : [0-9]+ '.' [0-9]+ (EXPONENT)?
    | [0-9]+ EXPONENT
    ;

fragment EXPONENT : [eE] [+-]? [0-9]+ ;

// 문자열 리터럴
STRING_LITERAL
    : '\'' ( ~['\r\n\\] | '\\' . )* '\''
    ;

WSTRING_LITERAL
    : '"' ( ~["\r\n\\] | '\\' . )* '"'
    ;

// 시간 리터럴 (예: T#10s, TIME#1h30m)
TIME_LITERAL
    : ('TIME' | 'T' | 't') '#' TIME_VALUE
    ;

fragment TIME_VALUE
    : [0-9]+ ('d'|'h'|'m'|'s'|'ms') TIME_VALUE?
    ;

// 절대 어드레싱 토큰 (Direct Address)
// 예: %IX0.0, %QX1.5, %MB100, %IB256, %MD0, %MW10, %MX100.0
DIRECT_ADDRESS
    : '%' [IQMKiqmk] [XBWDLxbwdl]? [0-9]+ ('.' [0-9]+)?
    ;

// ----------------------------------------------------------------------------
// 주석 및 공백
// ----------------------------------------------------------------------------

// 블록 주석 (Pascal 스타일)
BLOCK_COMMENT
    : '(*' .*? '*)' -> channel(HIDDEN)
    ;

// 라인 주석 (C++ 스타일)
LINE_COMMENT
    : '//' ~[\r\n]* -> channel(HIDDEN)
    ;

// 공백 문자
WS
    : [ \t\r\n]+ -> skip
    ;

// ----------------------------------------------------------------------------
// 종료
// ----------------------------------------------------------------------------
