using AutoNumber.ViewModels;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Text;

namespace AutoNumber.Model
{
    internal static class BitmapExtensions
    {
        /// <summary>
        /// Loads a bitmap from file without holding a GDI+ file lock.
        /// The file is read entirely into a MemoryStream so the file handle is released immediately.
        /// </summary>
        public static Bitmap LoadBitmapFromFile(string filename)
        {
            var bytes = File.ReadAllBytes(filename);
            var ms = new MemoryStream(bytes);
            return new Bitmap(ms); // GDI+ keeps ms alive internally; no file lock held
        }

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


        public static Bitmap AddMetadata(this Bitmap bitmap, ImageVM model, LabelManager lm, NameManager nm, TitleManager tm)
        {
            var md = new AutoNumMetaData_V2(model, lm, nm, tm);

            var jsonString = md.ToJson();


            byte[] jsonBytes = Encoding.Unicode.GetBytes(jsonString + "\0"); // Null-terminate the string

            PropertyItem propItem = CreatePropertyItem();
            propItem.Id = 0x9286; // UserComment
            propItem.Type = 7;
            propItem.Value = jsonBytes;
            propItem.Len = jsonBytes.Length;

            bitmap.SetPropertyItem(propItem);

            // EXIF Software tag — identifies the application that produced the image
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var versionString = version is not null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0";
            byte[] swBytes = Encoding.ASCII.GetBytes($"AutoNum {versionString}\0");

            PropertyItem swItem = CreatePropertyItem();
            swItem.Id = 0x0131; // Software
            swItem.Type = 2;    // ASCII
            swItem.Value = swBytes;
            swItem.Len = swBytes.Length;

            bitmap.SetPropertyItem(swItem);
            return bitmap;
        }

        /// <summary>
        /// Copies property items from a cached array to the destination bitmap.
        /// Skips Orientation (already applied) and UserComment (overwritten by AddMetadata).
        /// </summary>
        public static void CopyMetadataFrom(this Bitmap destination, PropertyItem[]? source)
        {
            if (source is null) return;

            const int ExifOrientationId = 0x0112;
            const int ExifUserCommentId = 0x9286;

            foreach (var prop in source)
            {
                if (prop.Id is ExifOrientationId or ExifUserCommentId)
                    continue;

                try
                {
                    destination.SetPropertyItem(prop);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Skipped metadata tag 0x{prop.Id:X4}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Reconstructs the clean base image from a numbered composite bitmap.
        /// Crops the title/footer regions using the stored dimensions and pastes
        /// cached pixel patches back over the label positions.
        /// </summary>
        public static Bitmap RestoreFromPatches(this Bitmap composite, AutoNumMetaData_V2 md, List<PatchData> patches)
        {
            // Composite layout: [title bar (TitleHeight) | original image | footer]
            // Step 1: Paste patches onto composite at their saved coordinates (composite space)
            using (var g = Graphics.FromImage(composite))
            {
                foreach (var patch in patches)
                {
                    using var pngStream = new MemoryStream(patch.PngBytes);
                    using var patchBmp = new Bitmap(pngStream);
                    g.DrawImage(patchBmp, patch.X, patch.Y, patch.Width, patch.Height);
                }
            }

            // Step 2: Crop to original image region (skip title at top, footer at bottom)
            var cropRect = new Rectangle(0, md.TitleHeight, md.OriginalImageWidth, md.OriginalImageHeight);
            return composite.Clone(cropRect, composite.PixelFormat);
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
