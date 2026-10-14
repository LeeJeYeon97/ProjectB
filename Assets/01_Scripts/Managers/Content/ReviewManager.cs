using System;
using UnityEngine;

/// <summary>손님 리뷰를 계산하고 누적 평판에 따른 수익 배율을 관리합니다.</summary>
public class ReviewManager
{
    // 리뷰 기록은 게임을 다시 실행해도 유지되도록 기존 공용 저장 매니저에 보관합니다.
    private const string ReviewCountKey = "ProjectB.Review.Count";
    private const string ReviewStarTotalKey = "ProjectB.Review.StarTotal";

    // 리뷰가 반영되는 대기 시간 경계입니다. 초 단위이며 순서대로 평가가 한 단계씩 낮아집니다.
    private const float FirstWaitLimit = 5f;
    private const float SecondWaitLimit = 15f;
    private const float ThirdWaitLimit = 30f;

    // 누적 평판은 기본 수익의 최대 25%까지 높이며, 리뷰 한 별 차이는 5%로 계산합니다.
    private const float RevenueStepPerStar = 0.05f;
    private const float MaximumRevenueBonus = 0.25f;

    private int reviewCount;
    private int starTotal;

    /// <summary>평균 별점이 바뀌었을 때 화면이나 다른 시스템이 갱신할 수 있도록 알립니다.</summary>
    public event Action<float, int> OnReviewChanged;

    /// <summary>저장된 리뷰 통계를 읽어 매니저 상태를 준비합니다.</summary>
    public void Init()
    {
        reviewCount = Managers.PlayerPrefs.GetInt(ReviewCountKey, 0);
        starTotal = Managers.PlayerPrefs.GetInt(ReviewStarTotalKey, 0);

        // 저장 자료가 잘못되었더라도 평균 계산에서 음수나 불가능한 별점이 나오지 않게 합니다.
        reviewCount = Mathf.Max(0, reviewCount);
        starTotal = Mathf.Clamp(starTotal, 0, reviewCount * 5);
    }

    /// <summary>손님의 기다림과 식사 방해 기록으로 리뷰를 만들고 해당 결제 금액을 계산합니다.</summary>
    public ReviewResult CreateReview(float waitSeconds, int interruptionCount, int basePay, bool menuMatched)
    {
        int stars = CalculateStars(waitSeconds, interruptionCount, menuMatched);
        Define.ReviewGrade grade = (Define.ReviewGrade)stars;

        // 이번 리뷰를 평균에 먼저 반영하여 현재 손님 결제에도 새 평판 배율이 적용되게 합니다.
        reviewCount++;
        starTotal += stars;
        SaveReviewData();

        float averageStars = AverageStars;
        float revenueMultiplier = GetRevenueMultiplier(averageStars);
        int finalPay = Mathf.Max(0, Mathf.RoundToInt(Mathf.Max(0, basePay) * revenueMultiplier));

        ReviewResult result = new ReviewResult(
            grade,
            stars,
            Mathf.Max(0f, waitSeconds),
            Mathf.Max(0, interruptionCount),
            menuMatched,
            averageStars,
            revenueMultiplier,
            finalPay);

        OnReviewChanged?.Invoke(averageStars, reviewCount);
        Debug.Log($"[ReviewManager] 손님 리뷰: {stars}점({grade}), 메뉴 일치: {menuMatched}, 평균 {averageStars:0.00}점, 결제 {finalPay}원");
        return result;
    }

    /// <summary>대기 시간과 식사 방해 횟수를 1~5점으로 바꿉니다.</summary>
    private int CalculateStars(float waitSeconds, int interruptionCount, bool menuMatched)
    {
        int stars = 5;
        float safeWaitSeconds = Mathf.Max(0f, waitSeconds);
        int safeInterruptionCount = Mathf.Max(0, interruptionCount);

        // 오래 기다릴수록 별점을 낮추며, 정한 각 경계를 넘을 때 한 단계씩 감점합니다.
        if (safeWaitSeconds > FirstWaitLimit)
            stars--;
        if (safeWaitSeconds > SecondWaitLimit)
            stars--;
        if (safeWaitSeconds > ThirdWaitLimit)
            stars--;

        // 식사 중 한 번의 방해마다 한 단계씩 낮추되 최저 별점은 1점으로 제한합니다.
        stars -= safeInterruptionCount;

        // 주문한 메뉴와 다른 메뉴를 받으면 손님이 크게 실망하므로 두 별을 낮춥니다.
        if (!menuMatched)
            stars -= 2;

        return Mathf.Clamp(stars, 1, 5);
    }

    /// <summary>평균 3점을 기준으로 수익 배율을 계산하고 좋은 리뷰에만 추가 수익을 줍니다.</summary>
    private float GetRevenueMultiplier(float averageStars)
    {
        float bonus = Mathf.Clamp((averageStars - 3f) * RevenueStepPerStar, 0f, MaximumRevenueBonus);
        return 1f + bonus;
    }

    /// <summary>리뷰 수와 별점 합계를 저장해 다음 실행에도 평판을 유지합니다.</summary>
    private void SaveReviewData()
    {
        Managers.PlayerPrefs.SetInt(ReviewCountKey, reviewCount);
        Managers.PlayerPrefs.SetInt(ReviewStarTotalKey, starTotal);
        Managers.PlayerPrefs.Save();
    }

    /// <summary>현재까지 받은 리뷰의 평균 별점을 돌려줍니다.</summary>
    public float AverageStars => reviewCount > 0 ? (float)starTotal / reviewCount : 0f;

    /// <summary>현재 평판으로 적용되는 수익 배율을 돌려줍니다.</summary>
    public float RevenueMultiplier => reviewCount > 0 ? GetRevenueMultiplier(AverageStars) : 1f;

    /// <summary>누적 리뷰 수를 돌려줍니다.</summary>
    public int ReviewCount => reviewCount;
}

/// <summary>한 손님에게 적용한 평가와 결제 결과를 함께 전달합니다.</summary>
public struct ReviewResult
{
    public Define.ReviewGrade Grade { get; }
    public int Stars { get; }
    public float WaitSeconds { get; }
    public int InterruptionCount { get; }
    public bool MenuMatched { get; }
    public float AverageStars { get; }
    public float RevenueMultiplier { get; }
    public int FinalPay { get; }

    /// <summary>리뷰 계산 결과를 한 번에 보관합니다.</summary>
    public ReviewResult(
        Define.ReviewGrade grade,
        int stars,
        float waitSeconds,
        int interruptionCount,
        bool menuMatched,
        float averageStars,
        float revenueMultiplier,
        int finalPay)
    {
        Grade = grade;
        Stars = stars;
        WaitSeconds = waitSeconds;
        InterruptionCount = interruptionCount;
        MenuMatched = menuMatched;
        AverageStars = averageStars;
        RevenueMultiplier = revenueMultiplier;
        FinalPay = finalPay;
    }
}
