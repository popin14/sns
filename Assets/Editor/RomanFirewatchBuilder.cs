using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEditor.SceneManagement;

public class RomanFirewatchBuilder
{
    [MenuItem("AutoSetup/7. Zbuduj Rzymskie Miasto, Szpital, Kosciol i Koloseum na Wzgorzu")]
    public static void BuildRomanWorld()
    {
        if (!AssetDatabase.IsValidFolder("Assets/SavedMaterials"))
            AssetDatabase.CreateFolder("Assets", "SavedMaterials");

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        /* 1. Rzymski zachód slonca w stylu Firewatch (Zloto, Terakota i Purpura) */
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.95f, 0.55f, 0.35f);
        RenderSettings.fogDensity = 0.009f;

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.98f, 0.65f, 0.38f);
        RenderSettings.ambientEquatorColor = new Color(0.75f, 0.38f, 0.35f);
        RenderSettings.ambientGroundColor = new Color(0.28f, 0.16f, 0.24f);

        Light sun = Object.FindFirstObjectByType<Light>();
        if (sun != null && sun.type == LightType.Directional)
        {
            sun.transform.rotation = Quaternion.Euler(16f, -25f, 0f);
            sun.color = new Color(1.0f, 0.72f, 0.42f);
            sun.intensity = 2.0f;
            sun.shadows = LightShadows.Soft;
        }

        /* Czyszczenie poprzedniego otoczenia */
        GameObject oldFw = GameObject.Find("Firewatch_Environment");
        if (oldFw != null) Object.DestroyImmediate(oldFw);
        GameObject oldRoman = GameObject.Find("Roman_World");
        if (oldRoman != null) Object.DestroyImmediate(oldRoman);

        GameObject root = new GameObject("Roman_World");

        /* Tworzenie i zapisywanie materialów na dysku (kompatybilne z GitHubem i Maciem) */
        Material sandMat = GetOrCreateMat(urpLit, "Roman_Sand", new Color(0.86f, 0.66f, 0.44f));
        Material grassHillMat = GetOrCreateMat(urpLit, "Roman_Hills", new Color(0.58f, 0.48f, 0.28f));
        Material cobbleMat = GetOrCreateMat(urpLit, "Roman_Cobble", new Color(0.62f, 0.56f, 0.50f));
        Material marbleMat = GetOrCreateMat(urpLit, "Roman_Marble", new Color(0.94f, 0.90f, 0.84f));
        Material plasterMat = GetOrCreateMat(urpLit, "Roman_Plaster", new Color(0.92f, 0.80f, 0.64f));
        Material terracottaMat = GetOrCreateMat(urpLit, "Roman_Terracotta", new Color(0.78f, 0.30f, 0.20f));
        Material woodMat = GetOrCreateMat(urpLit, "Roman_Wood", new Color(0.42f, 0.24f, 0.14f));
        Material cypressMat = GetOrCreateMat(urpLit, "Roman_Cypress", new Color(0.16f, 0.32f, 0.20f));
        Material cypressGoldMat = GetOrCreateMat(urpLit, "Roman_CypressGold", new Color(0.45f, 0.42f, 0.18f));
        Material redCrossMat = GetOrCreateMat(urpLit, "Roman_HospitalRed", new Color(0.85f, 0.15f, 0.15f));
        Material goldMat = GetOrCreateMat(urpLit, "Roman_Gold", new Color(0.95f, 0.75f, 0.20f));
        Material mountainMat = GetOrCreateMat(urpLit, "Roman_Mountains", new Color(0.42f, 0.22f, 0.32f));

        /* 2. Podstawa terenu i Dziedziniec (Courtyard) */
        GameObject floor = GameObject.Find("Floor");
        if (floor == null)
        {
            floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
        }
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(35f, 1f, 35f);
        floor.GetComponent<Renderer>().sharedMaterial = sandMat;

        GameObject courtyard = CreateBox("Town_Courtyard_Pavement", root.transform, new Vector3(0f, 0.05f, 4f), new Vector3(32f, 0.1f, 32f), cobbleMat);

        /* 3. Naturalne nierownosci terenu (Pofaldowane wzgorza wokol dziedzinca) */
        Random.InitState(101);
        for (int i = 0; i < 28; i++)
        {
            float angle = i * (360f / 28f) * Mathf.Deg2Rad;
            float dist = Random.Range(32f, 65f);
            Vector3 pos = new Vector3(Mathf.Cos(angle) * dist, -1.2f, Mathf.Sin(angle) * dist + 15f);
            if (Mathf.Abs(pos.x) < 16f && pos.z > 0f && pos.z < 90f) continue; /* Zostaw miejsce na drozke i Koloseum */

            GameObject hill = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hill.name = "NaturalHill_" + i;
            hill.transform.SetParent(root.transform);
            hill.transform.position = pos;
            hill.transform.localScale = new Vector3(Random.Range(18f, 36f), Random.Range(4f, 11f), Random.Range(18f, 36f));
            hill.GetComponent<Renderer>().sharedMaterial = grassHillMat;
        }

        /* Odlegle gory na horyzoncie w stylu Firewatch */
        for (int i = 0; i < 30; i++)
        {
            float angle = i * 12f * Mathf.Deg2Rad;
            float dist = Random.Range(95f, 135f);
            Vector3 pos = new Vector3(Mathf.Cos(angle) * dist, -4f, Mathf.Sin(angle) * dist + 20f);
            GameObject peak = CreateBox("HorizonPeak_" + i, root.transform, pos, new Vector3(45f, Random.Range(35f, 65f), 45f), mountainMat);
            peak.transform.rotation = Quaternion.Euler(20f, i * 15f, 15f);
        }

        /* 4. Wzgorze pod Koloseum + Kamienna Drozka z Rampa */
        /* Wzgorze schodkowe z tarasem na szczycie (wysokosc Y = 8) */
        CreateBox("Colosseum_Hill_Base", root.transform, new Vector3(0f, 2.0f, 68f), new Vector3(68f, 4.0f, 54f), grassHillMat);
        CreateBox("Colosseum_Hill_Mid", root.transform, new Vector3(0f, 5.0f, 70f), new Vector3(58f, 4.0f, 46f), grassHillMat);
        CreateBox("Colosseum_Hill_Top", root.transform, new Vector3(0f, 7.2f, 72f), new Vector3(48f, 1.6f, 40f), sandMat);

        /* Plaska czesc drozki od dziedzinca do podnoza wzgorza */
        CreateBox("Road_Flat", root.transform, new Vector3(0f, 0.08f, 24f), new Vector3(7f, 0.15f, 18f), cobbleMat);

        /* Nachylona rampa prowadzaca prosto na szczyt wzgorza pod brame Koloseum */
        GameObject ramp = CreateBox("Road_Hill_Ramp", root.transform, new Vector3(0f, 4.0f, 42.5f), new Vector3(7.5f, 0.4f, 22f), cobbleMat);
        ramp.transform.rotation = Quaternion.Euler(-21.5f, 0f, 0f);

        /* 5. KOLOSEUM NA WZGORZU (Swords and Sandals Arena) */
        GameObject colosseum = new GameObject("Colosseum_On_Hill");
        colosseum.transform.SetParent(root.transform);
        colosseum.transform.position = new Vector3(0f, 8.0f, 72f);

        int segments = 24;
        float radius = 17f;
        for (int i = 0; i < segments; i++)
        {
            /* Zostaw otwarta brame wejsciowa od strony poludniowej (od strony drozki) */
            if (i == 17 || i == 18 || i == 19) continue;

            float ang = i * (360f / segments) * Mathf.Deg2Rad;
            Vector3 wallPos = new Vector3(Mathf.Cos(ang) * radius, 0f, Mathf.Sin(ang) * radius);
            Quaternion wallRot = Quaternion.Euler(0f, -i * (360f / segments) + 90f, 0f);

            /* Dolne filary i luki */
            GameObject pillar = CreateBox("Col_Pillar_" + i, colosseum.transform, Vector3.zero, new Vector3(2.2f, 11f, 2.2f), marbleMat);
            pillar.transform.localPosition = wallPos + new Vector3(0f, 5.5f, 0f);
            pillar.transform.localRotation = wallRot;

            /* Gorny gzyms Koloseum */
            GameObject topArch = CreateBox("Col_TopRing_" + i, colosseum.transform, Vector3.zero, new Vector3(4.8f, 2.5f, 2.6f), marbleMat);
            topArch.transform.localPosition = wallPos + new Vector3(0f, 11.5f, 0f);
            topArch.transform.localRotation = wallRot;
        }
        CreateSignText(colosseum.transform, "KOLOSEUM ARENA", new Vector3(0f, 13.5f, -16.5f), 50, Color.yellow);

        /* 6. SZPITAL (Miejsce Respawnu po smierci) - Zachodnia strona dziedzinca */
        GameObject hospital = BuildRomanBuilding("Building_Hospital", root.transform, new Vector3(-16f, 0f, 6f), Quaternion.Euler(0f, 90f, 0f), new Vector3(9f, 5.5f, 8f), marbleMat, terracottaMat, "SZPITAL (RESPAWN)");
        /* Czerwony Krzyz na fasadzie Szpitala */
        CreateBox("Cross_Vert", hospital.transform, new Vector3(0f, 4.2f, 4.1f), new Vector3(0.6f, 2.0f, 0.2f), redCrossMat);
        CreateBox("Cross_Horiz", hospital.transform, new Vector3(0f, 4.2f, 4.1f), new Vector3(2.0f, 0.6f, 0.2f), redCrossMat);

        GameObject hospSpawn = new GameObject("HospitalSpawnPoint");
        hospSpawn.transform.SetParent(root.transform);
        hospSpawn.transform.position = new Vector3(-10.5f, 1.2f, 6f);
        hospSpawn.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        /* 7. KOSCIOL / SWIATYNIA - Wschodnia strona dziedzinca */
        GameObject church = BuildRomanBuilding("Building_Church", root.transform, new Vector3(16f, 0f, 6f), Quaternion.Euler(0f, -90f, 0f), new Vector3(10f, 7.5f, 11f), marbleMat, terracottaMat, "KOSCIOL / SWIATYNIA");
        /* Dzwonnica i zloty krzyz na szczycie Kosciola */
        CreateBox("Church_Steeple", church.transform, new Vector3(0f, 9.5f, 3.5f), new Vector3(2.8f, 5.0f, 2.8f), marbleMat);
        CreateBox("Church_Cross_V", church.transform, new Vector3(0f, 13.0f, 3.5f), new Vector3(0.35f, 2.4f, 0.35f), goldMat);
        CreateBox("Church_Cross_H", church.transform, new Vector3(0f, 13.3f, 3.5f), new Vector3(1.5f, 0.35f, 0.35f), goldMat);

        /* 8. LOKALE MIEJSKIE Z SWORDS AND SANDALS (Zbrojownia, Kuznia, Tawerna) */
        BuildRomanBuilding("Shop_Armory", root.transform, new Vector3(-15f, 0f, -7f), Quaternion.Euler(0f, 60f, 0f), new Vector3(7.5f, 4.8f, 6.5f), plasterMat, terracottaMat, "ZBROJOWNIA (ARMORY)");
        BuildRomanBuilding("Shop_Weaponsmith", root.transform, new Vector3(15f, 0f, -7f), Quaternion.Euler(0f, -60f, 0f), new Vector3(7.5f, 4.8f, 6.5f), plasterMat, terracottaMat, "KUZNIA MIECZY");
        BuildRomanBuilding("Shop_Tavern", root.transform, new Vector3(9f, 0f, 16f), Quaternion.Euler(0f, -150f, 0f), new Vector3(8.5f, 5.0f, 7.0f), woodMat, terracottaMat, "TAWERNA GLADIATORA");

        /* 9. Rzymskie Cyprysy i Kolumny wzdluz drozki do Koloseum */
        for (int z = 16; z <= 50; z += 7)
        {
            SpawnRomanCypress(root.transform, new Vector3(-5.2f, z > 32 ? (z - 32) * 0.38f : 0f, z), cypressMat, woodMat);
            SpawnRomanCypress(root.transform, new Vector3(5.2f, z > 32 ? (z - 32) * 0.38f : 0f, z), cypressGoldMat, woodMat);
        }

        for (int i = 0; i < 55; i++)
        {
            Vector2 c = Random.insideUnitCircle * 55f;
            if (c.magnitude < 20f || (Mathf.Abs(c.x) < 12f && c.y > 0f)) continue;
            SpawnRomanCypress(root.transform, new Vector3(c.x, 0f, c.y + 10f), (i % 2 == 0) ? cypressMat : cypressGoldMat, woodMat);
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Rzymskie miasto, Szpital, Kosciol i Koloseum na wzgorzu gotowe i zapisane!");
    }

    private static GameObject BuildRomanBuilding(string name, Transform parent, Vector3 pos, Quaternion rot, Vector3 size, Material wallMat, Material roofMat, string signTitle)
    {
        GameObject b = new GameObject(name);
        b.transform.SetParent(parent);
        b.transform.position = pos;
        b.transform.rotation = rot;

        CreateBox("Walls", b.transform, new Vector3(0f, size.y / 2f, 0f), size, wallMat);
        CreateBox("Roof_Main", b.transform, new Vector3(0f, size.y + 0.6f, 0f), new Vector3(size.x + 1.0f, 1.2f, size.z + 1.0f), roofMat);
        CreateBox("Roof_Top", b.transform, new Vector3(0f, size.y + 1.5f, 0f), new Vector3(size.x * 0.65f, 0.9f, size.z * 0.85f), roofMat);
        CreateBox("Door", b.transform, new Vector3(0f, 1.4f, size.z / 2f + 0.05f), new Vector3(1.8f, 2.8f, 0.2f), GetOrCreateMat(wallMat.shader, "DoorDark", new Color(0.22f, 0.12f, 0.08f)));

        CreateSignText(b.transform, signTitle, new Vector3(0f, size.y + 2.4f, size.z / 2f), 36, Color.white);
        return b;
    }

    private static void SpawnRomanCypress(Transform parent, Vector3 pos, Material leafMat, Material trunkMat)
    {
        GameObject tree = new GameObject("RomanCypress");
        tree.transform.SetParent(parent);
        tree.transform.position = pos;
        float s = Random.Range(0.9f, 1.4f);
        tree.transform.localScale = Vector3.one * s;

        CreateBox("Trunk", tree.transform, new Vector3(0f, 1.0f, 0f), new Vector3(0.4f, 2.0f, 0.4f), trunkMat);
        CreateBox("Leaves_Bot", tree.transform, new Vector3(0f, 3.2f, 0f), new Vector3(1.3f, 2.8f, 1.3f), leafMat);
        CreateBox("Leaves_Mid", tree.transform, new Vector3(0f, 5.4f, 0f), new Vector3(1.0f, 2.4f, 1.0f), leafMat);
        CreateBox("Leaves_Top", tree.transform, new Vector3(0f, 7.1f, 0f), new Vector3(0.6f, 1.6f, 0.6f), leafMat);
    }

    private static GameObject CreateBox(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPos;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = mat;
        return obj;
    }

    private static void CreateSignText(Transform parent, string text, Vector3 localPos, int fontSize, Color col)
    {
        GameObject txtObj = new GameObject("Sign_" + text);
        txtObj.transform.SetParent(parent);
        txtObj.transform.localPosition = localPos;
        txtObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        txtObj.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
        TextMesh tm = txtObj.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = fontSize;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = col;
    }

    private static Material GetOrCreateMat(Shader s, string matName, Color col)
    {
        string path = "Assets/SavedMaterials/" + matName + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(s);
            m.color = col;
            m.SetColor("_BaseColor", col);
            m.SetFloat("_Smoothness", 0.08f);
            AssetDatabase.CreateAsset(m, path);
        }
        else
        {
            m.color = col;
            m.SetColor("_BaseColor", col);
        }
        return m;
    }
}
