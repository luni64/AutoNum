// Face recognition spike for AutoNum — see docs/implementation/FACE_RECOGNITION_PLAN.md.
//
//   extract  [photosDir] [outDir]          detect faces, write labeling crops + faces.json
//   pairs    [outDir]                      unsupervised: most similar cross-photo pairs + contact sheets
//   labels-from-pdf [photosDir] [outDir]   label faces from AutoNum _num.pdf payloads (names + label positions)
//   evaluate [outDir] [--threshold 0.363]  compare labeled crops, report similarity statistics
//   suggest  [newPhotosDir] [outDir] [--threshold 0.40]
//                                          the app's use case: detect faces in new photos and suggest names
//                                          from the labeled store (nearest neighbour + margin rule)
//
// Labeling by hand: move files from <out>/crops/ into <out>/labeled/<Person Name>/ folders.

using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using UglyToad.PdfPig;

// Resolved against the output folder, not the CWD (dotnet run starts in the project folder).
string YuNetModel = Path.Combine(AppContext.BaseDirectory, "Models", "face_detection_yunet_2023mar.onnx");
string SFaceModel = Path.Combine(AppContext.BaseDirectory, "Models", "face_recognition_sface_2021dec.onnx");
const double DefaultThreshold = 0.363; // OpenCV's published cosine threshold for SFace

var spikeRoot = FindSpikeRoot();
var command = args.Length > 0 ? args[0].ToLowerInvariant() : "help";
double threshold = DefaultThreshold;
string modelName = "sface"; // --model sface | arcface (InsightFace w600k_r50) | arcface-small (w600k_mbf)
var positional = new List<string>();
for (int i = 1; i < args.Length; i++)
{
    if (args[i] == "--threshold" && i + 1 < args.Length)
        threshold = double.Parse(args[++i], CultureInfo.InvariantCulture);
    else if (args[i] == "--model" && i + 1 < args.Length)
        modelName = args[++i].ToLowerInvariant();
    else if (!args[i].StartsWith("--"))
        positional.Add(args[i]);
}

// Each model gets its own output folder (embeddings differ); labels are copied over from the SFace run.
string DefaultOut() => Path.Combine(spikeRoot, modelName == "sface" ? "out" : $"out_{modelName}");
string PhotosArg() => positional.Count > 0 ? positional[0] : Path.Combine(spikeRoot, "photos");
string OutArg(int index) => positional.Count > index ? positional[index] : DefaultOut();
IEmbedder MakeEmbedder() => modelName switch
{
    "sface" => new SFaceEmbedder(),
    "arcface" => new ArcFaceEmbedder(Path.Combine(AppContext.BaseDirectory, "Models", "w600k_r50.onnx"), "arcface"),
    "arcface-small" => new ArcFaceEmbedder(Path.Combine(AppContext.BaseDirectory, "Models", "w600k_mbf.onnx"), "arcface-small"),
    _ => throw new ArgumentException($"unknown --model {modelName}"),
};
Console.WriteLine($"[model: {modelName}]");

switch (command)
{
    case "extract":
        Extract(PhotosArg(), OutArg(1));
        break;
    case "pairs":
        Pairs(OutArg(0));
        break;
    case "labels-from-pdf":
        LabelsFromPdf(PhotosArg(), OutArg(1));
        break;
    case "evaluate":
        Evaluate(OutArg(0), threshold);
        break;
    case "suggest":
        Suggest(positional.Count > 0 ? positional[0] : Path.Combine(spikeRoot, "photos", "new"), OutArg(1), threshold);
        break;
    default:
        Console.WriteLine("usage: extract [photosDir] [outDir] | pairs [outDir] | labels-from-pdf [photosDir] [outDir] | evaluate [outDir] [--threshold 0.363] | suggest [newPhotosDir] [outDir] [--threshold 0.40]");
        break;
}

// ---------------------------------------------------------------- extract

void Extract(string photosDir, string outDir)
{
    if (!Directory.Exists(photosDir))
    {
        Console.WriteLine($"Photos folder not found: {photosDir}");
        return;
    }

    var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".tif", ".tiff", ".bmp" };
    var photos = Directory.EnumerateFiles(photosDir, "*", SearchOption.AllDirectories)
        .Where(f => extensions.Contains(Path.GetExtension(f)))
        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
        .ToList();
    Console.WriteLine($"{photos.Count} photos in {photosDir}");

    using var pipeline = new Pipeline(YuNetModel, SFaceModel, MakeEmbedder(), outDir);
    var faces = new List<FaceRecord>();
    var sw = Stopwatch.StartNew();

    foreach (var photo in photos)
    {
        using var image = CvInvoke.Imread(photo, ImreadModes.Color);
        if (image.IsEmpty)
        {
            Console.WriteLine($"  skip (unreadable): {photo}");
            continue;
        }
        var found = pipeline.Process(image, Path.GetFileName(photo));
        Console.WriteLine($"  {Path.GetFileNameWithoutExtension(photo)}: {found.Count} faces ({image.Width}x{image.Height})");
        faces.AddRange(found);
    }

    SaveFaces(outDir, faces);
    Directory.CreateDirectory(Path.Combine(outDir, "labeled"));

    Console.WriteLine();
    Console.WriteLine($"{faces.Count} faces in {sw.Elapsed.TotalSeconds:F1}s. Face width distribution (px):");
    PrintSizeBuckets(faces.Select(f => f.W));
    Console.WriteLine();
    Console.WriteLine($"Crops:    {pipeline.CropsDir}");
    Console.WriteLine($"Overview: {pipeline.OverviewDir}  (numbered boxes per photo, to identify who is who)");
    Console.WriteLine($"Now move crops into {Path.Combine(outDir, "labeled")}\\<Person Name>\\ and run 'evaluate'.");
}

// ---------------------------------------------------------------- labels-from-pdf

