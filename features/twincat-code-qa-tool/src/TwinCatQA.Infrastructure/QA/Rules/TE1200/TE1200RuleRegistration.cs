using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.QA.Rules.TE1200;

/// <summary>
/// TE1200 Static Analysis 규칙 등록 헬퍼
/// Beckhoff TE1200 호환 180개 SA 규칙을 등록합니다
/// </summary>
public static class TE1200RuleRegistration
{
    /// <summary>
    /// 모든 TE1200 SA 규칙을 규칙 목록에 등록합니다
    /// </summary>
    /// <param name="rules">규칙을 추가할 목록</param>
    public static void RegisterAllSARules(List<IQARuleChecker> rules)
    {
        // SA0001-SA0030: 도달 불가능/미사용 코드, 기본 검사
        RegisterSA0001_SA0030(rules);

        // SA0031-SA0050: 미사용 변수, 연산, 매직 넘버
        RegisterSA0031_SA0050(rules);

        // SA0051-SA0070: 메트릭스, 주석, 포인터
        RegisterSA0051_SA0070(rules);

        // SA0071-SA0100: 명명 규칙, 타입 변환, 초기화
        RegisterSA0071_SA0100(rules);

        // SA0101-SA0130: 고급 검사, OOP, 동시성
        RegisterSA0101_SA0130(rules);

        // SA0131-SA0160: 안전, 병렬, IEC 준수
        RegisterSA0131_SA0160(rules);

        // SA0161-SA0180: 고급 분석, 성능, 문서화
        RegisterSA0161_SA0180(rules);
    }

    /// <summary>
    /// 카테고리별 규칙만 등록
    /// </summary>
    public static void RegisterByCategory(List<IQARuleChecker> rules, SARuleCategory category)
    {
        var allRules = GetAllSARules();
        rules.AddRange(allRules.Where(r => r.Category == category));
    }

    /// <summary>
    /// 심각도별 규칙만 등록
    /// </summary>
    public static void RegisterBySeverity(List<IQARuleChecker> rules, Domain.Models.QA.Severity severity)
    {
        var allRules = GetAllSARules();
        rules.AddRange(allRules.Where(r => r.Severity == severity));
    }

