lexer grammar STLexer;

// ===== 키워드 (Keywords) =====

// 프로그램 구조
PROGRAM         : 'PROGRAM' ;
FUNCTION        : 'FUNCTION' ;
FUNCTION_BLOCK  : 'FUNCTION_BLOCK' ;
END_PROGRAM     : 'END_PROGRAM' ;
END_FUNCTION    : 'END_FUNCTION' ;
END_FUNCTION_BLOCK : 'END_FUNCTION_BLOCK' ;

// 변수 선언
VAR             : 'VAR' ;
VAR_INPUT       : 'VAR_INPUT' ;
VAR_OUTPUT      : 'VAR_OUTPUT' ;
VAR_IN_OUT      : 'VAR_IN_OUT' ;
VAR_TEMP        : 'VAR_TEMP' ;
VAR_GLOBAL      : 'VAR_GLOBAL' ;
VAR_EXTERNAL    : 'VAR_EXTERNAL' ;
VAR_STAT        : 'VAR_STAT' ;
END_VAR         : 'END_VAR' ;
CONSTANT        : 'CONSTANT' ;
RETAIN          : 'RETAIN' ;
PERSISTENT      : 'PERSISTENT' ;

// 데이터 타입
BOOL            : 'BOOL' ;
BYTE            : 'BYTE' ;
WORD            : 'WORD' ;
DWORD           : 'DWORD' ;
LWORD           : 'LWORD' ;
SINT            : 'SINT' ;
INT             : 'INT' ;
DINT            : 'DINT' ;
LINT            : 'LINT' ;
USINT           : 'USINT' ;
UINT            : 'UINT' ;
UDINT           : 'UDINT' ;
ULINT           : 'ULINT' ;
REAL            : 'REAL' ;
LREAL           : 'LREAL' ;
STRING          : 'STRING' ;
WSTRING         : 'WSTRING' ;
TIME            : 'TIME' ;
DATE            : 'DATE' ;
DATE_AND_TIME   : 'DATE_AND_TIME' ;
DT              : 'DT' ;
TIME_OF_DAY     : 'TIME_OF_DAY' ;
TOD             : 'TOD' ;
POINTER         : 'POINTER' ;
REFERENCE       : 'REFERENCE' ;

// 제어문
IF              : 'IF' ;
THEN            : 'THEN' ;
ELSIF           : 'ELSIF' ;
ELSE            : 'ELSE' ;
END_IF          : 'END_IF' ;
CASE            : 'CASE' ;
OF              : 'OF' ;
END_CASE        : 'END_CASE' ;
FOR             : 'FOR' ;
TO              : 'TO' ;
BY              : 'BY' ;
DO              : 'DO' ;
END_FOR         : 'END_FOR' ;
WHILE           : 'WHILE' ;
END_WHILE       : 'END_WHILE' ;
REPEAT          : 'REPEAT' ;
UNTIL           : 'UNTIL' ;
END_REPEAT      : 'END_REPEAT' ;
EXIT            : 'EXIT' ;
RETURN          : 'RETURN' ;
CONTINUE        : 'CONTINUE' ;

// 구조체 및 열거형
STRUCT          : 'STRUCT' ;
END_STRUCT      : 'END_STRUCT' ;
TYPE            : 'TYPE' ;
END_TYPE        : 'END_TYPE' ;
ENUM            : 'ENUM' ;
END_ENUM        : 'END_ENUM' ;
ARRAY           : 'ARRAY' ;
UNION           : 'UNION' ;
END_UNION       : 'END_UNION' ;

// 객체지향
CLASS           : 'CLASS' ;
END_CLASS       : 'END_CLASS' ;
INTERFACE       : 'INTERFACE' ;
END_INTERFACE   : 'END_INTERFACE' ;
METHOD          : 'METHOD' ;
END_METHOD      : 'END_METHOD' ;
PROPERTY        : 'PROPERTY' ;
END_PROPERTY    : 'END_PROPERTY' ;
THIS            : 'THIS' ;
SUPER           : 'SUPER' ;
ABSTRACT        : 'ABSTRACT' ;
FINAL           : 'FINAL' ;
IMPLEMENTS      : 'IMPLEMENTS' ;
EXTENDS         : 'EXTENDS' ;

