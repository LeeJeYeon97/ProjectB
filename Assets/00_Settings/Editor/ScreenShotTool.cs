#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class ScreenshotTool : EditorWindow
{
    [MenuItem("Tools/스토어용 고화질 스크린샷 찍기")]
    public static void TakeScreenshot()
    {
        // 1. 저장될 폴더 경로 설정 (Assets 폴더 바깥에 Screenshots 폴더 생성)
        string folderPath = Directory.GetParent(Application.dataPath).FullName + "/Screenshots";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // 2. 파일 이름 설정 (현재 시간 기준)
        string fileName = $"Screenshot_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
        string filePath = folderPath + "/" + fileName;

        // 3. 현재 Game 뷰에 설정된 해상도 그대로 1배수로 찰칵!
        // (만약 2를 넣으면 해상도가 2배로 뻥튀기 됩니다)
        ScreenCapture.CaptureScreenshot(filePath, 1);

        Debug.Log($"<color=green>[스크린샷 저장 성공!]</color>\n경로: {filePath}");

        // 4. 저장된 폴더를 자동으로 열어줍니다.
        EditorUtility.RevealInFinder(filePath);
    }
}
#endif