    /// <summary>
    /// 모든 SA 규칙 인스턴스 반환
    /// </summary>
    public static List<SARuleBase> GetAllSARules()
    {
        var rules = new List<SARuleBase>();

        // SA0001-SA0002
        rules.Add(new SA0001_UnreachableCode());
        rules.Add(new SA0002_EmptyObjects());

        // SA0003-SA0015
        rules.Add(new SA0003_EmptyStatements());
        rules.Add(new SA0004_MultipleWriteOnOutput());
        rules.Add(new SA0006_MultiTaskWriteAccess());
        rules.Add(new SA0007_AddressOfConstant());
        rules.Add(new SA0008_SubrangeTypeCheck());
        rules.Add(new SA0009_UnusedReturnValue());
        rules.Add(new SA0010_SingleElementArray());
        rules.Add(new SA0011_SingleMemberEnum());
        rules.Add(new SA0012_VariableCouldBeConstant());
        rules.Add(new SA0013_SameVariableName());
        rules.Add(new SA0014_InstanceAssignment());
        rules.Add(new SA0015_GlobalAccessInFBInit());

        // SA0016-SA0030
        rules.Add(new SA0016_GapsInStructures());
        rules.Add(new SA0017_IrregularPointerAssignment());
        rules.Add(new SA0018_UnusualBitAccess());
        rules.Add(new SA0019_ImplicitPointerConversion());
        rules.Add(new SA0020_TruncatedRealAssignment());
        rules.Add(new SA0021_AddressOfTemporary());
        rules.Add(new SA0022_NonRejectedReturnValue());
        rules.Add(new SA0023_ComplexReturnValue());
        rules.Add(new SA0024_UntypedLiterals());
        rules.Add(new SA0025_UnqualifiedEnumConstants());
        rules.Add(new SA0026_UseOfDirectAddresses());
        rules.Add(new SA0027_UnsafeTypeConversion());
        rules.Add(new SA0028_NestedComments());
        rules.Add(new SA0029_TODO_Comments());
        rules.Add(new SA0030_MissingErrorHandling());

        // SA0031-SA0050
        rules.Add(new SA0031_UnusedSignatures());
        rules.Add(new SA0032_UnusedEnumConstants());
        rules.Add(new SA0033_UnusedVariables());
        rules.Add(new SA0034_UnusedInputVariables());
        rules.Add(new SA0035_UnusedOutputVariables());
        rules.Add(new SA0036_UnusedInOutVariables());
        rules.Add(new SA0037_UnusedTempVariables());
        rules.Add(new SA0038_WriteOnlyVariables());
        rules.Add(new SA0039_ReadOnlyAsVariable());
        rules.Add(new SA0040_DivisionByZero());
        rules.Add(new SA0041_LoopInvariantCode());
        rules.Add(new SA0042_InconsistentNamespaceAccess());
        rules.Add(new SA0043_SuspiciousSemicolon());
        rules.Add(new SA0044_ParenthesisMismatch());
        rules.Add(new SA0045_AssignmentInCondition());
        rules.Add(new SA0046_UnnecessaryComparison());
        rules.Add(new SA0047_DuplicateCondition());
        rules.Add(new SA0048_InefficientStringConcat());
        rules.Add(new SA0049_MagicNumbers());
        rules.Add(new SA0050_ComplexExpression());

        // SA0051-SA0070
        rules.Add(new SA0051_FunctionTooLong());
        rules.Add(new SA0052_TooManyParameters());
        rules.Add(new SA0053_NestingTooDeep());
        rules.Add(new SA0054_CyclomaticComplexity());
        rules.Add(new SA0055_CognitiveComplexity());
        rules.Add(new SA0056_InsufficientComments());
        rules.Add(new SA0057_MissingHeaderComment());
        rules.Add(new SA0058_OutdatedComments());
        rules.Add(new SA0059_CommentedOutCode());
        rules.Add(new SA0060_IneffectiveOperation());
        rules.Add(new SA0061_SuspiciousPointerOperation());
        rules.Add(new SA0062_ConstantCondition());
        rules.Add(new SA0063_FloatEquality());
        rules.Add(new SA0064_SuspiciousPointerArithmetic());
        rules.Add(new SA0065_UninitializedVariable());
        rules.Add(new SA0066_ArrayOutOfBounds());
        rules.Add(new SA0067_GlobalInFunction());
        rules.Add(new SA0068_CircularReference());
        rules.Add(new SA0069_UnimplementedInterface());
        rules.Add(new SA0070_EmptyCaseBranch());

        // SA0071-SA0100
        rules.Add(new SA0071_MissingElse());
        rules.Add(new SA0072_CaseMissingDefault());
        rules.Add(new SA0073_VariableNamingViolation());
        rules.Add(new SA0074_FBNamingViolation());
        rules.Add(new SA0075_InterfaceNamingViolation());
        rules.Add(new SA0076_EnumNamingViolation());
        rules.Add(new SA0077_StructNamingViolation());
        rules.Add(new SA0078_ConstantNamingViolation());
        rules.Add(new SA0079_GlobalVarNamingViolation());
        rules.Add(new SA0080_ImplicitConversion());
        rules.Add(new SA0081_DangerousConversion());
        rules.Add(new SA0082_SignedUnsignedConversion());
        rules.Add(new SA0083_StringLengthOverflow());
        rules.Add(new SA0084_TimerCounterNotReset());
        rules.Add(new SA0085_PersistentInitialization());
        rules.Add(new SA0086_RetainVariableWarning());
        rules.Add(new SA0087_AtDirectiveWarning());
        rules.Add(new SA0088_VarAccessUsage());
        rules.Add(new SA0089_AttributeUsage());
        rules.Add(new SA0090_PragmaUsage());
        rules.Add(new SA0091_DuplicateTypeDefinition());
        rules.Add(new SA0092_CircularTypeDependency());
        rules.Add(new SA0093_NonStandardDataType());
        rules.Add(new SA0094_ExitStatement());
        rules.Add(new SA0095_ContinueStatement());
        rules.Add(new SA0096_JmpStatement());
        rules.Add(new SA0097_EmptyLoop());
        rules.Add(new SA0098_PotentialInfiniteLoop());
        rules.Add(new SA0099_ForLoopVariableModification());
        rules.Add(new SA0100_ImproperSizeOf());

        // SA0101-SA0130
        rules.Add(new SA0101_UnusedLibraryReference());
        rules.Add(new SA0102_InefficientArrayInit());
        rules.Add(new SA0103_ExcessiveVariableScope());
        rules.Add(new SA0104_UnsafeMemcpy());
        rules.Add(new SA0105_RecursiveCall());
        rules.Add(new SA0106_DynamicMemory());
        rules.Add(new SA0107_OutputInitInFbInit());
        rules.Add(new SA0108_MissingSuperCall());
        rules.Add(new SA0109_ThisPointerStorage());
        rules.Add(new SA0110_NonVirtualOverride());
        rules.Add(new SA0111_InterfaceSegregation());
        rules.Add(new SA0112_SingleResponsibility());
        rules.Add(new SA0113_HighCoupling());
        rules.Add(new SA0114_LowCohesion());
        rules.Add(new SA0115_HardcodedIP());
        rules.Add(new SA0116_HardcodedPath());
        rules.Add(new SA0117_BitOperationPrecedence());
        rules.Add(new SA0118_IntegerOverflow());
        rules.Add(new SA0119_TimeOperation());
        rules.Add(new SA0120_StringWstringMix());
        rules.Add(new SA0121_EnumRangeOverflow());
        rules.Add(new SA0122_NestedStructDepth());
        rules.Add(new SA0123_UnsafeCast());
        rules.Add(new SA0124_MultipleInheritance());
        rules.Add(new SA0125_PropertyMisuse());
        rules.Add(new SA0126_StringBufferSize());
        rules.Add(new SA0127_ArraySizeMismatch());
        rules.Add(new SA0128_ActionMisuse());
        rules.Add(new SA0129_FbReinitUsage());
        rules.Add(new SA0130_DirectIOAccess());

        // SA0131-SA0160
        rules.Add(new SA0131_UnsafePointerDereference());
        rules.Add(new SA0132_ArrayIndexValidation());
        rules.Add(new SA0133_FloatLoopCounter());
        rules.Add(new SA0134_MissingUnitTest());
        rules.Add(new SA0135_FixmeComment());
        rules.Add(new SA0136_DangerousCast());
        rules.Add(new SA0137_RedundantConditionCheck());
        rules.Add(new SA0138_BooleanLiteralReturn());
        rules.Add(new SA0139_EmptyExceptionHandler());
        rules.Add(new SA0140_TooManyReturns());
        rules.Add(new SA0141_SharedVariable());
        rules.Add(new SA0142_SemaphoreUsage());
        rules.Add(new SA0143_TaskPriority());
        rules.Add(new SA0144_BlockingCall());
        rules.Add(new SA0145_SpinLockPattern());
        rules.Add(new SA0146_AtomicOperationNeeded());
        rules.Add(new SA0147_CycleTimeRisk());
        rules.Add(new SA0148_WatchdogConsideration());
        rules.Add(new SA0149_DeadlockRisk());
        rules.Add(new SA0150_InterruptDisable());
        rules.Add(new SA0151_PLCopenFBRule());
        rules.Add(new SA0152_IECTypeSize());
        rules.Add(new SA0153_DirectAddressNotation());
        rules.Add(new SA0154_LanguageCompatibility());
        rules.Add(new SA0155_VarConfigUsage());
        rules.Add(new SA0156_UseStandardLibrary());
        rules.Add(new SA0157_BitAccessNotation());
        rules.Add(new SA0158_DataTypeRangeDoc());
        rules.Add(new SA0159_UnitConsistency());
        rules.Add(new SA0160_ProgramStructureComplexity());

        // SA0161-SA0180
        rules.Add(new SA0161_CircularDependency());
        rules.Add(new SA0162_ModuleSizeExceeded());
        rules.Add(new SA0163_ConditionalCompilation());
        rules.Add(new SA0164_DuplicateConstants());
        rules.Add(new SA0165_IncompleteInitialization());
        rules.Add(new SA0166_MemoryAlignment());
        rules.Add(new SA0167_ComplexInheritance());
        rules.Add(new SA0168_HardcodedTiming());
        rules.Add(new SA0169_IncompleteImplementation());
        rules.Add(new SA0170_UnusedUsing());
        rules.Add(new SA0171_SafetyVariableProtection());
        rules.Add(new SA0172_DangerousOperationOrder());
        rules.Add(new SA0173_InfiniteRetry());
        rules.Add(new SA0174_ExpensiveOperation());
        rules.Add(new SA0175_CacheInefficientAccess());
        rules.Add(new SA0176_StringOperationOptimization());
        rules.Add(new SA0177_BitOperationOptimization());
        rules.Add(new SA0178_ResourceLeak());
        rules.Add(new SA0179_StateMachineCompleteness());
        rules.Add(new SA0180_DocumentationLevel());

        return rules;
    }

