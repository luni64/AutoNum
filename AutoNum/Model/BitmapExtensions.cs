using AutoNumber.ViewModels;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace AutoNumber.Model
{
    internal static class BitmapExtensions
    {
        public static AutoNumMetaData_V1? GetMetadata(this Bitmap bitmap)        
        {
            Trace.WriteLine("Check if bitmap has AutoNum metadata in user_comment tag");

            var EXIF_UserCommentID = 0x9286;
            var UserCommentItem = bitmap.PropertyItems.FirstOrDefault(pi => pi.Id == EXIF_UserCommentID);
            if (UserCommentItem != null && UserCommentItem.Value?.Length > 0)
            {
                Trace.WriteLine("- has user_comment tag");

                var json = Encoding.Unicode.GetString(UserCommentItem.Value).TrimEnd('\0');
                bool OK = AutoNumMetaData_V1.FromJson(json, out AutoNumMetaData_V1? metaData);
                Trace.WriteLine($"- Parsing: {(OK ? "OK" : "Error")}");
                return OK ? metaData : null;
            }
            Trace.WriteLine("- no user_comment tag found");
            return null;
        }


        public static Bitmap AddMetadata(this Bitmap bitmap, ImageModel model, LabelManager lm, NameManager nm, TitleManager tm)
        {
            var md = new AutoNumMetaData_V1(model, lm, nm, tm);

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
        /// <summary>
        /// Reads the EXIF orientation tag and applies the corresponding rotation/flip
        /// so the pixel data matches the intended display orientation.
        /// </summary>
        public static void ApplyExifOrientation(this Bitmap bitmap)
        {
            const int ExifOrientationId = 0x0112;

            if (!bitmap.PropertyIdList.Contains(ExifOrientationId))
                return;

            var prop = bitmap.GetPropertyItem(ExifOrientationId);
            if (prop?.Value is null || prop.Value.Length < 2)
                return;

            int orientation = BitConverter.ToUInt16(prop.Value, 0);

            var flip = orientation switch
            {
                2 => RotateFlipType.RotateNoneFlipX,
                3 => RotateFlipType.Rotate180FlipNone,
                4 => RotateFlipType.RotateNoneFlipY,
                5 => RotateFlipType.Rotate90FlipX,
                6 => RotateFlipType.Rotate90FlipNone,
                7 => RotateFlipType.Rotate270FlipX,
                8 => RotateFlipType.Rotate270FlipNone,
                _ => RotateFlipType.RotateNoneFlipNone,
            };

            if (flip is RotateFlipType.RotateNoneFlipNone)
                return;

            bitmap.RotateFlip(flip);

            // Reset orientation tag to Normal so it won't be double-applied later
            prop.Value = BitConverter.GetBytes((ushort)1);
            bitmap.SetPropertyItem(prop);
        }

        static PropertyItem CreatePropertyItem() // PropertyItem has no constructor => work around
        {
            Type type = typeof(PropertyItem);
            PropertyItem item = (PropertyItem)Activator.CreateInstance(type, true)!; // can not be null here
            return item;
        }
    }
}
