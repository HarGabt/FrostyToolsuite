using System;
using System.Collections.Generic;

namespace FrostySdk.Managers.Entries
{
    public class EbxAssetEntry : AssetEntry
    {
        public override string Name
        {
            get
            {
                // TODO: @techdebt find better method to move blueprint bundles to sub-folder, this will most likely break writing.
                if (ProfilesLibrary.IsLoaded(ProfileVersion.Battlefield2042, ProfileVersion.Battlefield6) & !base.Name.StartsWith("win32/"))
                {
                    string name = base.Name;
                    if (name.StartsWith("cd_", StringComparison.OrdinalIgnoreCase))
                    {
                        return $"win32/cd/{base.Name}";
                    }
                    else if (name.StartsWith("dpf_", StringComparison.OrdinalIgnoreCase))
                    {
                        return $"win32/dpf/{base.Name}";
                    }
                    else if (name.StartsWith("md_", StringComparison.OrdinalIgnoreCase) || name.StartsWith("assaultrifle_md", StringComparison.OrdinalIgnoreCase))
                    {
                        return $"win32/md/{base.Name}";
                    }
                    else if (name.StartsWith("pf_", StringComparison.OrdinalIgnoreCase))
                    {
                        return $"win32/pf/{base.Name}";
                    }
                    else if (name.Contains("-common/"))
                    {
                        return $"win32/edgemodel/{base.Name}";
                    }
                    else if (name.StartsWith("cha_", StringComparison.OrdinalIgnoreCase))
                    {
                        return $"win32/cha/{base.Name}";
                    }
                    else if (name.StartsWith("dsp_", StringComparison.OrdinalIgnoreCase))
                    {
                        return $"win32/dsp/{base.Name}";
                    }
                    else if (name.StartsWith("com_", StringComparison.OrdinalIgnoreCase))
                    {
                        return $"win32/com/{base.Name}";
                    }
                    else if (name.StartsWith("gad_", StringComparison.OrdinalIgnoreCase) || name.StartsWith("ob_", StringComparison.OrdinalIgnoreCase))
                    {
                        return $"win32/ob/{base.Name}";
                    }
                    else if (name.StartsWith("ov_", StringComparison.OrdinalIgnoreCase))
                    {
                        return $"win32/ov/{base.Name}";
                    }
                    else if (name.StartsWith("dogtag_", StringComparison.OrdinalIgnoreCase))
                    {
                        return $"win32/dogtag/{base.Name}";
                    }
                    else if (name.StartsWith("bf_sp_", StringComparison.OrdinalIgnoreCase) || name.StartsWith("bf_gla_sp_", StringComparison.OrdinalIgnoreCase))
                    {
                        return $"win32/toplevel/{base.Name}";
                    }
                }

                return base.Name;
            }
        }
        
        public Guid Guid;
        public List<Guid> DependentAssets = new List<Guid>();
        public override string AssetType => "ebx";

        public bool ContainsDependency(Guid guid)
        {
            return HasModifiedData ? ModifiedEntry.DependentAssets.Contains(guid) : DependentAssets.Contains(guid);
        }

        public IEnumerable<Guid> EnumerateDependencies()
        {
            if (HasModifiedData)
            {
                foreach (Guid guid in ModifiedEntry.DependentAssets)
                {
                    yield return guid;
                }
            }
            else
            {
                foreach (Guid guid in DependentAssets)
                {
                    yield return guid;
                }
            }
        }
    }
}