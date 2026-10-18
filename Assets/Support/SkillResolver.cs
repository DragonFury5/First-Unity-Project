using UnityEngine;

public static class SkillResolver
{
    public static void ApplySkill(SupportSkill skill, Card supportCard)
    {
        Debug.Log($"[SkillResolver] Resolving {skill.skillName} ({skill.effectType} {skill.amount}) for {skill.target}");
        
        Card[] allCards = Object.FindObjectsByType<Card>(FindObjectsInactive.Exclude);

        // 1. Handle Single Target Skills
        if (skill.target == SupportTarget.SingleAlly || skill.target == SupportTarget.SingleEnemy)
        {
            bool wantAlly = (skill.target == SupportTarget.SingleAlly);

            if (supportCard.ownerId == Team.Player)
            {
                // Player picks a target using the UI
                if (TargetSelector.Instance != null)
                {
                    TargetSelector.Instance.BeginSelection(Team.Player, wantAlly, (selectedCard) =>
                    {
                        ApplyEffectToCard(skill, selectedCard);
                    });
                }
            }
            else
            {
                // Enemy AI auto-picks a target
                Card bestTarget = null;
                foreach (Card c in allCards)
                {
                    if (c == null || c.IsDead || c.IsSupport) continue;
                    bool isAlly = c.ownerId == Team.Enemy;

                                // Skip cards in hand
            Draggable d = c.GetComponent<Draggable>();
            if (d != null && d.IsInHand) continue;
                    
                    if ((wantAlly && isAlly) || (!wantAlly && !isAlly))
                    {
                        // AI logic: If healing, pick the most hurt ally. If damaging, pick lowest HP enemy.
                        if (skill.effectType == SupportEffectType.Heal || skill.effectType == SupportEffectType.GrantShield)
                        {
                            if (bestTarget == null || c.currentHealth < bestTarget.currentHealth) bestTarget = c;
                        }
                        else
                        {
                            if (bestTarget == null || c.currentHealth < bestTarget.currentHealth) bestTarget = c;
                        }
                    }
                }
                
                if (bestTarget != null) ApplyEffectToCard(skill, bestTarget);
            }
            return; // Stop here, the rest is for AoE
        }

        // 2. Handle AoE (All Allies / All Enemies) Skills
        foreach (Card c in allCards)
        {
            if (c == null || c.IsDead || c.IsSupport) continue;

            bool isAlly = c.ownerId == supportCard.ownerId;
            
            if (skill.target == SupportTarget.AllAllies && !isAlly) continue;
            if (skill.target == SupportTarget.AllEnemies && isAlly) continue;

            ApplyEffectToCard(skill, c);
        }
    }

    // Helper method to apply the effect so we don't have to write this twice
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