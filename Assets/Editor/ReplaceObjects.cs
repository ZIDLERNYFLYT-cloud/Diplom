using UnityEngine;
using UnityEditor;

public class ReplaceObjects : EditorWindow
{
    private string searchName = "OldObjectName";
    private GameObject replacementPrefab;

    [MenuItem("Tools/Replace Objects by Name")]
    public static void ShowWindow()
    {
        GetWindow<ReplaceObjects>("Replace Objects");
    }

    void OnGUI()
    {
        searchName = EditorGUILayout.TextField("Имя для поиска:", searchName);
        replacementPrefab = (GameObject)EditorGUILayout.ObjectField("На что заменяем:", replacementPrefab, typeof(GameObject), false);

        if (GUILayout.Button("Заменить все"))
        {
            Replace();
        }
    }

    void Replace()
    {
        GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int count = 0;

        foreach (GameObject go in allObjects)
        {
            if (go.name == searchName)
            {
                GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(replacementPrefab);
                newObj.transform.position = go.transform.position;
                newObj.transform.rotation = go.transform.rotation;
                newObj.transform.localScale = go.transform.localScale;
                newObj.transform.SetParent(go.transform.parent);

                Undo.RegisterCreatedObjectUndo(newObj, "Replace Objects");
                Undo.DestroyObjectImmediate(go);
                count++;
            }
        }
        Debug.Log($"Заменено объектов: {count}");
    }
}