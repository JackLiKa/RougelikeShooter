using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 0.015f;
    

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

   private void FixedUpdate(){
        Move();
   }

    void Move(){
          if (Input.GetKey(KeyCode.A))
        {
            // Debug.Log("A KeyDown");
            this.transform.localScale = new Vector3(-1f, 1f, 1f);
            this.transform.position = this.transform.position + new Vector3(-moveSpeed, 0, 0);
        }
        if (Input.GetKey(KeyCode.D))
        {
            // Debug.Log("D KeyDown");\
            this.transform.localScale = new Vector3(1f, 1f, 1f);
            this.transform.position = this.transform.position + new Vector3(moveSpeed, 0, 0);

        }
        if (Input.GetKey(KeyCode.W))
        {
            // Debug.Log("W KeyDown");
            this.transform.position = this.transform.position + new Vector3(0, moveSpeed, 0);

        }
        if (Input.GetKey(KeyCode.S))
        {
            this.transform.position = this.transform.position + new Vector3(0, -moveSpeed, 0);

        }
    }
}