void LabelsFromPdf(string photosDir, string outDir)
{
    var pdfs = Directory.Exists(photosDir)
        ? Directory.EnumerateFiles(photosDir, "*.pdf", SearchOption.AllDirectories).OrderBy(f => f).ToList()
        : [];
    if (pdfs.Count == 0)
    {
        Console.WriteLine($"No *.pdf in {photosDir}. Copy AutoNum _num.pdf files there.");
        return;
    }

    var faces = LoadFaces(outDir);
    using var pipeline = new Pipeline(YuNetModel, SFaceModel, MakeEmbedder(), outDir);
    var labeledDir = Path.Combine(outDir, "labeled");
    Directory.CreateDirectory(labeledDir);

    foreach (var pdf in pdfs)
    {
        Console.WriteLine($"{Path.GetFileName(pdf)}");
        if (!TryReadAutoNumPayload(pdf, out var metadataJson, out var baseImageBytes))
        {
            Console.WriteLine("  no AutoNum payload found (not saved by AutoNum as editable PDF?)");
            continue;
        }

        using var baseImage = new Mat();
        CvInvoke.Imdecode(baseImageBytes!, ImreadModes.Color, baseImage);
        var persons = ParsePersons(metadataJson!);
        Console.WriteLine($"  base image {baseImage.Width}x{baseImage.Height}, {persons.Count} labels, {persons.Count(p => p.Name.Length > 0)} with names");

        // Reuse the detections of an already extracted photo when the payload's base image is
        // pixel-identical to it (otherwise the same faces would be in faces.json twice).
        string photoKey = Path.GetFileName(pdf);
        var twin = FindIdenticalPhoto(baseImage, photosDir, faces);
        List<FaceRecord> photoFaces;
        if (twin is not null)
        {
            photoKey = twin;
            photoFaces = faces.Where(f => f.Photo == twin).ToList();
            Console.WriteLine($"  base image is identical to photos/{twin} — reusing its {photoFaces.Count} detections");
        }
        else
        {
            faces.RemoveAll(f => f.Photo == photoKey);
            photoFaces = pipeline.Process(baseImage, photoKey);
            faces.AddRange(photoFaces);
            Console.WriteLine($"  detected {photoFaces.Count} faces on the embedded base image");
        }

        // Optimal (minimum total distance) label-to-face assignment. Labels are routinely dragged
        // off the face (above the head in back rows, below the chin in front rows, 1-1.5 face
        // heights away), so plain containment fails, and greedy nearest-first can steal a face
        // from the label that is its only plausible owner. Hungarian on distances in units of
        // face height, pairs beyond the tolerance are forbidden.
        const double maxDistInFaceHeights = 2.5;
        var namedPersons = persons.Where(p => p.Name.Length > 0).ToList();
        var cost = new double[namedPersons.Count, photoFaces.Count];
        for (int pi = 0; pi < namedPersons.Count; pi++)
            for (int fi = 0; fi < photoFaces.Count; fi++)
            {
                var p = namedPersons[pi];
                var f = photoFaces[fi];
                double dx = f.X + f.W / 2 - p.CenterX, dy = f.Y + f.H / 2 - p.CenterY;
                double d = Math.Sqrt(dx * dx + dy * dy) / Math.Max(f.W, f.H);
                cost[pi, fi] = d <= maxDistInFaceHeights ? d : Hungarian.Forbidden;
            }
        var match = Hungarian.Solve(cost);

        var assignment = new Dictionary<string, (PersonLabel Person, double Dist)>();
        var usedLabels = new HashSet<int>();
        for (int pi = 0; pi < namedPersons.Count; pi++)
        {
            int fi = match[pi];
            if (fi < 0 || cost[pi, fi] >= Hungarian.Forbidden) continue;
            assignment[photoFaces[fi].Key] = (namedPersons[pi], cost[pi, fi]);
            usedLabels.Add(namedPersons[pi].Number);
        }
        var unmatched = persons.Where(p => p.Name.Length > 0 && !usedLabels.Contains(p.Number)).ToList();
        Console.WriteLine($"  assignment distances (face heights): {Describe(assignment.Values.Select(a => a.Dist))}");
        if (Environment.GetEnvironmentVariable("SPIKE_VERBOSE") == "1")
        {
            Console.WriteLine($"  metadata version {MetadataVersion(metadataJson!)}");
            foreach (var p in persons.OrderBy(p => p.Number))
            {
                var a = assignment.FirstOrDefault(kv => kv.Value.Person.Number == p.Number);
                var f = a.Key is null ? null : photoFaces.First(x => x.Key == a.Key);
                Console.WriteLine(f is null
                    ? $"    #{p.Number,2} label ({p.CenterX,6:F0},{p.CenterY,6:F0})  -> none   {Shorten(p.Name)}"
                    : $"    #{p.Number,2} label ({p.CenterX,6:F0},{p.CenterY,6:F0})  -> {f.Key} box ({f.X + f.W / 2,6:F0},{f.Y + f.H / 2,6:F0}) {f.W:F0}px d={a.Value.Dist:F2}  {Shorten(p.Name)}");
            }
        }

        int copied = 0;
        foreach (var (key, (person, _)) in assignment)
        {
            var src = Path.Combine(pipeline.CropsDir, key + ".png");
            if (!File.Exists(src)) continue;
            var folder = Path.Combine(labeledDir, FolderName(person.Name));
            Directory.CreateDirectory(folder);
            File.Copy(src, Path.Combine(folder, key + ".png"), overwrite: true);
            copied++;
        }
        Console.WriteLine($"  labeled {copied} faces; {unmatched.Count} named labels without a detected face:");
        foreach (var p in unmatched)
            Console.WriteLine($"    #{p.Number} {p.Name}");

        // Check image: base image with face boxes and the assigned label number, for eyeballing.
        using var check = baseImage.Clone();
        int thickness = Math.Max(1, check.Width / 1500);
        foreach (var f in photoFaces)
        {
            var rect = Rectangle.Round(new RectangleF(f.X, f.Y, f.W, f.H));
            bool assigned = assignment.TryGetValue(f.Key, out var a);
            CvInvoke.Rectangle(check, rect, assigned ? new MCvScalar(0, 200, 0) : new MCvScalar(0, 0, 255), thickness);
            if (assigned)
                CvInvoke.PutText(check, a.Person.Number.ToString(), new Point(rect.X, Math.Max(12, rect.Y - 4)),
                    FontFace.HersheySimplex, Math.Max(0.5, check.Width / 2500.0), new MCvScalar(0, 200, 0), thickness);
        }
        foreach (var p in persons)
            CvInvoke.Circle(check, new Point((int)p.CenterX, (int)p.CenterY), Math.Max(3, check.Width / 400), new MCvScalar(255, 0, 255), -1);
        var checkPath = Path.Combine(outDir, $"labels_check_{Path.GetFileNameWithoutExtension(pdf)}.jpg");
        CvInvoke.Imwrite(checkPath, check);
        Console.WriteLine($"  check image: {checkPath}  (green box = labeled with number, red = no label, magenta dot = label position)");
    }

    SaveFaces(outDir, faces);
}

