using UnityEngine;
using Assets.Scripts;
using Assets.Scripts.DTO;
using System.Collections.Generic;
using WebSocketSharp;
using System;

public class GameController : MonoBehaviour {

    public GameObject ballsGameObject;
    public GameObject ballToShot;
    public GameObject line;

    const float forceFactor = 10000.0f; // 調整
    const float decisionRotationDeg = 0.5f;
    // 各ボールの停止チェック用
    const int periodFramesBallStopCheck = 120;
    const float nearlyStoppedSpeed = 0.3f;

    GameControllerState state;
    int countFramesBallStopped;

    // Update() 内で呼ぶものは毎回 new しないようにする
    DTOClientState dtoClientState;
    List<GameObject> bufferBalls;

    // Updateスレッドで実行する Action
    Action actionTask;

    WebSocket ws_connection;

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

    void Initialize()
    {
        state.Shots = false;
        countFramesBallStopped = 0;
        bufferBalls = new List<GameObject>();
        dtoClientState = new DTOClientState();

        // WebSocket で Server に接続
        if (GameControllerState.IsOnline)
            ConnectToServer();
    }

    void ConnectToServer()
    {
        ws_connection = new WebSocket($"ws://{Constants.serverHost}:{Constants.serverPort}/ws");
        // ハンドラ登録
        ws_connection.OnOpen += (sender, e) => {
            Debug.Log("ws.OnOpen()");
        };
        ws_connection.OnMessage += (sender, e) => {
            Debug.Log($"ws.OnMessage(): {e.Data}");
            // 初回
            if (string.Compare(Constants.srvm_activeTurn, e.Data) == 0) {
                if (state.IsWatingTurn)
                    state.Change();
                else
                    state.ErrorState();
            } else if (string.Compare(Constants.srvm_notActiveTurn, e.Data) == 0) {
                // nop
            } else {
                // 初回以降
                dtoClientState = JsonUtility.FromJson<DTOClientState>(e.Data);
                //Debug.Log($"dto: {dto}");
                if (dtoClientState == null)
                    state.ErrorWebSocket();
                else {
                    // このコンテキストで GameObject の transform を変更するとエラー発生 -> Update() で実行させる
                    actionTask = () => {
                        Debug.Log("actionTask()/Sync");

                        // ボール位置の同期
                        if (!SyncBallPositions(dtoClientState.BallStates)) {
                            state.ErrorWebSocket();
                            return;
                        }

                        // ゲームオーバー判定
                        if (dtoClientState.IsGameOver) {
                            state.ChangeToGameOver();
                            return;
                        }

                        if (dtoClientState.IsActiveTurn) {
                            if (state.IsWatingTurn)
                                state.Change();
                            else
                                state.ErrorState();
                        } else {
                            // nop
                        }
                    };
                }
            }
        };
        ws_connection.OnClose += (sender, e) => {
            Debug.Log("ws.OnClose()");
            state.ErrorWebSocket();
            ws_connection = null;
        };
        ws_connection.Connect();
    }

    bool SyncBallPositions(DTOBallState[] ballStates)
    {
        if (ballStates == null)
            return false;
        if (ballStates.Length != ballsGameObject.transform.childCount) {
            Debug.Log($"ballStates.Length: {ballStates.Length}");
            Debug.Log($"ballsGameObject.transform.childCount: {ballsGameObject.transform.childCount}");
            return false;
        }

        for (var i = 0; i < ballsGameObject.transform.childCount; ++i) {
            var ball = ballsGameObject.transform.GetChild(i).gameObject;
            var ballState = ballStates[i];
            ball.transform.position = ballState.Position.UnityVector3;
            ball.transform.rotation = Quaternion.Euler(ballState.Rotation.UnityVector3);
        }

        return true;
    }

    void Start()
    {
        ballToShot.SetActive(false);

        dtoClientState.BallStates = new DTOBallState[ballsGameObject.transform.childCount];
        for (var i = 0; i < dtoClientState.BallStates.Length; ++i)
            dtoClientState.BallStates[i] = new DTOBallState();
    }
	
