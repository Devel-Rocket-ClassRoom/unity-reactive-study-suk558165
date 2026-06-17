using System;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MyOperatorsDemo : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI m_DoubleClickText;

    [SerializeField]
    private TMP_InputField m_SearchInput;

    [SerializeField]
    private TextMeshProUGUI m_SearchResultText;

    [SerializeField]
    private Button m_TapButton;

    [SerializeField]
    private TextMeshProUGUI m_TapText;

    [SerializeField]
    private TextMeshProUGUI m_CooldownText;

    private void Start()
    {
        SetupDoubleClick();      // Chunk + Debounce로 250ms 내 2회 이상 클릭을 더블클릭으로 감지
        SetupSearchDebounce();   // Debounce로 500ms 입력 멈춤 후 검색 실행, DistinctUntilChanged로 중복 방지
        SetupTapAndCooldown();   // Chunk로 0.5초 내 3연타 감지 + ThrottleFirst로 2초 쿨다운 적용
    }

    private void SetupDoubleClick()
    {
        var clickStream = Observable
        .EveryUpdate()
        .Where(_ => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        .Share();


    clickStream
       .Chunk(clickStream.Debounce(TimeSpan.FromMilliseconds(250)))
       .Where(clicks => clicks.Length >= 2) 
       .Subscribe(clicks => 
            SetText(m_DoubleClickText, $"더블 클릭 {clicks.Length}"))
       .AddTo(this);    
    }

    private void SetupSearchDebounce()
    {
        m_SearchInput
        .onValueChanged.AsObservable()
        .Debounce(TimeSpan.FromMilliseconds(500))
        .DistinctUntilChanged()
        .Subscribe(query => SetText(m_SearchResultText, $"검색 실행: {query}"))
        .AddTo(this);
    }

    private void SetupTapAndCooldown()
    {
        var taps = m_TapButton.OnClickAsObservable().Share();

        taps.Chunk(TimeSpan.FromMilliseconds(500),3)
        .Where(xs => xs.Length >= 3)
        .Subscribe(_ => SetText(m_TapText, $"3클릭!!"))
        .AddTo(this);

        taps.ThrottleFirst(TimeSpan.FromSeconds(2))
        .Subscribe(_ => SetText(m_CooldownText, $"발행!"))
        .AddTo(this);
    }

    private static void SetText(TextMeshProUGUI label, string text)
    {
        if (label != null)
            label.text = text;
    }
}