static bool TryReadAutoNumPayload(string pdfPath, out string? metadataJson, out byte[]? baseImageBytes)
{
    metadataJson = null;
    baseImageBytes = null;
    byte[]? zipBytes = null;
    using (var document = PdfDocument.Open(pdfPath))
    {
        if (!document.Advanced.TryGetEmbeddedFiles(out var embedded)) { Console.WriteLine("  (PdfPig: no embedded files)"); return false; }
        Console.WriteLine($"  embedded files: {string.Join(", ", embedded.Select(e => $"{e.Name} ({e.Bytes.Length} bytes)"))}");
        // PdfPig reports the attachment under its key ("autonum-data"), not its file name — same lookup as the app.
        var file = embedded.FirstOrDefault(e => e.Name.Equals("autonum-payload.zip", StringComparison.OrdinalIgnoreCase)
                                             || e.Name.Equals("autonum-data", StringComparison.OrdinalIgnoreCase))
                ?? embedded.FirstOrDefault(e => e.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
        if (file is null) return false;
        zipBytes = file.Bytes.ToArray();
    }

    using var ms = new MemoryStream(zipBytes);
    using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
    var meta = zip.GetEntry("autonum/metadata.json");
    var img = zip.GetEntry("autonum/base.jpg");
    if (meta is null || img is null) { Console.WriteLine($"  zip entries: {string.Join(", ", zip.Entries.Select(e => e.FullName))}"); return false; }
    using (var r = new StreamReader(meta.Open(), Encoding.UTF8)) metadataJson = r.ReadToEnd();
    using (var s = img.Open()) using (var buf = new MemoryStream()) { s.CopyTo(buf); baseImageBytes = buf.ToArray(); }
    return true;
}

static string MetadataVersion(string metadataJson)
{
    using var doc = JsonDocument.Parse(metadataJson);
    return doc.RootElement.TryGetProperty("Version", out var v) ? v.GetString() ?? "?" : "?";
}

static List<PersonLabel> ParsePersons(string metadataJson)
{
    using var doc = JsonDocument.Parse(metadataJson);
    var root = doc.RootElement;
    var version = root.TryGetProperty("Version", out var v) ? v.GetString() ?? "V1" : "V1";
    // Pre-V5 files store the circle's top-left corner, V5 the true center (see ImageVM.InitFromMetadata).
    double labelSize = root.TryGetProperty("LabelsSize", out var ls) && ls.ValueKind == JsonValueKind.Number ? ls.GetDouble() : double.NaN;
    if (!double.IsFinite(labelSize) && root.TryGetProperty("LabelsFont", out var lf) && lf.TryGetProperty("Size", out var fs))
        labelSize = fs.GetDouble() * 0.95;
    double offset = version == "V5" ? 0 : (double.IsFinite(labelSize) ? labelSize / 2 : 0);

    var result = new List<PersonLabel>();
    if (!root.TryGetProperty("Persons", out var persons)) return result;
    foreach (var p in persons.EnumerateArray())
    {
        var label = p.GetProperty("Label");
        var name = p.TryGetProperty("Name", out var n) && n.TryGetProperty("Text", out var t) ? t.GetString() ?? "" : "";
        result.Add(new PersonLabel(
            label.GetProperty("Number").GetInt32(),
            label.GetProperty("CenterX").GetDouble() + offset,
            label.GetProperty("CenterY").GetDouble() + offset,
            Regex.Replace(name.Trim(), @"\s+", " ")));
    }
    return result;
}

static string? FindIdenticalPhoto(Mat baseImage, string photosDir, List<FaceRecord> faces)
{
    foreach (var photo in faces.Select(f => f.Photo).Distinct())
    {
        var path = Directory.EnumerateFiles(photosDir, photo, SearchOption.AllDirectories).FirstOrDefault();
        if (path is null) continue;
        using var other = CvInvoke.Imread(path, ImreadModes.Color);
        if (other.Width != baseImage.Width || other.Height != baseImage.Height) continue;
        using var diff = new Mat();
        CvInvoke.AbsDiff(baseImage, other, diff);
        var mean = CvInvoke.Mean(diff);
        if ((mean.V0 + mean.V1 + mean.V2) / 3 < 2.0) return photo; // JPEG re-encoding noise at most
    }
    return null;
}

static string FolderName(string name)
{
    var cleaned = Regex.Replace(name, "[<>:\"/\\\\|?*]", " ");
    cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim().TrimEnd('.');
    return cleaned.Length > 80 ? cleaned[..80].Trim() : cleaned;
}

// ---------------------------------------------------------------- pairs (unsupervised, no labels needed)

void Pairs(string outDir)
{
    var faces = LoadFaces(outDir);
    if (faces.Count == 0) { Console.WriteLine("Run 'extract' first."); return; }
    var vecs = faces.Select(f => Normalize(f.Embedding)).ToList();

    // Cross-photo pairs only: one person cannot appear twice in the same photo.
    var pairs = new List<(FaceRecord A, FaceRecord B, double Sim)>();
    for (int i = 0; i < faces.Count; i++)
        for (int j = i + 1; j < faces.Count; j++)
            if (faces[i].Photo != faces[j].Photo)
                pairs.Add((faces[i], faces[j], Cosine(vecs[i], vecs[j])));
    pairs.Sort((x, y) => y.Sim.CompareTo(x.Sim));

    Console.WriteLine($"{faces.Count} faces, {pairs.Count} cross-photo pairs. Similarity distribution:");
    Console.WriteLine($"  {Describe(pairs.Select(p => p.Sim))}");
    var within = new List<double>();
    for (int i = 0; i < faces.Count; i++)
        for (int j = i + 1; j < faces.Count; j++)
            if (faces[i].Photo == faces[j].Photo) within.Add(Cosine(vecs[i], vecs[j]));
    var across = pairs.Select(p => p.Sim).ToList();
    double Share(List<double> v, double t) => v.Count(x => x >= t) / (double)Math.Max(1, v.Count);
    Console.WriteLine("  same-photo bias (pairs within one photo are always different persons):");
    Console.WriteLine($"    within photo : {Describe(within)}  >=0.40: {Share(within, 0.40):P1}  >=0.50: {Share(within, 0.50):P1}");
    Console.WriteLine($"    across photos: {Describe(across)}  >=0.40: {Share(across, 0.40):P1}  >=0.50: {Share(across, 0.50):P1}");
    Console.WriteLine();
    Console.WriteLine("Top cross-photo matches (the model's candidates for 'same person'):");
    const int top = 30;
    foreach (var (a, b, sim) in pairs.Take(top))
        Console.WriteLine($"  {sim:F3}  {a.Key} ({a.W:F0}px)  vs  {b.Key} ({b.W:F0}px)");

    // Contact sheets, 8 pairs each: crop A | crop B | score, for quick visual confirmation.
    var cropsDir = Path.Combine(outDir, "crops");
    const int tile = 200, textW = 420, perSheet = 8;
    var shown = pairs.Take(top).ToList();
    for (int sheetNo = 0; sheetNo * perSheet < shown.Count; sheetNo++)
    {
        var chunk = shown.Skip(sheetNo * perSheet).Take(perSheet).ToList();
        using var sheet = new Mat(tile * chunk.Count, tile * 2 + textW, DepthType.Cv8U, 3);
        sheet.SetTo(new MCvScalar(255, 255, 255));
        for (int row = 0; row < chunk.Count; row++)
        {
            var (a, b, sim) = chunk[row];
            int rank = sheetNo * perSheet + row + 1;
            PlaceCrop(sheet, Path.Combine(cropsDir, a.Key + ".png"), 0, row * tile, tile);
            PlaceCrop(sheet, Path.Combine(cropsDir, b.Key + ".png"), tile, row * tile, tile);
            CvInvoke.PutText(sheet, $"#{rank}  {sim:F3}", new Point(tile * 2 + 10, row * tile + 40), FontFace.HersheySimplex, 1.0, new MCvScalar(0, 0, 200), 2);
            CvInvoke.PutText(sheet, Shorten(a.Key), new Point(tile * 2 + 10, row * tile + 90), FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 0), 1);
            CvInvoke.PutText(sheet, Shorten(b.Key), new Point(tile * 2 + 10, row * tile + 120), FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 0), 1);
            CvInvoke.PutText(sheet, $"{a.W:F0}px / {b.W:F0}px", new Point(tile * 2 + 10, row * tile + 150), FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 0), 1);
            CvInvoke.Line(sheet, new Point(0, (row + 1) * tile - 1), new Point(sheet.Width, (row + 1) * tile - 1), new MCvScalar(200, 200, 200), 1);
        }
        CvInvoke.Imwrite(Path.Combine(outDir, $"pairs_top_{sheetNo + 1}.png"), sheet);
    }
    Console.WriteLine();
    Console.WriteLine($"Contact sheets: {Path.Combine(outDir, "pairs_top_<n>.png")} ({perSheet} pairs each)");
}

