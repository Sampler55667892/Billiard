using UnityEngine;
using Assets.Scripts;
using System.Collections.Generic;
using WebSocketSharp;

public class GameController : MonoBehaviour, INotifyStopping {

    public GameObject ballsGameObject;

    const int mouseLeftButtonId = 0;
    const int mouseRightButtonId = 1;
    const float forceFactor = 10000.0f; // 調整
    const float decisionRotationDeg = 0.5f;
    const string mainBallTag = "MainBall";
    // 各ボールの停止チェック用
    const int periodFramesBallStopCheck = 120;
    const float nearlyStoppedSpeed = 0.3f;

    GameControllerState state;
    GameObject selectedBall;
    bool shots;
    List<GameObject> bufferBalls;
    int countFramesBallStopped;

    WebSocket ws_socket;

    UnityEngine.UI.Slider UISlider
    {
        get {
            var canvas = FindObjectOfType<Canvas>();
            var slider = canvas.transform.Find("Slider").gameObject.GetComponent<UnityEngine.UI.Slider>();
            return slider;
        }
    }

    GameObject ShotButton
    {
        get {
            var canvas = FindObjectOfType<Canvas>();
            var button = canvas.transform.Find("Button").gameObject;
            return button;
        }
    }

    void Awake()
    {
        state = new GameControllerState();
        //xxxxx
        state._Test(1);

        Initialize();
    }

    void Initialize()
    {
        shots = false;
        bufferBalls = new List<GameObject>();
        countFramesBallStopped = 0;

        // WebSocket で Server に接続
        if (!ConnectToServer())
            state.Abort();
    }

    bool ConnectToServer()
    {
        ws_socket = new WebSocket("ws://127.0.0.1:8080/ws");
        // ハンドラ登録
        ws_socket.OnOpen += (sender, e) => {
            Debug.Log("ws.OnOpen()");
        };
        ws_socket.OnMessage += (sender, e) => {
            Debug.Log($"ws.OnMessage(): {e.Data}");
        };
        ws_socket.OnClose += (sender, e) => {
            Debug.Log("ws.OnClose()");
        };
        //Debug.Log("Pre ws.Connect()");
        ws_socket.Connect();
        //Debug.Log("Post ws.Connect()");
        //ws_socket.Send("_TEST_ ws");

        //ws_socket.Close();

        return true;
    }

    void Start () {
		
	}
	
	void Update () {
        UpdateUIText();

        if (ProcessMouseEvent())
            return;
        if (ProcessKeyEvent())
            return;

        if (CheckAllBallStopped()) {
            // サーバにショット後に停止したボール位置を送信
            if (ws_socket != null) {
                ws_socket.Send("ball positions...");
            }

            state.ChangeState();
            //Debug.Log($"state.IsWatingTurn: {state.IsWatingTurn}");
        }
	}

    void UpdateUIText()
    {
        var canvas = FindObjectOfType<Canvas>();
        var uiText = canvas.transform.Find("Text").gameObject.GetComponent<UnityEngine.UI.Text>();
        var slider = canvas.transform.Find("Slider").gameObject;

        if (state.IsSelectingBall) {
            uiText.text = "Please select a ball (Click).";
            slider.SetActive(false);
            ShotButton.SetActive(false);
        } else if (state.IsDecidingDirection) {
            uiText.text = "Please decide a direction (Left/Right + Click).";
            slider.SetActive(false);
            ShotButton.SetActive(false);
        } else if (state.IsDecidingShotPower) {
            uiText.text = "Please decide shot power.";
            slider.SetActive(true);
            ShotButton.SetActive(true);
        }
    }

    bool ProcessMouseEvent()
    {
        if (ProcessMouseLeftEvent())
            return true;
        if (ProcessMouseRightEvent())
            return true;
        return false;
    }

    bool ProcessMouseLeftEvent()
    {
        if (Input.GetMouseButtonDown(mouseLeftButtonId)) {
            if (state.IsSelectingBall) {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                var hitObject = GetHitObject(ray);
                if (hitObject != null && string.Compare(hitObject.tag, mainBallTag) == 0) {
                    selectedBall = hitObject;
                    state.ChangeState();
                }
            } else if (state.IsDecidingDirection)
                state.ChangeState();
            else if (state.IsDecidingShotPower) {
                //state.ChangeState();
                //ShotBall();
            }

            return true;
        } else if (Input.GetMouseButtonUp(mouseLeftButtonId)) {
            //if (state.IsDecidingShotPower) {
            //    state.ChangeState();
            //    ShotBall();
            //}
        }
        return false;
    }

    bool ProcessMouseRightEvent()
    {
        if (Input.GetMouseButtonDown(mouseRightButtonId)) {
        }
        return false;
    }

