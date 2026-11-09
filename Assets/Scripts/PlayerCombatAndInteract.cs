using UnityEngine;
using UnityEngine.InputSystem;

public enum StatType { None, Strength, Stamina, Intelligence, Knowledge, Finesse, Charisma }

public class PlayerCombatAndInteract : MonoBehaviour
{
    public Transform swordPivot;
    public float attackRange = 3.2f;
    public int baseDamage = 20;

    [Header("Player Health")]
    public int maxHp = 100;
    public int currentHp = 100;

    [Header("Permanent Stats")]
    public int strength = 1;
    public int stamina = 1;
    public int intelligence = 1;
    public int knowledge = 1;
    public int finesse = 1;
    public int charisma = 1;

    [Header("Unspent Level-Up Pool")]
    public int unspentStrength = 0;
    public int unspentStamina = 0;
    public int unspentIntelligence = 0;
    public int unspentKnowledge = 0;
    public int unspentFinesse = 0;
    public int unspentCharisma = 0;

    [HideInInspector] public bool isBlocking = false;
    [HideInInspector] public bool isSwinging = false;

    private float swingTimer = 0f;
    private Quaternion startRot;
    private Vector3 startPos;
    private NPCFighter activeNPC = null;
    private NPCFighter hoveredNPC = null;
    private int dialogueStage = 0;
    private string outcomeMessage = "";

    private void Start()
    {
        if (swordPivot != null)
        {
            startRot = swordPivot.localRotation;
            startPos = swordPivot.localPosition;
        }
        RecalculateStats();
    }

    public void AddStat(StatType stat, int amount)
    {
        switch (stat)
        {
            case StatType.Strength: strength += amount; unspentStrength += amount; break;
            case StatType.Stamina: stamina += amount; unspentStamina += amount; RecalculateStats(); break;
            case StatType.Intelligence: intelligence += amount; unspentIntelligence += amount; break;
            case StatType.Knowledge: knowledge += amount; unspentKnowledge += amount; break;
            case StatType.Finesse: finesse += amount; unspentFinesse += amount; break;
            case StatType.Charisma: charisma += amount; unspentCharisma += amount; break;
        }
    }

    private void RecalculateStats()
    {
        maxHp = 100 + (stamina * 15);
        if (currentHp > maxHp) currentHp = maxHp;
    }

    public void TakePlayerDamage(int dmg)
    {
        if (isBlocking)
        {
            dmg = Mathf.Max(1, Mathf.RoundToInt(dmg * 0.2f));
        }
        currentHp -= dmg;
        if (currentHp <= 0)
        {
            currentHp = maxHp;
            transform.position = new Vector3(0f, 1.5f, -5f);
        }
    }