static void PlaceCrop(Mat sheet, string path, int x, int y, int tile)
{
    if (!File.Exists(path)) return;
    using var crop = CvInvoke.Imread(path, ImreadModes.Color);
    double f = Math.Min((double)tile / crop.Width, (double)tile / crop.Height);
    using var resized = new Mat();
    CvInvoke.Resize(crop, resized, Size.Empty, f, f, Inter.Area);
    using var roi = new Mat(sheet, new Rectangle(x, y, resized.Width, resized.Height));
    resized.CopyTo(roi);
}

static string Shorten(string key) => key.Length > 38 ? key[..18] + ".." + key[^18..] : key;

// ---------------------------------------------------------------- evaluate

void Evaluate(string outDir, double threshold)
{
    var all = LoadFaces(outDir).ToDictionary(f => f.Key, StringComparer.OrdinalIgnoreCase);
    if (all.Count == 0) { Console.WriteLine("Run 'extract' first."); return; }

    EnsureLabels(outDir);
    var labeledDir = Path.Combine(outDir, "labeled");
    var samples = new List<Sample>();
    foreach (var personDir in Directory.EnumerateDirectories(labeledDir))
    {
        var name = Path.GetFileName(personDir);
        foreach (var file in Directory.EnumerateFiles(personDir, "*.png"))
        {
            var key = Path.GetFileNameWithoutExtension(file);
            if (!all.TryGetValue(key, out var rec))
            {
                Console.WriteLine($"  warning: {key} not in faces.json (re-run extract?)");
                continue;
            }
            samples.Add(new Sample(name, rec, Normalize(rec.Embedding)));
        }
    }

    var identities = samples.GroupBy(s => s.Name).OrderByDescending(g => g.Count()).ToList();
    Console.WriteLine($"{samples.Count} labeled faces, {identities.Count} identities, {all.Count - samples.Count} unlabeled.");
    foreach (var g in identities)
        Console.WriteLine($"  {Shorten(g.Key),-40} {g.Count(),3} samples, face width {g.Min(s => s.Face.W),5:F0}..{g.Max(s => s.Face.W),5:F0} px");
    if (identities.Count < 2 || samples.Count < 3)
    {
        Console.WriteLine("Need at least two identities and three samples.");
        return;
    }

    // ---- pairwise similarities
    var same = new List<Pair>();
    var diff = new List<Pair>();
    for (int i = 0; i < samples.Count; i++)
        for (int j = i + 1; j < samples.Count; j++)
        {
            var p = new Pair(samples[i], samples[j], Cosine(samples[i].Vec, samples[j].Vec));
            (p.A.Name == p.B.Name ? same : diff).Add(p);
        }

    Console.WriteLine();
    Console.WriteLine("Cosine similarity, same-person pairs vs different-person pairs");
    Console.WriteLine($"  same  : {Describe(same.Select(p => p.Sim))}");
    Console.WriteLine($"  diff  : {Describe(diff.Select(p => p.Sim))}");
    Console.WriteLine();
    PrintHistogram(same.Select(p => p.Sim), diff.Select(p => p.Sim));

    // ---- threshold sweep
    Console.WriteLine();
    Console.WriteLine("Threshold sweep  (TPR = same pairs accepted, FPR = different pairs accepted)");
    double bestT = 0, bestScore = -1;
    for (double t = 0.15; t <= 0.70001; t += 0.01)
    {
        double tpr = same.Count(p => p.Sim >= t) / (double)Math.Max(1, same.Count);
        double fpr = diff.Count(p => p.Sim >= t) / (double)Math.Max(1, diff.Count);
        double balanced = (tpr + (1 - fpr)) / 2;
        if (balanced > bestScore) { bestScore = balanced; bestT = t; }
        if (Math.Abs(t * 100 % 5) < 0.001 || Math.Abs(t - DefaultThreshold) < 0.005)
            Console.WriteLine($"  t={t:F2}  TPR={tpr,6:P1}  FPR={fpr,6:P1}");
    }
    Console.WriteLine($"  best balanced accuracy at t={bestT:F2} ({bestScore:P1}); OpenCV default is {DefaultThreshold}");

    // ---- leave-one-out nearest neighbour (the app's matching strategy)
    Console.WriteLine();
    Console.WriteLine($"Leave-one-out nearest neighbour at threshold {threshold:F3} (best score per identity, margin to runner-up)");
    int correct = 0, wrong = 0, noMatch = 0, unmatchable = 0;
    var wrongList = new List<string>();
    var margins = new List<double>();
    var byWidth = new Dictionary<string, (int n, int ok)>();
    foreach (var s in samples)
    {
        var others = samples.Where(o => !ReferenceEquals(o, s)).ToList();
        if (!others.Any(o => o.Name == s.Name)) { unmatchable++; continue; }

        var perIdentity = others.GroupBy(o => o.Name)
            .Select(g => (Name: g.Key, Score: g.Max(o => Cosine(s.Vec, o.Vec))))
            .OrderByDescending(x => x.Score).ToList();
        var best = perIdentity[0];
        double margin = perIdentity.Count > 1 ? best.Score - perIdentity[1].Score : 1;
        var bucket = WidthBucket(s.Face.W);
        var stats = byWidth.GetValueOrDefault(bucket);

        if (best.Score < threshold) { noMatch++; byWidth[bucket] = (stats.n + 1, stats.ok); continue; }
        if (best.Name == s.Name)
        {
            correct++; margins.Add(margin);
            byWidth[bucket] = (stats.n + 1, stats.ok + 1);
        }
        else
        {
            wrong++;
            byWidth[bucket] = (stats.n + 1, stats.ok);
            wrongList.Add($"    {s.Face.Key} ({Shorten(s.Name)}, {s.Face.W:F0}px) -> {Shorten(best.Name)} {best.Score:F3}, own identity {perIdentity.First(x => x.Name == s.Name).Score:F3}");
        }
    }
    int evaluated = correct + wrong + noMatch;
    Console.WriteLine($"  correct {correct}/{evaluated}  wrong {wrong}  no match {noMatch}  (single-sample identities skipped: {unmatchable})");
    if (margins.Count > 0)
        Console.WriteLine($"  margin of correct suggestions over runner-up: {Describe(margins)}");
    if (wrongList.Count > 0)
    {
        Console.WriteLine("  wrong suggestions:");
        wrongList.ForEach(Console.WriteLine);
    }
    Console.WriteLine("  by face width:");
    foreach (var kv in byWidth.OrderBy(k => BucketOrder(k.Key)))
        Console.WriteLine($"    {kv.Key,-10} {kv.Value.ok,3}/{kv.Value.n,-3} correct");

    // ---- hardest confusions
    Console.WriteLine();
    Console.WriteLine("Most similar different-person pairs (siblings?)");
    foreach (var p in diff.OrderByDescending(p => p.Sim).Take(10))
        Console.WriteLine($"  {p.Sim:F3}  {p.A.Face.Key} ({Shorten(p.A.Name)}, {p.A.Face.W:F0}px)  vs  {p.B.Face.Key} ({Shorten(p.B.Name)}, {p.B.Face.W:F0}px)");

    Console.WriteLine();
    Console.WriteLine("Least similar same-person pairs (aging, B/W, pose?)");
    foreach (var p in same.OrderBy(p => p.Sim).Take(10))
        Console.WriteLine($"  {p.Sim:F3}  {p.A.Face.Key} ({p.A.Face.W:F0}px)  vs  {p.B.Face.Key} ({p.B.Face.W:F0}px)  [{Shorten(p.A.Name)}]");

    // ---- same-person similarity by face size
    Console.WriteLine();
    Console.WriteLine("Same-person similarity by the smaller face width of the pair");
    foreach (var g in same.GroupBy(p => WidthBucket(Math.Min(p.A.Face.W, p.B.Face.W))).OrderBy(g => BucketOrder(g.Key)))
        Console.WriteLine($"  {g.Key,-10} n={g.Count(),-4} {Describe(g.Select(p => p.Sim))}");
}

