# Face Recognition — Name Suggestions from a Local Face Store

Design notes from the 2026-10-11 discussion. Status: **design + spike**, nothing implemented in the app yet.
The spike lives in `spike/FaceRecognitionSpike/` (see the end of this document).

## Goal
When a photo is opened and faces are detected, compare each face against faces the user has
already named in earlier photos and **prefill the name as a suggestion** when a match is
reasonably confident. The user confirms or overrides; confirmed names feed back into the store,
so the app learns as it is used.

## Non-Goals
- No automatic assignment. Recognition **suggests, never decides**. A wrong name saved unnoticed
  is worse than no prefill.
- No cloud services. Everything (model, embeddings, store) stays on the user's machine.
- No change to the saved JPG/PDF formats beyond one additive, optional metadata field (see
  Persistence). Embeddings are never written into images.
- No retroactive relinking UI in the first version.

## Background — how recognition works
Recognition is a three-step pipeline; the model does not "know" anyone:

1. **Detect + align.** The detector returns a face box plus five landmarks (eyes, nose, mouth
   corners). The recognizer uses the landmarks to warp the face into a standardized 112×112 crop.
   YuNet, which AutoNum already uses, returns these landmarks — the current `FaceDetector`
   discards them (columns 4..13 of the detection row).
2. **Embed.** The recognizer maps the crop to a 128-float vector. Same person → vectors point in
   nearly the same direction. The vector is abstract; the face cannot be reconstructed from it.
3. **Match.** Cosine similarity of the new vector against every stored vector. Brute force over
   tens of thousands of vectors takes milliseconds; no indexing needed at AutoNum's scale.

**Model choice.** OpenCV's **SFace** (`face_recognition_sface_2021dec.onnx`, from opencv_zoo) is
exposed by Emgu.CV 4.10 as `FaceRecognizerSF` (`AlignCrop`, `Feature`, `Match`). It is the
companion model to YuNet, so **no new dependency**, just a second ONNX file in `Classifiers/`.
OpenCV's published thresholds for SFace: cosine ≥ 0.363 → same person (L2 ≤ 1.128). These are
starting points to be validated by the spike on genealogy material.

Fallback if SFace disappoints: ArcFace/InsightFace-class models via ONNX Runtime (new dependency,
larger model, longer vector). The store design is unaffected — only the vector length changes,
so the store records the model id per embedding.

**What the library does *not* do:** there is no concept of a person, gallery, training or
multi-sample identity. Combining several samples per person is application logic (see Matching).

## Realistic expectations (genealogy material)
- **Tiny faces.** In a 40-person group a face may be 30–50 px wide; upscaling to 112 px gives a
  poor embedding. Good on portraits/small groups, weaker on large groups.
- **B/W, scanned, damaged.** Works, but scores drop and false matches rise.
- **Aging / children.** Children change too much across years. Adults across one or two decades
  usually work — *if* the store has samples from a similar era.
- **Relatives look alike.** Family photos are full of siblings. Expect confusion; the margin rule
  below exists for this.

## Architecture

### 1. The label ↔ face link (prerequisite)
Today a label is just a point the user owns; after detection there is no connection between a
label (and thus its name) and the face pixels. Recognition needs a stable answer to "which pixels
does this name belong to" that survives dragging, reopening and renaming.

**DetectedFace** — new session object (base-image coordinates, the same space labels use since V5):
- bounding box, five landmarks, detector score
- embedding (computed lazily, never persisted in the image)

**ImageVM** holds the full `DetectedFaces` list for the current image, including faces that
currently have no label.

**Person** gets a nullable `Face` reference to a `DetectedFace`. `null` = "not tied to a face",
a legitimate state.

Lifecycle rules:
- **Created at detection**, in the same place the label is placed on the anchor today
  (`LabelManager.SetLabels`). **Dragging never changes the link** — the user moves the number to
  where it looks good, the face stays known. Ctrl-drag bulk moves and row edits are unaffected.
- **Deleted label** → the face stays in `DetectedFaces` as *unlabeled*, flagged *rejected* so it
  is never offered as a suggestion target again in this session. A later manually added label
  near it snaps to it (re-links).
