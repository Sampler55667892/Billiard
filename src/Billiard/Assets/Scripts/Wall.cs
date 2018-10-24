using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wall : MonoBehaviour {

	void Start () {
		
	}
	
	void Update () {
		
	}

    public void OnTriggerEnter(Collider other)
    {
        //Debug.Log("OnTriggerEnter()");

        // 反射
        var rigidBody = other.GetComponent<Rigidbody>();
        if (rigidBody == null)
            return;
        // 衝突時の反作用
        //Debug.Log(rigidBody.mass);
        //Debug.Log(rigidBody.velocity);
    }

    // TODO: 壁反射の調整
    // OnTriggerEnter でなく OnCollisionEnter のタイミングで rigidBody に作用を与える (壁反射)
    public void OnCollisionEnter(Collision collision)
    {
        //Debug.Log("OnCollisionEnter()");

        if (collision.rigidbody == null)
            return;

        //Debug.Log(collision.impulse);
        //impulseCollision = collision.impulse;

        //collision.rigidbody.AddForce(new Vector3(0f, 0f, -100f));
        collision.rigidbody.AddForce(collision.impulse * 10f);
    }
}