    private void Update()
    {
        hoveredNPC = null;
        if (Camera.main != null)
        {
            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, attackRange))
            {
                hoveredNPC = hit.collider.GetComponentInParent<NPCFighter>();
            }
        }

        if (!SimplePlayerMovement.isMenuOpen && hoveredNPC != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (!hoveredNPC.isHostile && !hoveredNPC.isDead)
            {
                activeNPC = hoveredNPC;
                dialogueStage = 0;
                outcomeMessage = "";
                SimplePlayerMovement.isMenuOpen = true;
            }
        }

        if (SimplePlayerMovement.isMenuOpen)
        {
            isBlocking = false;
            return;
        }

        isBlocking = (Mouse.current != null && Mouse.current.rightButton.isPressed && !isSwinging);

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !isSwinging && !isBlocking)
        {
            isSwinging = true;
            swingTimer = 0f;

            if (hoveredNPC != null && !hoveredNPC.isDead)
            {
                hoveredNPC.OnPlayerAttackedNotice();
                int totalDmg = baseDamage + (strength * 5) + (finesse * 2);
                hoveredNPC.TakeDamage(totalDmg, this);
            }
        }

        if (swordPivot != null)
        {
            if (isSwinging)
            {
                float swingSpeed = 8f + (finesse * 0.5f);
                swingTimer += Time.deltaTime * swingSpeed;
                float angle = Mathf.Sin(swingTimer * Mathf.PI) * 70f;
                swordPivot.localRotation = startRot * Quaternion.Euler(angle, -angle * 0.4f, 0f);
                swordPivot.localPosition = startPos;
                if (swingTimer >= 1f)
                {
                    isSwinging = false;
                    swordPivot.localRotation = startRot;
                }
            }
            else if (isBlocking)
            {
                Quaternion blockRot = Quaternion.Euler(10f, -55f, 65f);
                Vector3 blockPos = new Vector3(0.1f, -0.15f, 0.55f);
                swordPivot.localRotation = Quaternion.Lerp(swordPivot.localRotation, blockRot, Time.deltaTime * 14f);
                swordPivot.localPosition = Vector3.Lerp(swordPivot.localPosition, blockPos, Time.deltaTime * 14f);
            }
            else
            {
                swordPivot.localRotation = Quaternion.Lerp(swordPivot.localRotation, startRot, Time.deltaTime * 12f);
                swordPivot.localPosition = Vector3.Lerp(swordPivot.localPosition, startPos, Time.deltaTime * 12f);
            }
        }
    }

    private void OnGUI()
    {
        GUI.skin.box.fontSize = 13;
        GUI.skin.button.fontSize = 13;
        GUI.skin.label.fontSize = 14;

        GUILayout.BeginArea(new Rect(15, 15, 310, 230), GUI.skin.box);
        GUILayout.Label("<b>GRACZ HP: " + currentHp + " / " + maxHp + (isBlocking ? " [BLOK PPM]" : "") + "</b>");
        GUILayout.Space(4);
        GUILayout.Label("<b>CHARACTER DEVELOPMENT (Total | Pool):</b>");
        GUILayout.Label("Strength: " + strength + " (+" + unspentStrength + ") | Dmg: " + (baseDamage + strength * 5 + finesse * 2));
        GUILayout.Label("Stamina: " + stamina + " (+" + unspentStamina + ")");
        GUILayout.Label("Intelligence: " + intelligence + " (+" + unspentIntelligence + ")");
        GUILayout.Label("Knowledge: " + knowledge + " (+" + unspentKnowledge + ")");
        GUILayout.Label("Finesse: " + finesse + " (+" + unspentFinesse + ")");
        GUILayout.Label("Charisma: " + charisma + " (+" + unspentCharisma + ")");
        GUILayout.EndArea();

        GUI.Label(new Rect(Screen.width / 2 - 5, Screen.height / 2 - 10, 20, 20), "+");

        if (hoveredNPC != null && !SimplePlayerMovement.isMenuOpen && !hoveredNPC.isDead)
        {
            string msg = hoveredNPC.isHostile
                ? "[LPM] Atakuj  |  [PPM] Blokuj (" + hoveredNPC.npcName + ": " + hoveredNPC.hp + "/" + hoveredNPC.maxHp + " HP)"
                : "[E] Rozmawiaj z: " + hoveredNPC.npcName + "  |  [LPM] Zaatakuj  |  [PPM] Garda";
            GUI.Box(new Rect(Screen.width / 2 - 240, Screen.height - 80, 480, 35), msg);
        }

        if (SimplePlayerMovement.isMenuOpen && activeNPC != null)
        {
            DrawDialogueWindow();
        }
    }

    private void DrawDialogueWindow()
    {
        GUILayout.BeginArea(new Rect(Screen.width / 2 - 260, Screen.height / 2 - 190, 520, 380), GUI.skin.box);

        GUILayout.Label("<b>POZIOM TRUDNOSCI AI PRZECIWNIKA:</b>");
        GUILayout.BeginHorizontal();
        if (GUILayout.Toggle(activeNPC.difficulty == AIDifficulty.Easy, "Latwy (Wolny)", "Button")) activeNPC.SetDifficulty(AIDifficulty.Easy);
        if (GUILayout.Toggle(activeNPC.difficulty == AIDifficulty.Medium, "Sredni (Zbalansowany)", "Button")) activeNPC.SetDifficulty(AIDifficulty.Medium);
        if (GUILayout.Toggle(activeNPC.difficulty == AIDifficulty.Hard, "Trudny (Paruje ciosy)", "Button")) activeNPC.SetDifficulty(AIDifficulty.Hard);
        GUILayout.EndHorizontal();
        GUILayout.Space(10);

        GUILayout.Label("<b>" + activeNPC.npcName + ":</b>");

        if (dialogueStage == 0)
        {
            GUILayout.Label("\"Patrzcie go! Nowy na arenie! Dawaj sakiewke na wino, albo zaraz sprawdze na Tobie swoj miecz!\"");
            GUILayout.Space(12);

            if (GUILayout.Button("1. [Charisma] Postawie Ci dzban wina, opowiedz mi o mistrzu areny (+1 Charisma)", GUILayout.Height(34)))
            {
                AddStat(StatType.Charisma, 1);
                dialogueStage = 1;
            }
            if (GUILayout.Button("2. [Intelligence] Przyjrzyj sie jego pracy nog i dziurawej gardzie (+1 Intelligence)", GUILayout.Height(34)))
            {
                AddStat(StatType.Intelligence, 1);
                dialogueStage = 2;
            }
            if (GUILayout.Button("3. [Prowokacja] 'Ledwo stoisz na nogach, pijaku. Pokaz co potrafisz!' (Walka)", GUILayout.Height(34)))
            {
                AddStat(StatType.Strength, 1);
                activeNPC.isHostile = true;
                SimplePlayerMovement.isMenuOpen = false;
                activeNPC = null;
            }
            if (GUILayout.Button("4. [Finesse] Wytrac mu kufel z reki szybkim ruchem i odskocz (+1 Finesse, Walka)", GUILayout.Height(34)))
            {
                AddStat(StatType.Finesse, 1);
                activeNPC.TakeDamage(15, this);
                activeNPC.isHostile = true;
                SimplePlayerMovement.isMenuOpen = false;
                activeNPC = null;
            }
        }
        else if (dialogueStage == 1)
        {
            GUILayout.Label("\"Ha! Masz gest! Sluchaj: mistrz areny zawsze zaczyna od ciezkiego ciecia z gory. Jak zablokujesz pierwszy cios, jest Twoj!\"");
            GUILayout.Space(12);

            if (GUILayout.Button("1. Zapamietaj te wiedze taktyczna (+2 Knowledge, Pokojowe zakonczenie)", GUILayout.Height(36)))
            {
                AddStat(StatType.Knowledge, 2);
                outcomeMessage = "Uniknales rozlewu krwi i zdobyles bezcenna wiedze o arenie (+2 Knowledge)!";
                dialogueStage = 99;
            }
            if (GUILayout.Button("2. 'Dzieki za rade. A teraz rozgrzejmy sie w sparingu!' (+1 Stamina, Walka)", GUILayout.Height(36)))
            {
                AddStat(StatType.Stamina, 1);
                activeNPC.isHostile = true;
                SimplePlayerMovement.isMenuOpen = false;
                activeNPC = null;
            }
            if (GUILayout.Button("3. Dolej mu jeszcze wiecej wina, zeby oslabic go przed walka (+1 Intelligence, Oslabiony Boss)", GUILayout.Height(36)))
            {
                AddStat(StatType.Intelligence, 1);
                activeNPC.hp = Mathf.Max(20, activeNPC.hp - 40);
                activeNPC.isHostile = true;
                SimplePlayerMovement.isMenuOpen = false;
                activeNPC = null;
            }
        }
        else if (dialogueStage == 2)
        {
            GUILayout.Label("Zauwazasz, ze przez alkohol wolno podnosi lewa reke do bloku. Mozesz to wykorzystac.");
            GUILayout.Space(12);

            if (GUILayout.Button("1. [Knowledge] Wytknij mu bledy w postawie - zawstydz go (+1 Knowledge, +1 Charisma)", GUILayout.Height(36)))
            {
                AddStat(StatType.Knowledge, 1);
                AddStat(StatType.Charisma, 1);
                outcomeMessage = "Gladiator spuscil glowe i odszedl cwiczyc. Wygrywasz bez wyciagania broni!";
                dialogueStage = 99;
            }
            if (GUILayout.Button("2. [Finesse] Zaatakuj w jego martwy punkt zanim uniesie miecz (+1 Finesse, Darmowe trafienie)", GUILayout.Height(36)))
            {
                AddStat(StatType.Finesse, 1);
                activeNPC.TakeDamage(30, this);
                activeNPC.isHostile = true;
                SimplePlayerMovement.isMenuOpen = false;
                activeNPC = null;
            }
        }
        else if (dialogueStage == 99)
        {
            GUILayout.Label("<b>WYNIK INTERAKCJI:</b>\n" + outcomeMessage);
            GUILayout.Space(20);
            if (GUILayout.Button("Zamknij i kontynuuj eksploracje", GUILayout.Height(40)))
            {
                SimplePlayerMovement.isMenuOpen = false;
                activeNPC = null;
            }
        }

        GUILayout.EndArea();
    }
}
