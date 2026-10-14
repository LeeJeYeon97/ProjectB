using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 사이에서 카메라 리그를 유지하고 현재 씬의 플레이어를 따라가게 합니다.
/// </summary>
public class CameraManager
{
    // Resources 폴더에 저장할 공용 카메라 프리팹의 경로입니다.
    private const string CameraRigResourceName = "CameraRig";

    // 공용 카메라 리그와 플레이어 추적 카메라를 보관합니다.
    private GameObject cameraRig;
    private CinemachineCamera followCamera;
    private bool isInitialized;

    /// <summary>
    /// 씬 전환 알림을 한 번만 구독해 새 씬의 플레이어를 자동으로 연결합니다.
    /// </summary>
    public void Init()
    {
        if (isInitialized)
            return;

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        isInitialized = true;
    }

    /// <summary>
    /// 현재 플레이어를 카메라의 추적 대상으로 지정합니다.
    /// 플레이어가 없는 경우에는 다른 씬의 카메라가 보이도록 공용 리그를 끕니다.
    /// </summary>
    /// <param name="target">카메라가 따라갈 플레이어의 Transform입니다.</param>
    public void SetTarget(Transform target)
    {
        if (target == null)
        {
            if (cameraRig != null)
                cameraRig.SetActive(false);

            return;
        }

        if (!TryGetCameraRig())
            return;

        cameraRig.SetActive(true);
        followCamera.Follow = target;
    }

    /// <summary>
    /// 새 씬이 로드되면 그 씬에 포함된 플레이어를 찾아 카메라에 연결합니다.
    /// </summary>
    /// <param name="scene">방금 로드된 씬입니다.</param>
    /// <param name="mode">씬을 불러온 방식입니다.</param>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayerController player = FindPlayer(scene);
        SetTarget(player != null ? player.transform : null);
    }

    /// <summary>
    /// 씬이 언로드되면 남아 있는 활성 씬에서 플레이어를 찾아 카메라를 다시 연결합니다.
    /// </summary>
    /// <param name="scene">방금 언로드된 씬입니다.</param>
    private void OnSceneUnloaded(Scene scene)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        PlayerController player = FindPlayer(activeScene);

        if (player == null)
        {
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene loadedScene = SceneManager.GetSceneAt(index);
                player = FindPlayer(loadedScene);

                if (player != null)
                    break;
            }
        }

        SetTarget(player != null ? player.transform : null);
    }

    /// <summary>
    /// 지정한 씬의 오브젝트 안에서 플레이어를 찾아 돌려줍니다.
    /// </summary>
    /// <param name="scene">플레이어를 찾을 씬입니다.</param>
    /// <returns>씬 안에 플레이어가 있으면 그 컴포넌트, 없으면 null입니다.</returns>
    private PlayerController FindPlayer(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            PlayerController player = rootObject.GetComponentInChildren<PlayerController>(true);

            if (player != null)
                return player;
        }

        return null;
    }

    /// <summary>
    /// 씬에 있는 CameraRig를 찾고, 없으면 Resources 프리팹으로 만듭니다.
    /// 생성한 리그는 씬 전환 뒤에도 유지해 카메라 연출을 재사용합니다.
    /// </summary>
    /// <returns>추적 카메라를 사용할 수 있으면 true를 반환합니다.</returns>
    private bool TryGetCameraRig()
    {
        if (cameraRig != null && followCamera != null)
            return true;

        // GameScene에 놓인 프리팹을 먼저 찾아 편집 화면에서도 구조를 확인할 수 있게 합니다.
        cameraRig = GameObject.Find("CameraRig");

        if (cameraRig == null)
        {
            GameObject cameraRigPrefab = Resources.Load<GameObject>(CameraRigResourceName);

            if (cameraRigPrefab == null)
            {
                Debug.LogError(
                    $"[CameraManager] Resources/{CameraRigResourceName}.prefab을 찾을 수 없습니다.");
                return false;
            }

            cameraRig = Object.Instantiate(cameraRigPrefab);
            cameraRig.name = "CameraRig";
        }

        followCamera = cameraRig.GetComponentInChildren<CinemachineCamera>(true);

        if (followCamera == null)
        {
            Debug.LogError("[CameraManager] CameraRig 안에 CinemachineCamera가 없습니다.");
            Object.Destroy(cameraRig);
            cameraRig = null;
            return false;
        }

        // 리그가 씬에 놓여 있거나 런타임에 만들어져도 다음 씬까지 유지합니다.
        Object.DontDestroyOnLoad(cameraRig);
        return true;
    }
}