    #region 범위별 등록 메서드

    private static void RegisterSA0001_SA0030(List<IQARuleChecker> rules)
    {
        rules.Add(new SA0001_UnreachableCode());
        rules.Add(new SA0002_EmptyObjects());
        rules.Add(new SA0003_EmptyStatements());
        rules.Add(new SA0004_MultipleWriteOnOutput());
        rules.Add(new SA0006_MultiTaskWriteAccess());
        rules.Add(new SA0007_AddressOfConstant());
        rules.Add(new SA0008_SubrangeTypeCheck());
        rules.Add(new SA0009_UnusedReturnValue());
        rules.Add(new SA0010_SingleElementArray());
        rules.Add(new SA0011_SingleMemberEnum());
        rules.Add(new SA0012_VariableCouldBeConstant());
        rules.Add(new SA0013_SameVariableName());
        rules.Add(new SA0014_InstanceAssignment());
        rules.Add(new SA0015_GlobalAccessInFBInit());
        rules.Add(new SA0016_GapsInStructures());
        rules.Add(new SA0017_IrregularPointerAssignment());
        rules.Add(new SA0018_UnusualBitAccess());
        rules.Add(new SA0019_ImplicitPointerConversion());
        rules.Add(new SA0020_TruncatedRealAssignment());
        rules.Add(new SA0021_AddressOfTemporary());
        rules.Add(new SA0022_NonRejectedReturnValue());
        rules.Add(new SA0023_ComplexReturnValue());
        rules.Add(new SA0024_UntypedLiterals());
        rules.Add(new SA0025_UnqualifiedEnumConstants());
        rules.Add(new SA0026_UseOfDirectAddresses());
        rules.Add(new SA0027_UnsafeTypeConversion());
        rules.Add(new SA0028_NestedComments());
        rules.Add(new SA0029_TODO_Comments());
        rules.Add(new SA0030_MissingErrorHandling());
    }

