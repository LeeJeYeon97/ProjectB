using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>프리팹에 배치된 요소를 연결해 현재 퀘스트를 표시합니다.</summary>
public class UI_Quest : UI_Base
{
    private TMP_Text _titleText;
    private TMP_Text _descriptionText;
    private TMP_Text _progressText;
    private UnityEngine.UI.Image _progressFill;
    private RectTransform _progressFillRect;
    private RectTransform _progressBarRect;
    private QuestManager _questManager;

    /// <summary>프리팹 요소를 enum 이름으로 찾아 연결하고 초기 막대를 비웁니다.</summary>
    public override void Init()
    {
        // 이미 초기화된 UI를 다시 연결해 사전 바인딩 자료가 중복되는 일을 막습니다.
        if (_init)
            return;

        base.Init();
        Bind<TMP_Text>(typeof(Define.QuestTexts));
        Bind<UnityEngine.UI.Image>(typeof(Define.QuestImages));

        _titleText = GetTMP((int)Define.QuestTexts.Text_QuestTitle);
        _descriptionText = GetTMP((int)Define.QuestTexts.Text_QuestDescription);
        _progressText = GetTMP((int)Define.QuestTexts.Text_QuestProgress);
        _progressFill = GetImage((int)Define.QuestImages.Image_ProgressFill);
        _progressFillRect = _progressFill != null ? _progressFill.rectTransform : null;
        _progressBarRect = _progressFillRect != null ? _progressFillRect.parent as RectTransform : null;

        // 진행 값이 아직 전달되지 않은 첫 프레임에는 막대가 반드시 비어 보이게 합니다.
        SetProgressFill(0f);
    }

    /// <summary>퀘스트 매니저의 변경 알림을 연결하고 현재 진행 정보를 즉시 표시합니다.</summary>
    public void Bind(QuestManager questManager)
    {
        // 비활성 UI도 이벤트를 받기 전에 요소를 찾아 두어 첫 알림을 놓치지 않게 합니다.
        Init();

        if (_questManager != null)
            _questManager.OnQuestChanged -= UpdateQuest;

        _questManager = questManager;
        if (_questManager == null)
            return;

        _questManager.OnQuestChanged -= UpdateQuest;
        _questManager.OnQuestChanged += UpdateQuest;
        _questManager.NotifyCurrentQuest();
    }

    /// <summary>퀘스트 정보와 진행 비율을 화면의 미리 배치된 텍스트와 막대에 반영합니다.</summary>
    private void UpdateQuest(string title, string description, int progress, int target, bool isComplete)
    {
        if (_titleText == null || _descriptionText == null || _progressText == null)
            return;

        float ratio = target > 0 ? Mathf.Clamp01((float)progress / target) : (isComplete ? 1f : 0f);
        _titleText.text = title;
        _descriptionText.text = description;
        _progressText.text = isComplete
            ? "완료"
            : $"진행  {progress:N0} / {target:N0}{GetProgressUnit()}";
        SetProgressFill(ratio);
    }

    /// <summary>현재 퀘스트 행동에 알맞은 단위를 골라 목표 수량을 표시합니다.</summary>
    private string GetProgressUnit()
    {
        QuestData data = _questManager != null && _questManager.CurrentQuest != null
            ? _questManager.CurrentQuest.Data
            : null;
        if (data == null)
            return "회";

        switch (data.objectiveType)
        {
            case Define.QuestObjectiveType.Produce:
            case Define.QuestObjectiveType.Serve:
                if (data.targetType == Define.QuestTargetType.Beer)
                    return "잔";
                if (data.targetType == Define.QuestTargetType.TestMeal)
                    return "개";
                return "회";

            case Define.QuestObjectiveType.Unlock:
            case Define.QuestObjectiveType.PlaceFurniture:
                return "개";

            default:
                return "회";
        }
    }

    /// <summary>진행 막대의 왼쪽 기준 너비만 바꾸어 0일 때 채움이 보이지 않게 합니다.</summary>
    private void SetProgressFill(float ratio)
    {
        if (_progressFillRect == null || _progressBarRect == null)
            return;

        // 막대 이미지는 심플 타입으로 두고, 목표 비율에 맞춰 실제 너비를 조절합니다.
        _progressFillRect.anchorMin = new Vector2(0f, 0f);
        _progressFillRect.anchorMax = new Vector2(0f, 1f);
        _progressFillRect.pivot = new Vector2(0f, 0.5f);
        _progressFillRect.anchoredPosition = Vector2.zero;
        float barWidth = _progressBarRect.rect.width;
        if (barWidth <= 0f)
            barWidth = _progressBarRect.sizeDelta.x;

        _progressFillRect.sizeDelta = new Vector2(barWidth * Mathf.Clamp01(ratio), 0f);
    }

    /// <summary>화면이 사라질 때 퀘스트 매니저 이벤트를 해제해 참조가 남지 않게 합니다.</summary>
    public override void Clear()
    {
        base.Clear();
        if (_questManager != null)
        {
            _questManager.OnQuestChanged -= UpdateQuest;
            _questManager = null;
        }
    }
}
