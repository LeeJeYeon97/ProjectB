using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>기계 업그레이드 월드 UI 프리팹을 만들고 맥주 기계 프리팹에 연결합니다.</summary>
public static class CreateMachineUpgradePrefabEditor
{
    private const string UiPrefabPath = "Assets/03_Prefabs/UI/SubItem/UI_MachineUpgrade.prefab";
    private const string MachinePrefabPath = "Assets/03_Prefabs/Interaction/BeerMachine.prefab";
    private const string KoreanFontPath = "Assets/00_Settings/Fonts/NotoSansKR SDF.asset";

    /// <summary>버튼, 글자, 배경을 미리 만든 월드 공간 UI 프리팹을 저장합니다.</summary>
    [MenuItem("Tools/ProjectB/기계 업그레이드 UI 프리팹 만들기")]
    public static void CreateAndConnect()
    {
        TMP_FontAsset koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
        if (koreanFont == null)
        {
            Debug.LogError($"한글 폰트 에셋을 찾지 못했습니다: {KoreanFontPath}");
            return;
        }

        GameObject root = CreateUiRoot(koreanFont);
        PrefabUtility.SaveAsPrefabAsset(root, UiPrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        ConnectUiToBeerMachine();
        Debug.Log($"기계 업그레이드 UI 프리팹 생성 및 연결 완료: {UiPrefabPath}");
    }

    /// <summary>월드 공간 Canvas와 미리 준비한 패널 요소를 가진 UI 오브젝트를 만듭니다.</summary>
    private static GameObject CreateUiRoot(TMP_FontAsset koreanFont)
    {
        GameObject root = new GameObject(
            "UI_MachineUpgrade",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup),
            typeof(UI_MachineUpgrade));

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(440f, 310f);
        rootRect.localScale = Vector3.one * 0.004f;

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        Image tail = CreateImage("Image_Tail", root.transform, new Vector2(34f, 34f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -132f), new Color(0.055f, 0.09f, 0.14f, 0.98f));
        tail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        tail.raycastTarget = false;

        RectTransform panel = CreateRect("Panel_Background", root.transform, new Vector2(440f, 270f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 18f));
        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.055f, 0.09f, 0.14f, 0.98f);
        panelImage.raycastTarget = true;

        RectTransform header = CreateRect("Panel_Header", panel, new Vector2(0f, 58f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0.5f, 1f));
        Image headerImage = header.gameObject.AddComponent<Image>();
        headerImage.color = new Color(0.08f, 0.35f, 0.45f, 1f);
        headerImage.raycastTarget = false;