- **Manually added label** with no unlabeled detected face nearby → stays unlinked. Can be
  named/numbered/exported as today; contributes nothing to recognition, receives no suggestion.
- **Redetect / rotate** already clear all persons → rebuild list and links from scratch.
- **Relinking by the user**: out of scope for v1. Natural later gesture: drop a label with a
  modifier key onto an unlabeled face box.

### 2. Visual hint (preview only, never exported)
- **While dragging** a label: thin outline of its face box + connector line from label center to
  box center; disappears on drop. Same pattern as the existing row-preview highlight.
- **Toggle "Gesichter anzeigen"**: all face boxes dimmed, unlabeled faces in a distinct colour.
  Useful on its own — shows which faces the detector missed or the user deleted.
- Implementation note: markers are hand-rolled in `PictureDisplay` code-behind (flagged in
  `ARCHITECTURE.md` as the riskiest area). Prefer a **separate overlay canvas layer bound to
  `DetectedFaces`** over adding more programmatic bindings per marker.

### 3. Persistence — metadata V6
The box alone is not enough: the recognizer aligns the crop with the landmarks; without them the
embedding is worse. Per person, as a **new optional field** in a V6 schema:
- face box + five landmarks, base-image coordinates
- nothing else. The embedding is recomputable from the clean base image (patch restore / PDF
  payload already provide it on reopen). 128 floats per person would bloat the EXIF comment on
  large groups.

Unlabeled detected faces are **not** persisted: on reopen the detector runs again on the restored
base image (deterministic on identical pixels) and the unlabeled set is the detection list minus
the persisted linked boxes.

**Pre-V6 files**: same redetection, then **infer links by containment** — a label whose position
lies inside exactly one face box gets linked; anything ambiguous stays unlinked. Good enough for
suggestions; the next save upgrades the file to V6 with explicit links.

PDF payloads carry the same metadata JSON → nothing extra needed. Follows the existing pattern
(`AutoNumMetaData_V5.cs` + router in `AutoNumMetaData.cs`).

### 4. Recognition layer (Model/)
- **FaceEmbedder** — wraps `FaceRecognizerSF`; `Embed(baseImage, DetectedFace) → float[128]`.
  `FaceDetector` is extended to return `DetectedFace` (box + landmarks + score) instead of
  `Rectangle`.
- **FaceStore** — the local identity database. Location: `%AppData%/AutoNum/faces/` next to
  `settings.json` (open question: user-selectable folder so relatives can share one store).
  Format: start with JSON index + binary embedding blob (zero dependencies; fine for a few
  thousand faces). SQLite only if size/query needs demand it.
  Schema (conceptually):
  - `Identity { Id, Name, Notes, Created }`
  - `Sample { Id, IdentityId, ModelId, Embedding, SourceFile, PersonNumber, FaceSizePx,
    DetectorScore, Thumbnail(png, small), Added }`
- **FaceMatcher** — `Suggest(embedding) → ranked candidates (Identity, score, margin)`.

### 5. Matching strategy — multiple samples per identity
Libraries only compare two vectors. Combining several samples per person is ours:

- **Nearest neighbour** (chosen): compare against every stored sample, report per identity the
  best score. Robust to variation (age, pose, B/W vs colour). Weakness: one wrongly learned
  sample yields confident wrong matches forever → hygiene rules below.
- *Centroid* (rejected): averaging blurs exactly the variation we want to keep (young vs old).
- **Margin rule** (chosen, hybrid): a suggestion needs the best identity to beat the runner-up by
  a margin, not just pass the threshold. 0.45 vs 0.44 against a sibling is not a confident
  suggestion. Threshold and margin come from the spike.
- Show the score to the user as a hint, not a verdict.

More samples help by **covering variation, not by averaging noise**: ten portraits from one
sitting add almost nothing; five photos across decades/conditions add a lot. Returns flatten after
a handful of diverse samples per identity.

### 6. Store hygiene (matters more than sample count)
- **Quality gate**: learn only from faces above a minimum pixel size and detector score.
- **Deduplication**: skip a new sample when the identity already has one with very high
  similarity to it.
- **Provenance**: every sample knows `SourceFile` + `PersonNumber`. Renaming a person in a
  reopened file **moves** the sample, never duplicates it. A polluted identity can be inspected
  and the offending sample removed.
