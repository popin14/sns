using UnityEngine;

public enum AIDifficulty { Easy, Medium, Hard }

public class NPCFighter : MonoBehaviour
{
    public string npcName = "Pijany Gladiator";
    public AIDifficulty difficulty = AIDifficulty.Medium;
    public int maxHp = 220;
    public int hp = 220;
    public bool isHostile = false;
    public bool isDead = false;
    public bool isBlocking = false;
    public Transform weaponPivot;

    private Renderer rend;
    private Transform playerTransform;
    private PlayerCombatAndInteract playerCombat;

    private float moveSpeed = 3.4f;
    private int attackDamage = 18;
    private float attackCooldown = 1.8f;
    private float actionTimer = 0f;
    private float stunTimer = 0f;

    /* Czerwona strefa Telegraphingu (jak skillshot Bossa w LoL-u) */
    private bool isChargingAoE = false;
    private float aoeChargeTimer = 0f;
    private GameObject redZoneTelegraph;

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

    public void SetDifficulty(AIDifficulty diff)
    {
        difficulty = diff;
    }

    public void ApplyStun(float duration)
    {
        if (isDead) return;
        stunTimer = duration;
        isBlocking = false;
        if (isChargingAoE)
        {
            isChargingAoE = false;
            if (redZoneTelegraph != null) Object.Destroy(redZoneTelegraph);
        }
    }

    private void CreateOverheadHealthBar()
    {
        hpBarRoot = new GameObject("OverheadHPBar");
        hpBarRoot.transform.SetParent(transform);
        hpBarRoot.transform.localPosition = new Vector3(0f, 1.5f, 0f);

        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlit == null) unlit = Shader.Find("Standard");

        GameObject fillPivot = new GameObject("FillPivot");
        fillPivot.transform.SetParent(hpBarRoot.transform);
        fillPivot.transform.localPosition = new Vector3(-0.65f, 0f, -0.01f);

        GameObject fill = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fill.transform.SetParent(fillPivot.transform);
        fill.transform.localPosition = new Vector3(0.65f, 0f, 0f);
        fill.transform.localScale = new Vector3(1.3f, 0.16f, 1f);
        Object.Destroy(fill.GetComponent<Collider>());
        Material fillMat = new Material(unlit);
        fillMat.color = new Color(0.85f, 0.15f, 0.15f);
        fillMat.SetColor("_BaseColor", new Color(0.85f, 0.15f, 0.15f));
        fill.GetComponent<Renderer>().material = fillMat;
        hpFillBar = fillPivot.transform;

        GameObject txtObj = new GameObject("HP_Text");
        txtObj.transform.SetParent(hpBarRoot.transform);
        txtObj.transform.localPosition = new Vector3(0f, 0.28f, -0.02f);
        txtObj.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
        hpTextMesh = txtObj.AddComponent<TextMesh>();
        hpTextMesh.anchor = TextAnchor.MiddleCenter;
        hpTextMesh.alignment = TextAlignment.Center;
        hpTextMesh.fontSize = 36;
        hpTextMesh.color = Color.white;
    }

    private void Update()
    {
        if (hpBarRoot != null && Camera.main != null)
        {
            hpBarRoot.transform.rotation = Camera.main.transform.rotation;
            hpFillBar.localScale = new Vector3(Mathf.Clamp01((float)hp / maxHp), 1f, 1f);
            string state = stunTimer > 0f ? " [STUN!]" : (isChargingAoE ? " [LADUJE ULT!]" : "");
            hpTextMesh.text = npcName + "\n" + hp + "/" + maxHp + " HP" + state;
            hpTextMesh.color = stunTimer > 0f ? Color.yellow : Color.white;
        }

        if (isDead || SimplePlayerMovement.isMenuOpen || playerTransform == null) return;

        if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;
            return;
        }

        if (!isHostile) return;

        if (isChargingAoE)
        {
            aoeChargeTimer -= Time.deltaTime;
            if (aoeChargeTimer <= 0f)
            {
                isChargingAoE = false;
                if (redZoneTelegraph != null)
                {
                    if (Vector3.Distance(playerTransform.position, redZoneTelegraph.transform.position) <= 3.6f && playerCombat != null)
                    {
                        playerCombat.TakePlayerDamage(40);
                    }
                    Object.Destroy(redZoneTelegraph);
                }
            }
            return;
        }

        Vector3 lookPos = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);
        transform.LookAt(lookPos);
        float dist = Vector3.Distance(transform.position, lookPos);

        if (dist > 2.4f)
        {
            transform.position = Vector3.MoveTowards(transform.position, lookPos, moveSpeed * Time.deltaTime);
        }
        else
        {
            actionTimer -= Time.deltaTime;
            if (actionTimer <= 0f)
            {
                if (Random.value < 0.45f)
                {
                    StartBossRedZoneSkill();
                }
                else if (playerCombat != null)
                {
                    playerCombat.TakePlayerDamage(attackDamage);
                }
                actionTimer = attackCooldown;
            }
        }
    }

    private void StartBossRedZoneSkill()
    {
        isChargingAoE = true;
        aoeChargeTimer = 1.25f;
        redZoneTelegraph = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        redZoneTelegraph.transform.position = new Vector3(playerTransform.position.x, 0.08f, playerTransform.position.z);
        redZoneTelegraph.transform.localScale = new Vector3(6.5f, 0.05f, 6.5f);
        Object.Destroy(redZoneTelegraph.GetComponent<Collider>());

        Shader s = Shader.Find("Universal Render Pipeline/Unlit");
        if (s == null) s = Shader.Find("Standard");
        Material m = new Material(s);
        m.color = new Color(0.95f, 0.1f, 0.1f);
        m.SetColor("_BaseColor", new Color(0.95f, 0.1f, 0.1f));
        redZoneTelegraph.GetComponent<Renderer>().material = m;
    }

    public void OnPlayerAttackedNotice() { }

    public void TakeDamage(int dmg, PlayerCombatAndInteract player)
    {
        if (isDead) return;
        isHostile = true;
        hp -= dmg;

        if (hp <= 0)
        {
            hp = 0;
            isDead = true;
            if (redZoneTelegraph != null) Object.Destroy(redZoneTelegraph);
            player.AddStat(StatType.Strength, 2);
            player.AddStat(StatType.Finesse, 1);
            PlayerInventory inv = player.GetComponent<PlayerInventory>();
            if (inv != null)
            {
                inv.AddItemToBackpack(new EquipmentItem("Sandaly Pijanego Gladiatora", EquipSlot.Boots, 5, 1, 1, 3, true));
            }
            if (rend != null) rend.material.color = Color.gray;
            transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            transform.position = new Vector3(transform.position.x, 0.5f, transform.position.z);
        }
    }
}