	void Update () {
        if (actionTask != null) {
            actionTask();
            actionTask = null;
            return;
        }

        UpdateUIText();

        if (ProcessMouseEvent())
            return;
        if (ProcessKeyEvent())
            return;

        GetBallRefs();

        if (CheckAllBallStopped()) {
            // 手玉を回収
            ballToShot.SetActive(false);

            var isGameOver = CheckBall9Dropped();
            // オフラインなら通信なしで状態遷移
            if (GameControllerState.IsOnline)
                SendClientState(isGameOver);
            else
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
        } else if (state.IsGameOver)
            uiText.text = "Game is over.";
        else if (state.IsAbort)
            uiText.text = "Error.";
        else if (state.IsWebSocketError)
            uiText.text = "Web socket connection error.";
        else if (state.IsStateError)
            uiText.text = "State error.";
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
                if (hitObject == null)
                    return false;
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
            if (Input.GetKey(KeyCode.LeftArrow)) {
                Camera.main.transform.RotateAround(ballToShot.transform.position, Vector3.up, decisionRotationDeg);
                ShowShotLine();
            } else if (Input.GetKey(KeyCode.RightArrow)) {
                Camera.main.transform.RotateAround(ballToShot.transform.position, Vector3.up, -decisionRotationDeg);
                ShowShotLine();
            }
            return false;
        }

        return false;
    }

    void ShowShotLine()
    {
        line.SetActive(true);

        var cameraPosition = Camera.main.transform.position;
        cameraPosition.y = 0f;
        var ballToShotPosition = ballToShot.transform.position;
        var lineRenderer = line.GetComponent<LineRenderer>();
        lineRenderer.SetPositions(new Vector3[] { cameraPosition, ballToShotPosition });
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
            if (ball.transform.position.y < -1f) // 誤差対策
                return true;
        }

        return false;
    }

    void ShotBall()
    {
        System.Diagnostics.Debug.Assert(ballToShot != null);

        var rigidBody = ballToShot.GetComponent<Rigidbody>();

        // 線を非表示
        line.SetActive(false);

        // カメラは位置 + 局所座標系を持つ + 並行移動 + 追加の回転あり
        var cameraPosition = Camera.main.transform.position;
        var ballToShotPosition = ballToShot.transform.position;

        var direction = ballToShotPosition - cameraPosition;
        direction.y = 0f;
        direction.Normalize();
        var forceScale = UISlider.value * forceFactor;
        Debug.Log($"forceScale: {forceScale}");
        var force = direction * forceScale;
        rigidBody.AddForce(force);

        Debug.Log("Added Force.");

        //_DEBUG
        //UpdateDTOClientState(false);
        //Debug.Log(dtoClientState);
        //var json = JsonUtility.ToJson(dtoClientState);
        //Debug.Log(json);
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

    void SendClientState(bool isGameOver)
    {
        Debug.Log("SendClientState()");

        // サーバにショット後に停止したボール位置などを送信
        if (ws_connection == null)
            return;

        UpdateDTOClientState(isGameOver);
        var json = JsonUtility.ToJson(dtoClientState);
        Debug.Log(json);

        ws_connection.Send(json);
        // 再送しないために状態遷移
        state.Change(isGameOver);
    }

    void UpdateDTOClientState(bool isGameOver)
    {
        dtoClientState.IsActiveTurn = true;
        dtoClientState.IsGameOver = isGameOver;

        for (var i = 0; i < ballsGameObject.transform.childCount; ++i)
            UpdateDTOBallStates(i, ballsGameObject.transform.GetChild(i).gameObject);
    }

    void UpdateDTOBallStates(int index, GameObject ball)
    {
        dtoClientState.BallStates[index].Position = DTOVector3.ConvertFrom(ball.transform.position);
        dtoClientState.BallStates[index].Rotation = DTOVector3.ConvertFrom(ball.transform.rotation.eulerAngles);
    }

    void OnDestroy()
    {
        // ソケットの明示的な切断
        if (ws_connection != null) {
            ws_connection.Close();
            ws_connection = null;
        }
    }
}
