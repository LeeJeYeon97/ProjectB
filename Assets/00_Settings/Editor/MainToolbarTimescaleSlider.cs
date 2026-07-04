using UnityEditor;
using UnityEditor.Toolbars; // ★ 유니티 6.3의 새로운 툴바 API
using UnityEngine;
using UnityEngine.UIElements;

public class MainToolbarTimescaleSlider
{
    // 슬라이더의 최소/최대 속도 범위
    const float k_minTimeScale = 0f;
    const float k_maxTimeScale = 5f;

    // 1. 타임 스케일 슬라이더 추가
    [MainToolbarElement("Timescale/Slider", defaultDockPosition = MainToolbarDockPosition.Middle)]
    public static MainToolbarElement TimeSlider()
    {
        var content = new MainToolbarContent("Time Scale", "Time Scale");

        // 슬라이더 생성 (이름, 현재값, 최소값, 최대값, 값이 변할 때 실행될 함수)
        var slider = new MainToolbarSlider(content, Time.timeScale, k_minTimeScale, k_maxTimeScale, OnSliderValueChanged);

        // (선택) 슬라이더 우클릭 시 'Reset' 메뉴 추가
        slider.populateContextMenu = (menu) =>
        {
            menu.AppendAction("Reset", _ =>
            {
                Time.timeScale = 1f;
                MainToolbar.Refresh("Timescale/Slider"); // UI 갱신 필수!
            });
        };

        //// UI 툴킷을 이용한 여백(패딩) 스타일링
        //MainToolbarElementStyler.StyleElement<VisualElement>("Timescale/Slider", (element) =>
        //{
        //    element.style.paddingLeft = 10f;
        //});

        return slider;
    }

    // 슬라이더를 움직일 때마다 실제 게임 속도(Time.timeScale)를 변경
    static void OnSliderValueChanged(float newValue)
    {
        Time.timeScale = newValue;
    }

    // 2. 초기화(Reset) 버튼 추가
    [MainToolbarElement("Timescale/Reset", defaultDockPosition = MainToolbarDockPosition.Middle)]
    public static MainToolbarElement ResetTimeScaleButton()
    {
        // 유니티 내장 아이콘 중 'Refresh' 아이콘 가져오기
        var icon = EditorGUIUtility.IconContent("Refresh").image as Texture2D;
        var content = new MainToolbarContent(icon, "Reset");

        // 버튼 생성 및 클릭 시 실행할 로직
        var button = new MainToolbarButton(content, () =>
        {
            Time.timeScale = 1f;
            MainToolbar.Refresh("Timescale/Slider"); // 슬라이더 UI 갱신
        });

        //// 버튼 크기 및 여백 스타일링 (UI가 예쁘게 정렬되도록 세밀한 조정)
        //MainToolbarElementStyler.StyleElement<EditorToolbarButton>("Timescale/Reset", element =>
        //{
        //    element.style.paddingLeft = 0f;
        //    element.style.paddingRight = 0f;
        //    element.style.marginLeft = 0f;
        //    element.style.marginRight = 0f;
        //    element.style.minWidth = 20f;
        //    element.style.maxWidth = 20f;
        //});

        return button;
    }
}