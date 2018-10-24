using UnityEngine;
using Assets.Scripts;

public class Ball : MonoBehaviour {

    public GameObject gameController;
    const float stopCriterionSpeed = 0.1f;

	void Update () {
		//Stop();
	}

    // Rigidbody を持っていること前提
    void Stop()
    {
        var rigidBody = base.GetComponent<Rigidbody>();
        System.Diagnostics.Debug.Assert(rigidBody != null);

        // 既に停止 -> 何もしない
        if (rigidBody.velocity == Vector3.zero)
            return;

        // 速度 0 付近で止めると方向変換 (壁での反射) が出来なくなる
        //if (rigidBody.velocity.sqrMagnitude < stopCriterionSpeed) {
        //    rigidBody.velocity = Vector3.zero;
        //    NotifyStoppingToGameController();
        //}
    }

    void NotifyStoppingToGameController()
    {
        //Debug.Log("NotifyStoppingToGameController()");
        //UnityEngine.EventSystems.ExecuteEvents.Execute<INotifyStopping>(gameController, null, (x, y) => x.OnNotifyStopping(base.gameObject.name));
    }
}
