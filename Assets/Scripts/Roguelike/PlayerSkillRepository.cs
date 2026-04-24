using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerSkillProfile
{
    public PlayerType PlayerType;
    public string SkillName;
    public string Description;
    public float Cooldown;
    public float Duration;
    public bool AutoActivate;
    public bool InfiniteCooldown;
    public bool UsesAimDirection;
    public float EffectRange;
    public float DamageMultiplier;
    public float DamagePercentOfTargetMaxHp;
    public float CompanionDamagePercentOfTargetMaxHp;
    public int BuffAttackBonus;
    public int BuffMaxHpBonus;
    public float BuffAttackPercent;
    public float BuffMaxHpPercent;
    public float BuffMoveSpeedBonus;
    public float BuffMoveSpeedPercent;
    public float BuffShootSpeedBonus;
    public int RepeatedShots;
    public float RepeatInterval;
    public string IconResourcePath;
    public string AnimationResourcePath;

    private Sprite cachedIcon;
    private AnimationClip cachedAnimationClip;

    public Sprite LoadIcon()
    {
        if (cachedIcon == null && !string.IsNullOrWhiteSpace(IconResourcePath))
        {
            cachedIcon = Resources.Load<Sprite>(IconResourcePath);
        }

        return cachedIcon;
    }

    public AnimationClip LoadAnimationClip()
    {
        if (cachedAnimationClip == null && !string.IsNullOrWhiteSpace(AnimationResourcePath))
        {
            cachedAnimationClip = Resources.Load<AnimationClip>(AnimationResourcePath);
        }

        return cachedAnimationClip;
    }

    public PlayerSkillProfile Clone()
    {
        return new PlayerSkillProfile
        {
            PlayerType = PlayerType,
            SkillName = SkillName,
            Description = Description,
            Cooldown = Cooldown,
            Duration = Duration,
            AutoActivate = AutoActivate,
            InfiniteCooldown = InfiniteCooldown,
            UsesAimDirection = UsesAimDirection,
            EffectRange = EffectRange,
            DamageMultiplier = DamageMultiplier,
            DamagePercentOfTargetMaxHp = DamagePercentOfTargetMaxHp,
            CompanionDamagePercentOfTargetMaxHp = CompanionDamagePercentOfTargetMaxHp,
            BuffAttackBonus = BuffAttackBonus,
            BuffMaxHpBonus = BuffMaxHpBonus,
            BuffAttackPercent = BuffAttackPercent,
            BuffMaxHpPercent = BuffMaxHpPercent,
            BuffMoveSpeedBonus = BuffMoveSpeedBonus,
            BuffMoveSpeedPercent = BuffMoveSpeedPercent,
            BuffShootSpeedBonus = BuffShootSpeedBonus,
            RepeatedShots = RepeatedShots,
            RepeatInterval = RepeatInterval,
            IconResourcePath = IconResourcePath,
            AnimationResourcePath = AnimationResourcePath,
            cachedIcon = cachedIcon,
            cachedAnimationClip = cachedAnimationClip
        };
    }
}

public static class PlayerSkillRepository
{
    private static readonly Dictionary<PlayerType, PlayerSkillProfile> Profiles = new Dictionary<PlayerType, PlayerSkillProfile>
    {
        {
            PlayerType.Player1,
            new PlayerSkillProfile
            {
                PlayerType = PlayerType.Player1,
                SkillName = "吸星大法",
                Description = "把地图上所有经验吸到角色身上。",
                Cooldown = 30f,
                Duration = 1.2f,
                UsesAimDirection = false,
                InfiniteCooldown = false,
                AutoActivate = false,
                EffectRange = 999f,
                IconResourcePath = "SkillIcons/Player1SkillIcon",
                AnimationResourcePath = "SkillAnimations/Player1SkillAnimation"
            }
        },
        {
            PlayerType.Player2,
            new PlayerSkillProfile
            {
                PlayerType = PlayerType.Player2,
                SkillName = "萌宠出击",
                Description = "开始游戏时一只伴生萌宠陪伴自己战斗，优先攻击离角色最近的敌人，攻速与角色一致，每次造成目标 30% 最大生命值伤害。",
                Cooldown = float.PositiveInfinity,
                Duration = 0f,
                UsesAimDirection = false,
                InfiniteCooldown = true,
                AutoActivate = true,
                EffectRange = 34f,
                CompanionDamagePercentOfTargetMaxHp = 0.3f,
                IconResourcePath = "SkillIcons/Player2SkillIcon"
            }
        },
        {
            PlayerType.Player3,
            new PlayerSkillProfile
            {
                PlayerType = PlayerType.Player3,
                SkillName = "疾风刀气",
                Description = "在 5 秒内发出 10 道刀气，每道刀气对路径上的所有怪物造成其最大生命值 10% 的伤害；技能持续期间角色获得 10% 移动速度加成。",
                Cooldown = 25f,
                Duration = 5f,
                UsesAimDirection = true,
                InfiniteCooldown = false,
                AutoActivate = false,
                EffectRange = 42f,
                DamagePercentOfTargetMaxHp = 0.1f,
                BuffMoveSpeedPercent = 0.1f,
                RepeatedShots = 10,
                RepeatInterval = 0.5f,
                IconResourcePath = "SkillIcons/Player3SkillIcon"
            }
        },
        {
            PlayerType.Player4,
            new PlayerSkillProfile
            {
                PlayerType = PlayerType.Player4,
                SkillName = "无限成长",
                Description = "每次释放可永久获得 3 点移动速度、30% 攻击力和 50% 最大生命值提升，并在当局内持续叠加。",
                Cooldown = 300f,
                Duration = 0.85f,
                UsesAimDirection = false,
                InfiniteCooldown = false,
                AutoActivate = false,
                EffectRange = 0f,
                BuffAttackPercent = 0.3f,
                BuffMaxHpPercent = 0.5f,
                BuffMoveSpeedBonus = 3f,
                IconResourcePath = "SkillIcons/Player4SkillIcon",
                AnimationResourcePath = "SkillAnimations/Player4SkillAnimation"
            }
        }
    };

    public static PlayerSkillProfile GetProfile(PlayerType playerType)
    {
        if (Profiles.TryGetValue(playerType, out PlayerSkillProfile profile))
        {
            return profile.Clone();
        }

        return Profiles[PlayerType.Player1].Clone();
    }
}