    private static void RegisterSA0031_SA0050(List<IQARuleChecker> rules)
    {
        rules.Add(new SA0031_UnusedSignatures());
        rules.Add(new SA0032_UnusedEnumConstants());
        rules.Add(new SA0033_UnusedVariables());
        rules.Add(new SA0034_UnusedInputVariables());
        rules.Add(new SA0035_UnusedOutputVariables());
        rules.Add(new SA0036_UnusedInOutVariables());
        rules.Add(new SA0037_UnusedTempVariables());
        rules.Add(new SA0038_WriteOnlyVariables());
        rules.Add(new SA0039_ReadOnlyAsVariable());
        rules.Add(new SA0040_DivisionByZero());
        rules.Add(new SA0041_LoopInvariantCode());
        rules.Add(new SA0042_InconsistentNamespaceAccess());
        rules.Add(new SA0043_SuspiciousSemicolon());
        rules.Add(new SA0044_ParenthesisMismatch());
        rules.Add(new SA0045_AssignmentInCondition());
        rules.Add(new SA0046_UnnecessaryComparison());
        rules.Add(new SA0047_DuplicateCondition());
        rules.Add(new SA0048_InefficientStringConcat());
        rules.Add(new SA0049_MagicNumbers());
        rules.Add(new SA0050_ComplexExpression());
    }