// ---------------------------------------------------------------- suggest (the app's use case)

void Suggest(string newPhotosDir, string outDir, double threshold)
{
    const double minMargin = 0.05;
    if (!Directory.Exists(newPhotosDir)) { Console.WriteLine($"Folder not found: {newPhotosDir}"); return; }

    // The store: every labeled crop with its embedding, grouped by identity.
    EnsureLabels(outDir);
    var faces = LoadFaces(outDir);
    var byKey = faces.ToDictionary(f => f.Key, StringComparer.OrdinalIgnoreCase);
    var store = new List<Sample>();
    foreach (var personDir in Directory.EnumerateDirectories(Path.Combine(outDir, "labeled")))
        foreach (var file in Directory.EnumerateFiles(personDir, "*.png"))
            if (byKey.TryGetValue(Path.GetFileNameWithoutExtension(file), out var rec))
                store.Add(new Sample(Path.GetFileName(personDir), rec, Normalize(rec.Embedding)));
    var identities = store.GroupBy(s => s.Name).ToList();
    Console.WriteLine($"Store: {store.Count} samples, {identities.Count} identities. Threshold {threshold:F2}, margin {minMargin:F2}.");

    // Detect + embed the new photos (also merged into faces.json / crops so they can be labeled later).
    var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".tif", ".tiff", ".bmp" };
    var photos = Directory.EnumerateFiles(newPhotosDir, "*", SearchOption.AllDirectories)
        .Where(f => extensions.Contains(Path.GetExtension(f))).OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
    using var pipeline = new Pipeline(YuNetModel, SFaceModel, MakeEmbedder(), outDir);
    var results = new List<(FaceRecord Face, List<(string Name, double Score, FaceRecord Best)> Ranked, string Verdict)>();
    foreach (var photo in photos)
    {
        using var image = CvInvoke.Imread(photo, ImreadModes.Color);
        if (image.IsEmpty) continue;
        var photoName = Path.GetFileName(photo);
        faces.RemoveAll(f => f.Photo == photoName);
        var found = pipeline.Process(image, photoName);
        faces.AddRange(found);
        Console.WriteLine();
        Console.WriteLine($"{photoName}: {found.Count} faces ({image.Width}x{image.Height})");

        foreach (var f in found)
        {
            var vec = Normalize(f.Embedding);
            // Design rule: samples from the same source photo are never witnesses (same-photo bias, self-match).
            var ranked = identities
                .Select(g => g.Where(s => s.Face.Photo != f.Photo).ToList())
                .Where(g => g.Count > 0)
                .Select(g => { var best = g.OrderByDescending(s => Cosine(vec, s.Vec)).First(); return (Name: best.Name, Score: Cosine(vec, best.Vec), Best: best.Face); })
                .OrderByDescending(x => x.Score).ToList();
            var top = ranked[0];
            double margin = ranked.Count > 1 ? top.Score - ranked[1].Score : 1;
            string verdict = top.Score < threshold ? "none" : margin < minMargin ? "WEAK (2 candidates)" : "SUGGEST";
            results.Add((f, ranked, verdict));
            Console.WriteLine($"  {f.Key,-52} {f.W,4:F0}px  {verdict,-20} {top.Score:F3} {Shorten(top.Name)}  | 2nd {ranked[1].Score:F3} {Shorten(ranked[1].Name)}");
        }
    }
    SaveFaces(outDir, faces);

    // Contact sheets: new face | best stored sample | runner-up stored sample | text.
    var cropsDir = pipeline.CropsDir;
    const int tile = 200, textW = 520, perSheet = 8;
    for (int sheetNo = 0; sheetNo * perSheet < results.Count; sheetNo++)
    {
        var chunk = results.Skip(sheetNo * perSheet).Take(perSheet).ToList();
        using var sheet = new Mat(tile * chunk.Count, tile * 3 + textW, DepthType.Cv8U, 3);
        sheet.SetTo(new MCvScalar(255, 255, 255));
        for (int row = 0; row < chunk.Count; row++)
        {
            var (f, ranked, verdict) = chunk[row];
            PlaceCrop(sheet, Path.Combine(cropsDir, f.Key + ".png"), 0, row * tile, tile);
            PlaceCrop(sheet, Path.Combine(cropsDir, ranked[0].Best.Key + ".png"), tile, row * tile, tile);
            PlaceCrop(sheet, Path.Combine(cropsDir, ranked[1].Best.Key + ".png"), tile * 2, row * tile, tile);
            var color = verdict.StartsWith("SUGGEST") ? new MCvScalar(0, 160, 0) : verdict.StartsWith("WEAK") ? new MCvScalar(0, 140, 220) : new MCvScalar(0, 0, 200);
            int x = tile * 3 + 10, y = row * tile;
            CvInvoke.PutText(sheet, $"#{sheetNo * perSheet + row + 1} {verdict}", new Point(x, y + 35), FontFace.HersheySimplex, 0.8, color, 2);
            CvInvoke.PutText(sheet, $"{Shorten(f.Key)} {f.W:F0}px", new Point(x, y + 70), FontFace.HersheySimplex, 0.45, new MCvScalar(0, 0, 0), 1);
            CvInvoke.PutText(sheet, $"1st {ranked[0].Score:F3} {Shorten(ranked[0].Name)}", new Point(x, y + 110), FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 0), 1);
            CvInvoke.PutText(sheet, $"    via {Shorten(ranked[0].Best.Key)}", new Point(x, y + 135), FontFace.HersheySimplex, 0.4, new MCvScalar(90, 90, 90), 1);
            CvInvoke.PutText(sheet, $"2nd {ranked[1].Score:F3} {Shorten(ranked[1].Name)}", new Point(x, y + 165), FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 0), 1);
            CvInvoke.PutText(sheet, $"    via {Shorten(ranked[1].Best.Key)}", new Point(x, y + 190), FontFace.HersheySimplex, 0.4, new MCvScalar(90, 90, 90), 1);
            CvInvoke.Line(sheet, new Point(0, (row + 1) * tile - 1), new Point(sheet.Width, (row + 1) * tile - 1), new MCvScalar(200, 200, 200), 1);
        }
        CvInvoke.Imwrite(Path.Combine(outDir, $"suggest_{sheetNo + 1}.png"), sheet);
    }
    Console.WriteLine();
    Console.WriteLine($"Contact sheets: {Path.Combine(outDir, "suggest_<n>.png")}  (new face | best stored | runner-up stored)");
}

