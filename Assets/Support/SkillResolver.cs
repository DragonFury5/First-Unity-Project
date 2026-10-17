using UnityEngine;

public static class SkillResolver
{
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
            // We will handle OnRecover in PhaseManager
        }
    }

    public static void ApplySkill(SupportSkill skill, Card supportCard)
    {
        Debug.Log($"[SkillResolver] Resolving {skill.skillName} ({skill.effectType} {skill.amount}) for {skill.target}");
        
        Card[] allCards = Object.FindObjectsByType<Card>(FindObjectsInactive.Exclude);

        foreach (Card c in allCards)
        {
            if (c == null || c.IsDead || c.IsSupport) continue; // Support cards can't be targeted

            bool isAlly = c.ownerId == supportCard.ownerId;
            
            // Check targeting rules
            if (skill.target == SupportTarget.AllAllies && !isAlly) continue;
            if (skill.target == SupportTarget.AllEnemies && isAlly) continue;
            
            if (skill.target == SupportTarget.SingleAlly || skill.target == SupportTarget.SingleEnemy)
            {
                // TODO: We will build the UI for this next!
                Debug.LogWarning("Single target skills need the TargetSelector UI.");
                continue; 
            }

            // Apply the effect
            switch (skill.effectType)
            {
                case SupportEffectType.Heal:
                    c.Heal(skill.amount);
                    break;
                case SupportEffectType.GrantShield:
                    c.AddShield(skill.amount);
                    break;
                case SupportEffectType.BuffDamage:
                    c.temporaryDamageBonus += skill.amount;
                    break;
                case SupportEffectType.DebuffDamage:
                    c.temporaryDamagePenalty += skill.amount;
                    break;
                case SupportEffectType.DirectDamage:
                    c.TakeDamage(skill.amount);
                    if (EnemyAI.Instance != null) 
                        EnemyAI.Instance.ShowDamagePopUp(c.transform.position, skill.amount);
                    break;
                case SupportEffectType.Refresh:
                    c.SetExhausted(false);
                    break;
            }
        }
    }
}