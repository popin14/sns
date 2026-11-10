using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class SetupGrandmaScene
{
    [MenuItem("AutoSetup/6. Dodaj Babcie-Bossa i Laweczke")]
    public static void SpawnGrandmaAndBench()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        GameObject oldBench = GameObject.Find("Grandma_Bench_Area");
        if (oldBench != null) Object.DestroyImmediate(oldBench);

        GameObject areaRoot = new GameObject("Grandma_Bench_Area");
        areaRoot.transform.position = new Vector3(-4.5f, 0f, 1.5f);
        areaRoot.transform.rotation = Quaternion.Euler(0f, 35f, 0f);

        Material woodMat = new Material(urpLit);
        woodMat.SetColor("_BaseColor", new Color(0.45f, 0.25f, 0.12f));

        /* Drewniana laweczka */
        GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
        seat.name = "BenchSeat";
        seat.transform.SetParent(areaRoot.transform);
        seat.transform.localPosition = new Vector3(0f, 0.45f, 0f);
        seat.transform.localScale = new Vector3(2.2f, 0.12f, 0.7f);
        seat.GetComponent<Renderer>().sharedMaterial = woodMat;

        GameObject backrest = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backrest.name = "BenchBackrest";
        backrest.transform.SetParent(areaRoot.transform);
        backrest.transform.localPosition = new Vector3(0f, 0.85f, -0.3f);
        backrest.transform.localScale = new Vector3(2.2f, 0.65f, 0.1f);
        backrest.GetComponent<Renderer>().sharedMaterial = woodMat;

        GameObject seatPoint = new GameObject("SeatPoint");
        seatPoint.transform.SetParent(areaRoot.transform);
        seatPoint.transform.localPosition = new Vector3(0f, 0.85f, 0.05f);
        seatPoint.transform.localRotation = Quaternion.identity;

        /* Postac Babci */
        GameObject grandma = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        grandma.name = "NPC_GrandmaBoss";
        grandma.transform.SetParent(areaRoot.transform);
        grandma.transform.localPosition = new Vector3(1.4f, 0.9f, 1.2f);
        grandma.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);

        Material grandmaMat = new Material(urpLit);
        grandmaMat.SetColor("_BaseColor", new Color(0.75f, 0.55f, 0.82f));
        grandma.GetComponent<Renderer>().sharedMaterial = grandmaMat;

        /* Siwy kok na glowie i laska */
        GameObject bun = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bun.name = "HairBun";
        bun.transform.SetParent(grandma.transform);
        bun.transform.localPosition = new Vector3(0f, 0.95f, -0.15f);
        bun.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
        Object.DestroyImmediate(bun.GetComponent<Collider>());

        GameObject cane = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cane.name = "WalkingCane";
        cane.transform.SetParent(grandma.transform);
        cane.transform.localPosition = new Vector3(0.55f, -0.1f, 0.35f);
        cane.transform.localScale = new Vector3(0.08f, 0.65f, 0.08f);
        Object.DestroyImmediate(cane.GetComponent<Collider>());
        cane.GetComponent<Renderer>().sharedMaterial = woodMat;

        GrandmaBoss bossScript = grandma.AddComponent<GrandmaBoss>();
        bossScript.benchSeatPoint = seatPoint.transform;
        bossScript.caneWeapon = cane.transform;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Babcia i laweczka dodane i zapisane w scenie!");
    }
}
