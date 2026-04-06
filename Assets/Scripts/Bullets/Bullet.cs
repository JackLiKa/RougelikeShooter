using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Bullet : MonoBehaviour
{


    public int damage;
    public int attack;
    public float speed;

    public float arrawDistance;
    public float lifeTime;

    private Rigidbody2D rg2d;
    private Collider2D other;

    
    
    private Vector2 startPos;

    // Start is called before the first frame update
    void Start()
    {
        rg2d=GetComponent<Rigidbody2D>();
        rg2d.velocity=transform.right*speed;
        startPos=transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        // if(other.gameObject.CompareTag("Enemy"))
        // {
        //     Destroy(gameObject);
        //     other.gameObject.GetComponent<Enemy>().TakeDamage(damage);
        // }
    }
}
