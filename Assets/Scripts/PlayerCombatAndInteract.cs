using UnityEngine;
using UnityEngine.InputSystem;

public enum StatType { None, Strength, Stamina, Intelligence, Knowledge, Finesse, Charisma }

public class PlayerCombatAndInteract : MonoBehaviour
{
    public Transform swordPivot;
    public float attackRange = 3.2f;
    public int baseDamage = 18;

    [Header("Player Health")]
    public int maxHp = 120;
    public int currentHp = 120;

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

    [Header("MOBA Cooldowns (1, 2, 3, 4)")]
    public float cdSkill1 = 4f;
    public float cdSkill2 = 5f;
    public float cdSkill3 = 8f;
    public float cdSkill4 = 12f;

    private float timerSkill1 = 0f;
    private float timerSkill2 = 0f;
    private float timerSkill3 = 0f;
    private float timerSkill4 = 0f;
    private int passiveStacks = 0;

    [HideInInspector] public bool isBlocking = false;
    [HideInInspector] public bool isSwinging = false;
    [HideInInspector] public float playerStunTimer = 0f;

    private float swingTimer = 0f;
    private Quaternion startRot;
    private Vector3 startPos;
    private CharacterController charController;
    private Vector3 dashVelocity = Vector3.zero;
    private float dashTimer = 0f;

    private NPCFighter activeNPC = null;
    private NPCFighter hoveredNPC = null;
    private GrandmaBoss activeGrandma = null;
    private GrandmaBoss hoveredGrandma = null;

    private int dialogueStage = 0;
    private string combatFloatingText = "";
    private float floatingTextTimer = 0f;

    private void Start()
    {
        charController = GetComponent<CharacterController>();
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
        maxHp = 100 + (stamina * 20);
        if (currentHp > maxHp) currentHp = maxHp;
    }

    public void ShowCombatText(string txt)
    {
        combatFloatingText = txt;
        floatingTextTimer = 2.0f;
    }

    public void ApplyPlayerStun(float duration)
    {
        playerStunTimer = duration;
        isBlocking = false;
        ShowCombatText("JESTES OGLUSZONY PRZEZ BABCIE! (" + duration.ToString("F1") + "s)");
    }

    public void TakePlayerDamage(int dmg)
    {
        if (dashTimer > 0f)
        {
            ShowCombatText("UNIK (DASH)!");
            return;
        }
        if (isBlocking)
        {
            dmg = Mathf.Max(1, Mathf.RoundToInt(dmg * 0.25f));
            ShowCombatText("ZABLOKOWANO! (-" + dmg + " HP)");
        }
        currentHp -= dmg;
        if (currentHp <= 0)
        {
            RespawnPlayer();
        }
    }

    private void RespawnPlayer()
    {
        currentHp = maxHp;
        playerStunTimer = 0f;
        if (charController != null) charController.enabled = false;
        transform.position = new Vector3(0f, 1.5f, -6f);
        transform.rotation = Quaternion.identity;
        if (charController != null) charController.enabled = true;

        ShowCombatText("ZGINALES! Odrodzono na spawnie (Babcia siedzi na lawce).");

        GrandmaBoss[] grandmas = Object.FindObjectsByType<GrandmaBoss>(FindObjectsSortMode.None);
        foreach (var g in grandmas)
        {
            g.OnPlayerKilledByGrandma();
        }
    }

