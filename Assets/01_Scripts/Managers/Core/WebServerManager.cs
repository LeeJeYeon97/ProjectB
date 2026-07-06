using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class WebServerManager
{
    string _baseUrl = "https://localhost:44377/api";

    public void Init() { }

    // Create
    public void SendPostRequest(string url, object obj, Action<UnityWebRequest> callBack)
    {
        // 코루틴 대신 비동기 함수 호출
        _ = SendWebRequestAsync(url, "POST", obj, callBack);
    }
    // Read
    public void SendGetAllRequest(string url, Action<UnityWebRequest> callBack)
    {
        // Task를 반환하지만, 콜백으로 처리할 것이므로 반환된 Task는 '_' 로 무시하겠다는 의미입니다. (경고창 방지)
        _ = SendWebRequestAsync(url, "GET", null, callBack);
    }

    //IEnumerator CoSendWebRequest(string url, string method, object obj, Action<UnityWebRequest> callBack)
    //{
    //    string sendUrl = $"{_baseUrl}/{url}/";

    //    byte[] jsonBytes = null;

    //    if (obj != null)
    //    {
    //        string jsonStr = JsonUtility.ToJson(obj);
    //        jsonBytes = Encoding.UTF8.GetBytes(jsonStr);
    //    }

    //    var uwr = new UnityWebRequest(sendUrl, method);
    //    uwr.uploadHandler = new UploadHandlerRaw(jsonBytes);
    //    uwr.downloadHandler = new DownloadHandlerBuffer();
    //    uwr.SetRequestHeader("Content-type", "application/json");

    //    yield return uwr.SendWebRequest();

    //    if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
    //    {
    //        Debug.LogError("연결 실패: " + uwr.error);
    //    }
    //    else
    //    {
    //        Debug.Log("Recv + " + uwr.downloadHandler.text);
    //        callBack.Invoke(uwr);
    //    }
    //}
    private async Task SendWebRequestAsync(string url, string method, object obj, Action<UnityWebRequest> callBack)
    {
        string sendUrl = $"{_baseUrl}/{url}";

        byte[] jsonBytes = null;
        if (obj != null)
        {
            string jsonStr = JsonUtility.ToJson(obj);
            jsonBytes = Encoding.UTF8.GetBytes(jsonStr);
        }

        // using문을 쓰면 통신이 끝난 후 메모리 누수를 자동으로 방지해 줍니다.
        using (var uwr = new UnityWebRequest(sendUrl, method))
        {
            if (jsonBytes != null)
            {
                uwr.uploadHandler = new UploadHandlerRaw(jsonBytes);
                uwr.SetRequestHeader("Content-type", "application/json");
            }
            uwr.downloadHandler = new DownloadHandlerBuffer();

            // 향후 결제나 인벤토리 요청을 보낼 때, LoginManager가 가진 토큰을 헤더에 넣는 로직
            // Managers 패턴을 사용 중이시라면 아래처럼 접근 가능합니다.
            /* if (!string.IsNullOrEmpty(Managers.Login.JwtToken))
            {
                uwr.SetRequestHeader("Authorization", $"Bearer {Managers.Login.JwtToken}");
            }
            */

            // 1. 서버에 요청 보내기
            var operation = uwr.SendWebRequest();

            // 2. 통신이 완료될 때까지 메인 스레드를 멈추지 않고 프레임 단위로 대기
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            // 3. 결과 처리
            if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"[{url}] 연결 실패: " + uwr.error);
            }
            else
            {
                Debug.Log($"[{url}] Recv: " + uwr.downloadHandler.text);
            }

            // 4. 콜백으로 결과 전달
            callBack?.Invoke(uwr);
        }
    }
}