// ---------------------------------------------------------------- helpers

/// <summary>Labels live in spike/out/labeled (SFace run); other model folders get a copy on first use.</summary>
void EnsureLabels(string outDir)
{
    var target = Path.Combine(outDir, "labeled");
    var source = Path.Combine(spikeRoot, "out", "labeled");
    if (!Directory.Exists(source) || (Directory.Exists(target) && Directory.EnumerateDirectories(target).Any())) return;
    foreach (var dir in Directory.EnumerateDirectories(source))
    {
        var dest = Path.Combine(target, Path.GetFileName(dir));
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.EnumerateFiles(dir)) File.Copy(file, Path.Combine(dest, Path.GetFileName(file)));
    }
    Console.WriteLine($"  labels copied from {source}");
}

static List<FaceRecord> LoadFaces(string outDir)
{
    var path = Path.Combine(outDir, "faces.json");
    return File.Exists(path) ? JsonSerializer.Deserialize<List<FaceRecord>>(File.ReadAllText(path)) ?? [] : [];
}

static void SaveFaces(string outDir, List<FaceRecord> faces)
{
    Directory.CreateDirectory(outDir);
    File.WriteAllText(Path.Combine(outDir, "faces.json"), JsonSerializer.Serialize(faces));
}

static float[] Normalize(float[] v)
{
    double n = Math.Sqrt(v.Sum(x => (double)x * x));
    return n == 0 ? v : v.Select(x => (float)(x / n)).ToArray();
}

