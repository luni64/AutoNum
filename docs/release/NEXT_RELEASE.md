# Next Release

## Features

## Bug Fixes

- Very large scans (roughly 230 megapixels and up, e.g. a 695 MB TIFF) closed the program without any message when opened. The face detector ran on the full-size image and crashed inside OpenCV; it now works on a reduced copy (max. 40 megapixels) for such images. Opening large scans is also much faster and needs far less memory, and faces in them are found more reliably. (Forum report)