- **Confirmed only**: learn from names the user typed or explicitly accepted — never from an
  unaccepted suggestion (otherwise a wrong suggestion reinforces itself).

### 7. Flows
- **Open** (fresh or saved): detect → (restore/infer links) → embed linked faces → `FaceMatcher`
  → candidates attached to each `Person` → names view shows the top candidate in a
  distinguishable "suggested" style until accepted (click/Enter) or overtyped.
- **Save**: every linked face with a *confirmed* name → `FaceStore` (quality gate, dedup,
  provenance update).
- **Settings → Erkennung**: opt-in toggle for recognition (default off), link to the store
  management dialog.
- **Management dialog** (phase 3): browse identities with thumbnails, rename, merge two
  identities, delete identity/sample, export/import the store.

### 8. Privacy
Face embeddings are biometric data. Everything stays local; the manual must say so plainly. For
living persons in Germany this is special-category data under GDPR → opt-in, easy deletion,
no silent learning. Faces the detector missed can be named but not learned (no landmarks).

### 9. Independent cheap win
**Name autocomplete** from previously used names (frequency list, from the store's identities
and/or a plain history). Useful on its own and the natural fallback when there is no confident
match. Do this regardless of the recognition outcome.

## Model comparison — 2026-10-11 (SFace vs InsightFace ArcFace)

Same photos, same detector and alignment (`FaceRecognizerSF.AlignCrop` yields the standard
112×112 ArcFace-template crop, so only the embedding network differs), same labels (54 samples,
41 identities incl. the two user-confirmed Franziska faces). ArcFace models run through
OpenCV's DNN module (`DnnInvoke.ReadNetFromONNX`) — **no new runtime dependency**. Input RGB,
(x−127.5)/127.5, 512-d output. `--model arcface` (w600k_r50, 174 MB) and `--model
arcface-small` (w600k_mbf, 14 MB), from the InsightFace `buffalo_l` / `buffalo_sc` packs.

**Separation on the labeled set** (23 same-person pairs, 1408 different-person pairs)

| | SFace | ArcFace r50 | ArcFace mbf |
|---|---|---|---|
| same-person median / p10 | 0.49 / 0.38 | 0.44 / 0.30 | 0.43 / 0.23 |
| different median / p90 / max | 0.16 / 0.33 / 0.62 | 0.08 / 0.22 / 0.59 | 0.10 / 0.24 / 0.56 |
| FPR at 91 % TPR | 14 % (t=0.30) | **2.7 % (t=0.30)** | — |
| leave-one-out NN (19 faces) | 14 right, 1 wrong, 4 none (t=0.45) | **19 right, 0 wrong** (t=0.30) | 16 right, 3 wrong |
| same-photo bias, within vs across ≥ 0.40 | 5.0 % vs 1.6 % | **0.8 % vs 0.5 %** | 1.1 % vs 0.4 % |
| time, 116 faces incl. detection | 12 s | 47 s | 16 s |

**Suggestion use case on the 8 new photos** (30 faces, 4 with ground truth, same-photo samples
excluded as witnesses):

| | SFace (prefill ≥ 0.50) | ArcFace r50 (prefill ≥ 0.40) | ArcFace mbf (prefill ≥ 0.40) |
|---|---|---|---|
| true identity ranked first | 2 of 4 | **4 of 4** | 3 of 4 |
| known faces prefilled right / wrong | 1 / 0 | 1 / 0 | 1 / 0 |
| known faces in candidate band, right / wrong | 1 / 2 | **3 / 0** | 2 / 1 |
| strangers: false prefill / false candidates | 0 / 10 | **0 / 2** | 0 / 7 |

With ArcFace r50 the two blond boys are ranked as Ludwig (0.40) and Hans (0.35) from their 37-px
1953 samples — the case SFace got wrong with confident sibling suggestions — and the bride is
Franziska at 0.33 with a 0.15 margin over the runner-up. The highest stranger score is 0.34.
The hardest sibling pair (Ludwig vs Michael, 38 px, same photo) is still 0.585, so the margin
rule and the candidate list stay.

