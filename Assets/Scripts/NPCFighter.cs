using UnityEngine;

public enum AIDifficulty { Easy, Medium, Hard }

public class NPCFighter : MonoBehaviour
{
    public string npcName = "Pijany Gladiator";
    public AIDifficulty difficulty = AIDifficulty.Medium;
    public int maxHp = 120;
    public int hp = 120;
    public bool isHostile = false;
    public bool isDead = false;
    public bool isBlocking = false;
    public Transform weaponPivot;

    private Renderer rend;
    private Transform playerTransform;
    private PlayerCombatAndInteract playerCombat;

    private float moveSpeed = 3.2f;
    private int attackDamage = 15;
    private float attackCooldown = 1.6f;
    private float blockChance = 0.35f;

    private float actionTimer = 0f;
    private float blockTimer = 0f;
    private float swingTimer = 0f;
    private bool isSwinging = false;
    private float strafeDir = 1f;
    private float strafeTimer = 0f;

    private GameObject hpBarRoot;
    private Transform hpFillBar;
    private TextMesh hpTextMesh;
    private Quaternion startWeaponRot;

    private void Start()
    {
        rend = GetComponent<Renderer>();
        GameObject p = GameObject.Find("Player");
        if (p != null)
        {
            playerTransform = p.transform;
            playerCombat = p.GetComponent<PlayerCombatAndInteract>();
        }
        if (weaponPivot != null) startWeaponRot = weaponPivot.localRotation;

        SetDifficulty(difficulty);
        CreateOverheadHealthBar();
    }

    public void SetDifficulty(AIDifficulty diff)
    {
        difficulty = diff;
        if (diff == AIDifficulty.Easy)
        {
            maxHp = 100; hp = 100; moveSpeed = 2.4f; attackDamage = 10; attackCooldown = 2.2f; blockChance = 0.15f;
        }
        else if (diff == AIDifficulty.Medium)
        {
            maxHp = 140; hp = 140; moveSpeed = 3.4f; attackDamage = 18; attackCooldown = 1.5f; blockChance = 0.40f;
        }
        else if (diff == AIDifficulty.Hard)
        {
            maxHp = 200; hp = 200; moveSpeed = 4.4f; attackDamage = 26; attackCooldown = 1.0f; blockChance = 0.70f;
        }
    }

    private void CreateOverheadHealthBar()
    {
        hpBarRoot = new GameObject("OverheadHPBar");
        hpBarRoot.transform.SetParent(transform);
        hpBarRoot.transform.localPosition = new Vector3(0f, 1.45f, 0f);

        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlit == null) unlit = Shader.Find("Unlit/Color");

        GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bg.name = "HP_BG";
        bg.transform.SetParent(hpBarRoot.transform);
        bg.transform.localPosition = Vector3.zero;
        bg.transform.localScale = new Vector3(1.3f, 0.18f, 1f);
        Object.Destroy(bg.GetComponent<Collider>());
        Material bgMat = new Material(unlit);
        bgMat.color = new Color(0.15f, 0.15f, 0.15f);
        bgMat.SetColor("_BaseColor", new Color(0.15f, 0.15f, 0.15f));
        bg.GetComponent<Renderer>().material = bgMat;

        GameObject fillPivot = new GameObject("FillPivot");
        fillPivot.transform.SetParent(hpBarRoot.transform);
        fillPivot.transform.localPosition = new Vector3(-0.65f, 0f, -0.01f);

        GameObject fill = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fill.name = "HP_Fill";
        fill.transform.SetParent(fillPivot.transform);
        fill.transform.localPosition = new Vector3(0.65f, 0f, 0f);
        fill.transform.localScale = new Vector3(1.3f, 0.15f, 1f);
        Object.Destroy(fill.GetComponent<Collider>());
        Material fillMat = new Material(unlit);
        fillMat.color = new Color(0.85f, 0.15f, 0.15f);
        fillMat.SetColor("_BaseColor", new Color(0.85f, 0.15f, 0.15f));
        fill.GetComponent<Renderer>().material = fillMat;
        hpFillBar = fillPivot.transform;

