using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum EquipSlot { Helmet, Armor, Boots }

[System.Serializable]
public class EquipmentItem
{
    public string itemName;
    public EquipSlot slot;
    public int armorBonus;
    public int bonusStrength;
    public int bonusStamina;
    public int bonusFinesse;
    public bool grantsDoubleJump;

    public EquipmentItem(string name, EquipSlot s, int arm, int str, int sta, int fin, bool dblJump = false)
    {
        itemName = name;
        slot = s;
        armorBonus = arm;
        bonusStrength = str;
        bonusStamina = sta;
        bonusFinesse = fin;
        grantsDoubleJump = dblJump;
    }
}

public class PlayerInventory : MonoBehaviour
{
    public List<EquipmentItem> backpack = new List<EquipmentItem>();
    public EquipmentItem equippedHelmet = null;
    public EquipmentItem equippedArmor = null;
    public EquipmentItem equippedBoots = null;

    public bool showInventory = false;
    private PlayerCombatAndInteract combat;
    private Vector2 scrollPos;

    private void Start()
    {
        combat = GetComponent<PlayerCombatAndInteract>();

        /* Przedmioty startowe w plecaku do przetestowania zakladania */
        backpack.Add(new EquipmentItem("Rzymski Helm Rekruta", EquipSlot.Helmet, 4, 0, 1, 0, false));
        backpack.Add(new EquipmentItem("Skorzany Napiersnik Areny", EquipSlot.Armor, 8, 1, 2, 0, false));
        backpack.Add(new EquipmentItem("Zwykle Rzymskie Sandaly", EquipSlot.Boots, 2, 0, 0, 1, false));
    }

    private void Update()
    {
        if (Keyboard.current != null && (Keyboard.current.iKey.wasPressedThisFrame || Keyboard.current.tabKey.wasPressedThisFrame))
        {
            showInventory = !showInventory;
            SimplePlayerMovement.isMenuOpen = showInventory;
        }
    }

    public bool CanDoubleJump()
    {
        return equippedBoots != null && equippedBoots.grantsDoubleJump;
    }

    public int GetTotalArmor()
    {
        int total = 0;
        if (equippedHelmet != null) total += equippedHelmet.armorBonus;
        if (equippedArmor != null) total += equippedArmor.armorBonus;
        if (equippedBoots != null) total += equippedBoots.armorBonus;
        return total;
    }

    public void AddItemToBackpack(EquipmentItem newItem)
    {
        backpack.Add(newItem);
        if (combat != null)
        {
            combat.ShowCombatText("ZDOBYTO PRZEDMIOT: " + newItem.itemName + "! (Wcisnij [I] lub [TAB])");
        }
    }

    public void EquipItem(EquipmentItem item)
    {
        if (item == null) return;
        backpack.Remove(item);

        if (item.slot == EquipSlot.Helmet)
        {
            if (equippedHelmet != null) UnequipItem(EquipSlot.Helmet);
            equippedHelmet = item;
        }
        else if (item.slot == EquipSlot.Armor)
        {
            if (equippedArmor != null) UnequipItem(EquipSlot.Armor);
            equippedArmor = item;
        }
        else if (item.slot == EquipSlot.Boots)
        {
            if (equippedBoots != null) UnequipItem(EquipSlot.Boots);
            equippedBoots = item;
        }

        ApplyItemStats(item, 1);
    }

    public void UnequipItem(EquipSlot slot)
    {
        EquipmentItem removed = null;
        if (slot == EquipSlot.Helmet && equippedHelmet != null)
        {
            removed = equippedHelmet;
            equippedHelmet = null;
        }
        else if (slot == EquipSlot.Armor && equippedArmor != null)
        {
            removed = equippedArmor;
            equippedArmor = null;
        }
        else if (slot == EquipSlot.Boots && equippedBoots != null)
        {
            removed = equippedBoots;
            equippedBoots = null;
        }

        if (removed != null)
        {
            ApplyItemStats(removed, -1);
            backpack.Add(removed);
        }
    }