**Decision: use ArcFace r50 if the licence allows it** — it removes the same-photo bias, ranks
every known face first, and makes zero wrong suggestions on this material. Thresholds for it:
prefill ≥ 0.40 with margin ≥ 0.05, candidate list from ≈ 0.28. The small variant (mbf) is a
usable fallback (one wrong candidate, bride ranked second), SFace is clearly the weakest.

**Licence caveat (must be resolved before shipping):** InsightFace states its pretrained models
are for *non-commercial research purposes only*, and WebFace600K (the `w600k_*` training set)
carries its own restrictions. AutoNum is free, but it is a distributed end-user product, not
research. Candidates with permissive licences to test next with the same harness:
the ONNX model zoo ArcFace (`arcfaceresnet100-8.onnx`, LResNet100E-IR trained on
MS1M-refine, Apache-2.0, ~250 MB) and AdaFace (MIT-licensed weights). Same input contract
(112×112 aligned RGB, 512-d), so only a model path changes in `ArcFaceEmbedder`.

Installer impact: +174 MB (r50) or +14 MB (mbf) on top of the current build; consider shipping
the small model and offering the large one as an optional download.

## Phases
1. **Link + hint**: `DetectedFace`, `Person.Face`, lifecycle rules, drag hint, "Gesichter
   anzeigen" toggle. No model risk; useful on its own.
2. **V6 persistence** + pre-V6 containment inference.
3. **Spike** (can run in parallel, see below): decides thresholds and whether SFace is good
   enough on the user's material.
4. **Store + suggestions**: `FaceEmbedder`, `FaceStore`, `FaceMatcher`, suggestion UI, learn on
   save, settings opt-in.
5. **Management dialog**, export/import, name autocomplete.

## Open questions
- Store location: fixed `%AppData%` vs user-selectable folder (sharing between relatives).
- Living persons in the store: affects how prominent the privacy/opt-in wording must be.
- Threshold / margin values and minimum face size — first numbers from the spike (0.40–0.45,
  margin ~0.05, ≥40 px), to be re-checked with more photos incl. colour/modern ones.
- Whether unlinked (manually added) labels should get a "draw a face box" path later (detector
  rerun inside the box at higher sensitivity to obtain landmarks).

## Spike — `spike/FaceRecognitionSpike/`
Standalone console project (not in `AutoNum.sln`, so it cannot affect the release build). Uses
the same Emgu.CV 4.10 package as the app, the app's YuNet model and the downloaded SFace model.
Photos go to `spike/photos/` (git-ignored — family photos never enter the repo).

Workflow:
1. `extract` — detect all faces in `spike/photos/**`, write aligned crops to
   `spike/out/crops/<photo>__<n>.png` plus `faces.json` (box, landmarks, size, score, embedding).
2. The user labels by **moving crop files into `spike/out/labeled/<Person Name>/`** folders.
   Unknown faces are simply left in `crops/`.
3. `evaluate` — reads the labeled folders, computes all pairwise cosine similarities and reports:
   same-person vs different-person distributions, the best separating threshold, leave-one-out
   nearest-neighbour accuracy with margins, worst confusions, and a breakdown by face size.

Questions the spike must answer: do typical face sizes yield usable embeddings; which threshold
and margin separate matches from non-matches (incl. siblings); how far apart do same-person
samples land across eras/B/W vs colour.

Additional commands: `pairs` (unsupervised top cross-photo matches + contact sheets, needs no
labels) and `labels-from-pdf` (reads names + label positions from AutoNum `_num.pdf` payloads
and labels the detected faces automatically — a prototype of the pre-V6 link inference).
`SPIKE_VERBOSE=1` prints the per-label assignment table. See `spike/FaceRecognitionSpike/README.md`.

## Spike results — 2026-10-11 (first run, 9 photos)

Material: 9 B/W family photos from the 1950s, 86 detected faces, 57 of them narrower than 45 px
(one 45-face group photo "inching" dominates). Labels: 40 faces of the group photo with proven
names from its `_num.pdf`; 12 more faces in the other photos labeled from file names plus a
visual check of the contact sheets (**assumed, not yet confirmed by the user**). That gave 6
identities with 2–4 samples, 18 same-person pairs and 1308 different-person pairs.

**Similarity distributions (cosine, SFace)**

