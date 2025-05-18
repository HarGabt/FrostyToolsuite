using Frosty.Core.Viewport;
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshSetPlugin.Resources
{
    public static class TangentSpaceCompression
    {
        static readonly float INV_SQRT_2 = 1f / (float)Math.Sqrt(2f);
        static readonly Vector3 PackRangeMin = new Vector3(-INV_SQRT_2, -INV_SQRT_2, 0f);
        static readonly Vector3 PackRangeMax = new Vector3(INV_SQRT_2, INV_SQRT_2, MathUtil.PiOverTwo);

        static int FindGreatestComponent(Vector3 vec)
        {
            var abs = Vector3.Abs(vec);
            return abs.X > abs.Y
                ? (abs.X > abs.Z ? 0 : 2)
                : (abs.Y > abs.Z ? 1 : 2);
        }

        public static Vector4 PackAxisAngle(Vector3 tangent, Vector3 binormal, Vector3 normal)
        {
            // get cubemap face index
            int maxComponent = FindGreatestComponent(normal);
            // 0-1: X faces, 2-3: Y faces, 4-5: Z faces
            uint flags = (uint)(maxComponent * 2 + (normal[maxComponent] < 0f ? 1 : 0));

            // swizzle dominant axis into z axis
            Vector3 swizzledNormal = normal;
            swizzledNormal = (flags & 4) != 0 ? new Vector3(swizzledNormal.Z, swizzledNormal.Y, swizzledNormal.X) : swizzledNormal; // Z face
            swizzledNormal = (flags & 2) != 0 ? new Vector3(swizzledNormal.Y, swizzledNormal.Z, swizzledNormal.X) : swizzledNormal; // Y face
            swizzledNormal = new Vector3(swizzledNormal.Z, swizzledNormal.Y, swizzledNormal.X);

            // calculate reference tangent
            Vector3 refTangent;
            if ((flags & 2) != 0) // Y face
                refTangent = Vector3.Normalize(new Vector3(-swizzledNormal.Y, swizzledNormal.X, 0));
            else // Z face
                refTangent = Vector3.Normalize(new Vector3(swizzledNormal.Z, 0, -swizzledNormal.X));

            // project tangent onto normal plane
            Vector3 tangentProjected = Vector3.Normalize(tangent - normal * Vector3.Dot(tangent, normal));
            float dot = Vector3.Dot(tangentProjected, refTangent);
            float angle = (float)Math.Acos(Math.Abs(MathUtil.Clamp(dot, -1f, 1f)));

            // store binormal sign in bit 3
            Vector3 computedBinormal = Vector3.Cross(tangentProjected, normal);
            if (Vector3.Dot(computedBinormal, binormal) < 0f)
                flags |= (1 << 3);

            Vector3 axisAngle = new Vector3(swizzledNormal.X, swizzledNormal.Y, angle);

            Vector3 normalized = (axisAngle - PackRangeMin) / ((PackRangeMax - PackRangeMin) / new Vector3(2, 2, 4));
            Vector3 integerPart = new Vector3(
                    (float)Math.Floor(normalized.X),
                    (float)Math.Floor(normalized.Y),
                    (float)Math.Floor(normalized.Z)
            );

            Vector3 fractionalPart = normalized - integerPart;

            // bits 4-7 hold the integer parts
            uint intParts = 0;
            intParts |= ((uint)integerPart.X & 1) << 4;
            intParts |= ((uint)integerPart.Y & 1) << 5;
            intParts |= ((uint)integerPart.Z & 3) << 6;

            return new Vector4(
                fractionalPart.X,
                fractionalPart.Y,
                fractionalPart.Z,
                (intParts | flags) / 255f
            );
        }

        static uint FindGreatestComponent(Quaternion quat)
        {
            int maxComponent = 0;
            float maxVal = Math.Abs(quat[0]);
            for (int k = 1; k < 4; ++k)
            {
                if (Math.Abs(quat[k]) > maxVal)
                {
                    maxVal = Math.Abs(quat[k]);
                    maxComponent = k;
                }
            }
            return (uint)maxComponent;
        }

        public static uint PackQuaternion(Vector3 tangent, Vector3 binormal, Vector3 normal)
        {
            // normalize first
            Vector3 t = Vector3.Normalize(tangent);
            Vector3 b = Vector3.Normalize(binormal);
            Vector3 n = Vector3.Normalize(normal);
            Vector3 computedTangent = Vector3.Normalize(t - Vector3.Dot(t, n) * n);
            Matrix mat = new Matrix
            {
                Row1 = (Vector4)n,
                Row2 = (Vector4)computedTangent,
                Row3 = (Vector4)Vector3.Cross(n, computedTangent)
            };

            Quaternion quat = Quaternion.RotationMatrix(mat); quat.Normalize();

            uint maxComponent = FindGreatestComponent(quat);
            //Debug.Assert(maxComponent > 0, "maxComponent can't be zero (X)");
            if (quat[(int)maxComponent] < 0f)
                quat = -quat;

            Vector4 packedQuat = Vector4.Zero;
            uint reflection = Vector3.Dot(binormal, (Vector3)mat.Row3) < 0f ? 0u : 1u;

            switch (maxComponent)
            {
                case 0:
                    // X is largest, store YZW
                    packedQuat.X = quat.Y;
                    packedQuat.Y = quat.Z;
                    packedQuat.Z = quat.W;
                    break;
                case 1:
                    // Y is largest, store XZW
                    packedQuat.X = quat.X;
                    packedQuat.Y = quat.Z;
                    packedQuat.Z = quat.W;
                    break;
                case 2:
                    // Z is largest, store XYW
                    packedQuat.X = quat.X;
                    packedQuat.Y = quat.Y;
                    packedQuat.Z = quat.W;
                    break;
                case 3:
                    // W is largest, store XYZ
                    packedQuat.X = quat.X;
                    packedQuat.Y = quat.Y;
                    packedQuat.Z = quat.Z;
                    break;

                default:
                    // we shouldn't get here
                    break;
            }

            packedQuat.X *= 1.4142135f;
            packedQuat.Z *= 1.4142135f;
            packedQuat.Y *= 1.4142135f;

            uint ts = 0;
            ts |= (uint)Math.Floor(MathUtil.Clamp(packedQuat.X * 0.5f + 0.5f, 0f, 1f) * 1023f + 0.5f) << 22; // packed as 10 bit int
            ts |= (uint)Math.Floor(MathUtil.Clamp(packedQuat.Y * 0.5f + 0.5f, 0f, 1f) * 511f + 0.5f) << 13;  // packed as 9 bit int
            ts |= (uint)Math.Floor(MathUtil.Clamp(packedQuat.Z * 0.5f + 0.5f, 0f, 1f) * 1023f + 0.5f) << 3;  // packed as 10 bit int
            ts |= ((maxComponent - 1) & 3) << 1;
            ts |= reflection;
            return ts;
        }
    }
}
