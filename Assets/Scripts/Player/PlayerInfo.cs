using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
    // Start is called before the first frame update
    class PlayerInformation
    {
        private int health = 100;
        private int ammo = 30;
        private int score = 0;
        private float speed = 5.0f;
        private float shootSpeed = 1.0f;
        private int damage = 10;
        private int attack = 10;

        public int Health { get => health; set => health = value; }
        public int Ammo { get => ammo; set => ammo = value; }
        public int Score { get => score; set => score = value; }
        public float Speed { get => speed; set => speed = value; }
        public float ShootSpeed { get => shootSpeed; set => shootSpeed = value; }
        public int Damage { get => damage; set => damage = value; }
        public int Attack { get => attack; set => attack = value; }
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
