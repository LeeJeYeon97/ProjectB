using UnityEngine;
using UnityEditor; // 에디터 툴 전용 네임스페이스
using System.IO;

public class SpriteBaker : EditorWindow
{
    //  유니티 상단 메뉴에 'Tools > 3D 메테오 PNG로 굽기' 버튼을 만들어줍니다.
    [MenuItem("Tools/3D 메테오 PNG로 굽기 (Bake)")]
    public static void BakeSelectedObject()
    {
        // 1. 현재 씬에서 내가 클릭(선택)한 오브젝트를 가져옵니다.
        GameObject selectedObj = Selection.activeGameObject;
        if (selectedObj == null)
        {
            Debug.LogWarning("캡처할 메테오 오브젝트를 먼저 클릭(선택)해 주세요!");
            return;
        }

        // 2. 캡처용 임시 카메라 생성
        GameObject camObj = new GameObject("TempBakeCamera");
        Camera bakeCam = camObj.AddComponent<Camera>();

        //  캡처 세팅 (가장 중요: 투명 배경 & 원근감 없는 직교 투영)
        bakeCam.clearFlags = CameraClearFlags.SolidColor;
        bakeCam.backgroundColor = new Color(0, 0, 0, 0); // 알파(A)값을 0으로 해서 완벽한 투명 배경
        bakeCam.orthographic = true; // 2D 스프라이트 느낌을 살리기 위해 Orthographic 사용
        bakeCam.orthographicSize = 2f; //  오브젝트가 짤리면 이 숫자를 키우고, 너무 작으면 줄이세요!

        // 카메라 위치를 오브젝트 정면(Z축 -5 위치)에 둡니다.
        camObj.transform.position = selectedObj.transform.position + new Vector3(0, 0, -5f);
        camObj.transform.LookAt(selectedObj.transform);

        // 3. 사진을 찍을 도화지(RenderTexture) 준비 (해상도 512x512)
        int resWidth = 512;
        int resHeight = 512;
        RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
        bakeCam.targetTexture = rt;

        // 4. 찰칵! (렌더링 후 Texture2D로 변환)
        Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.ARGB32, false);
        bakeCam.Render(); // 카메라 렌더링 강제 실행
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0); // 픽셀 데이터 읽어오기
        screenShot.Apply();

        // 5. 메모리 청소 및 임시 카메라 삭제
        bakeCam.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(camObj);
        DestroyImmediate(rt);

        // 6. PNG 파일로 컴퓨터에 저장하기
        byte[] bytes = screenShot.EncodeToPNG();

        // 저장 경로: Assets 폴더 바로 아래 (이름: Baked_오브젝트이름.png)
        string savePath = Application.dataPath + $"/Baked_{selectedObj.name}.png";
        File.WriteAllBytes(savePath, bytes);

        // 유니티 프로젝트 창 새로고침 (이걸 해야 파일이 바로 뜹니다)
        AssetDatabase.Refresh();

        Debug.Log($"<color=cyan>[굽기 성공!]</color> {selectedObj.name} 스프라이트가 저장되었습니다.\n경로: {savePath}");
    }
}