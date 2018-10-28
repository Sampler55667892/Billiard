using UnityEngine;

namespace Assets.Scripts.DTO
{
    // 小数点以下の有効桁数のコントロール用
    [System.Serializable]
    public class DTOVector3
    {
        public string X;
        public string Y;
        public string Z;

        internal static DTOVector3 ConvertFrom(Vector3 v) =>
            new DTOVector3 { X = Round(v.x), Y = Round(v.y), Z = Round(v.z) };

        static string Round(float v) => System.Math.Round((decimal)v, 2).ToString();

        // _Debug 用
        public override string ToString() => $"X:{X}, Y:{Y}, Z:{Z}";
    }
}
