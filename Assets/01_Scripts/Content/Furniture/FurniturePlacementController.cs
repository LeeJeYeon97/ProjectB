using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.SceneManagement;

/// <summary>가구 미리보기와 배치, 기존 가구 이동과 배치 정보 저장을 관리합니다.</summary>
public class FurniturePlacementController : MonoBehaviour
{
    public event Action OnInventoryChanged;
    private const string SaveKey = "ProjectB.Furniture.Save";
    private const int SaveVersion = 1;
    private const string InventorySaveKey = "ProjectB.Furniture.Inventory";

    /// <summary>가구의 배치 위치는 매장별로 보관하고, 기존 GameScene 저장은 그대로 읽습니다.</summary>
    private string GetPlacementSaveKey()
    {
        // 구매한 가구의 재고는 공통 키를 사용하지만, 놓은 좌표는 각 매장의 맵에 속합니다.
        // 이전부터 사용한 GameScene은 원래 키를 유지해 기존 플레이 기록을 보존합니다.
        string sceneName = gameObject.scene.name;
        return sceneName == "GameScene" ? SaveKey : $"{SaveKey}.{sceneName}";
    }

    [Header("가구와 바닥 설정")]
    [SerializeField] private FurnitureCatalog catalog;
    [SerializeField] private LayerMask floorMask = 1;
    [SerializeField] private float floorLevel;
    [SerializeField, Min(1f)] private float maxRayDistance = 100f;
    [SerializeField, Min(0.05f)] private float floorHeightTolerance = 0.2f;
    [SerializeField] private Color placeColor = new Color(0.55f, 1f, 0.55f, 1f);
    [SerializeField] private Color blockedColor = new Color(1f, 0.45f, 0.45f, 1f);

    private readonly List<GameObject> placedFurniture = new List<GameObject>();
    private readonly List<Renderer> previewRenderers = new List<Renderer>();
    private readonly List<Material> previewMaterials = new List<Material>();

    private GameObject preview;
    private FurnitureItem selectedItem;
    private Collider placementFloor;
    private ThirdPersonController playerMovement;
    private UI_DynamicJoystick joystick;
    private bool joystickWasActive;
    private GameObject placementControls;
    private GameObject inventoryPanel;
    private TMP_Text statusText;
    private Button placeButton;
    private MaterialPropertyBlock colorBlock;
    private LineRenderer rangeLine;
    private GameObject movingFurniture;
    private Vector3 movingStartPosition;
    private Quaternion movingStartRotation;
    private bool isMovingFurniture;
    private bool isPlacementMode;
    private bool isDraggingInventoryItem;
    private readonly List<Renderer> movingRenderers = new List<Renderer>();
    private readonly Dictionary<string, int> furnitureInventory = new Dictionary<string, int>();
    private bool inventoryLoaded;

    private bool canPlace;
    private bool hasPreviewPosition;
    private bool usesTouchInput;
    private bool playerMovementWasEnabled;
    private float rotationY;

    /// <summary>저장 데이터를 준비하고 배치 조작 UI를 만든 뒤 기존 가구를 불러옵니다.</summary>
    private void Start()
    {
        if (catalog == null)
            catalog = Managers.Resource.LoadResource<FurnitureCatalog>("FurnitureCatalog");

        colorBlock = new MaterialPropertyBlock();
        CreatePlacementControls();
        LoadFurnitureInventory();
        // UI가 먼저 만들어졌어도 저장된 재고에 맞춰 배치 버튼을 다시 갱신합니다.
        OnInventoryChanged?.Invoke();
        LoadPlacedFurniture();
    }