    private void Update()
    {
        if (timerSkill1 > 0f) timerSkill1 -= Time.deltaTime;
        if (timerSkill2 > 0f) timerSkill2 -= Time.deltaTime;
        if (timerSkill3 > 0f) timerSkill3 -= Time.deltaTime;
        if (timerSkill4 > 0f) timerSkill4 -= Time.deltaTime;
        if (floatingTextTimer > 0f) floatingTextTimer -= Time.deltaTime;

        if (playerStunTimer > 0f)
        {
            playerStunTimer -= Time.deltaTime;
            AnimatePlayerSword();
            return;
        }

        if (dashTimer > 0f && charController != null)
        {
            dashTimer -= Time.deltaTime;
            charController.Move(dashVelocity * Time.deltaTime);
        }

        hoveredNPC = null;
        hoveredGrandma = null;
        if (Camera.main != null)
        {
            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, attackRange + 2f))
            {
                hoveredNPC = hit.collider.GetComponentInParent<NPCFighter>();
                hoveredGrandma = hit.collider.GetComponentInParent<GrandmaBoss>();
            }
        }

        if (!SimplePlayerMovement.isMenuOpen && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (hoveredNPC != null && !hoveredNPC.isHostile && !hoveredNPC.isDead)
            {
                activeNPC = hoveredNPC;
                activeGrandma = null;
                dialogueStage = 0;
                SimplePlayerMovement.isMenuOpen = true;
            }
            else if (hoveredGrandma != null && !hoveredGrandma.isHostile && !hoveredGrandma.isDead)
            {
                activeGrandma = hoveredGrandma;
                activeNPC = null;
                dialogueStage = 0;
                SimplePlayerMovement.isMenuOpen = true;
            }
        }

        if (SimplePlayerMovement.isMenuOpen) return;

        isBlocking = (Mouse.current != null && Mouse.current.rightButton.isPressed && !isSwinging);

        /* Auto-Attack (LPM) */
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !isSwinging && !isBlocking)
        {
            TriggerSwordSwing();
            int totalDmg = baseDamage + (strength * 4);
            passiveStacks++;
            if (passiveStacks >= 3)
            {
                totalDmg += 15 + (finesse * 5);
                passiveStacks = 0;
                ShowCombatText("PASYWKA (3 HIT): KRYTYK! " + totalDmg + " DMG!");
            }

            if (hoveredNPC != null && !hoveredNPC.isDead && Vector3.Distance(transform.position, hoveredNPC.transform.position) <= attackRange)
            {
                hoveredNPC.TakeDamage(totalDmg, this);
            }
            if (hoveredGrandma != null && !hoveredGrandma.isDead && Vector3.Distance(transform.position, hoveredGrandma.transform.position) <= attackRange)
            {
                hoveredGrandma.TakeDamage(totalDmg, this);
            }
        }

        /* SKILL 1 [Klawisz 1]: Przebijajacy Skillshot */
        if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame && timerSkill1 <= 0f)
        {
            timerSkill1 = Mathf.Max(1.5f, cdSkill1 - (finesse * 0.2f));
            TriggerSwordSwing();
            SpawnSkillVisual(transform.position + transform.forward * 2.5f, new Vector3(1.2f, 0.2f, 4.5f), new Color(1f, 0.5f, 0.1f));

            int qDmg = 32 + (strength * 8);
            DamageEnemiesInRadius(5.5f, qDmg, 0f);
            ShowCombatText("[1] PRZEBIJAJACE OSTRZE! (" + qDmg + " DMG)");
        }

        /* SKILL 2 [Klawisz 2]: Dash */
        if (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame && timerSkill2 <= 0f)
        {
            timerSkill2 = Mathf.Max(2.0f, cdSkill2 - (finesse * 0.25f));
            Vector3 dir = transform.forward;
            if (Keyboard.current.sKey.isPressed) dir = -transform.forward;
            else if (Keyboard.current.aKey.isPressed) dir = -transform.right;
            else if (Keyboard.current.dKey.isPressed) dir = transform.right;

            dashVelocity = dir.normalized * 18f;
            dashTimer = 0.22f;
            ShowCombatText("[2] DASH!");
        }

        /* SKILL 3 [Klawisz 3]: Stun CC */
        if (Keyboard.current != null && Keyboard.current.digit3Key.wasPressedThisFrame && timerSkill3 <= 0f)
        {
            timerSkill3 = cdSkill3;
            SpawnSkillVisual(transform.position + transform.forward * 1.8f, new Vector3(2.5f, 0.3f, 2.5f), Color.cyan);
            DamageEnemiesInRadius(4.0f, 15 + (stamina * 4), 1.6f + (charisma * 0.2f));
            ShowCombatText("[3] OGLUSZENIE (STUN)!");
        }

        /* SKILL 4 [Klawisz 4]: Ultimate AoE */
        if (Keyboard.current != null && Keyboard.current.digit4Key.wasPressedThisFrame && timerSkill4 <= 0f)
        {
            timerSkill4 = Mathf.Max(5f, cdSkill4 - (intelligence * 0.5f));
            TriggerSwordSwing();
            Vector3 strikePos = transform.position + transform.forward * 3.2f;
            SpawnSkillVisual(new Vector3(strikePos.x, 0.15f, strikePos.z), new Vector3(4.5f, 0.4f, 4.5f), new Color(0.95f, 0.15f, 0.2f));

            int ultDmg = 55 + (strength * 8) + (intelligence * 8);
            DamageEnemiesInRadius(6.5f, ultDmg, 0f);
            ShowCombatText("[4] ULTIMATE: WYROK ARENY! (" + ultDmg + " DMG!)");
        }

        AnimatePlayerSword();
    }

    private void DamageEnemiesInRadius(float radius, int dmg, float stunDuration)
    {
        NPCFighter[] npcs = Object.FindObjectsByType<NPCFighter>(FindObjectsSortMode.None);
        foreach (var n in npcs)
        {
            if (!n.isDead && Vector3.Distance(transform.position, n.transform.position) <= radius)
            {
                if (stunDuration > 0f) n.ApplyStun(stunDuration);
                n.TakeDamage(dmg, this);
            }
        }

        GrandmaBoss[] grandmas = Object.FindObjectsByType<GrandmaBoss>(FindObjectsSortMode.None);
        foreach (var g in grandmas)
        {
            if (!g.isDead && Vector3.Distance(transform.position, g.transform.position) <= radius)
            {
                if (stunDuration > 0f) g.ApplyStun(stunDuration * 0.5f);
                g.TakeDamage(dmg, this);
            }
        }
    }

    private void TriggerSwordSwing()
    {
        isSwinging = true;
        swingTimer = 0f;
    }

    public void SpawnSkillVisual(Vector3 pos, Vector3 scale, Color col)
    {
        GameObject fx = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        fx.transform.position = new Vector3(pos.x, 0.15f, pos.z);
        fx.transform.rotation = transform.rotation;
        fx.transform.localScale = scale;
        Object.Destroy(fx.GetComponent<Collider>());
        Shader s = Shader.Find("Universal Render Pipeline/Unlit");
        if (s == null) s = Shader.Find("Standard");
        Material m = new Material(s);
        m.color = col;
        m.SetColor("_BaseColor", col);
        fx.GetComponent<Renderer>().material = m;
        Object.Destroy(fx, 0.45f);
    }

    private void AnimatePlayerSword()
    {
        if (swordPivot == null) return;
        if (isSwinging)
        {
            swingTimer += Time.deltaTime * (8.5f + finesse * 0.4f);
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

    private void OnGUI()
    {
        GUI.skin.box.fontSize = 13;
        GUI.skin.button.fontSize = 13;
        GUI.skin.label.fontSize = 14;

        GUILayout.BeginArea(new Rect(15, 15, 305, 220), GUI.skin.box);
        string stunTxt = playerStunTimer > 0f ? " [STUN!]" : "";
        GUILayout.Label("<b>HP: " + currentHp + " / " + maxHp + stunTxt + " | Pasywka: " + passiveStacks + "/3</b>");
        GUILayout.Label("Strength: " + strength + " (+" + unspentStrength + ")");
        GUILayout.Label("Stamina: " + stamina + " (+" + unspentStamina + ")");
        GUILayout.Label("Intelligence: " + intelligence + " (+" + unspentIntelligence + ")");
        GUILayout.Label("Knowledge: " + knowledge + " (+" + unspentKnowledge + ")");
        GUILayout.Label("Finesse: " + finesse + " (+" + unspentFinesse + ")");
        GUILayout.Label("Charisma: " + charisma + " (+" + unspentCharisma + ")");
        GUILayout.EndArea();

        float barW = 540f;
        GUILayout.BeginArea(new Rect(Screen.width / 2 - barW / 2, Screen.height - 85, barW, 70), GUI.skin.box);
        GUILayout.BeginHorizontal();
        DrawSkillSlot("[1] Q: Przebicie", timerSkill1);
        DrawSkillSlot("[2] W: Dash", timerSkill2);
        DrawSkillSlot("[3] E: Stun CC", timerSkill3);
        DrawSkillSlot("[4] R: Ultimate", timerSkill4);
        GUILayout.EndHorizontal();
        GUILayout.EndArea();

        if (floatingTextTimer > 0f)
        {
            GUI.Box(new Rect(Screen.width / 2 - 220, Screen.height / 2 - 90, 440, 35), "<b>" + combatFloatingText + "</b>");
        }

        GUI.Label(new Rect(Screen.width / 2 - 5, Screen.height / 2 - 10, 20, 20), "+");

        if (!SimplePlayerMovement.isMenuOpen)
        {
            if (hoveredNPC != null && !hoveredNPC.isDead && !hoveredNPC.isHostile)
                GUI.Box(new Rect(Screen.width / 2 - 180, Screen.height - 130, 360, 30), "[E] Rozmawiaj z: " + hoveredNPC.npcName);
            else if (hoveredGrandma != null && !hoveredGrandma.isDead && !hoveredGrandma.isHostile)
                GUI.Box(new Rect(Screen.width / 2 - 210, Screen.height - 130, 420, 30), "[E] Porozmawiaj ze Staruszka  |  [LPM] Zaatakuj (Na wlasne ryzyko!)");
        }

        if (SimplePlayerMovement.isMenuOpen)
        {
            if (activeNPC != null) DrawGladiatorDialogue();
            else if (activeGrandma != null) DrawGrandmaDialogue();
        }
    }

    private void DrawSkillSlot(string title, float cd)
    {
        string status = cd > 0f ? "\nCD: " + cd.ToString("F1") + "s" : "\n[GOTOWE]";
        GUILayout.Box("<b>" + title + "</b>" + status, GUILayout.Width(126), GUILayout.Height(55));
    }

    private void DrawGladiatorDialogue()
    {
        GUILayout.BeginArea(new Rect(Screen.width / 2 - 250, Screen.height / 2 - 150, 500, 260), GUI.skin.box);
        GUILayout.Label("<b>" + activeNPC.npcName + ":</b>");
        GUILayout.Label("\"Sprawdzimy Twoje skille na arenie?! Uwazaj na moje czerwone strefy uderzen!\"");
        GUILayout.Space(12);
        if (GUILayout.Button("1. [Charisma] Pogadajmy spokojnie przy ognisku (+1 Charisma, +1 Knowledge)", GUILayout.Height(36)))
        {
            AddStat(StatType.Charisma, 1);
            AddStat(StatType.Knowledge, 1);
            SimplePlayerMovement.isMenuOpen = false;
            activeNPC = null;
        }
        if (GUILayout.Button("2. [Walka MOBA] Stajemy do pojedynku! (+1 Strength, Start Walki)", GUILayout.Height(36)))
        {
            AddStat(StatType.Strength, 1);
            activeNPC.isHostile = true;
            SimplePlayerMovement.isMenuOpen = false;
            activeNPC = null;
        }
        GUILayout.EndArea();
    }

    private void DrawGrandmaDialogue()
    {
        GUILayout.BeginArea(new Rect(Screen.width / 2 - 260, Screen.height / 2 - 150, 520, 280), GUI.skin.box);
        GUILayout.Label("<b>Stara Babcia:</b>");

        if (activeGrandma.isSittingOnBench)
        {
            if (activeGrandma.defeatedPlayerBefore)
                GUILayout.Label("\"Siedze sobie i odpoczywam. Mam nadzieje, ze wyciagnales wnioski z naszej ostatniej lekcji, mlodziencze.\"");
            else
                GUILayout.Label("\"Dziekuje za pomoc z drewnem, wnusiu. Teraz moge spokojnie posiedziec na mojej laweczce.\"");

            GUILayout.Space(20);
            if (GUILayout.Button("Odejdz w pokoju", GUILayout.Height(38)))
            {
                SimplePlayerMovement.isMenuOpen = false;
                activeGrandma = null;
            }
        }
        else
        {
            GUILayout.Label("\"Oj, moje stare plecy... Mlodziencze, pomozesz starej kobiecie przeniesc ten ciezki kosz z drewnem do laweczki?\"");
            GUILayout.Space(15);

            if (GUILayout.Button("1. [Pomoc] 'Oczywiscie babciu, juz niose!' (+2 Charisma, +1 Stamina)", GUILayout.Height(38)))
            {
                AddStat(StatType.Charisma, 2);
                AddStat(StatType.Stamina, 1);
                activeGrandma.SitOnBenchPeacefully();
                SimplePlayerMovement.isMenuOpen = false;
                activeGrandma = null;
                ShowCombatText("Pomogles Staruszce! (+2 Charisma, +1 Stamina)");
            }
            GUILayout.Space(6);
            if (GUILayout.Button("2. [Zaatakuj] 'Dawaj emeryture staruszko!' (AKTYWUJE UKRYTEGO BOSSA)", GUILayout.Height(38)))
            {
                activeGrandma.TriggerBossFight(this);
                SimplePlayerMovement.isMenuOpen = false;
                activeGrandma = null;
            }
        }
        GUILayout.EndArea();
    }
}