    bool ProcessKeyEvent()
    {
        var moveVector = Vector3.zero;
        var moveFactor = 0.5f;

        if (state.IsSelectingBall) {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
                moveVector = Vector3.left * moveFactor;
            else if (Input.GetKeyDown(KeyCode.RightArrow))
                moveVector = Vector3.right * moveFactor;
            else if (Input.GetKeyDown(KeyCode.UpArrow))
                moveVector = Vector3.forward * moveFactor;
            else if (Input.GetKeyDown(KeyCode.DownArrow))
                moveVector = Vector3.back * moveFactor;

            if (moveVector == Vector3.zero)
                return false;

            var translation = Camera.main.transform.position;
            Camera.main.transform.Translate(moveVector);

            return true;
        } else if (state.IsDecidingDirection) {
            System.Diagnostics.Debug.Assert(selectedBall != null);
            // ボール中心の回転
            if (Input.GetKey(KeyCode.LeftArrow))
                Camera.main.transform.RotateAround(selectedBall.transform.position, Vector3.up, decisionRotationDeg);
            else if (Input.GetKey(KeyCode.RightArrow))
                Camera.main.transform.RotateAround(selectedBall.transform.position, Vector3.up, -decisionRotationDeg);
            return false;
        }

        return false;
    }

    // 各ボールが一定フレームの間、一定速度以下になったかどうかチェック
    bool CheckAllBallStopped()
    {
        if (!state.IsDecidingShotPower)
            return false;
        if (!shots)
            return false;

        bufferBalls.Clear();
        for (var i = 0; i < ballsGameObject.transform.childCount; ++i)
            bufferBalls.Add(ballsGameObject.transform.GetChild(i).gameObject);

        if (bufferBalls.Count == 0)
            return false;

        var allBallsAreNearlyStopped = true;
        for (var i = 0; i < bufferBalls.Count; ++i) {
            var rigidBody = bufferBalls[i].GetComponent<Rigidbody>();
            // ポケットしたボールは落下して速度が 0 にならないため除外
            if (bufferBalls[i].transform.position.y < 0f)
                continue;
            if (nearlyStoppedSpeed < rigidBody.velocity.magnitude) {
                allBallsAreNearlyStopped = false;
                break;
            }
        }
        if (!allBallsAreNearlyStopped)
            return false;

        ++countFramesBallStopped;
        //Debug.Log($"#3: countFramesBallStopped: {countFramesBallStopped}");
        if (periodFramesBallStopCheck <= countFramesBallStopped) {
            //Debug.Log("#4");

            countFramesBallStopped = 0;
            // 全ボールを完全に停止
            for (var i = 0; i < bufferBalls.Count; ++i) {
                var rigidBody = bufferBalls[i].GetComponent<Rigidbody>();
                rigidBody.velocity = Vector3.zero;
            }

            return true;
        }

        return false;
    }

    void ShotBall()
    {
        System.Diagnostics.Debug.Assert(selectedBall != null);

        var rigidBody = selectedBall.GetComponent<Rigidbody>();

		//var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		var ray = selectedBall.transform.position - Camera.main.transform.position;
        var direction = ray;
        direction.Normalize();
        var forceScale = UISlider.value * forceFactor;
        Debug.Log($"forceScale: {forceScale}");
        var force = direction * forceScale;
        rigidBody.AddForce(force);

        Debug.Log("Added Force.");
    }

    GameObject GetHitObject(Ray ray)
    {
        RaycastHit hitInfo;
        if (Physics.Raycast(ray, out hitInfo)) {
            var hitObject = hitInfo.collider.gameObject;
            return hitObject;
        }
        return null;
    }

    Rigidbody GetHitRigidBody(Ray ray)
    {
        GameObject hitObject = GetHitObject(ray);
        if (hitObject != null) {
            if (string.Compare(hitObject.tag, mainBallTag) == 0) {
                var rigidBody = hitObject.GetComponent<Rigidbody>();
                //Debug.Log(rigidBody);
                return rigidBody;
            }
        }
        return null;
    }

    public void OnShotButtonClicked()
    {
        Debug.Log("OnShotButtonClicked()");

        if (state.IsDecidingShotPower) {
            //state.ChangeState();
            shots = true;
            ShotBall();

            // TODO: 全ボールが停止した > Server に HTTP で移動後のボール座標とポケットしているかどうかを送信
            // > サーバからの Broadcast 待ち状態 (Wait turn) に状態遷移
            // 全ボールの速さが一定時間間隔内で一定値以下であり続けたら (反射による停止除外) 全て停止させる
        }
    }

    public void OnNotifyStopping(string name)
    {
        Debug.Log($"OnNotifyStopping(name: {name})");
    }
}
