namespace Frosty.Core
{
    public static class DriveHelper
    {
        public static bool IsZDrive(string path)
        {
            if (path == null)
            {
                return false;
            }

            path = path.Trim().Replace("\"", string.Empty);

            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            return path.Trim().ToLower()[0] == 'z';
        }
    }
}
