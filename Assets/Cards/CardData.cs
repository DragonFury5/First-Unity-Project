using System.Collections.Generic;
using UnityEngine;

public enum CardType { Unit, Support }

// Note: OnDeploy is kept for asset-serialization compatibility, but is no
// longer processed anywhere. Only OnRecover fires.
public enum SupportTrigger { OnDeploy, OnRecover }

public enum SupportEffectType { Heal, GrantShield, BuffDamage, DirectDamage, Refresh, DebuffDamage }

// SelfOnly deleted per design call.
public enum SupportTarget { AllAllies, AllEnemies, SingleAlly, SingleEnemy }

[System.Serializable]
public class SupportSkill
{
    public string skillName = "Skill";
    public SupportTrigger trigger = SupportTrigger.OnRecover;
    public SupportEffectType effectType = SupportEffectType.Heal;
    public int amount = 1;
    public SupportTarget target = SupportTarget.AllAllies;
    [TextArea(1, 3)] public string description = "";
}

[CreateAssetMenu(fileName = "NewCard", menuName = "Card Game/Card Data", order = 0)]
public class CardData : ScriptableObject
{
    [Header("Identity")]
    public string cardName = "New Card";
    public CardType type = CardType.Unit;
    public Sprite sprite;
    public Color tint = Color.white;

    [Header("Combat & Stats (Unit only)")]
    public int maxHealth = 10;
    public int damage = 2;
    public int shield = 0;
    public int cost = 1;

    [Header("Support Skills (Support only)")]
    public List<SupportSkill> skills = new List<SupportSkill>();

    [Header("Flavor")]
    [TextArea(2, 4)] public string description = "";
}