| | min | p10 | median | p90 | max |
|---|---|---|---|---|---|
| same person (n=18) | 0.22 | 0.38 | 0.49 | 0.65 | 0.73 |
| different persons (n=1308) | -0.14 | 0.03 | 0.16 | 0.34 | 0.62 |

**Threshold sweep (pairwise)**

| threshold | same accepted | different accepted |
|---|---|---|
| 0.36 (OpenCV default) | 89 % | 7.4 % |
| 0.40 | 78 % | 4.4 % |
| 0.45 | 56 % | 1.8 % |
| 0.50 | 44 % | 0.6 % |

**Leave-one-out nearest neighbour** (the app's strategy; 17 faces with a same-identity sample
elsewhere): at 0.45 → 13 correct, 1 wrong, 3 no match. At 0.363 → 14 correct, 3 wrong, 0 no
match. Margin of correct suggestions over the runner-up identity: median 0.19, but p10 0.03.
Adults at 30–45 px: 7/9 correct. The wrong ones were exactly the predicted cases: father vs son
(Anton Bauer sen./jun., margin 0.004) and children at 35 px matched to a sibling.

**Hardest confusions** are all siblings/relatives at ~35–42 px in the group photo: Ludwig vs
Michael Bauer 0.62, Lina vs Walburga Beck 0.61, Fanni vs Resi Beck 0.59. These exceed the
OpenCV threshold by a wide margin. Within-photo pairs (guaranteed different persons) above
0.363: 91 of 1135.

**Weakest same-person pairs**: Walburga Beck, field photo with headscarf at 48 px vs wedding
portrait with veil at 131 px → 0.22; Anton Bauer jun., wedding portrait at 179 px vs group photo
at 36 px → 0.28. Different era + headwear + resolution gap is the failure mode, not B/W per se.

**Conclusions for the design**

1. SFace is usable on this material for adults, including 35–40 px faces in group photos.
   Children and siblings are not reliably separable; do not try.
2. Threshold ~0.40–0.45 (not 0.36) **plus the margin rule** (runner-up identity at least ~0.05
   lower). When the margin is small, show the top two candidates instead of one — the
   father/son case is exactly that.
3. Quality gate for *learning*: face width ≥ ~40 px and detector score; samples from several
   eras per identity are what make matches possible (nearest neighbour, never centroid).
4. **Labels are routinely dragged off the face.** In the group photo PDF every label sits
   0.9–1.5 face heights away from its face (above the head in back rows, below the chin in
   front rows). This settles two design points: the V6 link must be persisted explicitly, and
   the pre-V6 inference must be a **global minimum-cost assignment** (Hungarian on distance in
   face-height units, tolerance ~2.5), not containment or greedy nearest — greedy mis-assigned
   two labels on this very photo, Hungarian got 40/40.
5. The model's suggestions for the other photos agreed with the file names (Anton Bauer /
   Walburga Beck in the wedding and field photos, Anton jun. as Rosa's groom, Fanni/Walli in
   the sisters' photo), which is the use case working end to end on real data.

Caveats: 18 same-person pairs is a small sample; the 12 assumed labels need user confirmation;
no colour/modern photos were included.

### Second run — suggestions for 8 unseen photos (`suggest` command)

The real use case: 30 faces in 8 new Schlagbauer/Bauer photos (1920s–1960s, faces 74–360 px)
against the store above (52 samples, 41 identities, dominated by 36-px faces from the 1953
group photo). Threshold 0.40, margin 0.05. Known overlaps from the file names: Franziska
Bauer (verh. Schlagbauer) in up to four photos, Hans and Ludwig Bauer in one.

- **Hits:** Franziska Bauer found strongly in the siblings' photo (0.57, via her 36-px group
  photo sample) and weakly as the bride at Martin Schlagbauer's wedding (0.41, runner-up her
  father at 0.36) and as a child at Fasching 1951 (0.47, runner-up a Beck girl 0.43).
- **Misses:** Hans and Ludwig Bauer (blond boys, 320–350 px) were not found — their only
  stored samples are 37-px faces from the group photo; both scored below 0.36. Instead their
  brothers Karl and Michael were suggested at 0.44, one of them as a confident SUGGEST with a
  margin of 0.08. **A confident, wrong suggestion among siblings.**
