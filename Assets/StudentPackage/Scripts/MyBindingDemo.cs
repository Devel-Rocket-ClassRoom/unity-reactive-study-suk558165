using System.Collections;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class MyBindingDemo : MonoBehaviour
{
    [SerializeField]
    private Button m_DamageButton;

    [SerializeField]
    private Button m_HealButton;

    [SerializeField]
    private Button m_UseMpButton;

    [SerializeField]
    private Button m_RestoreMpButton;

    [SerializeField]
    private Button m_SkillButton;

    [SerializeField]
    private TextMeshProUGUI m_HpText;

    [SerializeField]
    private TextMeshProUGUI m_MpText;

    [SerializeField]
    private TextMeshProUGUI m_HpStateText;

    [SerializeField]
    private TextMeshProUGUI m_SkillLogText;

    [SerializeField]
    private SerializableReactiveProperty<int> m_Hp = new(100);

    [SerializeField]
    private SerializableReactiveProperty<int> m_Mp = new(50);

    private const int SkillMpCost = 20;

    private void Start()
    {
        // HP 값이 바뀔 때마다 m_HpText UI 텍스트를 즉시 갱신
        m_Hp.Subscribe(hp => m_HpText.text = $"HP: {hp}").AddTo(this);
        // MP 값이 바뀔 때마다 m_MpText UI 텍스트를 즉시 갱신
        m_Mp.Subscribe(mp => m_MpText.text = $"MP: {mp}").AddTo(this);

        m_DamageButton.OnClickAsObservable()             // 데미지 버튼 클릭 Observable
        .Subscribe(_ =>
        m_Hp.Value = Mathf.Max(0, m_Hp.Value - 10, 0 )) // HP를 10 감소, 최솟값 0으로 고정
        .AddTo(this);

        m_HealButton.OnClickAsObservable()               // 힐 버튼 클릭 Observable
        .Subscribe(_ =>
        m_Hp.Value = Mathf.Min(0, m_Hp.Value + 10, 100 )) // HP를 10 증가, 최댓값 100으로 고정
        .AddTo(this);

        m_UseMpButton.OnClickAsObservable()              // MP 소모 버튼 클릭 Observable
        .Subscribe(_ =>
        m_Mp.Value = Mathf.Clamp(m_Mp.Value - 10, 0, 50)) // MP를 10 감소, 0~50 범위로 고정
        .AddTo(this);

        m_RestoreMpButton.OnClickAsObservable()          // MP 회복 버튼 클릭 Observable
        .Subscribe(_ =>
        m_Mp.Value = Mathf.Clamp(m_Mp.Value + 5, 0, 50))  // MP를 5 회복, 0~50 범위로 고정
        .AddTo(this);


        m_Hp
        .Select(hp => hp <= 0 ? "쓰러짐" : hp < 30 ? "위험" : hp < 70 ? "주의" : "안전") // HP 수치를 상태 문자열로 변환
        .DistinctUntilChanged()                          // 상태 문자열이 실제로 바뀔 때만 아래로 흘려보냄
        .Subscribe(state =>
        {
          m_HpStateText.text = $"상태: {state}";         // 상태 텍스트 UI 갱신
          m_HpStateText.color = state switch             // 상태에 따라 텍스트 색상 변경
          {
              "안전" => Color.green,
              "주의" => Color.yellow,
              "쓰러짐" => Color.darkGray,
              _ => Color.red                             // "위험" 포함 나머지는 빨간색
          };
        })
        .AddTo(this);

       // HP와 MP 두 값을 동시에 감시해 스킬 사용 가능 여부를 계산
       m_Hp.CombineLatest(m_Mp, (hp,mp) => hp > 0 && mp >= SkillMpCost)
       .Subscribe(canUse =>
        {
           m_SkillButton.interactable = canUse;          // 조건 충족 여부에 따라 버튼 활성/비활성
        })
       .AddTo(this);

       m_SkillButton
       .OnClickAsObservable()                            // 스킬 버튼 클릭 Observable
       .Subscribe(_ =>
       {
           m_Mp.Value = Mathf.Clamp(m_Mp.Value - SkillMpCost, 0, 50); // MP를 스킬 비용만큼 차감
           m_SkillLogText.text = $"스킬 사용 마나{SkillMpCost}소모";   // 스킬 사용 로그 표시
       })
       .AddTo(this);

    }

    private void OnDestroy()
    {
        m_Hp.Dispose();
        m_Mp.Dispose();
    }
}
