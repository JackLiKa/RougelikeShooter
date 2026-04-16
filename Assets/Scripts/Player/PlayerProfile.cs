using System;

[Serializable]
public class PlayerProfile
{
    public PlayerType PlayerType;
    public string PlayerKey;
    public string DisplayName;
    public int MaxHp;
    public int Attack;
    public float MoveSpeed;
    public float ShootSpeed;

    public PlayerProfile Clone()
    {
        return (PlayerProfile)MemberwiseClone();
    }
}
