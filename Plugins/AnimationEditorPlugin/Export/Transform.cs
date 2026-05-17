using System;
using System.Numerics;

namespace AssetBankPlugin.Export
{
    public class Transform
    {
        public Vector3 Position = new Vector3();
        public Quaternion Rotation = Quaternion.Identity;
        public Vector3 Scale = new Vector3();

        public Vector3 EulerAngles => ToEulerAngles(Rotation);
        public Matrix4x4 Matrix => Matrix4x4.Identity * Matrix4x4.CreateFromQuaternion(Rotation) * Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateTranslation(Position);

        public Transform(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Position = position;
            Scale = scale;
            Rotation = rotation;
        }

        public Transform(Matrix4x4 matrix)
        {
            Position = matrix.Translation;
            Rotation = Quaternion.CreateFromRotationMatrix(matrix);
            Scale = new Vector3(matrix.GetDeterminant());
        }

        public static Transform Interpolate(Transform a, Transform b, float progression)
        {
            var pos = Vector3.Lerp(a.Position, b.Position, progression);
            var rot = Quaternion.Slerp(a.Rotation, b.Rotation, progression);
            var scale = Vector3.Lerp(a.Position, b.Position, progression);
            return new Transform(pos, rot, scale);
        }

        public static Transform operator *(Transform a, Transform b)
        {
            return new Transform(a.Position * b.Position, a.Rotation * b.Rotation, a.Scale * b.Scale);
        }
        public static Transform operator +(Transform a, Transform b)
        {
            return new Transform(a.Position + b.Position, a.Rotation + b.Rotation, a.Scale + b.Scale);
        }
        public static Transform operator -(Transform a, Transform b)
        {
            return new Transform(a.Position - b.Position, a.Rotation - b.Rotation, a.Scale - b.Scale);
        }

        public static Transform operator *(Transform a, float b)
        {
            return new Transform(a.Position * b, a.Rotation * b, a.Scale * b);
        }

        public static Vector3 ToEulerAngles(Quaternion quaternion)
        {
            Vector3 result = new Vector3();

            float t0 = 2.0f * (quaternion.W * quaternion.X + quaternion.Y * quaternion.Z);
            float t1 = 1.0f - 2.0f * (quaternion.X * quaternion.X + quaternion.Y * quaternion.Y);

            result.X = (float)Math.Atan2(t0, t1);


            float t2 = 2.0f * (quaternion.W * quaternion.Y - quaternion.Z * quaternion.X);

            t2 = t2 > 1.0f ? 1.0f : t2;
            t2 = t2 < -1.0f ? -1.0f : t2;
            result.Y = (float)Math.Asin(t2);


            float t3 = +2.0f * (quaternion.W * quaternion.Z + quaternion.X * quaternion.Y);
            float t4 = +1.0f - 2.0f * (quaternion.Y * quaternion.Y + quaternion.Z * quaternion.Z);

            result.Z = (float)Math.Atan2(t3, t4);

            return result;
        }
    }
}