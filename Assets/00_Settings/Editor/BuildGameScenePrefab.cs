using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>UI_GameScene 프리팹에 게임 화면 요소를 한 번 구성합니다.</summary>
public static class BuildGameScenePrefab
{
    private const string PrefabPath = "Assets/03_Prefabs/UI/Scene/UI_GameScene.prefab";
    private static readonly Color PanelColor = new Color(0.06f, 0.08f, 0.1f, 0.88f);

    /// <summary>에디터 메뉴나 명령줄에서 프리팹 구성을 다시 적용합니다.</summary>
    [MenuItem("Tools/UI/Build Game Scene Prefab")]
    public static void BuildFromMenu()
    {
        Build();
    }

    /// <summary>에디터가 열린 뒤 프리팹을 열어 필요한 UI를 만들고 저장합니다.</summary>
    [InitializeOnLoadMethod]
    private static void BuildOnce()
    {
        if (SessionState.GetBool("BuildGameScenePrefab.Done", false))
            return;

        SessionState.SetBool("BuildGameScenePrefab.Done", true);
        EditorApplication.delayCall += Build;
    }

    /// <summary>기존 프리팹의 UI를 유지하면서 빠진 이름의 오브젝트만 보충합니다.</summary>
    private static void Build()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Canvas canvas = root.GetComponent<Canvas>();
            RectTransform rootRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
            if (rootRect == null)
                throw new System.InvalidOperationException("UI_GameScene Canvas를 찾을 수 없습니다.");

            EnsureLabel(root.transform, "Text_DishCount", "깨끗한 접시  0\n더러운 접시  0", new Vector2(32, -105), new Vector2(340, 112), TextAnchor.UpperLeft);
            EnsureButton(root.transform, "Button_Growth", "성장", new Vector2(1, 1), new Vector2(-115, -65), new Vector2(190, 66));
            EnsurePanel(root.transform, "Panel_Growth", new Vector2(1, 1), new Vector2(-18, -140), new Vector2(420, 820));

            Transform panel = root.transform.Find("Panel_Growth");
            EnsureLabel(panel, "Text_SeatCount", "현재 좌석 0개", new Vector2(12, -16), new Vector2(380, 44), TextAnchor.MiddleLeft);
            EnsureButton(panel, "Button_player_speed", "이동 속도", new Vector2(0, 1), new Vector2(12, -72), new Vector2(380, 58));
            EnsureButton(panel, "Button_player_stack", "운반 개수", new Vector2(0, 1), new Vector2(12, -138), new Vector2(380, 58));
            EnsureButton(panel, "Button_player_interact", "작업 시간", new Vector2(0, 1), new Vector2(12, -204), new Vector2(380, 58));
            EnsureButton(panel, "Button_machine_capacity", "기계 용량", new Vector2(0, 1), new Vector2(12, -270), new Vector2(380, 58));
            // 직원별 항목을 담을 목록을 먼저 만들고 제목과 복제용 버튼을 그 안에 둡니다.
            Transform employeeRoot = EnsureEmployeeGrowthRoot(panel);
            EnsureLabel(employeeRoot, "Text_EmployeeWorkGrowth", "직원별 능력치 (직원을 고용하세요)", Vector2.zero, new Vector2(380, 42), TextAnchor.MiddleLeft);
            Button template = EnsureButton(employeeRoot, "Button_EmployeeGrowthTemplate", "직원 능력치", Vector2.zero, Vector2.zero, new Vector2(380, 54));
            template.gameObject.SetActive(false);
            EnsureButton(panel, "Button_PlaceChair", "의자 배치", new Vector2(0, 1), new Vector2(12, -756), new Vector2(180, 54));
            panel.gameObject.SetActive(false);

            if (root.GetComponent<FurniturePlacementController>() == null)
                root.AddComponent<FurniturePlacementController>();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("UI_GameScene 프리팹 UI 구성을 완료했습니다.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    /// <summary>직원별 업그레이드 항목을 위에서 아래로 쌓아 표시할 영역을 준비합니다.</summary>
    private static Transform EnsureEmployeeGrowthRoot(Transform parent)
    {
        Transform found = parent.Find("Panel_EmployeeGrowth");
        GameObject root = found != null
            ? found.gameObject
            : new GameObject("Panel_EmployeeGrowth", typeof(RectTransform), typeof(VerticalLayoutGroup));

        if (found == null)
            root.transform.SetParent(parent, false);

        RectTransform rect = root.GetComponent<RectTransform>();
        Place(rect, new Vector2(0, 1), new Vector2(12, -338), new Vector2(380, 390));

        VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 4;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // 이전 버전에서 패널 바로 밑에 있던 오브젝트도 기존 참조를 유지하며 목록 안으로 옮깁니다.
        Transform title = parent.Find("Text_EmployeeWorkGrowth");
        if (title != null)
            title.SetParent(root.transform, false);

        Transform template = parent.Find("Button_EmployeeGrowthTemplate");
        if (template != null)
            template.SetParent(root.transform, false);

        return root.transform;
    }

    /// <summary>지정한 위치에 반투명 배경 패널을 만들거나 기존 패널을 반환합니다.</summary>
    private static GameObject EnsurePanel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
    {
        Transform found = parent.Find(name);
        GameObject panel = found != null
            ? found.gameObject
            : new GameObject(name, typeof(RectTransform), typeof(Image));
        if (found == null)
            panel.transform.SetParent(parent, false);
        Place(panel.GetComponent<RectTransform>(), anchor, position, size);
        Image image = panel.GetComponent<Image>();
        image.color = PanelColor;
        image.raycastTarget = false;
        return panel;
    }

    /// <summary>버튼과 버튼 안의 글자를 만들고 레이아웃 값을 설정합니다.</summary>
    private static Button EnsureButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size)
    {
        Transform found = parent.Find(name);
        GameObject buttonObject = found != null
            ? found.gameObject
            : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        if (found == null)
            buttonObject.transform.SetParent(parent, false);
        Place(buttonObject.GetComponent<RectTransform>(), anchor, position, size);
        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.15f, 0.3f, 0.38f, 0.96f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        TMP_Text text = EnsureLabel(buttonObject.transform, "Text_Label", label, Vector2.zero, size, TextAnchor.MiddleCenter);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(8, 4);
        text.rectTransform.offsetMax = new Vector2(-8, -4);
        return button;
    }

    /// <summary>이름으로 찾을 수 있는 TextMeshPro 글자를 만들고 정렬합니다.</summary>
    private static TMP_Text EnsureLabel(Transform parent, string name, string value, Vector2 position, Vector2 size, TextAnchor anchor)
    {
        Transform found = parent.Find(name);
        GameObject textObject = found != null
            ? found.gameObject
            : new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        if (found == null)
            textObject.transform.SetParent(parent, false);
        Place(textObject.GetComponent<RectTransform>(), new Vector2(0, 1), position, size);
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = value;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = 24;
        text.color = Color.white;
        text.alignment = anchor == TextAnchor.MiddleCenter ? TextAlignmentOptions.Center : TextAlignmentOptions.Left;
        text.raycastTarget = false;
        return text;
    }

    /// <summary>왼쪽 위 또는 지정 앵커 기준으로 크기와 위치를 통일해 설정합니다.</summary>
    private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