static double Cosine(float[] a, float[] b)
{
    double dot = 0;
    for (int i = 0; i < a.Length; i++) dot += a[i] * b[i];
    return dot; // inputs are unit vectors
}

static string Describe(IEnumerable<double> values)
{
    var v = values.OrderBy(x => x).ToList();
    if (v.Count == 0) return "n=0";
    double Q(double q) => v[(int)Math.Clamp(Math.Round(q * (v.Count - 1)), 0, v.Count - 1)];
    return $"n={v.Count,-5} min={v[0]:F3} p10={Q(0.1):F3} median={Q(0.5):F3} mean={v.Average():F3} p90={Q(0.9):F3} max={v[^1]:F3}";
}

static void PrintHistogram(IEnumerable<double> same, IEnumerable<double> diff)
{
    var s = same.ToList();
    var d = diff.ToList();
    int sMax = Math.Max(1, s.Count), dMax = Math.Max(1, d.Count);
    Console.WriteLine("  bin          same (% of same pairs)                 diff (% of diff pairs)");
    for (double lo = -0.3; lo < 1.0; lo += 0.05)
    {
        double hi = lo + 0.05;
        int ns = s.Count(x => x >= lo && x < hi);
        int nd = d.Count(x => x >= lo && x < hi);
        if (ns == 0 && nd == 0) continue;
        string bs = new('#', (int)Math.Round(40.0 * ns / sMax));
        string bd = new('#', (int)Math.Round(40.0 * nd / dMax));
        Console.WriteLine($"  {lo,5:F2}..{hi,5:F2} {bs,-40} {bd,-40}");
    }
}

static void PrintSizeBuckets(IEnumerable<float> widths)
{
    foreach (var g in widths.GroupBy(WidthBucket).OrderBy(g => BucketOrder(g.Key)))
        Console.WriteLine($"  {g.Key,-10} {g.Count()}");
}

static string WidthBucket(float w) => w switch
{
    < 30 => "<30",
    < 45 => "30-45",
    < 65 => "45-65",
    < 100 => "65-100",
    < 160 => "100-160",
    _ => ">=160",
};

static int BucketOrder(string b) => b switch
{
    "<30" => 0, "30-45" => 1, "45-65" => 2, "65-100" => 3, "100-160" => 4, _ => 5,
};

static string FindSpikeRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null && dir.Name != "FaceRecognitionSpike") dir = dir.Parent;
    return dir?.Parent?.FullName ?? Directory.GetCurrentDirectory();
}

/// <summary>Detect + align + embed for one image; writes labeling crops and an overview image.</summary>
sealed class Pipeline : IDisposable
{
    private const float ScoreThreshold = 0.7f;   // same as the app's FaceDetector
    private const float NmsThreshold = 0.3f;
    private const int TopK = 5000;

    private readonly FaceRecognizerSF _recognizer; // always needed: AlignCrop yields the standard 112x112 ArcFace-template crop
    private readonly IEmbedder _embedder;
    private readonly string _yunetModel;
    private FaceDetectorYN? _detector;
    private Size _detectorSize;

    public string CropsDir { get; }
    public string OverviewDir { get; }

    public Pipeline(string yunetModel, string sfaceModel, IEmbedder embedder, string outDir)
    {
        _yunetModel = yunetModel;
        _embedder = embedder;
        _recognizer = new FaceRecognizerSF(sfaceModel, string.Empty, Emgu.CV.Dnn.Backend.Default, Emgu.CV.Dnn.Target.Cpu);
        CropsDir = Path.Combine(outDir, "crops");
        OverviewDir = Path.Combine(outDir, "overview");
        Directory.CreateDirectory(CropsDir);
        Directory.CreateDirectory(OverviewDir);
    }

    public List<FaceRecord> Process(Mat image, string photoName)
    {
        var size = new Size(image.Width, image.Height);
        if (_detector is null || _detectorSize != size)
        {
            _detector?.Dispose();
            _detector = new FaceDetectorYN(_yunetModel, string.Empty, size, ScoreThreshold, NmsThreshold, TopK);
            _detectorSize = size;
        }

        using var detections = new Mat();
        _detector.Detect(image, detections);
        var faces = new List<FaceRecord>();
        int count = detections.Rows;
        if (count == 0) return faces;

        var stem = Path.GetFileNameWithoutExtension(photoName);
        var rows = (float[,])detections.GetData();
        using var overview = image.Clone();

        for (int i = 0; i < count; i++)
        {
            using var faceRow = new Mat(detections, new Rectangle(0, i, detections.Cols, 1));
            using var aligned = new Mat();
            _recognizer.AlignCrop(image, faceRow, aligned);
            var embedding = _embedder.Embed(aligned, _recognizer);

            var box = new RectangleF(rows[i, 0], rows[i, 1], rows[i, 2], rows[i, 3]);
            var landmarks = Enumerable.Range(4, 10).Select(c => rows[i, c]).ToArray();
            var key = $"{stem}__{i + 1:00}";
            faces.Add(new FaceRecord(key, photoName, i + 1, box.X, box.Y, box.Width, box.Height, landmarks, rows[i, 14], embedding));

            SaveLabelingCrop(image, box, Path.Combine(CropsDir, key + ".png"));

            var rect = Rectangle.Round(box);
            int thickness = Math.Max(1, image.Width / 1500);
            CvInvoke.Rectangle(overview, rect, new MCvScalar(0, 255, 0), thickness);
            CvInvoke.PutText(overview, (i + 1).ToString(), new Point(rect.X, Math.Max(12, rect.Y - 4)),
                FontFace.HersheySimplex, Math.Max(0.5, image.Width / 2500.0), new MCvScalar(0, 0, 255), thickness);
        }

        if (overview.Width > 2000)
        {
            double f = 2000.0 / overview.Width;
            CvInvoke.Resize(overview, overview, Size.Empty, f, f, Inter.Area);
        }
        CvInvoke.Imwrite(Path.Combine(OverviewDir, stem + ".jpg"), overview);
        return faces;
    }

