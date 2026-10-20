using UnityEngine;

public static class SkillResolver
{
    public static void ApplySkill(SupportSkill skill, Card supportCard)
    {
        if (skill == null || supportCard == null) return;

        Card[] allCards = Object.FindObjectsByType<Card>(FindObjectsInactive.Exclude);

        // Single target
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
                Card bestTarget = null;
                foreach (Card c in allCards)
                {
                    if (c == null || c.IsDead || c.IsSupport) continue;

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

        // AOE — deployed units only (hand cards and supports excluded)
        foreach (Card c in allCards)
        {
            if (c == null || c.IsDead || c.IsSupport) continue;

            Draggable d = c.GetComponent<Draggable>();
            if (d != null && d.IsInHand) continue;   // <-- the fix

            bool isAlly = c.ownerId == supportCard.ownerId;
            if (skill.target == SupportTarget.AllAllies && !isAlly) continue;
            if (skill.target == SupportTarget.AllEnemies && isAlly) continue;

            ApplyEffectToCard(skill, c);
        }
    }

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