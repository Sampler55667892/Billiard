using UnityEngine;

public class Wall : MonoBehaviour {
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
