using UnityEngine;

// 순수 C# 클래스들을 위해 코루틴을 대신 돌려주는 대리인(용병) 클래스입니다.
public class CoroutineHelper : MonoBehaviour
{
    //private static CoroutineHelper _instance;
    //public static CoroutineHelper Instance
    //{
    //    get
    //    {
    //        if (_instance == null)
    //        {
    //            // 게임이 시작될 때 아무도 모르게 빈 오브젝트를 만들어서 스스로를 붙입니다.
    //            GameObject go = new GameObject("@CoroutineHelper");
    //            _instance = go.AddComponent<CoroutineHelper>();
    //            DontDestroyOnLoad(go); // 씬이 넘어가도 파괴되지 않음
    //        }
    //        return _instance;
    //    }
    //}
}