        CreateText("Text_Title", header, koreanFont, "기계 업그레이드", new Vector2(270f, 42f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), 29f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        CreateButton("Button_Close", header, koreanFont, "×", new Vector2(42f, 42f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -8f), new Color(0.12f, 0.22f, 0.28f, 1f), 30f);

        CreateUpgradeRow(panel, koreanFont, "Capacity", new Vector2(0f, 28f), new Color(0.1f, 0.27f, 0.32f, 1f), new Color(0.12f, 0.48f, 0.55f, 1f), "잔 보관량", "Text_CapacityName", "Text_CapacityInfo", "Text_CapacityPrice", "Button_Capacity");
        CreateUpgradeRow(panel, koreanFont, "Speed", new Vector2(0f, -66f), new Color(0.28f, 0.2f, 0.14f, 1f), new Color(0.68f, 0.37f, 0.13f, 1f), "제조 속도", "Text_SpeedName", "Text_SpeedInfo", "Text_SpeedPrice", "Button_Speed");

        // UI_Base가 자식 이름으로 버튼과 글자를 찾도록 정해둔 이름을 그대로 사용합니다.
        UI_MachineUpgrade upgradeUI = root.GetComponent<UI_MachineUpgrade>();
        SerializedObject serializedUI = new SerializedObject(upgradeUI);
        serializedUI.FindProperty("worldCanvas").objectReferenceValue = canvas;
        serializedUI.FindProperty("panelGroup").objectReferenceValue = canvasGroup;
        serializedUI.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    /// <summary>한 업그레이드의 이름, 현재 단계, 구매 버튼을 미리 배치합니다.</summary>
    private static void CreateUpgradeRow(Transform parent, TMP_FontAsset koreanFont, string rowName, Vector2 position, Color rowColor, Color buttonColor, string title, string nameObject, string infoObject, string priceObject, string buttonObject)
    {
        RectTransform row = CreateRect("Panel_" + rowName, parent, new Vector2(408f, 78f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position);
        Image rowImage = row.gameObject.AddComponent<Image>();
        rowImage.color = rowColor;
        rowImage.raycastTarget = false;

        CreateText(nameObject, row, koreanFont, title, new Vector2(236f, 28f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -8f), 24f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        CreateText(infoObject, row, koreanFont, "", new Vector2(236f, 26f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f, 8f), 18f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        CreateButton(buttonObject, row, koreanFont, "구매", new Vector2(132f, 54f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10f, 0f), buttonColor, 20f, priceObject);
    }

    /// <summary>지정한 이름과 크기, 위치에 자식 RectTransform을 미리 만듭니다.</summary>
    private static RectTransform CreateRect(string objectName, Transform parent, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2? pivot = null)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform));
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    /// <summary>배경과 말풍선 꼬리에 사용할 Image를 미리 만듭니다.</summary>
    private static Image CreateImage(string objectName, Transform parent, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Color color)
    {
        RectTransform rect = CreateRect(objectName, parent, size, anchorMin, anchorMax, position);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    /// <summary>한글 폰트와 대비 색상을 적용한 TextMeshPro 글자를 미리 배치합니다.</summary>
    private static TMP_Text CreateText(string objectName, Transform parent, TMP_FontAsset koreanFont, string text, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, float fontSize, FontStyles style, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(objectName, parent, size, anchorMin, anchorMax, position);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = koreanFont;
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = Color.white;
        label.alignment = alignment;
        label.enableAutoSizing = true;
        label.fontSizeMin = fontSize - 4f;
        label.fontSizeMax = fontSize;
        label.raycastTarget = false;
        return label;
    }

    /// <summary>미리 만든 Image와 글자를 연결해 눌림 가능한 업그레이드 버튼을 만듭니다.</summary>
    private static Button CreateButton(string objectName, Transform parent, TMP_FontAsset koreanFont, string text, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Color color, float fontSize, string childTextName = null)
    {
        RectTransform rect = CreateRect(objectName, parent, size, anchorMin, anchorMax, position);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        string labelName = childTextName ?? "Text_" + objectName.Replace("Button_", "");
        TMP_Text label = CreateText(labelName, rect, koreanFont, text, new Vector2(-12f, -8f), Vector2.zero, Vector2.one, Vector2.zero, fontSize, FontStyles.Bold, TextAlignmentOptions.Center);
        label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        return button;
    }

    /// <summary>저장된 업그레이드 UI를 맥주 기계 프리팹 자식으로 두고 참조를 연결합니다.</summary>
    private static void ConnectUiToBeerMachine()
    {
        GameObject uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabPath);
        if (uiPrefab == null)
        {
            Debug.LogError($"UI 프리팹을 불러오지 못했습니다: {UiPrefabPath}");
            return;
        }

        GameObject machineRoot = PrefabUtility.LoadPrefabContents(MachinePrefabPath);
        try
        {
            BeerMachine machine = machineRoot.GetComponent<BeerMachine>();
            if (machine == null)
            {
                Debug.LogError($"기계 프리팹에 BeerMachine이 없습니다: {MachinePrefabPath}");
                return;
            }

            UI_MachineUpgrade oldUI = machineRoot.GetComponentInChildren<UI_MachineUpgrade>(true);
            if (oldUI != null)
                Object.DestroyImmediate(oldUI.gameObject);

            GameObject uiObject = PrefabUtility.InstantiatePrefab(uiPrefab, machineRoot.transform) as GameObject;
            if (uiObject == null)
            {
                Debug.LogError("UI 프리팹을 기계 프리팹 안에 넣지 못했습니다.");
                return;
            }

            RectTransform uiRect = uiObject.GetComponent<RectTransform>();
            uiRect.localPosition = new Vector3(0f, 2.2f, 0f);
            uiRect.localRotation = Quaternion.identity;
            uiRect.localScale = Vector3.one * 0.004f;

            SerializedObject serializedMachine = new SerializedObject(machine);
            serializedMachine.FindProperty("upgradeUI").objectReferenceValue = uiObject.GetComponent<UI_MachineUpgrade>();
            serializedMachine.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(machineRoot, MachinePrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(machineRoot);
        }
    }
}
