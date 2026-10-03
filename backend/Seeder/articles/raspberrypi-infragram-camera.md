# raspberrypi-infragram-camera

A Raspberry Pi camera system that produces NDVI (Normalised Difference Vegetation Index) images and video of crops, with a small Flask web UI for live viewing and GPS-tagged output. Written for an undergraduate final-year project (EEE4022S) at the University of Cape Town.

## What it does

Run `make run` and `src/app.py` starts a Flask server that opens an ngrok tunnel and serves four routes: `/` (a "NDVI Pro" landing page), `/files` (a directory browser over the `output/` tree, including serving the files themselves), `/live_stream`, and two MJPEG endpoints, `/video_feed` and `/ndvi_feed`. The live stream page shows the current GPS coordinates alongside two image streams — the raw camera feed and a colour-mapped NDVI render of the same frames, refreshed roughly every three seconds.

The backend modules can also capture raw 24-bit RGB stills, record h264 video, convert both to jpg/mp4, run one of five NDVI calibration modes over images or whole videos, and write EXIF GPS tags into a jpg. Those flows are only reachable by running each module's `if __name__ == "__main__"` block directly; nothing in the web app can trigger a capture.

## How it works

Entry point is `src/app.py` (159 lines). It constructs a single `Runner` object; `src/runner.py` (41 lines) defines `class Runner(Camera, GPS, NDVI)`, calls all three `__init__`s, and adds `tagNDVIImage()`, which reads a jpg with the `exif` library and writes `gps_latitude`/`gps_longitude` from the GPS class.

`src/camera.py` (129 lines) wraps `picamera.PiCamera` at 1024×768/32fps and shells out to ImageMagick (`convertRawToJPG`) and FFmpeg (`convertVideoToMP4`) via `os.system`. `src/gps.py` (174 lines) powers a SIM868 HAT over `/dev/serial0` with raw AT commands, parses `GNRMC` NMEA sentences into degrees/minutes/seconds dicts, and blocks until a fix. `src/ndvi.py` (245 lines) is the actual science: `performNDVIOperation()` extracts R/G/B channels and applies one of five modes (calibration-target exponentials, a 4×4 least-squares matrix, relative quantum efficiency, and two direct `(R−B)/(R+B)` variants), then min-max normalises. `analyzeVideo()` decodes a whole file to a numpy buffer with `ffmpeg-python`, renders every frame through matplotlib, and reassembles an mp4.

The live NDVI feed runs through a module-level `bufferFrames` list: `gen_frames()` appends every 96th raw frame, and `gen_ndvi_frames()` pops frames off it and re-renders them with matplotlib `savefig` into PNGs.

### Stack

Python 3, Flask + Jinja2 for the UI, `picamera` for capture, `pyserial`/`RPi.GPIO` for the GPS HAT, numpy + Pillow + matplotlib for NDVI, `ffmpeg-python` and ImageMagick for media conversion, `exif` for GPS tagging, `pyngrok` for the public tunnel. The front end is the stock Start Bootstrap "Freelancer" theme (`src/static/css/styles.css` is 11,508 lines of untouched template CSS).

## What works well

- The five NDVI modes are implemented side by side in one function, so calibration approaches can be benchmarked directly; `ndvi.py`'s main block times each run.
- GPS parsing converts NMEA to DMS and back to decimal in two well-separated functions (`getGPSData`, `convertToGPSDecimal`).
- Docstrings on every public method document arguments and units.
- The Makefile separates venv creation, `run`, and a `clean` target that purges generated media.

## What I'd change

- `app.py:43-61` joins the user-supplied `req_path` onto `output/` and passes it to `send_file` with no containment check — `GET /files/../../etc/passwd` escapes the output directory, and the ngrok tunnel makes this public.
- `getGPSData()` (`gps.py:70`) spins forever until a fix arrives; `/live_stream` calls it inline, so a cold GPS module hangs the request indefinitely. No timeout, no error path.
- `os.system` with f-string interpolation in `camera.py:46` and `camera.py:103` drops return codes and is injection-prone; a failed `convert` silently produces nothing.
- The web app has no capture/record/offline-NDVI routes — all of `camera.py` is dead code from the browser's perspective.
- Zero tests, and `performNDVIOperation` can emit NaN (division by zero, or `max == min` in normalisation) without guarding.

## Still outstanding

- No test directory, no CI, one single commit ("refactoring") in the whole history.
- `dependancies.txt` is 0 bytes — a leftover alongside the real `requirements.txt`.
- `bufferFrames` in `app.py` grows unbounded when `/ndvi_feed` isn't being consumed, and `gen_ndvi_frames` busy-loops with no sleep when the buffer is empty.
- Typo'd public API: `Camera.stopVideoRording()` (`camera.py:87`).
- GPS EXIF tagging (`Runner.tagNDVIImage`) is never called from any web route or pipeline.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