    /// <summary>선택한 가구의 위치와 유효 상태를 매 프레임 갱신합니다.</summary>
    private void Update()
    {
        // 배치 전후 어느 때든 가구를 누르면 새 설치보다 기존 가구 이동을 먼저 처리합니다.
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !IsPointerOverUI(-1))
        {
            if (TrySelectPlacedFurniture(Mouse.current.position.ReadValue()))
                return;
        }
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            Vector2 touchPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            int touchId = Touchscreen.current.primaryTouch.touchId.ReadValue();
            if (!IsPointerOverUI(touchId) && TrySelectPlacedFurniture(touchPosition))
                return;
        }

        if (selectedItem == null || preview == null)
            return;

        UpdatePreview();

        // 키보드는 에디터 확인용으로 유지하고, 모바일은 화면 버튼으로 같은 기능을 사용합니다.
        if (Keyboard.current != null)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CancelPlacement();
                return;
            }

            if (Keyboard.current.qKey.wasPressedThisFrame)
                RotateLeft();
            if (Keyboard.current.eKey.wasPressedThisFrame)
                RotateRight();
        }

        // 터치 누름은 위치 조정에만 사용합니다. 모바일 확정은 별도 배치 버튼에서만 일어납니다.
        // 마우스 클릭 확정은 에디터에서 빠르게 확인할 수 있도록 남겨 둡니다.
        if (Touchscreen.current == null &&
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame &&
            !IsPointerOverUI(-1))
        {
            ConfirmPlacement();
        }
    }

    /// <summary>ID에 맞는 가구를 찾아 미리보기를 만들고 배치 모드를 시작합니다.</summary>
    public bool BeginPlacement(string furnitureId)
    {
        if (selectedItem != null)
            CancelPlacement();
        // 배치 버튼을 누르면 미리보기 대신 보유 중인 미배치 가구 목록을 엽니다.
        isPlacementMode = true;
        isDraggingInventoryItem = false;
        selectedItem = null;
        hasPreviewPosition = false;
        canPlace = false;
        PrepareFurnitureMoveMode();
        rotationY = 0f;
        usesTouchInput = false;
        placementFloor = null;
        if (inventoryPanel != null)
            inventoryPanel.SetActive(true);
        RefreshInventoryPanel();
        SetStatus("아래 목록에서 보유 가구를 끌어오거나 기존 의자를 선택하세요.");
        return true;
    }

    /// <summary>보유 목록의 가구를 끌기 시작할 때 반투명 미리보기를 만듭니다.</summary>
    private bool BeginDraggingFurniture(string furnitureId, PointerEventData eventData)
    {
        if (!isPlacementMode || GetInventoryCount(furnitureId) <= 0)
            return false;

        FurnitureItem item = FindItem(furnitureId);
        if (item == null || item.prefab == null)
            return false;
        if (isMovingFurniture && movingFurniture != null)
        {
            // 새 가구를 고르면 진행 중이던 기존 가구 이동은 원래 자리로 되돌립니다.
            movingFurniture.transform.SetPositionAndRotation(movingStartPosition, movingStartRotation);
            SetMovingRenderersVisible(true);
            movingFurniture = null;
            isMovingFurniture = false;
        }
        if (selectedItem != null)
            CancelSelectedPreview();

        selectedItem = item;
        preview = Managers.Resource.InstantiateUnpooled(item.prefab);
        if (preview == null)
        {
            selectedItem = null;
            return false;
        }

        preview.name = "FurniturePreview";
        preview.transform.localScale = item.prefab.transform.localScale;
        preview.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
        PreparePreview();
        isMovingFurniture = false;
        movingFurniture = null;
        isDraggingInventoryItem = true;
        hasPreviewPosition = false;
        canPlace = false;
        usesTouchInput = eventData.pointerId >= 0;
        return true;
    }

    /// <summary>새 가구 미리보기만 없애고 배치 모드와 목록은 그대로 둡니다.</summary>
    private void CancelSelectedPreview()
    {
        if (preview != null)
            Managers.Resource.DestroyUnpooled(preview);
        preview = null;
        foreach (Material material in previewMaterials)
            if (material != null) Destroy(material);
        previewMaterials.Clear();
        previewRenderers.Clear();
        selectedItem = null;
        isDraggingInventoryItem = false;
        hasPreviewPosition = false;
        canPlace = false;
    }

    /// <summary>드래그를 마치면 미리보기 위치를 유지해 배치를 확정할 수 있게 합니다.</summary>
    private void EndDraggingFurniture()
    {
        isDraggingInventoryItem = false;
    }

    /// <summary>배치 모드에서 아직 설치하지 않은 가구만 아래쪽 목록에 보여줍니다.</summary>
    private void RefreshInventoryPanel()
    {
        if (inventoryPanel == null)
            return;

        // 이전 수량 표시를 지우고 현재 보유한 가구마다 드래그 항목을 다시 만듭니다.
        for (int index = inventoryPanel.transform.childCount - 1; index >= 0; index--)
            Destroy(inventoryPanel.transform.GetChild(index).gameObject);

        if (!isPlacementMode || catalog == null || catalog.items == null)
            return;

        int visibleIndex = 0;
        foreach (FurnitureItem item in catalog.items)
        {
            if (item == null || item.prefab == null || GetInventoryCount(item.id) <= 0)
                continue;

            CreateInventoryCard(item, visibleIndex);
            visibleIndex++;
        }

        if (visibleIndex == 0)
            CreateInventoryEmptyMessage();
    }

    /// <summary>설치하지 않은 가구가 없을 때 목록에 짧은 안내를 표시합니다.</summary>
    private void CreateInventoryEmptyMessage()
    {
        GameObject messageObject = new GameObject("Text_EmptyFurnitureInventory", typeof(RectTransform), typeof(TextMeshProUGUI));
        messageObject.transform.SetParent(inventoryPanel.transform, false);
        RectTransform messageRect = messageObject.GetComponent<RectTransform>();
        messageRect.anchorMin = Vector2.zero;
        messageRect.anchorMax = Vector2.one;
        messageRect.offsetMin = new Vector2(8f, 4f);
        messageRect.offsetMax = new Vector2(-8f, -4f);
        TMP_Text message = messageObject.GetComponent<TMP_Text>();
        message.font = TMP_Settings.defaultFontAsset;
        message.text = "배치할 가구가 없습니다. 먼저 가구를 구매해 주세요.";
        message.fontSize = 16f;
        message.color = Color.white;
        message.alignment = TextAlignmentOptions.Center;
        message.raycastTarget = false;
    }

    /// <summary>가구 이름과 수량을 표시하고 드래그 입력을 연결한 목록 항목을 만듭니다.</summary>
    private void CreateInventoryCard(FurnitureItem item, int index)
    {
        GameObject card = new GameObject("FurnitureItem_" + item.id, typeof(RectTransform), typeof(Image), typeof(EventTrigger));
        card.transform.SetParent(inventoryPanel.transform, false);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0f, 0.5f);
        cardRect.anchorMax = new Vector2(0f, 0.5f);
        cardRect.pivot = new Vector2(0f, 0.5f);
        cardRect.anchoredPosition = new Vector2(8f + index * 108f, 0f);
        cardRect.sizeDelta = new Vector2(100f, 76f);
        card.GetComponent<Image>().color = new Color(0.18f, 0.32f, 0.38f, 1f);

        GameObject labelObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(card.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(4f, 4f);
        labelRect.offsetMax = new Vector2(-4f, -4f);
        TMP_Text label = labelObject.GetComponent<TMP_Text>();
        label.font = TMP_Settings.defaultFontAsset;
        label.text = item.title + "\n보유 " + GetInventoryCount(item.id) + "개";
        label.fontSize = 16f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        EventTrigger trigger = card.GetComponent<EventTrigger>();
        trigger.triggers = new List<EventTrigger.Entry>();
        AddInventoryDragEvent(trigger, EventTriggerType.BeginDrag, data =>
        {
            if (BeginDraggingFurniture(item.id, (PointerEventData)data))
                UpdatePreview();
        });
        AddInventoryDragEvent(trigger, EventTriggerType.Drag, data =>
        {
            if (isDraggingInventoryItem)
                UpdatePreview();
        });
        AddInventoryDragEvent(trigger, EventTriggerType.EndDrag, data => EndDraggingFurniture());
    }

    /// <summary>목록 가구 항목에 드래그 시작, 이동, 종료 동작을 등록합니다.</summary>
    private static void AddInventoryDragEvent(EventTrigger trigger, EventTriggerType eventType, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }

    /// <summary>가구를 옮기는 동안 플레이어 입력을 막고 배치 조작 버튼을 보여줍니다.</summary>
    private void PrepareFurnitureMoveMode()
    {
        // 플레이어 이동 입력을 잠시 끄고 모드가 끝날 때 원래 상태로 복구합니다.
        playerMovement = PlayerController.Instance != null
            ? PlayerController.Instance.GetComponent<ThirdPersonController>()
            : null;
        if (playerMovement != null)
        {
            playerMovementWasEnabled = playerMovement.enabled;
            playerMovement.enabled = false;
        }

        // 터치 조이스틱이 바닥 입력을 가로채지 않도록 이동 모드 동안 숨깁니다.
        joystick = FindAnyObjectByType<UI_DynamicJoystick>();
        if (joystick != null)
        {
            joystickWasActive = joystick.gameObject.activeSelf;
            joystick.gameObject.SetActive(false);
        }

        if (placementControls != null)
            placementControls.SetActive(true);
    }

    /// <summary>가구를 먼저 구매해 재고에 넣고, 실제 배치는 나중에 진행하도록 분리합니다.</summary>
    public bool BuyFurniture(string furnitureId)
    {
        FurnitureItem item = FindItem(furnitureId);
        PlayerController player = PlayerController.Instance;
        if (item == null || player == null || !player.TrySpendMoney(item.price))
        {
            SetStatus("가구를 구매할 수 없습니다. 재화와 가구 설정을 확인해 주세요.");
            return false;
        }

        furnitureInventory[furnitureId] = GetInventoryCount(furnitureId) + 1;
        SaveFurnitureInventory();
        RefreshInventoryPanel();
        OnInventoryChanged?.Invoke();
        SetStatus($"{item.title} 구매 완료 · 보유 {GetInventoryCount(furnitureId)}개");
        return true;
    }

    /// <summary>현재 재고 수를 돌려줘 구매 버튼과 배치 버튼 상태에 활용합니다.</summary>
    public int GetInventoryCount(string furnitureId)
    {
        // UI 초기화가 Start보다 먼저 실행되어도 저장된 재고를 정확히 보여 줍니다.
        LoadFurnitureInventory();
        return furnitureInventory.TryGetValue(furnitureId, out int count) ? count : 0;
    }

    /// <summary>카탈로그에서 ID에 맞는 가구 설정을 찾아옵니다.</summary>
    private FurnitureItem FindItem(string furnitureId)
    {
        return catalog != null && catalog.items != null
            ? catalog.items.Find(item => item != null && item.id == furnitureId)
            : null;
    }

    /// <summary>구매한 가구 수를 재접속 뒤에도 유지합니다.</summary>
    private void LoadFurnitureInventory()
    {
        // 재고를 여러 곳에서 조회해도 저장 데이터를 한 번만 읽습니다.
        if (inventoryLoaded) return;
        inventoryLoaded = true;

        FurnitureInventoryData data = Managers.PlayerPrefs.GetJson<FurnitureInventoryData>(InventorySaveKey);
        if (data == null || data.items == null) return;
        foreach (FurnitureInventoryItem item in data.items)
            if (item != null && !string.IsNullOrEmpty(item.id) && item.count > 0)
                furnitureInventory[item.id] = item.count;
    }

    /// <summary>현재 보유 중인 가구 수를 저장합니다.</summary>
    private void SaveFurnitureInventory()
    {
        FurnitureInventoryData data = new FurnitureInventoryData();
        foreach (KeyValuePair<string, int> item in furnitureInventory)
            data.items.Add(new FurnitureInventoryItem { id = item.Key, count = item.Value });
        Managers.PlayerPrefs.SetJson(InventorySaveKey, data);
    }

    /// <summary>미리보기에는 보이는 렌더러만 남기고 물리와 동작 컴포넌트를 끕니다.</summary>
    private void PreparePreview()
    {
        previewRenderers.Clear();
        previewMaterials.Clear();
        previewRenderers.AddRange(preview.GetComponentsInChildren<Renderer>(true));

        // 프리뷰 전용 머티리얼을 만들어 반투명 처리합니다. 원본 가구 머티리얼은 수정하지 않습니다.
        foreach (Renderer previewRenderer in previewRenderers)
        {
            Material[] sourceMaterials = previewRenderer.sharedMaterials;
            Material[] fadedMaterials = new Material[sourceMaterials.Length];
            for (int index = 0; index < sourceMaterials.Length; index++)
            {
                if (sourceMaterials[index] == null)
                    continue;

                Material faded = new Material(sourceMaterials[index]);
                SetTransparent(faded);
                fadedMaterials[index] = faded;
                previewMaterials.Add(faded);
            }
            previewRenderer.sharedMaterials = fadedMaterials;
            previewRenderer.shadowCastingMode = ShadowCastingMode.Off;
            previewRenderer.receiveShadows = false;
        }

        // 미리보기는 실제 좌석이나 상호작용 대상으로 등록되면 안 되므로 하위 스크립트까지 모두 끕니다.
        foreach (MonoBehaviour script in preview.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (script != null)
                script.enabled = false;
        }

        // 미리보기 콜라이더가 바닥 광선과 가구 겹침 검사에 끼어들지 않도록 전부 비활성화합니다.
        foreach (Collider shape in preview.GetComponentsInChildren<Collider>(true))
        {
            if (shape != null)
                shape.enabled = false;
        }

        foreach (Rigidbody body in preview.GetComponentsInChildren<Rigidbody>(true))
        {
            if (body != null)
                body.isKinematic = true;
        }
    }

    /// <summary>프리뷰 전용 머티리얼의 셰이더 값을 바꿔 아래 장면이 비치도록 합니다.</summary>
    private static void SetTransparent(Material material)
    {
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);
        if (material.HasProperty("_AlphaClip"))
            material.SetFloat("_AlphaClip", 0f);

        // Synty 셰이더는 파이프라인별 속성을 함께 가지므로 프로젝트 기본 셰이더 값도 투명 상태로 맞춥니다.
        if (material.HasProperty("_BUILTIN_Surface"))
            material.SetFloat("_BUILTIN_Surface", 1f);
        if (material.HasProperty("_BUILTIN_Blend"))
            material.SetFloat("_BUILTIN_Blend", 0f);
        if (material.HasProperty("_BUILTIN_SrcBlend"))
            material.SetFloat("_BUILTIN_SrcBlend", (float)BlendMode.SrcAlpha);
        if (material.HasProperty("_BUILTIN_DstBlend"))
            material.SetFloat("_BUILTIN_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_BUILTIN_ZWrite"))
            material.SetFloat("_BUILTIN_ZWrite", 0f);

        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)RenderQueue.Transparent;
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
    }

    /// <summary>현재 터치 위치를 바닥으로 투영해 자유 위치 미리보기와 배치 가능 여부를 갱신합니다.</summary>
    private void UpdatePreview()
    {
        if (Camera.main == null || !TryReadPointer(out Vector2 pointer, out int pointerId))
            return;

        // UI 버튼을 누르는 동안은 직전 바닥 위치를 유지해 버튼 클릭이 바닥 이동으로 처리되지 않게 합니다.
        // 마우스는 UI 위에서도 위치를 계속 보여주되, 터치는 조작 버튼을 누를 때만 이동을 멈춥니다.
        if (pointerId >= 0 && IsPointerOverUI(pointerId) && !isDraggingInventoryItem)
            return;

        Ray ray = Camera.main.ScreenPointToRay(pointer);
        RaycastHit[] hits = Physics.RaycastAll(ray, maxRayDistance, floorMask, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
        bool foundFloor = false;
        RaycastHit hit = default;
        foreach (RaycastHit candidate in hits)
        {
            // 의자나 벽이 먼저 맞더라도 그 뒤에 있는 유효한 바닥까지 확인합니다.
            if (!IsValidFloorPoint(candidate.point, candidate.normal)) continue;
            hit = candidate;
            foundFloor = true;
            break;
        }
        if (!foundFloor)
        {
            placementFloor = null;
            canPlace = false;
            UpdatePlacementFeedback();
            return;
        }

        placementFloor = hit.collider;
        if (!IsValidFloorPoint(hit.point, hit.normal))
        {
            placementFloor = null;
            canPlace = false;
            UpdatePlacementFeedback();
            return;
        }

        Vector3 position = hit.point + Vector3.up * GetHeightOffset();
        // 회전 트윈이 도는 동안 방향을 매 프레임 덮어쓰지 않고 위치만 갱신합니다.
        preview.transform.position = position;
        hasPreviewPosition = true;
        canPlace = IsPositionFree(position);
        UpdatePlacementFeedback();
    }

    /// <summary>가구 바닥 크기와 회전을 기준으로 방 경계, 장애물, 기존 의자 간격을 확인합니다.</summary>
    private bool IsPositionFree(Vector3 position)
    {
        if (placementFloor == null || selectedItem == null || preview == null)
            return false;

        Vector2 size = GetPlacementSize();
        Vector3 halfSize = new Vector3(size.x * 0.5f, GetPlacementHeight() * 0.5f, size.y * 0.5f);
        Quaternion rotation = Quaternion.Euler(0f, rotationY, 0f);
        // 바닥과 맞닿는 면이 겹침으로 잡히지 않도록 검사 상자를 아주 조금 띄웁니다.
        Vector3 checkCenter = position + Vector3.up * (halfSize.y + 0.02f);

        // 중심만 바닥에 닿는 배치를 막고 가구 네 모서리도 같은 유효 바닥 위에 있는지 확인합니다.
        if (!IsFootprintOnFloor(position, size, rotation))
            return false;

        Collider[] nearby = Physics.OverlapBox(
            checkCenter,
            halfSize,
            rotation,
            ~0,
            QueryTriggerInteraction.Ignore);

        foreach (Collider shape in nearby)
        {
            if (shape == null || shape == placementFloor || IsFloorCollider(shape) || shape.transform.IsChildOf(preview.transform) ||
                (movingFurniture != null && (shape.transform == movingFurniture.transform || shape.transform.IsChildOf(movingFurniture.transform))))
                continue;

            // 사람은 움직이는 대상이므로 설치 불가 장애물로 취급하지 않습니다.
            if (shape.GetComponentInParent<PlayerController>() != null ||
                shape.GetComponentInParent<CustomerController>() != null ||
                shape.GetComponentInParent<EmployeeController>() != null)
                continue;

            return false;
        }

        // 좌석은 상호작용용 트리거를 쓰므로 별도로 검사해 너무 가까운 의자 배치를 막습니다.
        if (!isMovingFurniture && selectedItem.id == "chair" && IsTooCloseToSeat(position))
            return false;

        return true;
    }

    /// <summary>가구 밑면의 중심과 네 모서리가 배치 가능한 바닥 위에 있는지 확인합니다.</summary>
    private bool IsFootprintOnFloor(Vector3 position, Vector2 size, Quaternion rotation)
    {
        float halfX = size.x * 0.5f;
        float halfZ = size.y * 0.5f;
        Vector3[] corners =
        {
            new Vector3(-halfX, 0f, -halfZ),
            new Vector3(-halfX, 0f, halfZ),
            new Vector3(halfX, 0f, -halfZ),
            new Vector3(halfX, 0f, halfZ)
        };

        foreach (Vector3 corner in corners)
        {
            Vector3 worldCorner = position + rotation * corner;
            Vector3 floorCorner = worldCorner - Vector3.up * GetHeightOffset();
            Vector3 rayStart = floorCorner + Vector3.up * (floorHeightTolerance + 0.1f);
            float rayLength = floorHeightTolerance * 2f + 0.2f;

            float floorY = position.y - GetHeightOffset();
            RaycastHit[] floorHits = Physics.RaycastAll(rayStart, Vector3.down, rayLength, floorMask, QueryTriggerInteraction.Ignore);
            bool foundFloor = false;
            foreach (RaycastHit floorHit in floorHits)
            {
                // 여러 바닥 조각이 겹치거나 가구가 시야를 가려도 높이가 맞는 바닥을 찾습니다.
                if (!IsValidFloorPoint(floorHit.point, floorHit.normal) ||
                    Mathf.Abs(floorHit.point.y - floorY) > floorHeightTolerance)
                    continue;
                foundFloor = true;
                break;
            }
            if (!foundFloor)
                return false;
        }

        return true;
    }

    /// <summary>배치 높이에 맞닿은 얇은 바닥 콜라이더인지 확인해 장애물 검사에서 제외합니다.</summary>
    private bool IsFloorCollider(Collider shape)
    {
        if (shape == null || (floorMask.value & (1 << shape.gameObject.layer)) == 0)
            return false;

        Bounds bounds = shape.bounds;
        return bounds.size.y <= 0.5f && Mathf.Abs(bounds.max.y - floorLevel) <= floorHeightTolerance;
    }

    /// <summary>수평에 가까우며 정해진 매장 바닥 높이에 있는 면만 가구 배치 바닥으로 인정합니다.</summary>
    private bool IsValidFloorPoint(Vector3 point, Vector3 normal)
    {
        return Vector3.Dot(normal, Vector3.up) >= 0.8f &&
               Mathf.Abs(point.y - floorLevel) <= floorHeightTolerance;
    }

    /// <summary>이미 등록된 활성 좌석과 새 의자의 간격이 충분한지 확인합니다.</summary>
    private bool IsTooCloseToSeat(Vector3 position)
    {
        if (Managers.Station == null || Managers.Station.AllSeats == null)
            return false;

        foreach (SeatStation seat in Managers.Station.AllSeats)
        {
            if (seat == null || !seat.isActiveAndEnabled || seat.transform == preview.transform)
                continue;

            Vector2 seatPoint = new Vector2(seat.transform.position.x, seat.transform.position.z);
            Vector2 placePoint = new Vector2(position.x, position.z);
            if (Vector2.Distance(seatPoint, placePoint) < 0.9f)
                return true;
        }

        return false;
    }

    /// <summary>현재 카탈로그 항목의 배치 크기가 비어 있거나 잘못됐을 때 안전한 기본 크기를 돌려줍니다.</summary>
    private Vector2 GetPlacementSize()
    {
        Vector2 size = selectedItem != null ? selectedItem.placementSize : Vector2.zero;
        return new Vector2(Mathf.Max(0.2f, size.x), Mathf.Max(0.2f, size.y));
    }

    /// <summary>바닥 검사에 쓸 높이를 가져와 값이 너무 작지 않도록 제한합니다.</summary>
    private float GetPlacementHeight()
    {
        return selectedItem != null ? Mathf.Max(0.2f, selectedItem.placementHeight) : 1f;
    }

    /// <summary>프리팹 원점이 바닥에서 살짝 떠야 하는 가구의 높이 보정을 가져옵니다.</summary>
    private float GetHeightOffset()
    {
        return selectedItem != null ? selectedItem.heightOffset : 0f;
    }

    /// <summary>유효한 위치에 보유 가구를 설치하거나 기존 가구를 옮긴 뒤 결과를 저장합니다.</summary>
    private void ConfirmPlacement()
    {
        if (!canPlace || !hasPreviewPosition || selectedItem == null)
        {
            SetStatus("초록색으로 표시되는 바닥에 가구를 놓아 주세요.");
            return;
        }

        // 버튼을 누른 프레임에 장애물이 이동했을 수 있으므로 비용을 차감하기 직전에 다시 검사합니다.
        if (!IsPositionFree(preview.transform.position))
        {
            canPlace = false;
            UpdatePlacementFeedback();
            return;
        }

        string furnitureId = selectedItem.id;
        bool isNewPlacement = !isMovingFurniture;
        Vector3 position = preview.transform.position;
        Quaternion rotation = Quaternion.Euler(0f, rotationY, 0f);
        GameObject placed = movingFurniture;
        if (isMovingFurniture)
        {
            placed.transform.SetPositionAndRotation(position, rotation);
        }
        else
        {
            if (GetInventoryCount(furnitureId) <= 0)
            {
                SetStatus("배치할 가구가 없습니다. 먼저 구매해 주세요.");
                return;
            }
            placed = Managers.Resource.InstantiateUnpooled(selectedItem.prefab, position, rotation);
        }
        if (placed == null)
        {
            SetStatus("가구를 설치하지 못했습니다.");
            return;
        }

        placed.name = furnitureId;
        if (isMovingFurniture)
            SetMovingRenderersVisible(true);
        FurnitureMarker marker = placed.GetComponent<FurnitureMarker>();
        if (marker == null)
            marker = placed.AddComponent<FurnitureMarker>();
        marker.furnitureId = furnitureId;
        if (!isMovingFurniture)
        {
            furnitureInventory[furnitureId] = Mathf.Max(0, GetInventoryCount(furnitureId) - 1);
            SaveFurnitureInventory();
            RefreshInventoryPanel();
            OnInventoryChanged?.Invoke();
        }
        if (!placedFurniture.Contains(placed))
            placedFurniture.Add(placed);
        SavePlacedFurniture();
        // 새 가구 설치만 집계하고, 기존 가구를 옮긴 동작은 배치 퀘스트에 포함하지 않습니다.
        if (isNewPlacement && Managers.Quest != null)
            Managers.Quest.ReportFurniturePlacement(furnitureId);
        placed.transform.DOKill();
        Vector3 targetScale = selectedItem.prefab.transform.localScale;
        placed.transform.localScale = targetScale * 0.85f;
        placed.transform.DOScale(targetScale, 0.22f).SetEase(Ease.OutBack);

        // 취소 정리 단계에서 원래 위치로 되돌리지 않도록 이동 완료 상태를 먼저 끝냅니다.
        isMovingFurniture = false;
        movingFurniture = null;
        CancelPlacement();
        SetStatus("가구를 배치했습니다.");
    }

    /// <summary>배치 모드를 취소하고 미리보기와 입력 잠금을 정리합니다.</summary>
    public void CancelPlacement()
    {
        if (isMovingFurniture && movingFurniture != null)
        {
            movingFurniture.transform.SetPositionAndRotation(movingStartPosition, movingStartRotation);
            SetMovingRenderersVisible(true);
        }
        if (preview != null)
        {
            if (Managers.Resource != null)
                Managers.Resource.DestroyUnpooled(preview);
            else
                Destroy(preview);
        }

        // 프리뷰를 위해 복제한 머티리얼은 원본 가구와 별도로 정리해 메모리에 남지 않게 합니다.
        foreach (Material material in previewMaterials)
        {
            if (material != null)
                Destroy(material);
        }
        previewMaterials.Clear();
        previewRenderers.Clear();

        preview = null;
        movingFurniture = null;
        isMovingFurniture = false;
        selectedItem = null;
        placementFloor = null;
        hasPreviewPosition = false;
        usesTouchInput = false;
        canPlace = false;
        isPlacementMode = false;
        isDraggingInventoryItem = false;

        if (playerMovement != null)
            playerMovement.enabled = playerMovementWasEnabled;
        playerMovement = null;

        if (joystick != null)
            joystick.gameObject.SetActive(joystickWasActive);
        joystick = null;

        if (placementControls != null)
            placementControls.SetActive(false);
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
        if (rangeLine != null)
            rangeLine.enabled = false;
    }

    /// <summary>현재 터치 또는 마우스 좌표와 포인터 ID를 가져옵니다.</summary>
    private bool TryReadPointer(out Vector2 pointer, out int pointerId)
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            usesTouchInput = true;
            pointer = Touchscreen.current.primaryTouch.position.ReadValue();
            pointerId = Touchscreen.current.primaryTouch.touchId.ReadValue();
            return true;
        }

        // 모바일에는 마우스 장치가 함께 잡혀도 손을 뗀 뒤 마지막 터치 위치를 유지합니다.
        // 편집기에서 터치를 사용한 경우에도 배치 모드가 끝날 때까지 마우스 원점으로 돌아가지 않습니다.
        if (usesTouchInput || Application.isMobilePlatform)
        {
            pointer = Vector2.zero;
            pointerId = -1;
            return false;
        }

        if (Mouse.current != null)
        {
            pointer = Mouse.current.position.ReadValue();
            pointerId = -1;
            return true;
        }

        pointer = Vector2.zero;
        pointerId = -1;
        return false;
    }

    /// <summary>현재 입력 위치가 버튼이나 다른 UI 위에 있는지 확인합니다.</summary>
    private static bool IsPointerOverUI(int pointerId)
    {
        if (EventSystem.current == null)
            return false;

        return pointerId >= 0
            ? EventSystem.current.IsPointerOverGameObject(pointerId)
            : EventSystem.current.IsPointerOverGameObject();
    }

    /// <summary>저장된 가구 ID와 위치를 카탈로그에서 찾아 실제 씬 오브젝트로 복원합니다.</summary>
    private void LoadPlacedFurniture()
    {
        string placementSaveKey = GetPlacementSaveKey();
        if (!Managers.PlayerPrefs.HasKey(placementSaveKey) || catalog == null || catalog.items == null)
            return;

        FurnitureSaveData data;
        try
        {
            data = Managers.PlayerPrefs.GetJson<FurnitureSaveData>(placementSaveKey);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"가구 배치 저장을 읽지 못했습니다: {exception.Message}");
            return;
        }

        if (data == null || data.version != SaveVersion || data.items == null)
            return;

        foreach (FurnitureSaveItem saved in data.items)
        {
            if (saved == null || string.IsNullOrEmpty(saved.id))
                continue;

            FurnitureItem item = catalog.items.Find(entry => entry != null && entry.id == saved.id);
            if (item == null || item.prefab == null)
                continue;

            GameObject placed;
            if (saved.isSceneFurniture)
            {
                // 씬에 원래 있던 가구는 새로 만들지 않고 같은 오브젝트의 위치만 복구합니다.
                placed = FindSceneFurniture(saved.scenePath);
                if (placed == null)
                    continue;
                placed.transform.SetPositionAndRotation(saved.GetPosition(), saved.GetRotation());
            }
            else
            {
                placed = Managers.Resource.InstantiateUnpooled(item.prefab, saved.GetPosition(), saved.GetRotation());
            }
            if (placed == null)
                continue;

            placed.name = item.id;
            FurnitureMarker marker = placed.GetComponent<FurnitureMarker>();
            if (marker == null)
                marker = placed.AddComponent<FurnitureMarker>();
            marker.furnitureId = item.id;
            marker.isSceneFurniture = saved.isSceneFurniture;
            marker.scenePath = saved.scenePath;
            placedFurniture.Add(placed);
        }
    }

    /// <summary>현재 배치한 가구를 ID, 위치와 회전값으로 저장합니다.</summary>
    private void SavePlacedFurniture()
    {
        FurnitureSaveData data = new FurnitureSaveData { version = SaveVersion };
        foreach (GameObject placed in placedFurniture)
        {
            if (placed == null)
                continue;

            FurnitureMarker marker = placed.GetComponent<FurnitureMarker>();
            if (marker != null)
                data.items.Add(new FurnitureSaveItem(marker.furnitureId, placed.transform.position, placed.transform.rotation, marker.isSceneFurniture, marker.scenePath));
        }

        // 현재 매장에 놓은 위치만 저장합니다. 다른 매장의 배치는 건드리지 않습니다.
        Managers.PlayerPrefs.SetJson(GetPlacementSaveKey(), data);
    }

    /// <summary>배치 상태 문구와 버튼 활성 상태, 미리보기 색을 한 번에 맞춥니다.</summary>
    private void UpdatePlacementFeedback()
    {
        if (selectedItem == null)
            return;

        if (statusText != null)
        {
            statusText.text = canPlace
                ? $"{selectedItem.title}  ·  범위 안 · 설치 가능\n보유 {GetInventoryCount(selectedItem.id)}개"
                : $"{selectedItem.title}  ·  범위 밖 또는 겹침 · 설치 불가\n위치를 옮기거나 회전해 주세요";
        }

        if (placeButton != null)
            // 새 가구 설치에는 재고가 필요하고, 기존 가구 이동에는 재고가 필요하지 않습니다.
            placeButton.interactable = canPlace && (isMovingFurniture || GetInventoryCount(selectedItem.id) > 0);

        Color tint = canPlace ? placeColor : blockedColor;
        tint.a = 0.55f;
        foreach (Renderer previewRenderer in previewRenderers)
        {
            if (previewRenderer == null)
                continue;

            Material[] materials = previewRenderer.sharedMaterials;
            for (int index = 0; index < materials.Length; index++)
            {
                Material material = materials[index];
                if (material == null)
                    continue;

                previewRenderer.GetPropertyBlock(colorBlock, index);
                if (material.HasProperty("_BaseColor"))
                    colorBlock.SetColor("_BaseColor", tint);
                if (material.HasProperty("_Color"))
                    colorBlock.SetColor("_Color", tint);
                previewRenderer.SetPropertyBlock(colorBlock, index);
            }
        }
        UpdateRangeLine();
    }

    /// <summary>안내 문구가 이미 만들어져 있으면 즉시 갱신하고, 아니면 다음 배치 UI 갱신에 반영합니다.</summary>
    private void SetStatus(string value)
    {
        if (statusText != null)
            statusText.text = value;
    }

    /// <summary>화면 아래에 회전, 배치, 취소 버튼과 위치 안내를 만듭니다.</summary>
    private void CreatePlacementControls()
    {
        if (placementControls != null)
            return;

        placementControls = new GameObject("Panel_PlacementControls", typeof(RectTransform), typeof(Image));
        placementControls.transform.SetParent(transform, false);
        RectTransform panel = placementControls.GetComponent<RectTransform>();
        panel.anchorMin = new Vector2(0.5f, 0f);
        panel.anchorMax = new Vector2(0.5f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.anchoredPosition = new Vector2(0f, 28f);
        panel.sizeDelta = new Vector2(360f, 144f);
        placementControls.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.09f, 0.92f);

        GameObject labelObject = new GameObject("Text_PlacementStatus", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(placementControls.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(0f, -6f);
        labelRect.sizeDelta = new Vector2(-20f, 52f);
        statusText = labelObject.GetComponent<TMP_Text>();
        statusText.font = TMP_Settings.defaultFontAsset;
        statusText.fontSize = 16f;
        statusText.color = Color.white;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.raycastTarget = false;

        CreateControlButton("Button_RotateLeft", "↶", new Vector2(-120f, 16f), RotateLeft);
        CreateControlButton("Button_RotateRight", "↷", new Vector2(-40f, 16f), RotateRight);
        placeButton = CreateControlButton("Button_Place", "배치", new Vector2(40f, 16f), ConfirmPlacement);
        CreateControlButton("Button_CancelPlace", "취소", new Vector2(120f, 16f), CancelPlacement);
        CreateInventoryPanel();
        placementControls.SetActive(false);
        CreateRangeLine();
    }

    /// <summary>배치 조작 버튼 위쪽에 보유 가구를 보여줄 목록 판을 만듭니다.</summary>
    private void CreateInventoryPanel()
    {
        inventoryPanel = new GameObject("Panel_FurnitureInventory", typeof(RectTransform), typeof(Image));
        inventoryPanel.transform.SetParent(placementControls.transform, false);
        RectTransform panel = inventoryPanel.GetComponent<RectTransform>();
        panel.anchorMin = new Vector2(0.5f, 0f);
        panel.anchorMax = new Vector2(0.5f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.anchoredPosition = new Vector2(0f, 154f);
        panel.sizeDelta = new Vector2(460f, 92f);
        inventoryPanel.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.09f, 0.92f);
        inventoryPanel.SetActive(false);
    }

    /// <summary>공통 모양의 버튼과 한국어 글자를 만들고 알맞은 동작을 연결합니다.</summary>
    private Button CreateControlButton(string objectName, string title, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(placementControls.transform, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(72f, 54f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.15f, 0.3f, 0.38f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TMP_Text label = textObject.GetComponent<TMP_Text>();
        label.font = TMP_Settings.defaultFontAsset;
        label.text = title;
        label.fontSize = 22f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return button;
    }

    /// <summary>미리보기 방향을 90도 반시계 방향으로 바꿉니다.</summary>
    private void RotateLeft()
    {
        rotationY -= 90f;
        RefreshPreviewAfterRotation();
    }

    /// <summary>미리보기 방향을 90도 시계 방향으로 바꿉니다.</summary>
    private void RotateRight()
    {
        rotationY += 90f;
        RefreshPreviewAfterRotation();
    }

    /// <summary>회전 직후에도 현재 위치의 배치 가능 여부를 즉시 다시 계산합니다.</summary>
    private void RefreshPreviewAfterRotation()
    {
        if (preview == null || !hasPreviewPosition)
            return;

        preview.transform.DOKill();
        preview.transform.DORotate(new Vector3(0f, rotationY, 0f), 0.18f).SetEase(Ease.OutCubic);
        canPlace = IsPositionFree(preview.transform.position);
        UpdatePlacementFeedback();
    }

    /// <summary>화면에서 가구를 눌렀을 때 기존 배치물을 이동 모드로 불러옵니다.</summary>
    private bool TrySelectPlacedFurniture(Vector2 pointer)
    {
        if (Camera.main == null) return false;
        Ray ray = Camera.main.ScreenPointToRay(pointer);
        // 의자의 좌석 콜라이더가 트리거이므로 선택 광선에서는 트리거도 검사합니다.
        RaycastHit[] hits = Physics.RaycastAll(ray, maxRayDistance, ~0, QueryTriggerInteraction.Collide);
        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
        FurnitureMarker marker = null;
        SeatStation seat = null;
        foreach (RaycastHit hit in hits)
        {
            // 플레이어, 바닥, 벽 콜라이더는 건너뛰고 화면에서 누른 가구 후보를 찾습니다.
            marker = hit.collider.GetComponentInParent<FurnitureMarker>();
            seat = hit.collider.GetComponentInParent<SeatStation>();
            if (marker != null || seat != null)
                break;
        }
        // 씬에 미리 놓인 의자도 가구로 인식해 같은 이동 흐름에서 편집할 수 있게 합니다.
        if (marker == null && seat != null)
        {
            marker = seat.gameObject.AddComponent<FurnitureMarker>();
            marker.furnitureId = "chair";
            marker.isSceneFurniture = true;
            marker.scenePath = GetScenePath(marker.transform);
        }
        if (marker == null) return false;
        if (selectedItem != null && marker.furnitureId != selectedItem.id)
            return false;
        GameObject target = marker.gameObject;
        if (seat != null &&
            (seat.currentCustomer != null || seat.CurrentState != Define.SeatState.Empty))
        {
            SetStatus("손님이 이용 중인 의자는 옮길 수 없습니다.");
            return true;
        }
        FurnitureItem item = FindItem(marker.furnitureId);
        if (item == null) return false;

        // 새 의자 미리보기가 이미 켜져 있으면 정리한 다음 누른 가구를 이동 모드로 엽니다.
        if (selectedItem != null)
            CancelPlacement();

        movingFurniture = target;
        movingStartPosition = target.transform.position;
        movingStartRotation = target.transform.rotation;
        isMovingFurniture = true;
        movingRenderers.Clear();
        movingRenderers.AddRange(target.GetComponentsInChildren<Renderer>(true));
        SetMovingRenderersVisible(false);
        selectedItem = item;
        preview = Managers.Resource.InstantiateUnpooled(item.prefab);
        if (preview == null)
        {
            SetMovingRenderersVisible(true);
            selectedItem = null;
            movingFurniture = null;
            isMovingFurniture = false;
            return true;
        }
        PreparePreview();
        rotationY = target.transform.eulerAngles.y;
        preview.transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);
        hasPreviewPosition = true;
        canPlace = true;
        // 배치 모드 진입 때 저장한 원래 상태를 덮어쓰지 않도록 잠금 참조가 없을 때만 찾습니다.
        if (playerMovement == null)
        {
            playerMovement = PlayerController.Instance != null ? PlayerController.Instance.GetComponent<ThirdPersonController>() : null;
            if (playerMovement != null) { playerMovementWasEnabled = playerMovement.enabled; playerMovement.enabled = false; }
        }
        if (joystick == null)
        {
            joystick = FindAnyObjectByType<UI_DynamicJoystick>();
            if (joystick != null) { joystickWasActive = joystick.gameObject.activeSelf; joystick.gameObject.SetActive(false); }
        }
        if (placementControls != null) placementControls.SetActive(true);
        UpdatePlacementFeedback();
        return true;
    }

    /// <summary>이동 중인 실제 가구를 숨기거나 다시 보여 중복 렌더링을 막습니다.</summary>
    private void SetMovingRenderersVisible(bool visible)
    {
        foreach (Renderer movingRenderer in movingRenderers)
            if (movingRenderer != null)
                movingRenderer.enabled = visible;
        if (visible)
            movingRenderers.Clear();
    }

    /// <summary>현재 씬의 오브젝트를 형제 순번 경로로 기록해 다시 찾을 수 있게 합니다.</summary>
    private static string GetScenePath(Transform target)
    {
        List<int> indices = new List<int>();
        while (target != null)
        {
            indices.Add(target.GetSiblingIndex());
            target = target.parent;
        }
        indices.Reverse();
        return string.Join("/", indices);
    }

    /// <summary>저장된 형제 순번 경로를 따라 씬에 이미 있는 가구를 찾습니다.</summary>
    private static GameObject FindSceneFurniture(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        string[] parts = path.Split('/');
        if (!int.TryParse(parts[0], out int rootIndex)) return null;
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        if (rootIndex < 0 || rootIndex >= roots.Length) return null;
        Transform current = roots[rootIndex].transform;
        for (int index = 1; index < parts.Length; index++)
        {
            if (!int.TryParse(parts[index], out int childIndex) || childIndex < 0 || childIndex >= current.childCount)
                return null;
            current = current.GetChild(childIndex);
        }
        return current.gameObject;
    }

    /// <summary>미리보기 바닥 크기를 선으로 그려서 배치 영역을 한눈에 보여줍니다.</summary>
    private void CreateRangeLine()
    {
        GameObject lineObject = new GameObject("FurniturePlacementRange");
        lineObject.transform.SetParent(transform, false);
        rangeLine = lineObject.AddComponent<LineRenderer>();
        rangeLine.positionCount = 5;
        rangeLine.loop = false;
        rangeLine.useWorldSpace = true;
        rangeLine.startWidth = 0.045f;
        rangeLine.endWidth = 0.045f;
        rangeLine.material = new Material(Shader.Find("Sprites/Default"));
        rangeLine.enabled = false;
    }

    /// <summary>가구 회전에 맞춰 바닥 사각형과 유효 여부 색을 갱신합니다.</summary>
    private void UpdateRangeLine()
    {
        if (rangeLine == null || preview == null || !hasPreviewPosition) { if (rangeLine != null) rangeLine.enabled = false; return; }
        Vector2 size = GetPlacementSize();
        Vector3 center = preview.transform.position - Vector3.up * GetHeightOffset() + Vector3.up * 0.035f;
        Vector3[] points =
        {
            new Vector3(-size.x / 2f, 0f, -size.y / 2f),
            new Vector3(-size.x / 2f, 0f, size.y / 2f),
            new Vector3(size.x / 2f, 0f, size.y / 2f),
            new Vector3(size.x / 2f, 0f, -size.y / 2f),
            new Vector3(-size.x / 2f, 0f, -size.y / 2f)
        };
        for (int i = 0; i < points.Length; i++) rangeLine.SetPosition(i, center + preview.transform.rotation * points[i]);
        Color color = canPlace ? placeColor : blockedColor;
        rangeLine.startColor = color;
        rangeLine.endColor = color;
        rangeLine.enabled = true;
    }

    /// <summary>화면이 닫힐 때 미리보기와 플레이어 입력 잠금을 남기지 않습니다.</summary>
    private void OnDestroy()
    {
        CancelPlacement();
    }
}

/// <summary>구매했지만 아직 설치하지 않은 가구 수를 저장합니다.</summary>
[Serializable]
public class FurnitureInventoryData { public List<FurnitureInventoryItem> items = new List<FurnitureInventoryItem>(); }

/// <summary>가구 ID와 보유 수를 한 항목으로 저장합니다.</summary>
[Serializable]
public class FurnitureInventoryItem { public string id; public int count; }

/// <summary>저장 데이터의 버전과 가구 목록을 보관합니다.</summary>
[Serializable]
public class FurnitureSaveData
{
    public int version;
    public List<FurnitureSaveItem> items = new List<FurnitureSaveItem>();
}

/// <summary>가구 ID와 월드 위치, 회전을 파일에 저장할 수 있는 숫자로 보관합니다.</summary>
[Serializable]
public class FurnitureSaveItem
{
    public string id;
    public bool isSceneFurniture;
    public string scenePath;
    public float x, y, z;
    public float qx, qy, qz, qw;

    public FurnitureSaveItem() { }

    /// <summary>가구 ID, 월드 좌표와 씬 가구 여부를 저장 항목으로 만듭니다.</summary>
    public FurnitureSaveItem(string itemId, Vector3 position, Quaternion rotation, bool isSceneItem = false, string objectPath = null)
    {
        id = itemId;
        isSceneFurniture = isSceneItem;
        scenePath = objectPath;
        x = position.x;
        y = position.y;
        z = position.z;
        qx = rotation.x;
        qy = rotation.y;
        qz = rotation.z;
        qw = rotation.w;
    }

    /// <summary>저장된 좌표 숫자를 가구 월드 위치로 바꿉니다.</summary>
    public Vector3 GetPosition() => new Vector3(x, y, z);

    /// <summary>저장된 회전 숫자를 가구 방향으로 바꿉니다.</summary>
    public Quaternion GetRotation() => new Quaternion(qx, qy, qz, qw);
}