        GameObject txtObj = new GameObject("HP_Text");
        txtObj.transform.SetParent(hpBarRoot.transform);
        txtObj.transform.localPosition = new Vector3(0f, 0.25f, -0.02f);
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
            float ratio = Mathf.Clamp01((float)hp / maxHp);
            hpFillBar.localScale = new Vector3(ratio, 1f, 1f);
            hpTextMesh.text = npcName + " (" + difficulty + ")\n" + hp + "/" + maxHp + " HP" + (isBlocking ? " [BLOKUJE!]" : "");
            hpTextMesh.color = isBlocking ? Color.cyan : Color.white;
        }

        if (isDead || SimplePlayerMovement.isMenuOpen || playerTransform == null) return;

        if (isHostile)
        {
            RunCombatAI();
        }

        AnimateNPCWeapon();
    }

    private void RunCombatAI()
    {
        Vector3 lookPos = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);
        transform.LookAt(lookPos);

        float dist = Vector3.Distance(transform.position, lookPos);

        if (blockTimer > 0f)
        {
            blockTimer -= Time.deltaTime;
            isBlocking = true;
        }
        else
        {
            isBlocking = false;
        }

        if (dist > 2.3f && !isSwinging)
        {
            float spd = isBlocking ? moveSpeed * 0.5f : moveSpeed;
            transform.position = Vector3.MoveTowards(transform.position, lookPos, spd * Time.deltaTime);
        }
        else if (dist <= 2.3f)
        {
            strafeTimer -= Time.deltaTime;
            if (strafeTimer <= 0f)
            {
                strafeDir = Random.value > 0.5f ? 1f : -1f;
                strafeTimer = Random.Range(1.0f, 2.5f);
            }
            transform.position += transform.right * strafeDir * (moveSpeed * 0.45f) * Time.deltaTime;

            actionTimer -= Time.deltaTime;
            if (actionTimer <= 0f && !isBlocking && !isSwinging)
            {
                if (Random.value < 0.25f)
                {
                    blockTimer = Random.Range(0.8f, 1.5f);
                    actionTimer = 1.0f;
                }
                else
                {
                    StartNPCAttack();
                }
            }
        }
    }

    public void OnPlayerAttackedNotice()
    {
        if (isDead || isSwinging) return;
        if (Random.value <= blockChance)
        {
            isBlocking = true;
            blockTimer = 0.7f;
        }
    }

    private void StartNPCAttack()
    {
        isSwinging = true;
        swingTimer = 0f;
        actionTimer = attackCooldown;
    }

    private void AnimateNPCWeapon()
    {
        if (weaponPivot == null) return;

        if (isSwinging)
        {
            swingTimer += Time.deltaTime * 6f;
            float angle = Mathf.Sin(swingTimer * Mathf.PI) * 75f;
            weaponPivot.localRotation = startWeaponRot * Quaternion.Euler(angle, 0f, 0f);

            if (swingTimer >= 0.5f && swingTimer - Time.deltaTime * 6f < 0.5f)
            {
                if (Vector3.Distance(transform.position, playerTransform.position) <= 2.9f && playerCombat != null)
                {
                    playerCombat.TakePlayerDamage(attackDamage);
                }
            }

            if (swingTimer >= 1f)
            {
                isSwinging = false;
                weaponPivot.localRotation = startWeaponRot;
            }
        }
        else if (isBlocking)
        {
            Quaternion blk = Quaternion.Euler(0f, -45f, 65f);
            weaponPivot.localRotation = Quaternion.Lerp(weaponPivot.localRotation, blk, Time.deltaTime * 12f);
        }
        else
        {
            weaponPivot.localRotation = Quaternion.Lerp(weaponPivot.localRotation, startWeaponRot, Time.deltaTime * 10f);
        }
    }

    public void TakeDamage(int dmg, PlayerCombatAndInteract player)
    {
        if (isDead) return;
        isHostile = true;

        if (isBlocking)
        {
            dmg = Mathf.Max(1, Mathf.RoundToInt(dmg * 0.2f));
            if (rend != null) rend.material.color = Color.cyan;
        }
        else
        {
            if (rend != null) rend.material.color = new Color(0.9f, 0.2f, 0.2f);
            transform.position += (transform.position - player.transform.position).normalized * 0.25f;
        }

        hp -= dmg;

        if (hp <= 0)
        {
            hp = 0;
            isDead = true;
            isBlocking = false;
            player.AddStat(StatType.Strength, 2);
            player.AddStat(StatType.Stamina, 1);
            player.AddStat(StatType.Finesse, 1);
            if (rend != null) rend.material.color = Color.gray;
            transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            transform.position = new Vector3(transform.position.x, 0.5f, transform.position.z);
        }
    }
}
