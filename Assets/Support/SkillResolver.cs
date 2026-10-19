using UnityEngine;

public static class SkillResolver
{
    // 1. This is what Draggable calls when you drop the card
    public static void ResolveSupportSkills(Card supportCard)
    {
        if (supportCard == null || supportCard.data == null) return;
        if (supportCard.data.type != CardType.Support) return;

        foreach (var skill in supportCard.data.skills)
        {
            if (skill.trigger == SupportTrigger.OnDeploy && supportCard.ownerId == Team.Player)
            {
                ApplySkill(skill, supportCard);
            }
        }
    }

    // 2. This handles the actual effect logic
    public static void ApplySkill(SupportSkill skill, Card supportCard)
    {
        Debug.Log($"[SkillResolver] Resolving {skill.skillName} ({skill.effectType} {skill.amount}) for {skill.target}");
        
        Card[] allCards = Object.FindObjectsByType<Card>(FindObjectsInactive.Exclude);

        // --- HANDLE SINGLE TARGET ---
        if (skill.target == SupportTarget.SingleAlly || skill.target == SupportTarget.SingleEnemy)
        {
            bool wantAlly = (skill.target == SupportTarget.SingleAlly);

            if (supportCard.ownerId == Team.Player)
            {
                if (TargetSelector.Instance != null)
                {
                    TargetSelector.Instance.BeginSelection(Team.Player, wantAlly, (selectedCard) =>
                    {
                        ApplyEffectToCard(skill, selectedCard);
                    });
                }
                else
                {
                    Debug.LogWarning("[SkillResolver] TargetSelector is missing from the scene!");
                }
            }
            else
            {
                // Enemy AI auto-picks a target
                Card bestTarget = null;
                foreach (Card c in allCards)
                {
                    if (c == null || c.IsDead || c.IsSupport) continue;
                    
                    // Make sure AI doesn't pick hand cards
                    Draggable d = c.GetComponent<Draggable>();
                    if (d != null && d.IsInHand) continue;

                    bool isAlly = c.ownerId == Team.Enemy;
                    
                    if ((wantAlly && isAlly) || (!wantAlly && !isAlly))
                    {
                        if (bestTarget == null || c.currentHealth < bestTarget.currentHealth) 
                            bestTarget = c;
                    }
                }
                
                if (bestTarget != null) ApplyEffectToCard(skill, bestTarget);
            }
            return; 
        }

        // --- HANDLE AOE (ALL ALLIES / ALL ENEMIES) ---
        foreach (Card c in allCards)
        {
            if (c == null || c.IsDead || c.IsSupport) continue;

            bool isAlly = c.ownerId == supportCard.ownerId;
            
            if (skill.target == SupportTarget.AllAllies && !isAlly) continue;
            if (skill.target == SupportTarget.AllEnemies && isAlly) continue;

            ApplyEffectToCard(skill, c);
        }
    }

    // 3. Helper method to apply the effect
    private static void ApplyEffectToCard(SupportSkill skill, Card targetCard)
    {
        if (targetCard == null || targetCard.IsDead || targetCard.IsSupport) return;

        switch (skill.effectType)
        {
            case SupportEffectType.Heal:
                targetCard.Heal(skill.amount);
                break;
            case SupportEffectType.GrantShield:
                targetCard.AddShield(skill.amount);
                break;
            case SupportEffectType.BuffDamage:
                targetCard.temporaryDamageBonus += skill.amount;
                break;
            case SupportEffectType.DebuffDamage:
                targetCard.temporaryDamagePenalty += skill.amount;
                break;
            case SupportEffectType.DirectDamage:
                targetCard.TakeDamage(skill.amount);
                if (EnemyAI.Instance != null) 
                    EnemyAI.Instance.ShowDamagePopUp(targetCard.transform.position, skill.amount);
                break;
            case SupportEffectType.Refresh:
                targetCard.SetExhausted(false);
                break;
        }
    }
}