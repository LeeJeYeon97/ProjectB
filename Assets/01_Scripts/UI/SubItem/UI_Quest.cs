using System.Collections;
using TMPro;
using UnityEngine;
using UnityPipeline.Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>프리팹에 배치된 요소를 연결해 현재 퀘스트를 표시합니다.</summary>
public class UI_Quest : UI_Base
{
    /// <summary>퀘스트 UI에서 이름으로 연결할 텍스트 요소입니다.</summary>
    public enum QuestTexts
    {
        Text_QuestTitle,
        Text_QuestDescription,
        Text_QuestProgress,
    }

    /// <summary>퀘스트 UI에서 이름으로 연결할 이미지 요소입니다.</summary>
    public enum QuestImages
    {
        Image_ProgressFill,
    }

    #region 변수

    [Header("퀘스트 연출 시간")]
    [SerializeField, Min(0f)] private float progressDuration = 0.5f;
    [SerializeField, Min(0f)] private float completeDuration = 0.5f;
    [SerializeField, Min(0f)] private float rewardDuration = 0.5f;

    private TMP_Text _titleText;
    private TMP_Text _descriptionText;
    private TMP_Text _progressText;
    private UnityEngine.UI.Image _progressFill;
    private RectTransform _progressFillRect;
    private RectTransform _progressBarRect;
    private QuestManager _questManager;
    private Coroutine _progressRoutine;
    private Coroutine _completionRoutine;
    private float _shownRatio;
    private bool _hasShownQuest;

    #endregion

    #region Unity 제공 함수

    /// <summary>숨겼다가 다시 보여 준 퀘스트 UI를 현재 퀘스트에 다시 연결합니다.</summary>
    private void OnEnable()
    {
        if (!_init || _questManager == null)
            return;

        SubscribeToQuest();
        _questManager.NotifyCurrentQuest();
    }

    /// <summary>화면이 숨겨지면 연출을 멈추고 완료 대기 중인 퀘스트를 진행시킵니다.</summary>
    private void OnDisable()
    {
        UnsubscribeFromQuest();
        bool wasCompleting = StopPresentation();
        if (wasCompleting && _questManager != null)
            _questManager.AdvanceAfterPresentation();
    }

    #endregion

    #region 초기화와 정리

    /// <summary>프리팹 요소를 enum 이름으로 찾아 연결하고 초기 막대를 비웁니다.</summary>
    public override void Init()
    {
        // 이미 초기화된 UI를 다시 연결해 사전 바인딩 자료가 중복되는 일을 막습니다.
        if (_init)
            return;

        base.Init();
        Bind<TMP_Text>(typeof(QuestTexts));
        Bind<UnityEngine.UI.Image>(typeof(QuestImages));

        _titleText = GetText((int)QuestTexts.Text_QuestTitle);
        _descriptionText = GetText((int)QuestTexts.Text_QuestDescription);
        _progressText = GetText((int)QuestTexts.Text_QuestProgress);
        _progressFill = GetImage((int)QuestImages.Image_ProgressFill);
        if (_progressFill != null)
            _progressFillRect = _progressFill.rectTransform;

        if (_progressFillRect != null)
            _progressBarRect = _progressFillRect.parent as RectTransform;

        // 첫 알림 전에 막대를 비워 둔 뒤 매니저가 가진 실제 진행도를 표시합니다.
        SetProgressFill(0f);
        _questManager = Managers.Quest;
        SubscribeToQuest();
        if (_questManager != null)
            _questManager.NotifyCurrentQuest();
    }

    /// <summary>UI가 파괴될 때 이벤트와 코루틴을 정리하고 완료 대기를 해제합니다.</summary>
    public override void Clear()
    {
        UnsubscribeFromQuest();
        bool wasCompleting = StopPresentation();
        if (wasCompleting && _questManager != null)
            _questManager.AdvanceAfterPresentation();

        _questManager = null;
        base.Clear();
    }

    #endregion

    #region 퀘스트 알림과 연출

    /// <summary>퀘스트 변경과 완료 알림을 중복 없이 구독합니다.</summary>
    private void SubscribeToQuest()
    {
        if (_questManager == null)
            return;

        _questManager.OnQuestChanged -= UpdateQuest;
        _questManager.OnQuestCompleted -= PlayCompletion;
        _questManager.OnQuestChanged += UpdateQuest;
        _questManager.OnQuestCompleted += PlayCompletion;
    }

    /// <summary>숨겨진 UI가 다음 퀘스트의 알림을 받지 않게 구독을 해제합니다.</summary>
    private void UnsubscribeFromQuest()
    {
        if (_questManager == null)
            return;

        _questManager.OnQuestChanged -= UpdateQuest;
        _questManager.OnQuestCompleted -= PlayCompletion;
    }

