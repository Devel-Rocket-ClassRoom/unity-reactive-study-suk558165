using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MyTimingBarPresenter : MonoBehaviour
{
    [Header("트랙 / 마커")]
    [SerializeField]
    private Image m_TrackImage;

    [SerializeField]
    private RectTransform m_Marker;

    [Header("입력")]
    [SerializeField]
    private Button m_HitButton;

    [SerializeField]
    private Button m_StartButton;

    [Header("표시")]
    [SerializeField]
    private TextMeshProUGUI m_ScoreText;

    [SerializeField]
    private TextMeshProUGUI m_ComboText;

    [SerializeField]
    private TextMeshProUGUI m_AttemptText;

    [SerializeField]
    private TextMeshProUGUI m_JudgementText;

    [SerializeField]
    private TextMeshProUGUI m_StateText;

    [Header("설정")]
    [SerializeField]
    private float m_MarkerSpeed = 0.6f;

    [SerializeField]
    private int m_AttemptsPerRound = 12;

    private MyTimingBarModel m_Model;
    private float m_Phase;
    private float m_Direction = 1f;
    private bool m_IsPlaying;
    private Color m_TrackOriginalColor;                        // 트랙 바 원래 색 저장용
    private readonly Subject<Unit> m_HitSubject = new();       // 버튼 + 스페이스바 히트 입력을 하나로 통합

    private void Start()
    {
        m_Model = new MyTimingBarModel();
        m_TrackOriginalColor = m_TrackImage.color;             // 원래 색 저장 (Inspector에서 설정한 색)
        m_StateText.text = "스타트를 누르세요";                // 초기 안내 문구

        BindInput();
        BindView();
    }

    private void BindInput()
    {
        m_StartButton.OnClickAsObservable()
        .Where(_ => !m_IsPlaying)                                          // 게임 중이 아닐 때만 시작 가능
        .Subscribe(_ => StartRoundAsync(destroyCancellationToken).Forget()) // 비동기 라운드 시작
        .AddTo(this);

        // 버튼 클릭 → HitSubject로 전달
        m_HitButton.OnClickAsObservable()
        .Subscribe(_ => m_HitSubject.OnNext(Unit.Default))
        .AddTo(this);

        // HitSubject 구독 — 버튼과 스페이스바 모두 여기서 처리
        m_HitSubject
        .Where(_ => m_IsPlaying)                                           // 게임 중일 때만 판정
        .Subscribe(_ =>
        {
            var j = m_Model.ApplyHit(m_Phase);                             // 현재 마커 위치로 판정
            m_JudgementText.text = j.ToString();                           // 판정 결과 텍스트 표시
        })
        .AddTo(this);
    }

    private void BindView()
    {
        m_Model.Score.Subscribe(s => m_ScoreText.text = $"점수 {s}")       // Score 바뀔 때마다 텍스트 갱신
        .AddTo(this);
        m_Model.Combo.Subscribe(c => m_ComboText.text = c >= 2 ? $"{c} 콤보!" : "") // 콤보 2 이상일 때만 표시
        .AddTo(this);
    }

    private async UniTaskVoid StartRoundAsync(CancellationToken ct)
    {
        m_Model.ResetForNewGame();                             // Score/Combo/MaxCombo 초기화
        m_Phase = 0f;                                          // 마커 위치 초기화
        m_Direction = 1f;                                      // 마커 방향 초기화
        m_JudgementText.text = "";
        m_AttemptText.text = $"0 / {m_AttemptsPerRound}";     // 히트 카운터 초기화

        // 3초 카운트다운 (게임 시작 전 대기)
        for (int count = 3; count >= 1; count--)
        {
            m_StateText.text = $"{count}";                     // 가운데 흰 텍스트로 카운트 표시
            await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: ct);
        }
        m_StateText.text = "시작!";
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: ct);
        m_StateText.text = "존에 올 때 Space 또는 HIT!";      // 게임 중 안내 문구

        m_IsPlaying = true;                                    // 게임 시작 플래그 ON

        for (int i = 0; i < m_AttemptsPerRound; i++)
        {
            await m_HitSubject.FirstAsync(ct);                             // 버튼 또는 스페이스바 1회 대기
            m_AttemptText.text = $"{i + 1} / {m_AttemptsPerRound}";       // 히트 횟수 갱신
        }

        m_IsPlaying = false;                                               // 게임 종료 플래그 OFF
        m_TrackImage.color = m_TrackOriginalColor;                         // 트랙 색상 원래대로 복구
        m_StateText.text = $"점수: {m_Model.Score.Value}  최대콤보: {m_Model.MaxCombo.Value}"; // 최종 결과
        await UniTask.Delay(TimeSpan.FromSeconds(3), cancellationToken: ct); // 3초 결과 표시 후
        m_StateText.text = "스타트를 누르세요";                            // 다시 시작 안내 문구
    }

    private void Update()
    {
        // 스페이스바 입력 → 게임 중일 때만 HitSubject로 전달
        if (m_IsPlaying && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            m_HitSubject.OnNext(Unit.Default);

        if (!m_IsPlaying)
        {
            m_TrackImage.color = m_TrackOriginalColor;                  // 게임 중이 아니면 트랙 흰색 유지
            return;
        }

        // 마커 이동
        m_Phase += m_Direction * m_MarkerSpeed * Time.deltaTime; // 매 프레임 phase를 방향에 따라 이동
        if (m_Phase >= 1f)
        {
            m_Phase = 1f;
            m_Direction = -1f;                                 // 오른쪽 끝 도달 → 방향 반전
        }
        if (m_Phase <= 0f)
        {
            m_Phase = 0f;
            m_Direction = 1f;                                  // 왼쪽 끝 도달 → 방향 반전
        }

        m_Marker.anchorMin = new Vector2(m_Phase, m_Marker.anchorMin.y); // 마커 X 위치 갱신 (anchorMin)
        m_Marker.anchorMax = new Vector2(m_Phase, m_Marker.anchorMax.y); // 마커 X 위치 갱신 (anchorMax)
        m_Marker.anchoredPosition = Vector2.zero;              // 앵커 기준으로 오프셋 없이 정렬

        // 존에 따라 트랙 바 색상 즉시 변경
        m_TrackImage.color = MyTimingBarModel.ZoneOf(m_Phase) switch
        {
            Zone.Perfect => Color.yellow,                        // 퍼펙트 존 → 노란색
            Zone.Good    => new Color(0.3f, 0.6f, 1f),           // 굿 존 → 파란색
            _            => m_TrackOriginalColor,                 // 그 외 → 원래 색
        };
    }

    private void OnDestroy()
    {
        m_HitSubject.Dispose();                                // Subject 메모리 해제
        m_Model?.Dispose();                                    // 모델 메모리 해제
    }
}
