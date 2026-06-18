using System;
using R3;
using UnityEngine;

public enum Zone
{
    None,
    Good,
    Perfect,
}

public enum Judgement
{
    Miss,
    Good,
    Perfect,
}

public sealed class MyTimingBarModel : IDisposable
{
    public const float PerfectHalfWidth = 0.04f;
    public const float GoodHalfWidth = 0.12f;

    private const int PerfectScore = 3;
    private const int GoodScore = 1;
    private const int ComboPerMultiplier = 5;

    public ReactiveProperty<int> Score { get; } = new(0);
    public ReactiveProperty<int> Combo { get; } = new(0);
    public ReactiveProperty<int> MaxCombo { get; } = new(0);

    private readonly CompositeDisposable m_Disposables = new();

    public MyTimingBarModel()
    {
        Score.AddTo(m_Disposables);
        Combo.AddTo(m_Disposables);
        MaxCombo.AddTo(m_Disposables);
    }

    public static Zone ZoneOf(float phase)
    {
        float dist = Mathf.Abs(phase - 0.5f);  // 마커가 중앙(0.5)에서 얼마나 떨어졌는지 거리 계산
        if (dist <= PerfectHalfWidth)           // 거리가 ±0.04 이내면 Perfect 구간
        {
            return Zone.Perfect;
        }
        if (dist <= GoodHalfWidth)              // 거리가 ±0.12 이내면 Good 구간
        {
            return Zone.Good;
        }
        return Zone.None;                       // 그 외는 구간 밖(Miss)
    }

    public static Judgement Judge(float phase)
    {
        return ZoneOf(phase) switch             // ZoneOf 결과를 Judgement로 변환
        {
            Zone.Perfect => Judgement.Perfect,
            Zone.Good    => Judgement.Good,
            _            => Judgement.Miss,     // Zone.None 포함 나머지는 Miss
        };
    }

    public Judgement ApplyHit(float phase)
    {
        var j = Judge(phase);                               // 현재 phase로 판정 계산
        if (j == Judgement.Miss)
        {
            Combo.Value = 0;                                // 미스면 콤보 초기화
        }
        else
        {
            Combo.Value++;                                  // 성공 판정이면 콤보 1 증가
            if (Combo.Value > MaxCombo.Value)
                MaxCombo.Value = Combo.Value;               // 현재 콤보가 최고 기록이면 갱신
            int multiplier = 1 + Combo.Value / ComboPerMultiplier; // 5콤보마다 배율 +1
            int baseScore = j == Judgement.Perfect ? PerfectScore : GoodScore; // Perfect=3점, Good=1점
            Score.Value += baseScore * multiplier;          // 배율 적용한 점수 누적
        }
        return j;                                           // 판정 결과 반환 (Presenter에서 텍스트 표시용)
    }

    public void ResetForNewGame()
    {
        Score.Value = 0;     // 점수 초기화
        Combo.Value = 0;     // 콤보 초기화
        MaxCombo.Value = 0;  // 최대콤보 초기화
    }

    public void Dispose() => m_Disposables.Dispose();
}