// 액션 한정자 (Access Modifiers & Qualifiers)
CONST           : 'CONST' ;
EXPLICIT        : 'EXPLICIT' ;
IMPLICIT        : 'IMPLICIT' ;
INTERNAL        : 'INTERNAL' ;
OVERRIDE        : 'OVERRIDE' ;
PRIVATE         : 'PRIVATE' ;
PROTECTED       : 'PROTECTED' ;
PUBLIC          : 'PUBLIC' ;
STATIC          : 'STATIC' ;
VIRTUAL         : 'VIRTUAL' ;

// 프라그마/속성 (Pragmas & Attributes)
ALIGNED         : 'ALIGNED' ;
ATTRIBUTE       : 'ATTRIBUTE' ;
PACK_MODE       : 'PACK_MODE' ;
QUALIFIED_ONLY  : 'QUALIFIED_ONLY' ;
STRICT          : 'STRICT' ;

// 메모리 접근 (Memory Access)
KW_DELETE       : '__DELETE' ;
KW_NEW          : '__NEW' ;
MEMCMP          : 'MEMCMP' ;
MEMCPY          : 'MEMCPY' ;
MEMSET          : 'MEMSET' ;

// 고급 연산자 (Advanced Operators)
ROL             : 'ROL' ;
ROR             : 'ROR' ;
SEL             : 'SEL' ;
SHL             : 'SHL' ;
SHR             : 'SHR' ;

// 시스템 함수 (System Functions)
CONCAT          : 'CONCAT' ;
DELETE          : 'DELETE' ;
FIND            : 'FIND' ;
INSERT          : 'INSERT' ;
REPLACE         : 'REPLACE' ;

// 논리 연산자
AND             : 'AND' ;
OR              : 'OR' ;
XOR             : 'XOR' ;
NOT             : 'NOT' ;
MOD             : 'MOD' ;

// 특수 키워드
TRUE            : 'TRUE' ;
FALSE           : 'FALSE' ;
NULL            : 'NULL' ;
AT              : 'AT' ;
REF             : 'REF' ;
REF_TO          : 'REF_TO' ;
ADR             : 'ADR' ;
SIZEOF          : 'SIZEOF' ;

// ===== 연산자 (Operators) =====

// 할당
ASSIGN          : ':=' ;

// 비교
EQ              : '=' ;
NE              : '<>' ;
LT              : '<' ;
LE              : '<=' ;
GT              : '>' ;
GE              : '>=' ;

// 산술
PLUS            : '+' ;
MINUS           : '-' ;
STAR            : '*' ;
SLASH           : '/' ;
POWER           : '**' ;

// 비트 연산
BIT_AND         : '&' ;
BIT_OR          : '|' ;
BIT_XOR         : '^' ;
BIT_NOT         : '~' ;
LSHIFT          : '<<' ;
RSHIFT          : '>>' ;

// 구분자
LPAREN          : '(' ;
RPAREN          : ')' ;
LBRACK          : '[' ;
RBRACK          : ']' ;
DOT             : '.' ;
COMMA           : ',' ;
COLON           : ':' ;
SEMICOLON       : ';' ;
DOTDOT          : '..' ;
POUND           : '#' ;
CARET           : '^' ;

// ===== 리터럴 (Literals) =====

// 정수 리터럴 (16진수, 8진수, 2진수 포함)
INTEGER_LITERAL
    : DECIMAL_LITERAL
    | HEX_LITERAL
    | OCTAL_LITERAL
    | BINARY_LITERAL
    ;

fragment DECIMAL_LITERAL : [0-9]+ ('_' [0-9]+)* ;
fragment HEX_LITERAL     : '16#' [0-9A-Fa-f]+ ('_' [0-9A-Fa-f]+)* ;
fragment OCTAL_LITERAL   : '8#' [0-7]+ ('_' [0-7]+)* ;
fragment BINARY_LITERAL  : '2#' [01]+ ('_' [01]+)* ;