    private static void RegisterSA0051_SA0070(List<IQARuleChecker> rules)
    {
        rules.Add(new SA0051_FunctionTooLong());
        rules.Add(new SA0052_TooManyParameters());
        rules.Add(new SA0053_NestingTooDeep());
        rules.Add(new SA0054_CyclomaticComplexity());
        rules.Add(new SA0055_CognitiveComplexity());
        rules.Add(new SA0056_InsufficientComments());
        rules.Add(new SA0057_MissingHeaderComment());
        rules.Add(new SA0058_OutdatedComments());
        rules.Add(new SA0059_CommentedOutCode());
        rules.Add(new SA0060_IneffectiveOperation());
        rules.Add(new SA0061_SuspiciousPointerOperation());
        rules.Add(new SA0062_ConstantCondition());
        rules.Add(new SA0063_FloatEquality());
        rules.Add(new SA0064_SuspiciousPointerArithmetic());
        rules.Add(new SA0065_UninitializedVariable());
        rules.Add(new SA0066_ArrayOutOfBounds());
        rules.Add(new SA0067_GlobalInFunction());
        rules.Add(new SA0068_CircularReference());
        rules.Add(new SA0069_UnimplementedInterface());
        rules.Add(new SA0070_EmptyCaseBranch());
    }

    private static void RegisterSA0071_SA0100(List<IQARuleChecker> rules)
    {
        rules.Add(new SA0071_MissingElse());
        rules.Add(new SA0072_CaseMissingDefault());
        rules.Add(new SA0073_VariableNamingViolation());
        rules.Add(new SA0074_FBNamingViolation());
        rules.Add(new SA0075_InterfaceNamingViolation());
        rules.Add(new SA0076_EnumNamingViolation());
        rules.Add(new SA0077_StructNamingViolation());
        rules.Add(new SA0078_ConstantNamingViolation());
        rules.Add(new SA0079_GlobalVarNamingViolation());
        rules.Add(new SA0080_ImplicitConversion());
        rules.Add(new SA0081_DangerousConversion());
        rules.Add(new SA0082_SignedUnsignedConversion());
        rules.Add(new SA0083_StringLengthOverflow());
        rules.Add(new SA0084_TimerCounterNotReset());
        rules.Add(new SA0085_PersistentInitialization());
        rules.Add(new SA0086_RetainVariableWarning());
        rules.Add(new SA0087_AtDirectiveWarning());
        rules.Add(new SA0088_VarAccessUsage());
        rules.Add(new SA0089_AttributeUsage());
        rules.Add(new SA0090_PragmaUsage());
        rules.Add(new SA0091_DuplicateTypeDefinition());
        rules.Add(new SA0092_CircularTypeDependency());
        rules.Add(new SA0093_NonStandardDataType());
        rules.Add(new SA0094_ExitStatement());
        rules.Add(new SA0095_ContinueStatement());
        rules.Add(new SA0096_JmpStatement());
        rules.Add(new SA0097_EmptyLoop());
        rules.Add(new SA0098_PotentialInfiniteLoop());
        rules.Add(new SA0099_ForLoopVariableModification());
        rules.Add(new SA0100_ImproperSizeOf());
    }

    private static void RegisterSA0101_SA0130(List<IQARuleChecker> rules)
    {
        rules.Add(new SA0101_UnusedLibraryReference());
        rules.Add(new SA0102_InefficientArrayInit());
        rules.Add(new SA0103_ExcessiveVariableScope());
        rules.Add(new SA0104_UnsafeMemcpy());
        rules.Add(new SA0105_RecursiveCall());
        rules.Add(new SA0106_DynamicMemory());
        rules.Add(new SA0107_OutputInitInFbInit());
        rules.Add(new SA0108_MissingSuperCall());
        rules.Add(new SA0109_ThisPointerStorage());
        rules.Add(new SA0110_NonVirtualOverride());
        rules.Add(new SA0111_InterfaceSegregation());
        rules.Add(new SA0112_SingleResponsibility());
        rules.Add(new SA0113_HighCoupling());
        rules.Add(new SA0114_LowCohesion());
        rules.Add(new SA0115_HardcodedIP());
        rules.Add(new SA0116_HardcodedPath());
        rules.Add(new SA0117_BitOperationPrecedence());
        rules.Add(new SA0118_IntegerOverflow());
        rules.Add(new SA0119_TimeOperation());
        rules.Add(new SA0120_StringWstringMix());
        rules.Add(new SA0121_EnumRangeOverflow());
        rules.Add(new SA0122_NestedStructDepth());
        rules.Add(new SA0123_UnsafeCast());
        rules.Add(new SA0124_MultipleInheritance());
        rules.Add(new SA0125_PropertyMisuse());
        rules.Add(new SA0126_StringBufferSize());
        rules.Add(new SA0127_ArraySizeMismatch());
        rules.Add(new SA0128_ActionMisuse());
        rules.Add(new SA0129_FbReinitUsage());
        rules.Add(new SA0130_DirectIOAccess());
    }

