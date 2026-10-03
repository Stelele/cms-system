# papi-iot

A Python package for Raspberry Pi home security: face recognition from photos and video via `face_recognition`/OpenCV, Pi camera controls, and storage that syncs photos and footage to Google Cloud Storage, plus a Flask/waitress live-stream demo in `demonstration/`.

## What it does

You point it at a folder of known faces and a camera; it encodes them, compares incoming frames at `tolerance = 0.6`, draws boxes and names, and writes unmatched frames to `unknownFaces/` as numbered JPEGs. The demo (`demonstration/papi_livestream.py`) exposes `/video_feed`, `/new_user` and `/remove_user`, and emails a fixed recipient when a face does not match the previous one (`check_face_send`, over the Gmail API).

## How it works

`papi_iot/papi_iot.py` defines `PAPIIOT`, which composes three collaborators: `PapiFaceRecognition` (297 lines), `StorageManager` (9 lines, just holds an `OnlineStorage` and an `OfflineStorage`), and `PapiCameraVideo` (186 lines wrapping `picamera.PiCamera`).

`PapiFaceRecognition.__init__` opens `cv2.VideoCapture(0)` and calls `loadImages()`, which walks `knownFaces/`, appends `file.replace(".jpg","")` to `known_names` and the 128-d encoding to `known_face_encodings`. `getFrame()` resizes each frame to ¼, runs `face_locations`/`face_encodings` every 5th frame, matches with `compare_faces` plus `np.argmin(face_distance)`, saves unknown frames, and returns `(jpeg bytes, image, unknownPhotoName)` — the triple the Flask generator consumes. `faceRecognitionFromPhoto()` and `faceRecognitionFromVideoFile()` are separate paths for stills and files.

`OfflineStorage` (`papi_storage_offline.py`) creates `home/pi/photos/{knownFaces,unknownFaces}` and `home/pi/videos` relative to the working directory. `OnlineStorage` (`papi_storage_online.py`) connects with `storage.Client.from_service_account_json(credentialsFile)`, auto-creates the two buckets on `NotFound`, prefixes blobs `approved_`/`banned_`, and raises `BlobNotFound`/`NoBlobsFound` from `papi_exceptions.py`. `PapiCameraVideo` exposes photo/video modes with fixed ISO and locked shutter/AWB for low and normal light.

### Stack

Python 3.6+; `face-recognition` 1.3 + dlib for encodings, `opencv-python` 4.4 for frames, `picamera` for the Pi camera, `google-cloud-storage` 1.32 for the sync, `Flask` 1.1 + `waitress` for the demo server, `google-api-python-client` for Gmail. `matplotlib` is imported solely for `image.imread` in `papi_storage_offline.py`. Packaging is cookiecutter-pypackage: `setup.py`, `tox.ini`, Sphinx docs, Travis.

## What works well

- The online storage layer is the most finished code: `storeOnlinePhotos`/`getOnlinePhotos` handle prefixes and empty buckets with explicit exceptions rather than silent no-ops.
- `papi_exceptions.py` (49 lines) gives four typed exceptions with readable messages instead of bare `raise Exception`.
- Docstrings are consistently NumPy-style across all six modules, and `docs/` covers offline, online, camera and recognition separately.
- The demo is a real end-to-end flow: camera → recognition → snapshot → email, not just a library.

## What I'd change

- **`.travis.yml` line 20 ships a live PyPI API token** (`password: pypi-<redacted>...`) in plaintext, with `deploy on: tags`. It must be revoked.
- `loadImages()` (`papi_face_recognition.py:147`) swallows every failure with `except Exception: pass`, and appends the name at line 140 *before* the encoding is computed — one bad JPEG silently desynchronises `known_names` from `known_face_encodings`, so people get mislabelled.
- The same attribute means two things: `loadImages` sets `self.known_faces = face_recognition.load_image_file(file)` (line 142, an image, overwritten each loop) while `faceRecognitionFromPhoto` appends encodings to it (line 70).
- `getFrame()` never clears `self.face_names` (line 208) while replacing `self.locations` each processed frame, so the zip at line 215 pairs stale names with fresh boxes; line 228 then labels every box with the global `name_gui` instead of `name`.
- `autoAdjustToLight` (`papi_camera_video.py:108`) assigns `self.shutter_speed = 0` on the wrapper, not `self.camera.shutter_speed` — the camera never receives it. `storeNewKnownUsers` globs `'/' + picType` (line 165) instead of `'/*' + picType`, so it matches nothing.
- `setup.py` declares `install_requires = []`, so `pip install papi_iot` pulls none of the pinned dependencies; `__version__` is 3.7.6 in `papi_iot/__init__.py` but 3.7.8 in `setup.py`.

## Still outstanding

- Two tests only (`tests/test_papi_iot.py`, 28 lines), both checking directory creation; nothing covers the 297-line recognition module, online storage, camera or the demo. One assertion uses the typo `'unkownFaces'` and passes only because the `else` branch returns the unknown path.
- Notification logic is explicitly unwritten: `papi_face_recognition.py:199` — "This is where the logic for notification should be inserted I believe".
- `papi_iot/cli.py` is still the cookiecutter stub printing "Replace this message…", yet it is registered as the `papi_iot` console script.
- `OfflineStorage.getOfflinesVideo` reads video files with `matplotlib.image.imread`; no caller, no test.
- Travis CI targets a dead Python 3.6–3.8 matrix.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
