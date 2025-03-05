using AutoNumber.ViewModels;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
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


        public static Bitmap AddMetadata(this Bitmap bitmap, ImageModel model)
        {
            var md = new AutoNumMetaData_V1(model);

            var jsonString = md.ToJson();


            byte[] jsonBytes = Encoding.Unicode.GetBytes(jsonString + "\0"); // Null-terminate the string

            PropertyItem propItem = CreatePropertyItem();
            propItem.Id = 0x9286; // Image Description
            propItem.Type = 7;
            propItem.Value = jsonBytes;
            propItem.Len = jsonBytes.Length;

            // Set the metadata property to the bitmap
            bitmap.SetPropertyItem(propItem);
            return bitmap;
        }
        static PropertyItem CreatePropertyItem() // PropertyItem has no constructor => work around
        {
            Type type = typeof(PropertyItem);
            PropertyItem item = (PropertyItem)Activator.CreateInstance(type, true);
            return item;
        }
    }
}
