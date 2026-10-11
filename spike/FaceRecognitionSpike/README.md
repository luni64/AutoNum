# Face recognition spike

Standalone console project (not in `AutoNum.sln`) that answers whether OpenCV's SFace recognizer
is good enough on genealogy photos. Design context: `docs/implementation/FACE_RECOGNITION_PLAN.md`.

## One-time setup on a new machine

The SFace model is not checked in (38 MB). Download it into `Models/`:

```
curl -L -o Models/face_recognition_sface_2021dec.onnx https://github.com/opencv/opencv_zoo/raw/main/models/face_recognition_sface_2021dec.onnx
```

The YuNet detector model is linked from `AutoNum/Classifiers/` by the csproj, nothing to do.

For the model comparison (`--model arcface` / `--model arcface-small`), also download the
InsightFace packs and copy `w600k_r50.onnx` (buffalo_l) and `w600k_mbf.onnx` (buffalo_sc) into
`Models/` (both git-ignored; **non-commercial research licence**, see the plan document):

```
curl -L -o buffalo_l.zip  https://github.com/deepinsight/insightface/releases/download/v0.7/buffalo_l.zip
curl -L -o buffalo_sc.zip https://github.com/deepinsight/insightface/releases/download/v0.7/buffalo_sc.zip
```

Every command accepts `--model sface|arcface|arcface-small` (default sface). Non-default models
write to `spike/out_<model>/`; the labels are copied there from `spike/out/labeled/` on first use,
so label once (SFace run) and compare any model.

## Workflow

1. Copy photos (JPG/PNG/TIFF, subfolders allowed) into `spike/photos/` — git-ignored.
2. Extract faces:

   ```
   cd spike/FaceRecognitionSpike
   dotnet run -- extract
   ```

   Writes to `spike/out/` (git-ignored):
   - `crops/<photo>__<nn>.png` — one labeling crop per detected face
   - `overview/<photo>.jpg` — the photo with numbered boxes, to see who `nn` is
   - `faces.json` — box, landmarks, score and embedding per face

3. Label by **moving** crops into `spike/out/labeled/<Person Name>/` folders. Use the same
   folder name for the same person across photos. Leave unknown faces in `crops/`. Two or more
   identities with at least two samples each are needed; the more photos per person across
   years and conditions, the more informative the result.

   **Shortcut via AutoNum PDFs**: copy `_num.pdf` files that AutoNum saved (editable PDF with
   payload) into `spike/photos/` and run

   ```
   dotnet run -- labels-from-pdf
   ```

   It reads the names and label positions from the embedded payload, assigns each named label
   to the nearest detected face (within one face height) and fills `labeled/<Name>/` from that.
   `labels_check_<pdf>.jpg` in `spike/out/` shows the assignment for eyeballing. If the payload's
   base image is identical to a photo already in `photos/`, its detections are reused.

   **Unsupervised preview** (no labels needed): `dotnet run -- pairs` lists the most similar
   cross-photo pairs and writes contact sheets `pairs_top_<n>.png` for visual confirmation.

4. Evaluate:

   ```
   dotnet run -- evaluate
   dotnet run -- evaluate --threshold 0.40
   dotnet run -- evaluate --model arcface --threshold 0.30
   ```

   Reports same-person vs different-person similarity distributions, a threshold sweep,
   leave-one-out nearest-neighbour accuracy with margins, the hardest confusions, and a
   breakdown by face width.

5. Suggest names for unseen photos (the app's use case), e.g. everything in `spike/photos/new/`:

   ```
   dotnet run -- suggest --threshold 0.40
   dotnet run -- suggest --model arcface --threshold 0.35
   ```

   Nearest neighbour per identity with a margin rule; samples from the same photo are never
   used as witnesses. Writes `suggest_<n>.png` contact sheets (new face | best stored | runner-up).

Re-running `extract` overwrites `crops/` and `faces.json` but leaves `labeled/` alone.
Detector settings mirror the app (`ScoreThreshold` 0.7, NMS 0.3).
