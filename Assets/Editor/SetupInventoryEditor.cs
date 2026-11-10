using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class SetupInventoryEditor
{
    [MenuItem("AutoSetup/8. Dodaj Ekwipunek (I / TAB) i Buty Double Jump Pijaka")]
    public static void AttachInventoryToPlayer()
    {
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            if (player.GetComponent<PlayerInventory>() == null)
                player.AddComponent<PlayerInventory>();
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Ekwipunek i drop Butow Pijaka z Double Jumpem gotowe! Wcisnij Play, a potem klawisz [I] lub [TAB].");
    }
}