    private static void RegisterSA0131_SA0160(List<IQARuleChecker> rules)
    {
        rules.Add(new SA0131_UnsafePointerDereference());
        rules.Add(new SA0132_ArrayIndexValidation());
        rules.Add(new SA0133_FloatLoopCounter());
        rules.Add(new SA0134_MissingUnitTest());
        rules.Add(new SA0135_FixmeComment());
        rules.Add(new SA0136_DangerousCast());
        rules.Add(new SA0137_RedundantConditionCheck());
        rules.Add(new SA0138_BooleanLiteralReturn());
        rules.Add(new SA0139_EmptyExceptionHandler());
        rules.Add(new SA0140_TooManyReturns());
        rules.Add(new SA0141_SharedVariable());
        rules.Add(new SA0142_SemaphoreUsage());
        rules.Add(new SA0143_TaskPriority());
        rules.Add(new SA0144_BlockingCall());
        rules.Add(new SA0145_SpinLockPattern());
        rules.Add(new SA0146_AtomicOperationNeeded());
        rules.Add(new SA0147_CycleTimeRisk());
        rules.Add(new SA0148_WatchdogConsideration());
        rules.Add(new SA0149_DeadlockRisk());
        rules.Add(new SA0150_InterruptDisable());
        rules.Add(new SA0151_PLCopenFBRule());
        rules.Add(new SA0152_IECTypeSize());
        rules.Add(new SA0153_DirectAddressNotation());
        rules.Add(new SA0154_LanguageCompatibility());
        rules.Add(new SA0155_VarConfigUsage());
        rules.Add(new SA0156_UseStandardLibrary());
        rules.Add(new SA0157_BitAccessNotation());
        rules.Add(new SA0158_DataTypeRangeDoc());
        rules.Add(new SA0159_UnitConsistency());
        rules.Add(new SA0160_ProgramStructureComplexity());
    }

    private static void RegisterSA0161_SA0180(List<IQARuleChecker> rules)
    {
        rules.Add(new SA0161_CircularDependency());
        rules.Add(new SA0162_ModuleSizeExceeded());
        rules.Add(new SA0163_ConditionalCompilation());
        rules.Add(new SA0164_DuplicateConstants());
        rules.Add(new SA0165_IncompleteInitialization());
        rules.Add(new SA0166_MemoryAlignment());
        rules.Add(new SA0167_ComplexInheritance());
        rules.Add(new SA0168_HardcodedTiming());
        rules.Add(new SA0169_IncompleteImplementation());
        rules.Add(new SA0170_UnusedUsing());
        rules.Add(new SA0171_SafetyVariableProtection());
        rules.Add(new SA0172_DangerousOperationOrder());
        rules.Add(new SA0173_InfiniteRetry());
        rules.Add(new SA0174_ExpensiveOperation());
        rules.Add(new SA0175_CacheInefficientAccess());
        rules.Add(new SA0176_StringOperationOptimization());
        rules.Add(new SA0177_BitOperationOptimization());
        rules.Add(new SA0178_ResourceLeak());
        rules.Add(new SA0179_StateMachineCompleteness());
        rules.Add(new SA0180_DocumentationLevel());
    }

    #endregion

    /// <summary>
    /// 규칙 개수 정보
    /// </summary>
    public static class RuleCount
    {
        public const int Total = 180;
        public const int SA0001_SA0030 = 30;
        public const int SA0031_SA0050 = 20;
        public const int SA0051_SA0070 = 20;
        public const int SA0071_SA0100 = 30;
        public const int SA0101_SA0130 = 30;
        public const int SA0131_SA0160 = 30;
        public const int SA0161_SA0180 = 20;
    }
}
