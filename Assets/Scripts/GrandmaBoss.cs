using UnityEngine;

public class GrandmaBoss : MonoBehaviour
{
    public string npcName = "Stara Babcia";
    public int maxHp = 1200;
    public int hp = 1200;
    public int baseAttackDamage = 45;
    public float moveSpeed = 5.2f;

    public bool isHostile = false;
    public bool isDead = false;
    public bool isSittingOnBench = false;
    public bool defeatedPlayerBefore = false;

    public Transform benchSeatPoint;
    public Transform caneWeapon;

    private Transform playerTransform;
    private PlayerCombatAndInteract playerCombat;
    private Renderer rend;

    private float basicAttackTimer = 0f;
    private float copyComboTimer = 4.5f;
    private float stunTimer = 0f;
    private int comboStep = 0;
    private float comboStepDelay = 0f;

    private GameObject hpBarRoot;
    private Transform hpFillBar;
    private TextMesh hpTextMesh;

    private void Start()
    {
        rend = GetComponent<Renderer>();
        GameObject p = GameObject.Find("Player");
        if (p != null)
        {
            playerTransform = p.transform;
            playerCombat = p.GetComponent<PlayerCombatAndInteract>();
        }
        CreateOverheadHealthBar();
    }

    private void CreateOverheadHealthBar()
    {
        hpBarRoot = new GameObject("GrandmaHPBar");
        hpBarRoot.transform.SetParent(transform);
        hpBarRoot.transform.localPosition = new Vector3(0f, 1.55f, 0f);

        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlit == null) unlit = Shader.Find("Standard");

        GameObject fillPivot = new GameObject("FillPivot");
        fillPivot.transform.SetParent(hpBarRoot.transform);
        fillPivot.transform.localPosition = new Vector3(-0.75f, 0f, -0.01f);

        GameObject fill = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fill.transform.SetParent(fillPivot.transform);
        fill.transform.localPosition = new Vector3(0.75f, 0f, 0f);
        fill.transform.localScale = new Vector3(1.5f, 0.18f, 1f);
        Object.Destroy(fill.GetComponent<Collider>());
        Material fillMat = new Material(unlit);
        fillMat.color = new Color(0.65f, 0.15f, 0.85f);
        fillMat.SetColor("_BaseColor", new Color(0.65f, 0.15f, 0.85f));
        fill.GetComponent<Renderer>().material = fillMat;
        hpFillBar = fillPivot.transform;

