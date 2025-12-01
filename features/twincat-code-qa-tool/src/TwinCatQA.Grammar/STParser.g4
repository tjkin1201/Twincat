parser grammar STParser;

options {
    tokenVocab = STLexer;
}

// ===== 최상위 컴파일 유닛 (Compilation Unit) =====

// 전체 파일: 여러 선언들로 구성
compilationUnit
    : declaration* EOF
    ;

// 선언: 프로그램, 함수, 함수블록, 타입, 글로벌 변수 등
declaration
    : programDeclaration
    | functionDeclaration
    | functionBlockDeclaration
    | classDeclaration
    | interfaceDeclaration
    | typeDeclaration
    | globalVariableDeclaration
    ;

// ===== 프로그램 구조 (Program Structure) =====

// PROGRAM 선언
programDeclaration
    : PROGRAM identifier
      variableDeclarations?
      functionBody
      END_PROGRAM
    ;

// FUNCTION 선언
functionDeclaration
    : FUNCTION accessModifier? identifier COLON dataType
      variableDeclarations?
      functionBody
      END_FUNCTION
    ;

// FUNCTION_BLOCK 선언
functionBlockDeclaration
    : FUNCTION_BLOCK accessModifier? identifier extendsClause? implementsClause?
      variableDeclarations?
      methodDeclaration*
      propertyDeclaration*
      functionBody?
      END_FUNCTION_BLOCK
    ;

// CLASS 선언 (TwinCAT 3 OOP)
classDeclaration
    : CLASS accessModifier? classModifier* identifier extendsClause? implementsClause?
      variableDeclarations?
      methodDeclaration*
      propertyDeclaration*
      END_CLASS
    ;

// INTERFACE 선언
interfaceDeclaration
    : INTERFACE identifier extendsClause?
      methodSignature*
      propertySignature*
      END_INTERFACE
    ;

// METHOD 선언
methodDeclaration
    : METHOD accessModifier? methodModifier* identifier (COLON dataType)?
      variableDeclarations?
      functionBody
      END_METHOD
    ;

// METHOD 시그니처 (인터페이스용)
methodSignature
    : METHOD identifier (COLON dataType)?
      variableDeclarations?
    ;

// PROPERTY 선언
propertyDeclaration
    : PROPERTY accessModifier? identifier COLON dataType
      (getAccessor setAccessor? | setAccessor getAccessor?)?
      END_PROPERTY
    ;

// PROPERTY 시그니처
propertySignature
    : PROPERTY identifier COLON dataType
    ;

// GET 접근자
getAccessor
    : variableDeclarations?
      functionBody
    ;

// SET 접근자
setAccessor
    : variableDeclarations?
      functionBody
    ;

// ===== 변수 선언 (Variable Declarations) =====

// 여러 변수 선언 블록
variableDeclarations
    : variableDeclaration+
    ;

// 단일 변수 선언 블록
variableDeclaration
    : VAR varAttribute* varDeclList END_VAR
    | VAR_INPUT varAttribute* varDeclList END_VAR
    | VAR_OUTPUT varAttribute* varDeclList END_VAR
    | VAR_IN_OUT varAttribute* varDeclList END_VAR
    | VAR_TEMP varAttribute* varDeclList END_VAR
    | VAR_GLOBAL varAttribute* varDeclList END_VAR
    | VAR_EXTERNAL varAttribute* varDeclList END_VAR
    | VAR_STAT varAttribute* varDeclList END_VAR
    ;

// 글로벌 변수 선언 (파일 레벨)
globalVariableDeclaration
    : VAR_GLOBAL varAttribute* varDeclList END_VAR
    ;

// 변수 속성 (RETAIN, PERSISTENT 등)
varAttribute
    : CONSTANT
    | RETAIN
    | PERSISTENT
    ;

// 변수 선언 목록
varDeclList
    : varDecl (SEMICOLON varDecl)* SEMICOLON?
    ;

// 개별 변수 선언
varDecl
    : identifier (COMMA identifier)* COLON dataType (locationSpec)? (ASSIGN initialValue)?
    ;

// 메모리 위치 지정 (AT %IX1.0 등)
locationSpec
    : AT identifier
    ;

// 초기값
initialValue
    : expression
    | structInitializer
    | arrayInitializer
    ;

// 구조체 초기화
structInitializer
    : LPAREN structElementInit (COMMA structElementInit)* RPAREN
    ;

// 구조체 요소 초기화
structElementInit
    : identifier ASSIGN initialValue
    ;

// 배열 초기화
arrayInitializer
    : LBRACK (initialValue (COMMA initialValue)*)? RBRACK
    | LBRACK expression LPAREN initialValue RPAREN RBRACK  // 반복 초기화: [10(0)]
    ;