// 부동소수점 리터럴
REAL_LITERAL
    : [0-9]+ ('_' [0-9]+)* '.' [0-9]+ ('_' [0-9]+)* ([Ee] [+\-]? [0-9]+)?
    | [0-9]+ ('_' [0-9]+)* [Ee] [+\-]? [0-9]+
    ;

// 시간 리터럴
TIME_LITERAL
    : 'T#' TIME_VALUE
    | 'TIME#' TIME_VALUE
    | 't#' TIME_VALUE
    | 'time#' TIME_VALUE
    ;

fragment TIME_VALUE
    : [0-9]+ ('.' [0-9]+)? ('d' | 'D')  // 일
    | [0-9]+ ('.' [0-9]+)? ('h' | 'H')  // 시간
    | [0-9]+ ('.' [0-9]+)? ('m' | 'M')  // 분
    | [0-9]+ ('.' [0-9]+)? ('s' | 'S')  // 초
    | [0-9]+ ('.' [0-9]+)? ('ms' | 'MS') // 밀리초
    | [0-9]+ ('.' [0-9]+)? ('us' | 'US') // 마이크로초
    | [0-9]+ ('.' [0-9]+)? ('ns' | 'NS') // 나노초
    | ([0-9]+ 'd')? ([0-9]+ 'h')? ([0-9]+ 'm')? ([0-9]+ 's')? ([0-9]+ 'ms')? // 복합
    ;

// 날짜 리터럴
DATE_LITERAL
    : 'D#' DATE_VALUE
    | 'DATE#' DATE_VALUE
    | 'd#' DATE_VALUE
    | 'date#' DATE_VALUE
    ;

fragment DATE_VALUE : [0-9][0-9][0-9][0-9] '-' [0-9][0-9] '-' [0-9][0-9] ;

// 날짜+시간 리터럴
DATETIME_LITERAL
    : 'DT#' DATETIME_VALUE
    | 'DATE_AND_TIME#' DATETIME_VALUE
    | 'dt#' DATETIME_VALUE
    ;

fragment DATETIME_VALUE : DATE_VALUE '-' [0-9][0-9] ':' [0-9][0-9] ':' [0-9][0-9] ('.' [0-9]+)? ;

// 시각 리터럴
TIMEOFDAY_LITERAL
    : 'TOD#' TOD_VALUE
    | 'TIME_OF_DAY#' TOD_VALUE
    | 'tod#' TOD_VALUE
    ;

fragment TOD_VALUE : [0-9][0-9] ':' [0-9][0-9] ':' [0-9][0-9] ('.' [0-9]+)? ;

// 문자열 리터럴
STRING_LITERAL
    : '\'' ( ESC_SEQ | ~['\r\n\\] )* '\''
    | '"' ( ESC_SEQ | ~["\r\n\\] )* '"'
    ;

fragment ESC_SEQ
    : '\\' ['"\\nrtfbav]
    | '\\' [0-7] [0-7] [0-7]
    | '\\x' [0-9A-Fa-f] [0-9A-Fa-f]
    ;

// ===== 식별자 (Identifier) =====

IDENTIFIER
    : [a-zA-Z_][a-zA-Z0-9_]*
    ;

// ===== 주석 (Comments) =====

// 라인 주석
LINE_COMMENT
    : '//' ~[\r\n]* -> channel(HIDDEN)
    ;

// 블록 주석
BLOCK_COMMENT
    : '(*' .*? '*)' -> channel(HIDDEN)
    ;

// C 스타일 블록 주석 (TwinCAT 3 지원)
C_BLOCK_COMMENT
    : '/*' .*? '*/' -> channel(HIDDEN)
    ;

// ===== 공백 (Whitespace) =====

WHITESPACE
    : [ \t\r\n]+ -> skip
    ;

// ===== 전처리기 지시문 (Preprocessor) =====

PRAGMA
    : '{' ~[}]* '}' -> channel(HIDDEN)
    ;
