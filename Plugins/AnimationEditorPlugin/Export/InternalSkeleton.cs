using System.Collections.Generic;
using System.Numerics;

namespace AssetBankPlugin.Export
{
    public sealed class InternalSkeleton
    {
        public string Name = "Skeleton";
        public List<string> BoneNames = new List<string>();
        public List<int> BoneParents = new List<int>();
        public List<Transform> BoneTransforms = new List<Transform>();
        public List<Transform> LocalTransforms = new List<Transform>();

        public InternalSkeleton(string name, List<string> boneNames, List<int> boneParents, List<Transform> boneTransforms, List<Transform> localTranforms)
        {
            Name = name;
            BoneNames = boneNames;
            BoneParents = boneParents;
            BoneTransforms = boneTransforms;
            LocalTransforms = localTranforms;
        }

    }
}