using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

[InitializeOnLoad]
public static class MainToolbarSceneSwitcher
{
    // 1. 씬 드롭다운 툴바 요소 추가
    [MainToolbarElement("SceneSwitcher/Dropdown", defaultDockPosition = MainToolbarDockPosition.Left)]
    public static MainToolbarElement SceneDropdown()
    {
        // 툴바에 표시될 이름과 마우스를 올렸을 때 나올 툴팁 설정
        var content = new MainToolbarContent("Scenes", "빠른 씬 이동 메뉴");

        // MainToolbarDropdown 생성 (내용, 클릭했을 때 메뉴를 띄워줄 함수 연결)
        var dropdown = new MainToolbarDropdown(content, ShowSceneMenu);

        return dropdown; // 경고 없이 아주 깔끔하게 반환!
    }

    // 2. 드롭다운을 클릭했을 때 펼쳐질 메뉴 목록 만들기
    private static void ShowSceneMenu(Rect rect)
    {
        GenericMenu menu = new GenericMenu();

        // Build Settings에 등록된 씬 목록 가져오기
        var scenes = EditorBuildSettings.scenes;

        if (scenes.Length == 0)
        {
            // 씬이 없으면 클릭할 수 없는 안내 문구 추가
            menu.AddDisabledItem(new GUIContent("Build Settings에 씬을 추가하세요"));
        }
        else
        {
            foreach (var scene in scenes)
            {
                // 이름 충돌 방지용 풀네임 명시
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scene.path);

                // ★ 우리가 아까 배웠던 '클로저(Closure)의 함정' 방지용 지역 변수 복사!
                string scenePath = scene.path;

                // 메뉴에 씬 이름들 추가 (이름, 체크 여부, 클릭 시 실행할 로직)
                menu.AddItem(new GUIContent(sceneName), false, () =>
                {
                    // 저장 안 된 작업이 있으면 물어보고 씬 이동
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(scenePath);
                    }
                });
            }
        }

        // 완성된 메뉴를 마우스 클릭 위치에 짠! 하고 띄워줌
        menu.ShowAsContext();

    }
}