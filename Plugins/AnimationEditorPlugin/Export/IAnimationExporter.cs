namespace AssetBankPlugin.Export
{
    public abstract class IAnimationExporter
    {
        /// <summary> 
        /// Exports a <see cref="InternalAnimation"/> with a <see cref="InternalSkeleton"/> and saves it to a given path.
        /// </summary>
        public abstract void Export(InternalAnimation animation, InternalSkeleton skeleton, string path);
    }
}