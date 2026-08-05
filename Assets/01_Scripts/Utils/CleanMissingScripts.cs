/*
2026-07-23 AI-Tag
This was created with the help of Assistant, a Unity Artificial Intelligence product.
*/
using System;
using UnityEditor;
using UnityEngine;
using System.IO;

public class CleanMissingScripts
{
    [MenuItem("Tools/Clean Synty Missing Colliders")]
    public static void Clean()
    {
        string targetFolder = "Assets/99_Assets/Synty/PolygonGeneric/Models/Collision";
        if (!Directory.Exists(targetFolder))
        {
            Debug.LogError("대상 폴더를 찾을 수 없습니다: " + targetFolder);
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Object", new[] { targetFolder });
        int cleanedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".asset"))
            {
                // SerializedObject를 이용해 강제로 유실된 스크립트 프로퍼티를 null로 초기화합니다.
                UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (obj != null)
                {
                    SerializedObject so = new SerializedObject(obj);
                    SerializedProperty scriptProp = so.FindProperty("m_Script");
                    if (scriptProp != null && scriptProp.objectReferenceValue == null)
                    {
                        // 유실된 스크립트 참조를 확실하게 비워줍니다.
                        scriptProp.objectReferenceValue = null;
                        so.ApplyModifiedProperties();
                        EditorUtility.SetDirty(obj);
                        cleanedCount++;
                    }
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"총 {cleanedCount}개의 에셋에서 Missing Script 참조가 완벽히 정리되었습니다!");
    }
}
