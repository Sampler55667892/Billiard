namespace Assets.Scripts.DTO
{
    [System.Serializable]
    public class DTOBallState
    {
        // 拡大・縮小は不要
        public DTOVector3 Position;
        public DTOVector3 Rotation;

        // _Debug 用
        public override string ToString()
        {
            var str = $"Position: {Position.ToString()}, Rotation: {Rotation.ToString()}";
            return str;
        }
    }
}
