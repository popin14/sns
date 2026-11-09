using UnityEngine;
using UnityEditor;

public class AutoSceneSetup
{
    [MenuItem("AutoSetup/3. Zaktualizuj AI, Blok PPM, Pasek HP i 6 Statystyk")]
    public static void SetupAdvancedCombat()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            Object.DestroyImmediate(player.GetComponent<Collider>());
            player.AddComponent<CharacterController>();
            player.AddComponent<SimplePlayerMovement>();
        }

        PlayerCombatAndInteract combat = player.GetComponent<PlayerCombatAndInteract>();
        if (combat == null) combat = player.AddComponent<PlayerCombatAndInteract>();

        Camera cam = Camera.main;
        if (cam != null && cam.transform.Find("WeaponHolder") != null)
        {
            combat.swordPivot = cam.transform.Find("WeaponHolder");
        }

        GameObject oldNpc = GameObject.Find("NPC_Gladiator");
        if (oldNpc != null) Object.DestroyImmediate(oldNpc);

        GameObject npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        npc.name = "NPC_Gladiator";
        npc.transform.position = new Vector3(0f, 1f, 2.5f);
        Material npcMat = new Material(urpLit);
        npcMat.SetColor("_BaseColor", new Color(0.25f, 0.45f, 0.8f));
        npc.GetComponent<Renderer>().sharedMaterial = npcMat;

        GameObject npcWeaponHolder = new GameObject("NPC_WeaponPivot");
        npcWeaponHolder.transform.SetParent(npc.transform);
        npcWeaponHolder.transform.localPosition = new Vector3(0.55f, 0.2f, 0.4f);

        GameObject npcSword = GameObject.CreatePrimitive(PrimitiveType.Cube);
        npcSword.name = "NPC_Sword";
        npcSword.transform.SetParent(npcWeaponHolder.transform);
        npcSword.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        npcSword.transform.localScale = new Vector3(0.1f, 1.1f, 0.1f);
        Object.DestroyImmediate(npcSword.GetComponent<Collider>());
        Material metalMat = new Material(urpLit);
        metalMat.SetColor("_BaseColor", new Color(0.8f, 0.8f, 0.85f));
        npcSword.GetComponent<Renderer>().sharedMaterial = metalMat;

        NPCFighter fighter = npc.AddComponent<NPCFighter>();
        fighter.weaponPivot = npcWeaponHolder.transform;

        Debug.Log("Gotowe! Wcisnij Play: PPM = Blok, LPM = Atak, E = Rozbudowana rozmowa i wybor trudnosci AI.");
    }
}