- **False positives:** a man with a hat was matched to a nun (0.48, margin 0.07, SUGGEST);
  a grinning boy to a grinning boy of another family (0.47). Both stored witnesses are 36 px.
- Tally at 0.40/0.05: 6 SUGGEST of which 1 clearly right, 9 WEAK of which 2 plausibly right,
  15 none (mostly correct — people not in the store).

**What this changes**

1. The leave-one-out numbers were optimistic because they mostly compared faces *within*
   the same photo session. Across eras and sizes the usable region shrinks. A store full of
   36-px witnesses produces spurious 0.40–0.48 matches for unrelated people.
2. **Witness quality must enter the decision.** Proposal: a stored sample narrower than ~60 px
   can only produce a *confident* suggestion at ≥ 0.50; with both faces ≥ 60 px, ≥ 0.42 is
   enough. (On this run that rule keeps the one correct SUGGEST and drops the wrong ones.)
   Equivalent formulation: learn only from faces ≥ 60 px, and treat smaller ones as
   second-class witnesses that can confirm but not initiate a suggestion.
3. **Children: do not suggest, do not learn.** Every wrong confident suggestion involved a
   child on at least one side. Without an age estimator, a pragmatic proxy is the user's
   explicit choice (e.g. "Kind" flag) or simply very conservative thresholds for small faces.
4. Siblings remain the hard case even for adults (bride vs her father at 0.36). The margin
   rule and showing two candidates are mandatory, not optional.
5. The store learns as it goes: once the bride is confirmed, a second adult sample of
   Franziska exists and later adult photos of her have a realistic chance; the child samples
   alone do not bridge to adulthood (child vs bride 0.41, barely).

**Ground truth from the user** (2026-10-11): the bride *is* Franziska Bauer; the Fasching girl is
*not*; the three siblings are Hans, Franziska and Ludwig Bauer; nobody else in the new photos
is in the store. Scored against that (30 faces, 4 of them in the store):

| rule | in-store: right / wrong / missed | not in store: false SUGGEST / false WEAK |
|---|---|---|
| A: ≥0.40, margin 0.05 | SUGGEST 1 right 1 wrong, WEAK 1 right 1 wrong | 4 / 6 |
| B: ≥0.45, margin 0.05 | 1 right, 3 missed | 2 / 1 |
| C: size-aware (witness <60 px → 0.50, else 0.42) | SUGGEST 1 right **1 wrong**, WEAK 1 right 1 wrong | 0 / 10 |
| D: ≥0.50, margin 0.05 | 1 right, 3 missed | **0 / 0** |

The one wrong confident suggestion that survives rule C is a blond boy matched to his brother
Karl (0.44, margin 0.08, witness 110 px) — siblings, both children, nothing size-based catches
it. The two boys' true samples (37 px, 1953) scored 0.33–0.35, below every threshold.

**Same-photo bias.** After confirming Franziska's 292-px sample from the siblings' photo, her
two brothers *in the same photo* score 0.55 and 0.64 against it — higher than most genuine
cross-photo matches. Across the whole set, pairs from the same photo (always different
persons) exceed 0.40 three times as often as pairs from different photos (5.0 % vs 1.6 %;
p99 0.51 vs 0.42): the embedding carries scan/lighting/grain "style". Consequences: (a) never match a face against
samples whose `SourceFile` is the same photo (provenance makes this trivial); (b) expect
inflated scores between photos of one session/roll; (c) leave-one-out numbers computed
within one photo overstate real accuracy.

**Decision for v1**
- **Prefill** only at ≥ 0.50 with margin ≥ 0.05 (rule D): zero false positives here, at the
  price of finding 1 of 4 known faces. This is the right trade-off for "suggest, never decide".
- Everything in 0.40–0.50 goes into an optional **candidate list** (e.g. a dropdown on the
  name cell with the top 3 and their scores), never into the name field. On this run that
  band held the bride (right) plus ~11 wrong candidates — cheap for the user to glance at,
  harmful if prefilled.
- Learn only from confirmed faces ≥ 60 px; smaller witnesses stay in the store but cannot
  trigger a prefill on their own.
- Children remain out of reach for this model; the thresholds above make that mostly
  harmless rather than fixing it.
