using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>가구 선택부터 미리보기, 배치 검사, 구매와 저장까지 한 흐름으로 처리합니다.</summary>
public class FurniturePlacementController : MonoBehaviour
{
    private const string SaveKey = "ProjectB.Furniture.Save";
    private const int SaveVersion = 1;

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
    private GameObject placementControls;
    private TMP_Text statusText;
    private Button placeButton;
    private MaterialPropertyBlock colorBlock;

    private bool canPlace;
    private bool hasPreviewPosition;
    private bool playerMovementWasEnabled;
    private float rotationY;

    /// <summary>저장 데이터를 준비하고 배치 조작 UI를 만든 뒤 기존 가구를 불러옵니다.</summary>
    private void Start()
    {
        if (catalog == null)
            catalog = Managers.Resource.LoadResource<FurnitureCatalog>("FurnitureCatalog");

        colorBlock = new MaterialPropertyBlock();
        CreatePlacementControls();
        LoadPlacedFurniture();
    }

    /// <summary>선택한 가구의 위치와 유효 상태를 매 프레임 갱신합니다.</summary>
    private void Update()
    {
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

        if (catalog == null || catalog.items == null)
        {
            SetStatus("가구 목록을 찾을 수 없습니다.");
            return false;
        }

        selectedItem = catalog.items.Find(item => item != null && item.id == furnitureId);
        if (selectedItem == null || selectedItem.prefab == null)
        {
            selectedItem = null;
            SetStatus("가구 프리팹이 설정되지 않았습니다.");
            return false;
        }

        preview = Managers.Resource.InstantiateUnpooled(selectedItem.prefab);
        if (preview == null)
        {
            selectedItem = null;
            SetStatus("가구 미리보기를 만들지 못했습니다.");
            return false;
        }

        preview.name = "FurniturePreview";
        preview.transform.localScale = selectedItem.prefab.transform.localScale;
        PreparePreview();

        // 배치 중 이동 입력이 가구 조작과 겹치지 않게 하고, 끝날 때 원래 상태로 돌립니다.
        playerMovement = PlayerController.Instance != null
            ? PlayerController.Instance.GetComponent<ThirdPersonController>()
            : null;
        if (playerMovement != null)
        {
            playerMovementWasEnabled = playerMovement.enabled;
            playerMovement.enabled = false;
        }

        rotationY = 0f;
        hasPreviewPosition = false;
        placementFloor = null;
        canPlace = false;
        if (placementControls != null)
            placementControls.SetActive(true);

        UpdatePlacementFeedback();
        return true;
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
        if (IsPointerOverUI(pointerId))
            return;

        Ray ray = Camera.main.ScreenPointToRay(pointer);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, floorMask, QueryTriggerInteraction.Ignore))
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
        preview.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, rotationY, 0f));
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
        Quaternion rotation = preview.transform.rotation;
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
            if (shape == null || shape == placementFloor || shape.transform.IsChildOf(preview.transform))
                continue;

            // 사람은 움직이는 대상이므로 설치 불가 장애물로 취급하지 않습니다.
            if (shape.GetComponentInParent<PlayerController>() != null ||
                shape.GetComponentInParent<CustomerController>() != null ||
                shape.GetComponentInParent<EmployeeController>() != null)
                continue;

            return false;
        }

        // 좌석은 상호작용용 트리거를 쓰므로 별도로 검사해 너무 가까운 의자 배치를 막습니다.
        if (selectedItem.id == "chair" && IsTooCloseToSeat(position))
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
            if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayLength, floorMask, QueryTriggerInteraction.Ignore) ||
                !IsValidFloorPoint(hit.point, hit.normal) ||
                Mathf.Abs(hit.point.y - floorY) > floorHeightTolerance)
                return false;
        }

        return true;
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

    /// <summary>유효한 위치와 충분한 재화가 확인된 뒤 실제 가구를 생성하고 저장합니다.</summary>
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

        PlayerController player = PlayerController.Instance;
        if (player == null || !player.TrySpendMoney(selectedItem.price))
        {
            SetStatus("재화가 부족합니다.");
            return;
        }

        string furnitureId = selectedItem.id;
        Vector3 position = preview.transform.position;
        Quaternion rotation = preview.transform.rotation;
        GameObject placed = Managers.Resource.InstantiateUnpooled(selectedItem.prefab, position, rotation);
        if (placed == null)
        {
            // 생성 실패 시 구매 비용을 돌려주기 위한 보상 처리는 게임 재화 API에 맞춰 실행합니다.
            player.UpdateMoney(selectedItem.price);
            SetStatus("가구를 설치하지 못했습니다. 재화는 돌려드렸습니다.");
            return;
        }

        placed.name = furnitureId;
        FurnitureMarker marker = placed.GetComponent<FurnitureMarker>();
        if (marker == null)
            marker = placed.AddComponent<FurnitureMarker>();
        marker.furnitureId = furnitureId;
        placedFurniture.Add(placed);
        SavePlacedFurniture();

        CancelPlacement();
        SetStatus("가구를 배치했습니다.");
    }

    /// <summary>배치 모드를 취소하고 미리보기와 입력 잠금을 정리합니다.</summary>
    public void CancelPlacement()
    {
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
        selectedItem = null;
        placementFloor = null;
        hasPreviewPosition = false;
        canPlace = false;

        if (playerMovement != null)
            playerMovement.enabled = playerMovementWasEnabled;
        playerMovement = null;

        if (placementControls != null)
            placementControls.SetActive(false);
    }

    /// <summary>현재 터치 또는 마우스 좌표와 포인터 ID를 가져옵니다.</summary>
    private static bool TryReadPointer(out Vector2 pointer, out int pointerId)
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            pointer = Touchscreen.current.primaryTouch.position.ReadValue();
            pointerId = Touchscreen.current.primaryTouch.touchId.ReadValue();
            return true;
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
        if (!PlayerPrefs.HasKey(SaveKey) || catalog == null || catalog.items == null)
            return;

        FurnitureSaveData data;
        try
        {
            data = JsonUtility.FromJson<FurnitureSaveData>(PlayerPrefs.GetString(SaveKey));
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

            GameObject placed = Managers.Resource.InstantiateUnpooled(item.prefab, saved.GetPosition(), saved.GetRotation());
            if (placed == null)
                continue;

            placed.name = item.id;
            FurnitureMarker marker = placed.GetComponent<FurnitureMarker>();
            if (marker == null)
                marker = placed.AddComponent<FurnitureMarker>();
            marker.furnitureId = item.id;
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
                data.items.Add(new FurnitureSaveItem(marker.furnitureId, placed.transform.position, placed.transform.rotation));
        }

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    /// <summary>배치 상태 문구와 버튼 활성 상태, 미리보기 색을 한 번에 맞춥니다.</summary>
    private void UpdatePlacementFeedback()
    {
        if (selectedItem == null)
            return;

        if (statusText != null)
        {
            statusText.text = canPlace
                ? $"{selectedItem.title}  ·  {selectedItem.price:N0}원  ·  설치 가능\n손가락으로 이동하고 버튼으로 배치하세요"
                : $"{selectedItem.title}  ·  {selectedItem.price:N0}원  ·  설치 불가\n다른 바닥 위치를 선택해 주세요";
        }

        if (placeButton != null)
            placeButton.interactable = canPlace;

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
        placementControls.SetActive(false);
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

        preview.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
        canPlace = IsPositionFree(preview.transform.position);
        UpdatePlacementFeedback();
    }

    /// <summary>화면이 닫힐 때 미리보기와 플레이어 입력 잠금을 남기지 않습니다.</summary>
    private void OnDestroy()
    {
        CancelPlacement();
    }
}

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
    public float x, y, z;
    public float qx, qy, qz, qw;

    public FurnitureSaveItem() { }

    public FurnitureSaveItem(string itemId, Vector3 position, Quaternion rotation)
    {
        id = itemId;
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
