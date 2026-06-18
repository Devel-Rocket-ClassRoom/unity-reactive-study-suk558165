using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MyAsyncDemo : MonoBehaviour
{
    [SerializeField]
    private Button m_SequentialButton;

    [SerializeField]
    private Button m_DropButton;

    [SerializeField]
    private TextMeshProUGUI m_LogText;

    private readonly Queue<string> m_LogLines = new();
    private int m_SequentialCount;
    private int m_DropCount;

    private void Start()
    {
        m_SequentialButton                          // m_SequentialButton 버튼에
        .OnClickAsObservable()                      // 클릭 이벤트를 Observable로 변환하고
        .SubscribeAwait(async(_ , ct) =>            // 비동기 구독 시작 (ct: CancellationToken)
        {
            int id = ++m_SequentialCount;           // 클릭할 때마다 순번 1씩 증가
            Log($"[순차] 로드 #{id} 시작...");      // 시작 로그 출력
            await FakeLoadAsync(ct);               // 1.5초 가짜 로딩 비동기 대기
            Log($"[순차] 로드 #{id} 완료");        // 완료 로그 출력
        }, AwaitOperation.Sequential)              // 이전 작업이 끝난 후 순서대로 실행
        .AddTo(this);                              // GameObject 파괴 시 구독 자동 해제
    }

    private static async UniTask FakeLoadAsync(CancellationToken ct)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(1.5), cancellationToken: ct);
    }

    private void Log(string message)
    {
        m_LogLines.Enqueue(message);
        while (m_LogLines.Count > 6)
            m_LogLines.Dequeue();
        if (m_LogText != null)
            m_LogText.text = string.Join("\n", m_LogLines);
    }
}