        GameObject txtObj = new GameObject("HP_Text");
        txtObj.transform.SetParent(hpBarRoot.transform);
        txtObj.transform.localPosition = new Vector3(0f, 0.3f, -0.02f);
        txtObj.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
        hpTextMesh = txtObj.AddComponent<TextMesh>();
        hpTextMesh.anchor = TextAnchor.MiddleCenter;
        hpTextMesh.alignment = TextAlignment.Center;
        hpTextMesh.fontSize = 36;
        hpTextMesh.color = Color.white;
    }

    public void SitOnBenchPeacefully()
    {
        isHostile = false;
        isSittingOnBench = true;
        comboStep = 0;
        transform.localScale = new Vector3(0.9f, 0.75f, 0.9f);
        if (benchSeatPoint != null)
        {
            transform.position = benchSeatPoint.position;
            transform.rotation = benchSeatPoint.rotation;
        }
    }

    public void OnPlayerKilledByGrandma()
    {
        if (isHostile)
        {
            defeatedPlayerBefore = true;
            hp = maxHp;
            npcName = "Stara Babcia (Odpoczywa)";
            SitOnBenchPeacefully();
        }
    }

    public void TriggerBossFight(PlayerCombatAndInteract player)
    {
        if (isDead) return;
        isSittingOnBench = false;
        isHostile = true;
        transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
        transform.position = new Vector3(transform.position.x, 1.1f, transform.position.z);
        npcName = "ARCymistrzyni Babcia [BOSS]";
        if (rend != null) rend.material.color = new Color(0.55f, 0.1f, 0.75f);
        player.ShowCombatText("BABCIA WKROCZYLA W TRYB BOSSA! KOPIUJE TWOJE SKILLE!");
    }

    public void ApplyStun(float dur)
    {
        stunTimer = dur;
    }

    private void Update()
    {
        if (hpBarRoot != null && Camera.main != null)
        {
            hpBarRoot.transform.rotation = Camera.main.transform.rotation;
            hpFillBar.localScale = new Vector3(Mathf.Clamp01((float)hp / maxHp), 1f, 1f);
            string status = !isHostile
                ? (isSittingOnBench ? "[Siedzi na laweczce]" : "[Potrzebuje pomocy - E]")
                : (comboStep > 0 ? " [KOPIUJE TWOJE SKILLE!]" : " [BASE ATK: " + baseAttackDamage + "]");
            hpTextMesh.text = npcName + "\n" + (isHostile ? (hp + "/" + maxHp + " HP\n") : "") + status;
            hpTextMesh.color = isHostile ? new Color(1f, 0.4f, 1f) : Color.green;
        }

        if (isDead || !isHostile || SimplePlayerMovement.isMenuOpen || playerTransform == null) return;

        if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;
            return;
        }

        Vector3 lookPos = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);
        transform.LookAt(lookPos);
        float dist = Vector3.Distance(transform.position, lookPos);

        /* Sekwencja kopiowania wszystkich 4 zdolnosci gracza */
        if (comboStep > 0)
        {
            ExecuteCopiedAbilitiesCombo();
            return;
        }

        copyComboTimer -= Time.deltaTime;
        if (copyComboTimer <= 0f)
        {
            comboStep = 1;
            comboStepDelay = 0.1f;
            copyComboTimer = 6.5f;
            return;
        }

        if (dist > 2.3f)
        {
            transform.position = Vector3.MoveTowards(transform.position, lookPos, moveSpeed * Time.deltaTime);
        }
        else
        {
            basicAttackTimer -= Time.deltaTime;
            if (basicAttackTimer <= 0f)
            {
                if (caneWeapon != null) caneWeapon.localRotation = Quaternion.Euler(65f, 0f, 0f);
                playerCombat.TakePlayerDamage(baseAttackDamage);
                basicAttackTimer = 1.25f;
            }
            else if (caneWeapon != null)
            {
                caneWeapon.localRotation = Quaternion.Lerp(caneWeapon.localRotation, Quaternion.identity, Time.deltaTime * 8f);
            }
        }
    }

    private void ExecuteCopiedAbilitiesCombo()
    {
        comboStepDelay -= Time.deltaTime;
        if (comboStepDelay > 0f) return;

        if (comboStep == 1)
        {
            /* Kopia Skilla [2] Gracz: Blyskawiczny Dash do gracza */
            playerCombat.ShowCombatText("BABCIA KOPIUJE [2] DASH!");
            Vector3 targetPos = playerTransform.position - playerTransform.forward * 1.5f;
            transform.position = new Vector3(targetPos.x, transform.position.y, targetPos.z);
            playerCombat.SpawnSkillVisual(transform.position, new Vector3(2f, 0.2f, 2f), new Color(0.7f, 0.2f, 1f));
            comboStep = 2;
            comboStepDelay = 0.45f;
        }
        else if (comboStep == 2)
        {
            /* Kopia Skilla [3] Gracz: Stun CC skalowany Twoja Charisma i Stamina */
            playerCombat.ShowCombatText("BABCIA KOPIUJE [3] OGLUSZENIE (STUN)!");
            playerCombat.SpawnSkillVisual(transform.position + transform.forward * 1.5f, new Vector3(3f, 0.3f, 3f), Color.magenta);
            if (Vector3.Distance(transform.position, playerTransform.position) <= 4.5f)
            {
                playerCombat.ApplyPlayerStun(1.2f);
                playerCombat.TakePlayerDamage(20 + (playerCombat.stamina * 4));
            }
            comboStep = 3;
            comboStepDelay = 0.65f;
        }
        else if (comboStep == 3)
        {
            /* Kopia Skilla [1] Gracz: Przebijajace Ostrze skalowane Twoim Strength */
            playerCombat.ShowCombatText("BABCIA KOPIUJE [1] PRZEBIJAJACE OSTRZE!");
            playerCombat.SpawnSkillVisual(transform.position + transform.forward * 2.2f, new Vector3(1.5f, 0.25f, 4.5f), new Color(1f, 0.3f, 0.8f));
            if (Vector3.Distance(transform.position, playerTransform.position) <= 5.5f)
            {
                int copiedQDmg = 35 + (playerCombat.strength * 8);
                playerCombat.TakePlayerDamage(copiedQDmg);
            }
            comboStep = 4;
            comboStepDelay = 0.75f;
        }
        else if (comboStep == 4)
        {
            /* Kopia Skilla [4] Gracz: ULTIMATE Wyrok Areny */
            playerCombat.ShowCombatText("BABCIA UZYWA TWOJEGO [4] ULTIMATE!");
            playerCombat.SpawnSkillVisual(playerTransform.position, new Vector3(5.0f, 0.45f, 5.0f), new Color(0.9f, 0.05f, 0.3f));
            if (Vector3.Distance(transform.position, playerTransform.position) <= 6.5f)
            {
                int copiedUlt = 60 + (playerCombat.strength * 8) + (playerCombat.intelligence * 8);
                playerCombat.TakePlayerDamage(copiedUlt);
            }
            comboStep = 0;
        }
    }

    public void TakeDamage(int dmg, PlayerCombatAndInteract player)
    {
        if (isDead) return;
        if (!isHostile)
        {
            TriggerBossFight(player);
        }

        hp -= dmg;
        if (hp <= 0)
        {
            hp = 0;
            isDead = true;
            isHostile = false;
            player.AddStat(StatType.Strength, 5);
            player.AddStat(StatType.Intelligence, 5);
            player.AddStat(StatType.Finesse, 5);
            player.ShowCombatText("POKONALES BABCIE BOSS! (+5 DO STATYSTYK!)");
            if (rend != null) rend.material.color = Color.gray;
            transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}
