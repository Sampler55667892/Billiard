using UnityEngine;
using Assets.Scripts;
using System.Collections.Generic;
using WebSocketSharp;

public class GameController : MonoBehaviour {

    public GameObject ballsGameObject;
    public GameObject ballToShot;

    const float forceFactor = 10000.0f; // 調整
    const float decisionRotationDeg = 0.5f;
    // 各ボールの停止チェック用
    const int periodFramesBallStopCheck = 120;
    const float nearlyStoppedSpeed = 0.3f;

    GameControllerState state;
    int countFramesBallStopped;

    // Update() 内で呼ぶものは毎回 new しないようにする
    List<GameObject> bufferBalls;

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

        if (!GameControllerState.IsOnline)
            state.ForceSet(1);

        Initialize();
    }

    void Start()
    {
        ballToShot.SetActive(false);
    }

    void Initialize()
    {
        state.Shots = false;
        countFramesBallStopped = 0;
        bufferBalls = new List<GameObject>();

        // WebSocket で Server に接続
        if (GameControllerState.IsOnline)
            ConnectToServer();
    }

    void ConnectToServer()
    {
        ws_socket = new WebSocket("ws://127.0.0.1:8080/ws");
        // ハンドラ登録
        ws_socket.OnOpen += (sender, e) => {
            Debug.Log("ws.OnOpen()");
        };
        ws_socket.OnMessage += (sender, e) => {
            Debug.Log($"ws.OnMessage(): {e.Data}");
            if (string.Compare(Constants.srvm_activeTurn, e.Data) == 0) {
                state.Change();
            } else if (string.Compare(Constants.srvm_notActiveTurn, e.Data) == 0) {
                // nop
            }
        };
        ws_socket.OnClose += (sender, e) => {
            Debug.Log("ws.OnClose()");
            state.Abort();
            ws_socket = null;
        };
        ws_socket.Connect();
    }
	
	void Update () {
        UpdateUIText();

        if (ProcessMouseEvent())
            return;
        if (ProcessKeyEvent())
            return;

        GetBallRefs();

        if (CheckAllBallStopped()) {
            // 手玉を回収
            ballToShot.SetActive(false);

            // サーバにショット後に停止したボール位置を送信
            if (ws_socket != null) {
                //ws_socket.Send("ball positions...");
            }

            var isGameOver = CheckBall9Dropped();
            state.Change(isGameOver);
        }
	}

    void UpdateUIText()
    {
        var canvas = FindObjectOfType<Canvas>();
        var uiText = canvas.transform.Find("Text").gameObject.GetComponent<UnityEngine.UI.Text>();
        var slider = canvas.transform.Find("Slider").gameObject;

        if (state.IsWatingTurn) {
            uiText.text = "Waiting turn.";
            slider.SetActive(false);
            ShotButton.SetActive(false);
        } else if (state.IsSettingAtackBall) {
            uiText.text = "Please set a atack ball (Click).";
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
        } else if (state.IsGameOver) {
            uiText.text = "Game is over.";
        } else if (state.IsAbort) {
            uiText.text = "Web socket connection error.";
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
        if (Input.GetMouseButtonDown(Constants.mouseLeftButtonId)) {
            if (state.IsSettingAtackBall) {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hitInfo;
                var hitObject = GetHitObject(ray, out hitInfo);
                if (hitObject != null &&
                    (string.Compare(hitObject.tag, Constants.ballOtherTag) == 0 || string.Compare(hitObject.tag, Constants.ball9Tag) == 0)) {
                    // 別のボールにぶつかる (微妙な位置判定まではしていない)
                    return false;
                }
                if (string.Compare(hitObject.tag, Constants.mainBoardTag) == 0) { // これで置く場所の決定と配置位置がボード上かを判定
                    var positionToSet = hitInfo.point;
                    // y座標補正
                    positionToSet.y = 0.5f;
                    // 直前の速度を 0 化
                    var rigidBody = ballToShot.GetComponent<Rigidbody>();
                    rigidBody.velocity = Vector3.zero;
                    ballToShot.transform.position = positionToSet;
                    ballToShot.SetActive(true);

                    state.Change();
                }
            } else if (state.IsDecidingDirection)
                state.Change();
            else if (state.IsDecidingShotPower) {
            }

            return true;
        } else if (Input.GetMouseButtonUp(Constants.mouseLeftButtonId)) {
        }
        return false;
    }

    bool ProcessMouseRightEvent()
    {
        if (Input.GetMouseButtonDown(Constants.mouseRightButtonId)) {
        }
        return false;
    }

    bool ProcessKeyEvent()
    {
        var moveVector = Vector3.zero;
        var moveFactor = 0.5f;

        if (state.IsSettingAtackBall) {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
                moveVector = Vector3.left * moveFactor;
            else if (Input.GetKeyDown(KeyCode.RightArrow))
                moveVector = Vector3.right * moveFactor;
            else if (Input.GetKeyDown(KeyCode.UpArrow))
                moveVector = Vector3.up * moveFactor;
            else if (Input.GetKeyDown(KeyCode.DownArrow))
                moveVector = Vector3.down * moveFactor;

            if (moveVector == Vector3.zero)
                return false;

            var translation = Camera.main.transform.position;
            Camera.main.transform.Translate(moveVector);

            return true;
        } else if (state.IsDecidingDirection) {
            // 手玉中心の回転
            if (Input.GetKey(KeyCode.LeftArrow))      
                Camera.main.transform.RotateAround(ballToShot.transform.position, Vector3.up, decisionRotationDeg);
            else if (Input.GetKey(KeyCode.RightArrow))
                Camera.main.transform.RotateAround(ballToShot.transform.position, Vector3.up, -decisionRotationDeg);
            return false;
        }

        return false;
    }

    void GetBallRefs()
    {
        bufferBalls.Clear();

        for (var i = 0; i < ballsGameObject.transform.childCount; ++i)
            bufferBalls.Add(ballsGameObject.transform.GetChild(i).gameObject);
    }

    // 各ボールが一定フレームの間、一定速度以下になったかどうかチェック
    bool CheckAllBallStopped()
    {
        if (!state.IsDecidingShotPower)
            return false;
        if (!state.Shots)
            return false;

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

    bool CheckBall9Dropped()
    {
        if (bufferBalls.Count == 0)
            return false;

        for (var i = 0; i < bufferBalls.Count; ++i) {
            var ball = bufferBalls[i];
            if (string.Compare(ball.tag, Constants.ball9Tag) != 0)
                continue;
            if (ball.transform.position.y < 0f)
                return true;
        }

        return false;
    }

    void ShotBall()
    {
        System.Diagnostics.Debug.Assert(ballToShot != null);

        var rigidBody = ballToShot.GetComponent<Rigidbody>();

		var ray = ballToShot.transform.position - Camera.main.transform.position;
        var direction = ray;
        direction.Normalize();
        var forceScale = UISlider.value * forceFactor;
        Debug.Log($"forceScale: {forceScale}");
        var force = direction * forceScale;
        rigidBody.AddForce(force);

        Debug.Log("Added Force.");
    }

    GameObject GetHitObject(Ray ray, out RaycastHit hitInfo)
    {
        if (Physics.Raycast(ray, out hitInfo)) {
            var hitObject = hitInfo.collider.gameObject;
            return hitObject;
        }
        return null;
    }

    public void OnShotButtonClicked()
    {
        Debug.Log("OnShotButtonClicked()");

        if (state.IsDecidingShotPower) {
            state.Shots = true;
            ShotBall();

            // TODO: 全ボールが停止した > Server に HTTP で移動後のボール座標とポケットしているかどうかを送信
            // > サーバからの Broadcast 待ち状態 (Wait turn) に状態遷移
            // 全ボールの速さが一定時間間隔内で一定値以下であり続けたら (反射による停止除外) 全て停止させる
        }
    }
}
