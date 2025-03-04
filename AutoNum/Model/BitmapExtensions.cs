using System.Diagnostics;
using System.Drawing;
using System.Text;

namespace AutoNumber.Model
{
    internal static class BitmapExtensions
    {
        public static AutoNumMetaData_V1? getMetadata(this Bitmap bitmap)        
        {
            Trace.WriteLine("Check if bitmap has AutoNum metadata in user_comment tag");

            var EXIF_UserCommentID = 0x9286;
            var UserCommentItem = bitmap.PropertyItems.FirstOrDefault(pi => pi.Id == EXIF_UserCommentID);
            if (UserCommentItem != null && UserCommentItem.Value?.Length > 0)
            {
                Trace.WriteLine("- has user_comment tag");

                var json = Encoding.Unicode.GetString(UserCommentItem.Value).TrimEnd('\0');
                bool OK = AutoNumMetaData_V1.fromJson(json, out AutoNumMetaData_V1? metaData);
                Trace.WriteLine($"- Parsing: {(OK ? "OK" : "Error")}");
                return OK ? metaData : null;
            }
            Trace.WriteLine("- no user_comment tag found");
            return null;
        }
    }
}