// ===== 데이터 타입 (Data Types) =====

// 모든 데이터 타입
dataType
    : primitiveType
    | stringTypeWithLength
    | arrayType
    | pointerType
    | referenceType
    | identifier  // 사용자 정의 타입
    ;

// 기본 데이터 타입
primitiveType
    : BOOL | BYTE | WORD | DWORD | LWORD
    | SINT | INT | DINT | LINT
    | USINT | UINT | UDINT | ULINT
    | REAL | LREAL
    | STRING | WSTRING
    | TIME | DATE | DT | DATE_AND_TIME | TOD | TIME_OF_DAY
    ;

// 길이가 지정된 문자열
stringTypeWithLength
    : STRING LBRACK INTEGER_LITERAL RBRACK
    | WSTRING LBRACK INTEGER_LITERAL RBRACK
    ;

// 배열 타입
arrayType
    : ARRAY LBRACK arrayRange (COMMA arrayRange)* RBRACK OF dataType
    ;

// 배열 범위
arrayRange
    : expression DOTDOT expression
    ;

// 포인터 타입 (POINTER TO)
pointerType
    : POINTER TO dataType
    ;

// 참조 타입 (REFERENCE TO)
referenceType
    : REFERENCE TO dataType
    | REF_TO dataType
    ;

// ===== 타입 선언 (Type Declarations) =====

// 타입 정의
typeDeclaration
    : TYPE typeDefinition+ END_TYPE
    ;

// 타입 정의 항목
typeDefinition
    : identifier COLON typeSpec SEMICOLON?
    ;

// 타입 스펙
typeSpec
    : structType
    | enumType
    | unionType
    | dataType
    ;

// 구조체 타입
structType
    : STRUCT varAttribute*
      varDeclList
      END_STRUCT
    ;

// 열거형 타입
enumType
    : ENUM typeSpec?
      enumValueList
      END_ENUM
    ;

// 열거값 목록
enumValueList
    : enumValue (COMMA enumValue)* COMMA?
    ;

// 개별 열거값
enumValue
    : identifier (ASSIGN INTEGER_LITERAL)?
    ;

// 공용체 타입 (Union)
unionType
    : UNION
      varDeclList
      END_UNION
    ;

// ===== 함수 본문 (Function Body) =====

// 함수 본문: 명령문들의 시퀀스
functionBody
    : statementList?
    ;

// 명령문 목록
statementList
    : statement+
    ;

// ===== 명령문 (Statements) =====

// 개별 명령문
statement
    : assignmentStatement
    | functionCallStatement
    | ifStatement
    | caseStatement
    | forStatement
    | whileStatement
    | repeatStatement
    | returnStatement
    | exitStatement
    | continueStatement
    | emptyStatement
    ;

// 빈 명령문
emptyStatement
    : SEMICOLON
    ;

// 할당 명령문
assignmentStatement
    : reference ASSIGN expression SEMICOLON
    ;

// 함수 호출 명령문
functionCallStatement
    : functionCall SEMICOLON
    ;

// IF 명령문
ifStatement
    : IF expression THEN
      statementList?
      (ELSIF expression THEN statementList?)*
      (ELSE statementList?)?
      END_IF SEMICOLON?
    ;

// CASE 명령문
caseStatement
    : CASE expression OF
      caseElement+
      (ELSE COLON? statementList?)?
      END_CASE SEMICOLON?
    ;

// CASE 요소
caseElement
    : caseList COLON statementList?
    ;

// CASE 레이블 목록
caseList
    : caseRange (COMMA caseRange)*
    ;

// CASE 범위
caseRange
    : expression (DOTDOT expression)?
    ;

// FOR 루프
forStatement
    : FOR identifier ASSIGN expression TO expression (BY expression)? DO
      statementList?
      END_FOR SEMICOLON?
    ;

// WHILE 루프
whileStatement
    : WHILE expression DO
      statementList?
      END_WHILE SEMICOLON?
    ;

// REPEAT 루프
repeatStatement
    : REPEAT
      statementList?
      UNTIL expression
      END_REPEAT SEMICOLON?
    ;

// RETURN 명령문
returnStatement
    : RETURN SEMICOLON
    ;

// EXIT 명령문 (루프 탈출)
exitStatement
    : EXIT SEMICOLON
    ;

// CONTINUE 명령문 (다음 반복으로)
continueStatement
    : CONTINUE SEMICOLON
    ;

// ===== 표현식 (Expressions) =====

// 최상위 표현식
expression
    : logicalOrExpression
    ;