    private static void SaveLabelingCrop(Mat image, RectangleF box, string path)
    {
        // Context crop (box inflated by 40%) so a human can recognise the face; the aligned
        // 112x112 crop the recognizer sees would be too tight and blurry for labeling.
        float pad = 0.4f;
        int x0 = (int)Math.Max(0, box.X - box.Width * pad);
        int y0 = (int)Math.Max(0, box.Y - box.Height * pad);
        int x1 = (int)Math.Min(image.Width, box.Right + box.Width * pad);
        int y1 = (int)Math.Min(image.Height, box.Bottom + box.Height * pad);
        if (x1 <= x0 || y1 <= y0) return;

        using var crop = new Mat(image, new Rectangle(x0, y0, x1 - x0, y1 - y0));
        using var resized = new Mat();
        double f = 200.0 / crop.Height;
        CvInvoke.Resize(crop, resized, Size.Empty, f, f, f > 1 ? Inter.Cubic : Inter.Area);
        CvInvoke.Imwrite(path, resized);
    }

    public void Dispose()
    {
        _detector?.Dispose();
        _recognizer.Dispose();
        _embedder.Dispose();
    }
}

interface IEmbedder : IDisposable
{
    string Name { get; }
    /// <param name="aligned">112x112 BGR crop from FaceRecognizerSF.AlignCrop</param>
    float[] Embed(Mat aligned, FaceRecognizerSF recognizer);
}

sealed class SFaceEmbedder : IEmbedder
{
    public string Name => "sface";
    public float[] Embed(Mat aligned, FaceRecognizerSF recognizer)
    {
        using var feature = new Mat();
        recognizer.Feature(aligned, feature);
        return ((float[,])feature.GetData()).Cast<float>().ToArray();
    }
    public void Dispose() { }
}

/// <summary>InsightFace ArcFace ONNX (w600k_r50 / w600k_mbf) through OpenCV's DNN module: RGB, (x-127.5)/127.5, 512-d output.</summary>
sealed class ArcFaceEmbedder : IEmbedder
{
    private readonly Emgu.CV.Dnn.Net _net;
    public string Name { get; }
    public ArcFaceEmbedder(string modelPath, string name)
    {
        Name = name;
        if (!File.Exists(modelPath)) throw new FileNotFoundException($"ArcFace model missing: {modelPath} (see README)");
        _net = Emgu.CV.Dnn.DnnInvoke.ReadNetFromONNX(modelPath);
    }
    public float[] Embed(Mat aligned, FaceRecognizerSF recognizer)
    {
        using var blob = Emgu.CV.Dnn.DnnInvoke.BlobFromImage(aligned, 1.0 / 127.5, new Size(112, 112), new MCvScalar(127.5, 127.5, 127.5), swapRB: true, crop: false);
        _net.SetInput(blob);
        using var output = _net.Forward();
        return ((float[,])output.GetData()).Cast<float>().ToArray();
    }
    public void Dispose() => _net.Dispose();
}

/// <summary>Kuhn-Munkres minimum-cost assignment for a rectangular cost matrix (rows ≤ cols padded internally).</summary>
static class Hungarian
{
    public const double Forbidden = 1e6;

    /// <returns>For each row the assigned column, or -1.</returns>
    public static int[] Solve(double[,] cost)
    {
        int rows = cost.GetLength(0), cols = cost.GetLength(1);
        int n = Math.Max(rows, cols);
        // Pad to square with zero-cost dummies.
        var a = new double[n + 1, n + 1];
        for (int i = 1; i <= n; i++)
            for (int j = 1; j <= n; j++)
                a[i, j] = i <= rows && j <= cols ? cost[i - 1, j - 1] : 0;

        var u = new double[n + 1];
        var v = new double[n + 1];
        var p = new int[n + 1];
        var way = new int[n + 1];
        for (int i = 1; i <= n; i++)
        {
            p[0] = i;
            int j0 = 0;
            var minv = Enumerable.Repeat(double.PositiveInfinity, n + 1).ToArray();
            var used = new bool[n + 1];
            do
            {
                used[j0] = true;
                int i0 = p[j0], j1 = 0;
                double delta = double.PositiveInfinity;
                for (int j = 1; j <= n; j++)
                {
                    if (used[j]) continue;
                    double cur = a[i0, j] - u[i0] - v[j];
                    if (cur < minv[j]) { minv[j] = cur; way[j] = j0; }
                    if (minv[j] < delta) { delta = minv[j]; j1 = j; }
                }
                for (int j = 0; j <= n; j++)
                {
                    if (used[j]) { u[p[j]] += delta; v[j] -= delta; }
                    else minv[j] -= delta;
                }
                j0 = j1;
            } while (p[j0] != 0);
            do
            {
                int j1 = way[j0];
                p[j0] = p[j1];
                j0 = j1;
            } while (j0 != 0);
        }

        var result = Enumerable.Repeat(-1, rows).ToArray();
        for (int j = 1; j <= n; j++)
            if (p[j] >= 1 && p[j] <= rows && j <= cols)
                result[p[j] - 1] = j - 1;
        return result;
    }
}

record FaceRecord(string Key, string Photo, int Index, float X, float Y, float W, float H,
    float[] Landmarks, float Score, float[] Embedding);

record Sample(string Name, FaceRecord Face, float[] Vec);

record Pair(Sample A, Sample B, double Sim);

record PersonLabel(int Number, double CenterX, double CenterY, string Name);