    /// <summary>퀘스트 정보와 진행 비율을 화면의 미리 배치된 텍스트와 막대에 반영합니다.</summary>
    private void UpdateQuest(string title, string description, int progress, int target, bool isComplete)
    {
        if (_titleText == null || _descriptionText == null || _progressText == null)
            return;

        // 마지막 퀘스트 뒤에는 새 목표가 없으므로 전체 완료 상태를 그대로 표시합니다.
        if (isComplete && _questManager != null && _questManager.CurrentQuest == null)
        {
            _titleText.text = title;
            _descriptionText.text = description;
            _progressText.text = "완료";
            SetProgressFill(1f);
            return;
        }

        // 개별 퀘스트의 완료 알림은 완료 코루틴이 시간에 맞춰 처리합니다.
        if (isComplete || _completionRoutine != null)
            return;

        float ratio = 0f;
        if (target > 0)
            ratio = Mathf.Clamp01((float)progress / target);

        _titleText.text = title;
        _descriptionText.text = description;
        _progressText.text = $"진행  {progress:N0} / {target:N0}{GetProgressUnit()}";

        if (_progressRoutine != null)
            StopCoroutine(_progressRoutine);

        // 처음 나타나는 목표와 다음 목표의 0% 상태는 바로 표시합니다.
        if (!_hasShownQuest || ratio <= 0f || progressDuration <= 0f)
        {
            SetProgressFill(ratio);
        }
        else
        {
            _progressRoutine = StartCoroutine(AnimateProgress(ratio));
        }

        _hasShownQuest = true;
    }

    /// <summary>완료 알림을 받으면 진행바, 완료 문구, 보상 문구 순서로 연출합니다.</summary>
    private void PlayCompletion(int rewardMoney)
    {
        if (_completionRoutine != null)
            return;

        // 필수 글자나 막대가 빠진 프리팹은 연출을 실행하지 않고 다음 목표로 진행합니다.
        if (_progressText == null || _progressFillRect == null || _progressBarRect == null)
        {
            _questManager.AdvanceAfterPresentation();
            return;
        }

        if (_progressRoutine != null)
        {
            StopCoroutine(_progressRoutine);
            _progressRoutine = null;
        }

        _completionRoutine = StartCoroutine(ShowCompletion(rewardMoney));
    }

    /// <summary>현재 막대 너비에서 목표 너비까지 설정한 시간 동안 부드럽게 채웁니다.</summary>
    private IEnumerator AnimateProgress(float targetRatio)
    {
        float startRatio = _shownRatio;
        float elapsed = 0f;
        while (elapsed < progressDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float step = Mathf.Clamp01(elapsed / progressDuration);
            SetProgressFill(Mathf.Lerp(startRatio, targetRatio, step));
            yield return null;
        }

        SetProgressFill(targetRatio);
    }

    /// <summary>표시 시간이 모두 지난 뒤에만 매니저에 다음 퀘스트 시작을 알립니다.</summary>
    private IEnumerator ShowCompletion(int rewardMoney)
    {
        if (progressDuration > 0f)
            yield return AnimateProgress(1f);
        else
            SetProgressFill(1f);

        _progressText.text = "완료!";
        if (completeDuration > 0f)
            yield return new WaitForSecondsRealtime(completeDuration);

        if (rewardMoney > 0)
        {
            _progressText.text = $"보상 +{rewardMoney:N0}";
            if (rewardDuration > 0f)
                yield return new WaitForSecondsRealtime(rewardDuration);
        }

        // 시간이 모두 0으로 설정돼도 완료 문구가 최소 한 프레임은 보이게 합니다.
        yield return null;
        _completionRoutine = null;
        if (_questManager != null)
            _questManager.AdvanceAfterPresentation();
    }

    /// <summary>진행 중인 연출을 모두 멈추고 완료 연출 중이었는지 반환합니다.</summary>
    private bool StopPresentation()
    {
        bool wasCompleting = _completionRoutine != null;
        if (_progressRoutine != null)
            StopCoroutine(_progressRoutine);

        if (_completionRoutine != null)
            StopCoroutine(_completionRoutine);

        _progressRoutine = null;
        _completionRoutine = null;
        return wasCompleting;
    }

    /// <summary>현재 퀘스트 행동에 알맞은 단위를 골라 목표 수량을 표시합니다.</summary>
    private string GetProgressUnit()
    {
        QuestData data;
        if (_questManager != null && _questManager.CurrentQuest != null)
        {
            data = _questManager.CurrentQuest.Data;
        }
        else
        {
            data = null;
        }

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
        _shownRatio = Mathf.Clamp01(ratio);
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

        _progressFillRect.sizeDelta = new Vector2(barWidth * _shownRatio, 0f);
    }

    #endregion
}