// 논리 OR 표현식
logicalOrExpression
    : logicalXorExpression (OR logicalXorExpression)*
    ;

// 논리 XOR 표현식
logicalXorExpression
    : logicalAndExpression (XOR logicalAndExpression)*
    ;

// 논리 AND 표현식
logicalAndExpression
    : comparisonExpression (AND comparisonExpression)*
    ;

// 비교 표현식
comparisonExpression
    : bitwiseOrExpression ((EQ | NE | LT | LE | GT | GE) bitwiseOrExpression)?
    ;

// 비트 OR 표현식
bitwiseOrExpression
    : bitwiseXorExpression (BIT_OR bitwiseXorExpression)*
    ;

// 비트 XOR 표현식
bitwiseXorExpression
    : bitwiseAndExpression (BIT_XOR bitwiseAndExpression)*
    ;

// 비트 AND 표현식
bitwiseAndExpression
    : shiftExpression (BIT_AND shiftExpression)*
    ;

// 시프트 표현식
shiftExpression
    : additiveExpression ((LSHIFT | RSHIFT | SHL | SHR | ROL | ROR) additiveExpression)*
    ;

// 덧셈/뺄셈 표현식
additiveExpression
    : multiplicativeExpression ((PLUS | MINUS) multiplicativeExpression)*
    ;

// 곱셈/나눗셈 표현식
multiplicativeExpression
    : exponentialExpression ((STAR | SLASH | MOD) exponentialExpression)*
    ;

// 거듭제곱 표현식
exponentialExpression
    : unaryExpression (POWER unaryExpression)?
    ;

// 단항 표현식
unaryExpression
    : (PLUS | MINUS | NOT | BIT_NOT)? primaryExpression
    ;

// 기본 표현식
primaryExpression
    : literal
    | reference
    | functionCall
    | LPAREN expression RPAREN
    | castExpression
    | sizeofExpression
    | adrExpression
    | newExpression
    | deleteExpression
    ;

// 참조 (변수, 배열, 구조체 필드 등)
reference
    : (THIS DOT)? identifier referenceModifier*
    | SUPER DOT identifier referenceModifier*
    ;

// 참조 수정자 (배열 인덱스, 구조체 필드, 포인터 역참조)
referenceModifier
    : DOT identifier
    | LBRACK expression (COMMA expression)* RBRACK
    | CARET
    ;

// 함수 호출
functionCall
    : identifier LPAREN argumentList? RPAREN
    ;

// 인수 목록
argumentList
    : argument (COMMA argument)*
    ;

// 인수 (위치 인수 또는 명명된 인수)
argument
    : (identifier ASSIGN)? expression
    ;

// 타입 캐스팅
castExpression
    : identifier POUND literal
    | identifier POUND LPAREN expression RPAREN
    ;

// SIZEOF 표현식
sizeofExpression
    : SIZEOF LPAREN dataType RPAREN
    | SIZEOF LPAREN identifier RPAREN
    ;

// ADR 표현식 (주소 얻기)
adrExpression
    : ADR LPAREN reference RPAREN
    ;

// NEW 표현식 (동적 메모리 할당)
newExpression
    : KW_NEW LPAREN dataType (COMMA expression)? RPAREN
    ;

// DELETE 표현식 (동적 메모리 해제)
deleteExpression
    : KW_DELETE LPAREN reference RPAREN
    ;

// ===== 리터럴 (Literals) =====

// 모든 리터럴 값
literal
    : INTEGER_LITERAL
    | REAL_LITERAL
    | STRING_LITERAL
    | TIME_LITERAL
    | DATE_LITERAL
    | DATETIME_LITERAL
    | TIMEOFDAY_LITERAL
    | booleanLiteral
    | nullLiteral
    ;

// 불린 리터럴
booleanLiteral
    : TRUE
    | FALSE
    ;

// NULL 리터럴
nullLiteral
    : NULL
    ;

// ===== 수정자 및 절 (Modifiers & Clauses) =====

// 접근 수정자
accessModifier
    : PUBLIC
    | PRIVATE
    | PROTECTED
    | INTERNAL
    ;

// 클래스 수정자
classModifier
    : ABSTRACT
    | FINAL
    | STATIC
    ;

// 메서드 수정자
methodModifier
    : ABSTRACT
    | FINAL
    | OVERRIDE
    | VIRTUAL
    | STATIC
    ;

// EXTENDS 절
extendsClause
    : EXTENDS identifier
    ;

// IMPLEMENTS 절
implementsClause
    : IMPLEMENTS identifier (COMMA identifier)*
    ;

// ===== 식별자 (Identifier) =====

// 식별자 (변수명, 함수명 등)
identifier
    : IDENTIFIER
    ;
