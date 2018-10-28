namespace Assets.Scripts.DTO
{
    [System.Serializable]
    public class DTOClientState
    {
        // JsonUtility はプロパティをサポートしない > フィールドのみ
        public bool IsActiveTurn; // FromJson用
        public bool IsGameOver;
        public DTOBallState[] BallStates;

        // _Debug用
        public override string ToString()
        {
            var strBuilder = new System.Text.StringBuilder();
            strBuilder.AppendLine($"IsActiveTurn: {IsActiveTurn}");
            strBuilder.AppendLine($"IsGameOver: {IsGameOver}");
            if (BallStates != null) {
                for (var i = 0; i < BallStates.Length; ++i) {
                    var ballState = BallStates[i];
                    strBuilder.AppendLine($"{i}: {ballState.ToString()}");
                }
            }

            return strBuilder.ToString();
        }
    }
}
