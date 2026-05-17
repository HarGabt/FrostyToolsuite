using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AssetBankPlugin.Export
{
    public class SkeletonAssetExport
    {
        public static InternalSkeleton ConvertToInternal(dynamic dbx)
        {
            var boneNames = new List<string>();
            var modelPose = new List<Transform>();
            var localPose = new List<Transform>();

            foreach (var name in dbx.BoneNames)
            {
                boneNames.Add(name);
            }

                for (int i = 0; i < dbx.Hierarchy.Count; i++)
            {
                Vector3 right = new Vector3(
                    dbx.ModelPose[i].right.x,
                    dbx.ModelPose[i].right.y,
                    dbx.ModelPose[i].right.z
                    );
                Vector3 up = new Vector3(
                    dbx.ModelPose[i].up.x,
                    dbx.ModelPose[i].up.y,
                    dbx.ModelPose[i].up.z
                    );
                Vector3 forward = new Vector3(
                    dbx.ModelPose[i].forward.x,
                    dbx.ModelPose[i].forward.y,
                    dbx.ModelPose[i].forward.z
                    );
                Vector3 trans = new Vector3(
                    dbx.ModelPose[i].trans.x,
                    dbx.ModelPose[i].trans.y,
                    dbx.ModelPose[i].trans.z
                    );
                Matrix4x4 rotation = new Matrix4x4(
                    right.X, right.Y, right.Z, 0,
                    up.X, up.Y, up.Z, 0,
                    forward.X, forward.Y, forward.Z, 0,
                    0f, 0f, 0f, 1f);

                var transform = new Transform(trans, Quaternion.CreateFromRotationMatrix(rotation), Vector3.One);

                modelPose.Add(transform);
            }

            for (int i = 0; i < dbx.Hierarchy.Count; i++)
            {
                Vector3 right = new Vector3(
                    dbx.LocalPose[i].right.x,
                    dbx.LocalPose[i].right.y,
                    dbx.LocalPose[i].right.z
                    );
                Vector3 up = new Vector3(
                    dbx.LocalPose[i].up.x,
                    dbx.LocalPose[i].up.y,
                    dbx.LocalPose[i].up.z
                    );
                Vector3 forward = new Vector3(
                    dbx.LocalPose[i].forward.x,
                    dbx.LocalPose[i].forward.y,
                    dbx.LocalPose[i].forward.z
                    );
                Vector3 trans = new Vector3(
                    dbx.LocalPose[i].trans.x,
                    dbx.LocalPose[i].trans.y,
                    dbx.LocalPose[i].trans.z
                    );
                Matrix4x4 rotation = new Matrix4x4(
                    right.X, right.Y, right.Z, 0,
                    up.X, up.Y, up.Z, 0,
                    forward.X, forward.Y, forward.Z, 0,
                    0f, 0f, 0f, 1f);

                var transform = new Transform(trans, Quaternion.CreateFromRotationMatrix(rotation), Vector3.One);

                localPose.Add(transform);
            }


            return new InternalSkeleton(dbx.Name, boneNames, dbx.Hierarchy, modelPose, localPose);
        }
    }
}
