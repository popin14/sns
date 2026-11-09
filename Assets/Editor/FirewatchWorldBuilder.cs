using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class FirewatchWorldBuilder
{
    [MenuItem("AutoSetup/4. Zbuduj Swiat i Klimat FIREWATCH")]
    public static void BuildFirewatchScene()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        /* 1. Oswietlenie, Niebo i Mgla w stylu Firewatch */
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.92f, 0.45f, 0.32f); /* Koralowo-pomaranczowa mgla */
        RenderSettings.fogDensity = 0.014f;

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.95f, 0.52f, 0.30f);
        RenderSettings.ambientEquatorColor = new Color(0.65f, 0.30f, 0.42f);
        RenderSettings.ambientGroundColor = new Color(0.22f, 0.14f, 0.28f); /* Fioletowe cienie */

        Shader skyShader = Shader.Find("Skybox/Procedural");
        if (skyShader != null)
        {
            Material skyMat = new Material(skyShader);
            skyMat.SetFloat("_SunSize", 0.06f);
            skyMat.SetFloat("_SunSizeConvergence", 5f);
            skyMat.SetFloat("_AtmosphereThickness", 1.85f); /* Gesty, pomaranczowy zachod slonca */
            skyMat.SetColor("_SkyTint", new Color(0.85f, 0.35f, 0.45f));
            skyMat.SetColor("_GroundColor", new Color(0.35f, 0.18f, 0.28f));
            skyMat.SetFloat("_Exposure", 1.25f);
            AssetDatabase.CreateAsset(skyMat, "Assets/FirewatchSky.mat");
            RenderSettings.skybox = skyMat;
        }

        Light sun = Object.FindFirstObjectByType<Light>();
        if (sun != null && sun.type == LightType.Directional)
        {
            sun.transform.rotation = Quaternion.Euler(14f, -35f, 0f); /* Niskie slonce nad horyzontem */
            sun.color = new Color(1.0f, 0.62f, 0.32f);
            sun.intensity = 1.8f;
            sun.shadows = LightShadows.Soft;
        }

        /* Usuniecie starego szarego drzewa i przygotowanie kontenera swiata */
        GameObject oldTree = GameObject.Find("Tree");
        if (oldTree != null) Object.DestroyImmediate(oldTree);

        GameObject oldWorld = GameObject.Find("Firewatch_Environment");
        if (oldWorld != null) Object.DestroyImmediate(oldWorld);
        GameObject worldRoot = new GameObject("Firewatch_Environment");

        /* Helper do matowych materialow */
        Material groundMat = CreateFlatMat(urpLit, new Color(0.72f, 0.36f, 0.22f));
        Material mountainNearMat = CreateFlatMat(urpLit, new Color(0.42f, 0.20f, 0.30f));
        Material mountainFarMat = CreateFlatMat(urpLit, new Color(0.28f, 0.14f, 0.26f));
        Material trunkMat = CreateFlatMat(urpLit, new Color(0.28f, 0.15f, 0.12f));
        Material pineGreenMat = CreateFlatMat(urpLit, new Color(0.18f, 0.36f, 0.28f));
        Material pineOrangeMat = CreateFlatMat(urpLit, new Color(0.82f, 0.42f, 0.18f));
        Material pineGoldMat = CreateFlatMat(urpLit, new Color(0.90f, 0.60f, 0.20f));
        Material rockMat = CreateFlatMat(urpLit, new Color(0.45f, 0.32f, 0.35f));
        Material woodMat = CreateFlatMat(urpLit, new Color(0.48f, 0.26f, 0.16f));

        /* 2. Cieple podloze kanionu Wyoming */
        GameObject floor = GameObject.Find("Floor");
        if (floor == null)
        {
            floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
        }
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(30f, 1f, 30f);
        floor.GetComponent<Renderer>().sharedMaterial = groundMat;

        /* 3. Warstwowe sylwetki gor na horyzoncie */
        Random.InitState(42);
        for (int i = 0; i < 36; i++)
        {
            float angle = i * 10f * Mathf.Deg2Rad;
            float dist = Random.Range(65f, 115f);
            Vector3 pos = new Vector3(Mathf.Cos(angle) * dist, -5f, Mathf.Sin(angle) * dist);

            GameObject peak = GameObject.CreatePrimitive(PrimitiveType.Cube);
            peak.name = "MountainPeak_" + i;
            peak.transform.SetParent(worldRoot.transform);
            peak.transform.position = pos;
            float height = Random.Range(35f, 75f);
            float width = Random.Range(28f, 50f);
            peak.transform.localScale = new Vector3(width, height, width);
            peak.transform.rotation = Quaternion.Euler(Random.Range(15f, 35f), Random.Range(0f, 360f), Random.Range(15f, 35f));
            peak.GetComponent<Renderer>().sharedMaterial = (dist > 85f) ? mountainFarMat : mountainNearMat;
        }

        /* 4. Las stylizowanych sosen (Low-Poly Pines) */
        for (int i = 0; i < 85; i++)
        {
            Vector2 circle = Random.insideUnitCircle * 55f;
            if (circle.magnitude < 8.5f) continue; /* Zostaw wolna polane na srodku do walki */

            GameObject tree = new GameObject("StylizedPine_" + i);
            tree.transform.SetParent(worldRoot.transform);
            tree.transform.position = new Vector3(circle.x, 0f, circle.y);
            float scale = Random.Range(0.85f, 1.65f);
            tree.transform.localScale = Vector3.one * scale;

            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.transform.SetParent(tree.transform);
            trunk.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            trunk.transform.localScale = new Vector3(0.35f, 1.2f, 0.35f);
            trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;

            Material foliageMat = (i % 3 == 0) ? pineOrangeMat : ((i % 2 == 0) ? pineGreenMat : pineGoldMat);

            for (int tier = 0; tier < 3; tier++)
            {
                GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leaves.transform.SetParent(tree.transform);
                float yPos = 2.4f + (tier * 1.05f);
                float w = 2.1f - (tier * 0.5f);
                leaves.transform.localPosition = new Vector3(0f, yPos, 0f);
                leaves.transform.localScale = new Vector3(w, 1.1f, w);
                leaves.transform.localRotation = Quaternion.Euler(0f, tier * 30f, 0f);
                Object.DestroyImmediate(leaves.GetComponent<Collider>());
                leaves.GetComponent<Renderer>().sharedMaterial = foliageMat;
            }
        }

        /* 5. Glazy i skaly (Outer Wilds vibe) */
        for (int i = 0; i < 30; i++)
        {
            Vector2 circle = Random.insideUnitCircle * 40f;
            if (circle.magnitude < 6.5f) continue;

            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = "StylizedRock_" + i;
            rock.transform.SetParent(worldRoot.transform);
            rock.transform.position = new Vector3(circle.x, Random.Range(0.2f, 0.8f), circle.y);
            rock.transform.localScale = new Vector3(Random.Range(1.2f, 3.5f), Random.Range(0.8f, 2.5f), Random.Range(1.2f, 3.2f));
            rock.transform.rotation = Quaternion.Euler(Random.Range(0f, 45f), Random.Range(0f, 360f), Random.Range(0f, 45f));
            rock.GetComponent<Renderer>().sharedMaterial = rockMat;
        }

        /* 6. Drewniana Wieza Straznicza (Firewatch Lookout Tower) */
        GameObject tower = new GameObject("LookoutTower");
        tower.transform.SetParent(worldRoot.transform);
        tower.transform.position = new Vector3(-12f, 0f, 18f);
        tower.transform.rotation = Quaternion.Euler(0f, 25f, 0f);

        float[] legX = { -2.2f, 2.2f, -2.2f, 2.2f };
        float[] legZ = { -2.2f, -2.2f, 2.2f, 2.2f };
        for (int l = 0; l < 4; l++)
        {
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leg.transform.SetParent(tower.transform);
            leg.transform.localPosition = new Vector3(legX[l], 5f, legZ[l]);
            leg.transform.localScale = new Vector3(0.35f, 10f, 0.35f);
            leg.GetComponent<Renderer>().sharedMaterial = woodMat;
        }

        GameObject deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
        deck.transform.SetParent(tower.transform);
        deck.transform.localPosition = new Vector3(0f, 10f, 0f);
        deck.transform.localScale = new Vector3(6.2f, 0.4f, 6.2f);
        deck.GetComponent<Renderer>().sharedMaterial = woodMat;

        GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.transform.SetParent(tower.transform);
        cabin.transform.localPosition = new Vector3(0f, 11.8f, 0f);
        cabin.transform.localScale = new Vector3(4.6f, 3.2f, 4.6f);
        cabin.GetComponent<Renderer>().sharedMaterial = pineOrangeMat;

        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(tower.transform);
        roof.transform.localPosition = new Vector3(0f, 13.8f, 0f);
        roof.transform.localScale = new Vector3(5.8f, 0.8f, 5.8f);
        roof.GetComponent<Renderer>().sharedMaterial = trunkMat;

        /* Swiatlo w oknie wiezy */
        GameObject towerLightObj = new GameObject("TowerWarmLight");
        towerLightObj.transform.SetParent(tower.transform);
        towerLightObj.transform.localPosition = new Vector3(0f, 12.2f, -3f);
        Light towerLight = towerLightObj.AddComponent<Light>();
        towerLight.type = LightType.Point;
        towerLight.color = new Color(1f, 0.65f, 0.2f);
        towerLight.range = 18f;
        towerLight.intensity = 4f;

        /* 7. Ognisko obozowe na polanie obok Gladiatora (Outer Wilds Campfire) */
        GameObject campfire = new GameObject("Campfire");
        campfire.transform.SetParent(worldRoot.transform);
        campfire.transform.position = new Vector3(2.2f, 0.1f, 1.8f);

        for (int c = 0; c < 4; c++)
        {
            GameObject log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            log.transform.SetParent(campfire.transform);
            log.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            log.transform.localScale = new Vector3(0.18f, 0.65f, 0.18f);
            log.transform.localRotation = Quaternion.Euler(90f, c * 45f, 0f);
            log.GetComponent<Renderer>().sharedMaterial = trunkMat;
        }

        GameObject fireLightObj = new GameObject("CampfireLight");
        fireLightObj.transform.SetParent(campfire.transform);
        fireLightObj.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        Light fireLight = fireLightObj.AddComponent<Light>();
        fireLight.type = LightType.Point;
        fireLight.color = new Color(1f, 0.5f, 0.15f);
        fireLight.range = 14f;
        fireLight.intensity = 3.5f;

        AssetDatabase.SaveAssets();
        Debug.Log("Swiat w stylu Firewatch / Outer Wilds gotowy! Wcisnij Play i rozejrzyj sie po lesie!");
    }

    private static Material CreateFlatMat(Shader s, Color c)
    {
        Material m = new Material(s);
        m.color = c;
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", 0.05f);
        return m;
    }
}