    private void ApplyItemStats(EquipmentItem item, int sign)
    {
        if (combat == null) return;
        combat.strength += item.bonusStrength * sign;
        combat.stamina += item.bonusStamina * sign;
        combat.finesse += item.bonusFinesse * sign;
        combat.maxHp = 100 + (combat.stamina * 20);
        if (combat.currentHp > combat.maxHp) combat.currentHp = combat.maxHp;
    }

    private void OnGUI()
    {
        GUI.skin.box.fontSize = 13;
        GUI.skin.button.fontSize = 13;
        GUI.skin.label.fontSize = 14;

        /* Mala podpowiedz w prawym gornym rogu ekranu */
        GUI.Box(new Rect(Screen.width - 250, 15, 235, 55), "<b>[I] lub [TAB] - EKWIPUNEK</b>\nPancerz: " + GetTotalArmor() + (CanDoubleJump() ? " | <b>[DOUBLE JUMP!]</b>" : ""));

        if (!showInventory) return;

        GUILayout.BeginArea(new Rect(Screen.width / 2 - 280, Screen.height / 2 - 210, 560, 420), GUI.skin.box);
        GUILayout.Label("<b>=== EKWIPUNEK GLADIATORA (Wcisnij [I] lub [TAB] aby zamknac) ===</b>");
        GUILayout.Space(8);

        /* Sekcja zalozonego sprzetu */
        GUILayout.Label("<b>ZALOZONE ELEMENTY ZBROI (Pancerz Lacznie: " + GetTotalArmor() + "):</b>");
        DrawEquippedRow("HELM", equippedHelmet, EquipSlot.Helmet);
        DrawEquippedRow("ZBROJA", equippedArmor, EquipSlot.Armor);
        DrawEquippedRow("BUTY", equippedBoots, EquipSlot.Boots);

        GUILayout.Space(12);
        GUILayout.Label("<b>PLECAK (Kliknij 'Zaloz', aby wyposazyc):</b>");

        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(175));
        if (backpack.Count == 0)
        {
            GUILayout.Label("Plecak jest pusty.");
        }
        else
        {
            for (int i = 0; i < backpack.Count; i++)
            {
                EquipmentItem it = backpack[i];
                GUILayout.BeginHorizontal(GUI.skin.box);
                string statsInfo = "[" + it.slot + "] <b>" + it.itemName + "</b> (Armor: +" + it.armorBonus;
                if (it.bonusStrength > 0) statsInfo += ", Str: +" + it.bonusStrength;
                if (it.bonusStamina > 0) statsInfo += ", Sta: +" + it.bonusStamina;
                if (it.bonusFinesse > 0) statsInfo += ", Fin: +" + it.bonusFinesse;
                if (it.grantsDoubleJump) statsInfo += ", <b>EFEKT: DOUBLE JUMP!</b>";
                statsInfo += ")";

                GUILayout.Label(statsInfo, GUILayout.Width(410));
                if (GUILayout.Button("Zaloz", GUILayout.Width(85), GUILayout.Height(28)))
                {
                    EquipItem(it);
                    break;
                }
                GUILayout.EndHorizontal();
            }
        }
        GUILayout.EndScrollView();

        GUILayout.EndArea();
    }

    private void DrawEquippedRow(string slotTitle, EquipmentItem item, EquipSlot slot)
    {
        GUILayout.BeginHorizontal(GUI.skin.box);
        if (item != null)
        {
            string extra = item.grantsDoubleJump ? " <b>[DAJE DOUBLE JUMP!]</b>" : "";
            GUILayout.Label("<b>" + slotTitle + ":</b> " + item.itemName + " (+" + item.armorBonus + " Armor)" + extra, GUILayout.Width(420));
            if (GUILayout.Button("Zdejmij", GUILayout.Width(85), GUILayout.Height(26)))
            {
                UnequipItem(slot);
            }
        }
        else
        {
            GUILayout.Label("<b>" + slotTitle + ":</b> <i>[Pusty slot]</i>", GUILayout.Width(420));
        }
        GUILayout.EndHorizontal();
    }
}
