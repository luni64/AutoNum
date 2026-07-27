# Code signing & Windows Smart App Control (SAC)

**Status: researched 2026-07-27, deliberately NOT implemented yet.**
No field complaints so far. Implement at the next release that touches the
installer, or immediately if a user reports a block. Findings below are current
as of the date above — re-check the Microsoft doc before acting, SAC has moved
several times.

## Why this matters

Smart App Control is a Microsoft-managed code-integrity policy on Windows 11.
The critical difference to SmartScreen:

| | SmartScreen | Smart App Control |
|---|---|---|
| Checks | only the launched .exe from an untrusted origin | **every loaded code module** — DLLs, scripts, MSI |
| Reaction | warning, bypassable via "Run anyway" | **hard block, no bypass** |

So a signed .exe is not sufficient: if the process then loads an unsigned DLL,
SAC still blocks. That was its explicit design goal (DLL hijacking).

SAC starts in *evaluation mode* on clean Windows 11 installs and switches itself
to *enforcement* if the device looks like a good fit. Since 25H2 (April 2026) it
can be re-enabled after being switched off — previously that needed a Windows
reinstall.

## Finding 1 — the existing Certum certificate is sufficient

The "you need an EV certificate" claims found online come from certificate
resellers and SEO sites. Microsoft's own documentation says otherwise, in three
places:

> If the app intelligence service is unable to make a prediction, then Smart App
> Control will **still allow an app to run if it is signed with a certificate
> issued by a certificate authority (CA) within the Trusted Root Program.**

> Apps cannot be run unless they are recognized by Microsoft's app intelligence
> services, **or** they are signed with a trusted certificate.

Reputation **or** signature — EV is never mentioned. Verified against our cert
on 2026-07-27:

```
Subject               CN="Open Source Developer, Günther Lutz Niggl"
Thumbprint            7E32 3A2C B437 FE62 4D04 BE81 29A2 9D74 70D7 F4F9
Chain                 valid, no errors (incl. online revocation check)
Root                  Certum Trusted Network CA 2
  in Windows root store   yes -> distributed via Microsoft Trusted Root Program
Algorithm             RSA 3072 / SHA-256
EV policy OID         absent -> OV class
Valid until           2027-02-23
```

RSA matters: the Code Integrity path does not reliably support **ECC**
signatures. Do not switch the certificate to ECC when renewing.

An EV certificate would still buy something — instant **SmartScreen** reputation
for the installer download, which OV has to accumulate. That is a different
problem from SAC and not urgent.

## Finding 2 — the real gap: we only sign the installer

`setup.iss` signs the setup and the uninstaller (`SignTool=certum $f`,
`SignedUninstaller=yes`). Everything the installer *puts on disk* is unsigned,
and the portable ZIP is unsigned end to end.

Audit of `AutoNum\bin\Release\net8.0-windows\win-x64\publish` (v2.4.0, 39 PE
files): **20 signed, 19 not**. Unsigned, largest first:

```
opencv_videoio_ffmpeg4100_64.dll           25.2 MB
QuestPdfSkia.dll                            5.8 MB
qpdf.dll                                    4.2 MB
UglyToad.PdfPig.dll                         4.0 MB
MahApps.Metro.IconPacks.Material.dll        3.4 MB
UglyToad.PdfPig.Fonts.dll                   1.0 MB
Emgu.CV.dll                                 0.9 MB
QuestPDF.dll                                0.5 MB
AutoNumber.dll                              0.4 MB   <- ours
AutoNumber.exe                              0.2 MB   <- ours
UglyToad.PdfPig.DocumentLayoutAnalysis.dll, libusb-1.0.dll,
MahApps.Metro.IconPacks.Core.dll, UglyToad.PdfPig.Tokens/.Tokenization, ...
```

Already signed by their publishers: the .NET runtime assemblies (Microsoft),
MahApps.Metro (Jan Karger), CommunityToolkit.

## Planned change

Insert a signing pass over the publish output **between step 3 (publish) and
step 4/5 (ZIP, ISCC)** of `RELEASE_PROCESS.md`, so both the ZIP and the
installer payload are signed:

1. Collect `*.exe` and `*.dll` under the publish folder whose
   `Get-AuthenticodeSignature` status is not `Valid`.
2. Sign them with the same certum command already used for the installer
   (thumbprint + `/td sha256 /fd sha256 /tr http://time.certum.pl`), batched —
   `signtool` accepts many files per invocation, which matters because each call
   hits the timestamp server.
3. Re-verify: every PE file in the publish folder must come back `Valid`.
4. Only then build the ZIP and run ISCC.

Notes:

- Signing **third-party** DLLs with our own certificate is normal for
  redistributed binaries and technically unproblematic — but it means we vouch
  for those files as publisher. Accepted consequence.
- Do not re-sign files that are already validly signed by their publisher;
  leave Microsoft's and MahApps' signatures intact.
- Timestamping is essential: signatures must stay valid after the certificate
  expires (2027-02-23).
- This lengthens the release build noticeably (~19 files x timestamp round trip).

## If a complaint does arrive

Ask for the CodeIntegrity log from the affected machine — it names the exact
file that was rejected, which is far more useful than the toast notification:

```
Event Viewer -> Applications and Services Logs -> Microsoft -> Windows
             -> CodeIntegrity -> Operational
```

Blocks typically appear as event IDs 3077 (blocked) / 3076 (audit-only). Also
check whether SAC is in **evaluation** or **enforcement** mode on that machine
(Settings -> Windows Security -> App and Browser Control).

Note that there are Microsoft Q&A threads where correctly signed apps were
blocked anyway, so signing everything is necessary but not provably sufficient.
If blocks persist after the change, the CodeIntegrity entries are the only way
to tell what SAC actually objected to.

## Sources

- <https://learn.microsoft.com/en-us/windows/apps/develop/smart-app-control/overview>
- <https://learn.microsoft.com/en-us/windows/security/book/application-security-application-and-driver-control>
- <https://textslashplain.com/2026/04/28/smart-app-control/> (ECC caveat, all-modules checking)
- <https://learn.microsoft.com/en-us/answers/questions/5757633/how-come-our-code-signed-executable-file-is-blocke>
