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

        internal Vector3 UnityVector3
        {
            get {
                var v = new Vector3();
                float temp;
                if (float.TryParse(this.X, out temp))
                    v.x = temp;
                if (float.TryParse(this.Y, out temp))
                    v.y = temp;
                if (float.TryParse(this.Z, out temp))
                    v.z = temp;
                return v;
            }
        }

        static string Round(float v) => System.Math.Round((decimal)v, 2).ToString();

        // _Debug 用
        public override string ToString() => $"X:{X}, Y:{Y}, Z:{Z}";
    }
}
