namespace Assets.Scripts
{
    public class GameControllerState
    {
        int state;

        // ショットするボールの選択
        // 方向の決定 (ボールを回転中心点とした回転)
        // ショットの威力の決定
        // ボールをショット

        public bool IsWatingTurn => state == 0;
        public bool IsSelectingBall => state == 1;
        public bool IsDecidingDirection => state == 2;
        public bool IsDecidingShotPower => state == 3;
        public bool IsGameOver => state == 4;
        public bool IsAbort => state == -1;

        public void Initialize() => state = 0;

        public void Abort() => state = -1;

        public void ChangeState(bool isGameOver = false)
        {
            switch (state) {
                case 0:
                    state = 1;
                    break;
                case 1:
                    state = 2;
                    break;
                case 2:
                    state = 3;
                    break;
                case 3:
                    // 終了 or 次のターン待ち
                    if (isGameOver)
                        state = 4;
                    else
                        state = 0;
                    break;
            }
        }

        internal void _Test(int state) => this.state = state;
    }
}
