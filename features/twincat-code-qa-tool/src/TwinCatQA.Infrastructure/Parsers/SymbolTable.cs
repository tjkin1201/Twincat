using System;
using System.Collections.Generic;
using System.Linq;

namespace TwinCatQA.Infrastructure.Parsers;

/// <summary>
/// 변수 심볼 정보
/// </summary>
public record Symbol(string Name, string Type, int DeclarationLine)
{
    /// <summary>
    /// 초기화 여부
    /// </summary>
    public bool IsInitialized { get; set; } = false;

    /// <summary>
    /// 사용 여부
    /// </summary>
    public bool IsUsed { get; set; } = false;
}

/// <summary>
/// 변수 심볼 테이블 (스코프 관리)
/// 계층적 스코프를 지원하여 변수 선언과 참조를 추적합니다.
/// </summary>
public class SymbolTable
{
    /// <summary>
    /// 스코프 스택 (각 스코프는 심볼 맵)
    /// </summary>
    private readonly Stack<Dictionary<string, Symbol>> _scopes = new();

    /// <summary>
    /// 현재 스코프 이름 스택
    /// </summary>
    private readonly Stack<string> _scopeNames = new();

    /// <summary>
    /// 전역 스코프 (GVL 등)
    /// </summary>
    private readonly Dictionary<string, Symbol> _globalScope = new();

    public SymbolTable()
    {
        // 전역 스코프 초기화
        _scopes.Push(_globalScope);
        _scopeNames.Push("Global");
    }

    /// <summary>
    /// 새 스코프 진입
    /// </summary>
    /// <param name="scopeName">스코프 이름 (FB명, Function명 등)</param>
    public void EnterScope(string scopeName)
    {
        var newScope = new Dictionary<string, Symbol>();
        _scopes.Push(newScope);
        _scopeNames.Push(scopeName);
    }

    /// <summary>
    /// 현재 스코프 탈출
    /// </summary>
    public void ExitScope()
    {
        if (_scopes.Count > 1) // 전역 스코프는 유지
        {
            _scopes.Pop();
            _scopeNames.Pop();
        }
    }

    /// <summary>
    /// 현재 스코프 이름
    /// </summary>
    public string CurrentScope => _scopeNames.Peek();

    /// <summary>
    /// 변수 선언
    /// </summary>
    /// <param name="name">변수명</param>
    /// <param name="type">데이터 타입</param>
    /// <param name="line">선언 라인 번호</param>
    /// <returns>성공 여부 (중복 선언 시 false)</returns>
    public bool Declare(string name, string type, int line)
    {
        var currentScope = _scopes.Peek();

        if (currentScope.ContainsKey(name))
        {
            // 현재 스코프에 이미 선언됨
            return false;
        }

        currentScope[name] = new Symbol(name, type, line);
        return true;
    }

    /// <summary>
    /// 변수가 선언되어 있는지 확인 (현재 스코프 + 상위 스코프)
    /// </summary>
    /// <param name="name">변수명</param>
    /// <returns>선언 여부</returns>
    public bool IsDeclared(string name)
    {
        return Lookup(name) != null;
    }

    /// <summary>
    /// 현재 스코프에만 선언되어 있는지 확인
    /// </summary>
    public bool IsDeclaredInCurrentScope(string name)
    {
        return _scopes.Peek().ContainsKey(name);
    }

    /// <summary>
    /// 변수 조회 (현재 스코프부터 전역 스코프까지 검색)
    /// </summary>
    /// <param name="name">변수명</param>
    /// <returns>심볼 정보 (없으면 null)</returns>
    public Symbol? Lookup(string name)
    {
        foreach (var scope in _scopes)
        {
            if (scope.TryGetValue(name, out var symbol))
            {
                return symbol;
            }
        }

        return null;
    }

    /// <summary>
    /// 변수 사용 표시
    /// </summary>
    public void MarkAsUsed(string name)
    {
        var symbol = Lookup(name);
        if (symbol != null)
        {
            symbol.IsUsed = true;
        }
    }

    /// <summary>
    /// 변수 초기화 표시
    /// </summary>
    public void MarkAsInitialized(string name)
    {
        var symbol = Lookup(name);
        if (symbol != null)
        {
            symbol.IsInitialized = true;
        }
    }

    /// <summary>
    /// 현재 스코프의 모든 심볼 가져오기
    /// </summary>
    public IEnumerable<Symbol> GetCurrentScopeSymbols()
    {
        return _scopes.Peek().Values;
    }

    /// <summary>
    /// 모든 스코프의 심볼 가져오기
    /// </summary>
    public IEnumerable<Symbol> GetAllSymbols()
    {
        return _scopes.SelectMany(scope => scope.Values).Distinct();
    }

    /// <summary>
    /// 미사용 변수 가져오기
    /// </summary>
    public IEnumerable<Symbol> GetUnusedVariables()
    {
        return GetAllSymbols().Where(s => !s.IsUsed);
    }

    /// <summary>
    /// 초기화되지 않은 변수 가져오기
    /// </summary>
    public IEnumerable<Symbol> GetUninitializedVariables()
    {
        return GetAllSymbols().Where(s => !s.IsInitialized && !s.IsUsed);
    }

    /// <summary>
    /// 심볼 테이블 초기화
    /// </summary>
    public void Clear()
    {
        _scopes.Clear();
        _scopeNames.Clear();
        _globalScope.Clear();

        // 전역 스코프 재초기화
        _scopes.Push(_globalScope);
        _scopeNames.Push("Global");
    }

    /// <summary>
    /// 디버깅용 출력
    /// </summary>
    public override string ToString()
    {
        var result = $"SymbolTable (Current Scope: {CurrentScope})\n";
        var scopeList = _scopes.Reverse().ToList();
        var nameList = _scopeNames.Reverse().ToList();

        for (int i = 0; i < scopeList.Count; i++)
        {
            result += $"  Scope [{nameList[i]}]:\n";
            foreach (var symbol in scopeList[i].Values)
            {
                result += $"    - {symbol.Name}: {symbol.Type} (Line {symbol.DeclarationLine}, Used: {symbol.IsUsed})\n";
            }
        }

        return result;
    }
}
