using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class SaveSceneForGit
{
    [MenuItem("AutoSetup/5. Zapisz Materialy i Scene pod GitHuba (Mac + PC)")]
    public static void SaveEverythingToDisk()
    {
        if (!AssetDatabase.IsValidFolder("Assets/SavedMaterials"))
            AssetDatabase.CreateFolder("Assets", "SavedMaterials");

        Renderer[] allRenderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        int count = 0;
        foreach (Renderer r in allRenderers)
        {
            if (r.sharedMaterial != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(r.sharedMaterial)))
            {
                string path = "Assets/SavedMaterials/Mat_" + r.gameObject.name + "_" + count + ".mat";
                AssetDatabase.CreateAsset(new Material(r.sharedMaterial), path);
                r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Zapisano " + count + " materialow na dysk oraz plik sceny! Mozesz wyslac na GitHuba.");
    }
}
