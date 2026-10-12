"""Inspect the real 251 building Texture2D records in a Unity 6000.3 APK.

Uses the stripped serialized-file v22 layout emitted by the installed editor.
Only metadata and the first 256 bytes of each texture object are read. No image
payload is decoded and no APK extraction/dependency installation is necessary.
Fails on a different/ambiguous schema instead of reporting desired import
settings as proof of actual GPU compression.
"""
import argparse
from collections import Counter
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import re
import struct
import zipfile

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "Assets/Weedtopia/Production/Resources/WeedtopiaBuildings"
FORMATS = {4: "RGBA32", 47: "ETC2_RGBA8", 25: "BC7", 48: "ASTC_4x4", 50: "ASTC_6x6"}


class Reader:
    def __init__(self, data):
        self.data, self.position = data, 0

    def read(self, fmt):
        value = struct.unpack_from("<" + fmt, self.data, self.position)
        self.position += struct.calcsize("<" + fmt)
        return value[0] if len(value) == 1 else value

    def skip(self, length):
        self.position += length
        assert self.position <= len(self.data), "Metadata prefix exhausted"

    def align(self):
        self.position = (self.position + 3) & ~3

    def cstring(self):
        end = self.data.index(b"\0", self.position)
        value = self.data[self.position:end].decode("ascii")
        self.position = end + 1
        return value


def serialized_texture(stream, serialized_bytes):
        header = stream.read(48)
        assert len(header) == 48 and struct.unpack_from(">I", header, 8)[0] == 22, "Expected serialized-file version 22"
        assert header[16] == 0, "Expected little-endian object records"
        metadata_bytes = struct.unpack_from(">I", header, 20)[0]
        file_size, data_offset = struct.unpack_from(">QQ", header, 24)
        assert file_size == serialized_bytes and 48 < data_offset < 65536, "Unexpected serialized header"
        assert metadata_bytes <= data_offset, "Unexpected metadata length"
        metadata = Reader(stream.read(data_offset - 48))
        version = metadata.cstring()
        assert version.startswith("6000.3."), "Unknown Texture2D schema: " + version
        target = metadata.read("i")
        assert target == 13, "Expected Android serialized target"
        assert metadata.read("B") == 0, "Expected stripped type tree"
        classes = []
        for _ in range(metadata.read("i")):
            class_id = metadata.read("i")
            classes.append(class_id)
            metadata.read("B")  # stripped type flag
            script_index = metadata.read("h")
            assert script_index == -1 and class_id >= 0 and class_id != 114, "Unexpected script-bearing type"
            metadata.skip(16)  # old type hash
            # Stripped player files omit both the type tree and its dependencies.
        texture_objects = []
        for _ in range(metadata.read("i")):
            metadata.align()
            path_id, offset, size, type_index = metadata.read("qQIi")
            if classes[type_index] == 28:  # Unity Texture2D class ID
                texture_objects.append((offset, size))
        assert len(texture_objects) == 1, "Expected one Texture2D per building resource"
        offset, size = texture_objects[0]
        assert 0 <= offset and offset + size <= file_size - data_offset, "Texture object lies outside serialized file"
        # The entry is already at dataOffset after its metadata was read.
        if offset:
            stream.seek(data_offset + offset)
        record = Reader(stream.read(min(size, 256)))
        name_length = record.read("i")
        assert 0 < name_length < 128, "Invalid texture name length"
        name = record.data[record.position:record.position + name_length].decode("utf8")
        record.skip(name_length)
        record.align()
        record.skip(4)  # Unity 6000.3 texture fallback/alpha flag block
        width, height, image_bytes, stripped_mips, texture_format, mip_count = record.read("iiiiii")
        assert 0 < width <= 2048 and 0 < height <= 2048, "Unexpected texture dimensions"
        assert 0 < mip_count <= 12 and stripped_mips == 0 and 0 < image_bytes <= 128 * 1024 * 1024, "Texture schema mismatch"
        return dict(name=name, width=width, height=height, imageBytes=image_bytes,
                    mipCount=mip_count, format=FORMATS.get(texture_format, "unknown"), formatId=texture_format)


def texture_record(archive, entry):
    with archive.open(entry) as stream:
        return serialized_texture(stream, entry.file_size)


def decompress_block(data, mode, expected):
    if mode == 0:
        assert len(data) == expected, "Raw UnityFS block length mismatch"
        return data
    assert mode in (2, 3), "Unsupported UnityFS compression mode"
    output = bytearray()
    position = 0
    while position < len(data):
        token = data[position]
        position += 1
        literal_bytes = token >> 4
        if literal_bytes == 15:
            while True:
                extension = data[position]
                position += 1
                literal_bytes += extension
                if extension != 255:
                    break
        output.extend(data[position:position + literal_bytes])
        position += literal_bytes
        assert len(output) <= expected and position <= len(data), "LZ4 literal bounds exceeded"
        if position == len(data):
            break
        offset = struct.unpack_from("<H", data, position)[0]
        position += 2
        assert 0 < offset <= len(output), "Invalid LZ4 back reference"
        match_bytes = (token & 15) + 4
        if (token & 15) == 15:
            while True:
                extension = data[position]
                position += 1
                match_bytes += extension
                if extension != 255:
                    break
        assert len(output) + match_bytes <= expected, "LZ4 match bounds exceeded"
        for _ in range(match_bytes):
            output.append(output[-offset])
    assert len(output) == expected, "LZ4 decompressed size differs"
    return bytes(output)


def unpack_probe(bundle):
    import io
    data = bundle.read_bytes()
    assert len(data) <= 256 * 1024 * 1024, "Isolated probe exceeds bounded reader size"
    position = 0

    def cstring():
        nonlocal position
        end = data.index(b"\0", position)
        result = data[position:end].decode("utf8")
        position = end + 1
        return result

    assert cstring() == "UnityFS", "Expected UnityFS AssetBundle"
    version = struct.unpack_from(">I", data, position)[0]
    position += 4
    player, engine = cstring(), cstring()
    file_size, compressed_info, raw_info, flags = struct.unpack_from(">QIII", data, position)
    position += 20
    assert file_size == len(data) and version in (6, 7, 8), "Unknown UnityFS header"
    if version >= 7:
        position = (position + 15) & ~15
    block_info_position = file_size - compressed_info if flags & 0x80 else position
    info = decompress_block(data[block_info_position:block_info_position + compressed_info], flags & 0x3f, raw_info)
    if not flags & 0x80:
        position += compressed_info
    if flags & 0x200:
        position = (position + 15) & ~15
    info_position = 16  # Uncompressed data hash.
    block_count = struct.unpack_from(">I", info, info_position)[0]
    info_position += 4
    blocks = []
    for _ in range(block_count):
        raw, compressed, block_flags = struct.unpack_from(">IIH", info, info_position)
        info_position += 10
        blocks.append(decompress_block(data[position:position + compressed], block_flags & 0x3f, raw))
        position += compressed
    node_count = struct.unpack_from(">I", info, info_position)[0]
    info_position += 4
    payload = b"".join(blocks)
    extracted = bundle.parent / "extracted"
    extracted.mkdir(exist_ok=True)
    textures = []
    nodes = []
    for _ in range(node_count):
        offset, length, node_flags = struct.unpack_from(">QQI", info, info_position)
        info_position += 20
        end = info.index(b"\0", info_position)
        name = info[info_position:end].decode("utf8")
        info_position = end + 1
        assert "/" not in name and "\\" not in name and name not in (".", ".."), "Unsafe UnityFS node name"
        assert offset + length <= len(payload), "UnityFS node exceeds payload"
        node = payload[offset:offset + length]
        (extracted / name).write_bytes(node)
        nodes.append(dict(name=name, bytes=length, flags=node_flags))
        if name.startswith("CAB-") and not name.endswith(".resS"):
            textures.append(serialized_texture(io.BytesIO(node), length))
    assert len(textures) == 1, "Expected one isolated serialized building texture"
    return dict(bundle=str(bundle), bundleBytes=file_size, unityVersion=engine, nodes=nodes,
                texture=textures[0], actualEtc2CompressionVerified=textures[0]["format"] == "ETC2_RGBA8",
                actualAstc4CompressionVerified=textures[0]["format"] == "ASTC_4x4")


def audit_probe(probe_path):
    probe = json.loads(probe_path.read_text(encoding="utf8"))
    bundle = Path(probe["bundlePath"]).resolve(strict=True)
    assert (ROOT / ".utmp/CommercialReadiness/AndroidProbe").resolve() in bundle.parents, "Probe bundle lies outside its isolated directory"
    result = unpack_probe(bundle)
    source = ROOT / probe["sourcePath"]
    assert source.resolve() in [p.resolve() for p in ART.rglob("*.png")], "Unknown source texture"
    source_hash = hashlib.sha256(source.read_bytes()).hexdigest()
    assert source_hash == probe["sourceSha256"], "Probe source differs from current production pixels"
    result.update(utc=datetime.now(timezone.utc).isoformat(), sourcePath=probe["sourcePath"],
                  sourceSha256=source_hash, guid=probe["guid"],
                  bundleSha256=hashlib.sha256(bundle.read_bytes()).hexdigest(),
                  metaBytesPreserved=probe["metaBytesPreserved"], sourcePixelsPreserved=probe["sourcePixelsPreserved"],
                  spriteGeometryPreserved=probe["spriteGeometryPreserved"],
                  previewFormat=probe["compressedPreview"]["importedFormat"], deviceVisualAcceptance=False)
    output = probe_path.parent / (probe_path.stem + "-serialized-verification.json")
    output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf8")
    print(json.dumps(result, indent=2))


def audit(apk, output, baseline=None):
    paths = sorted(ART.rglob("*.png"))
    assert len(paths) == 251, "Expected 251 approved building resources"
    records = []
    with zipfile.ZipFile(apk) as archive:
        entries = {entry.filename: entry for entry in archive.infolist()}
        for path in paths:
            match = re.search(r"^guid: ([0-9a-f]{32})$", path.with_suffix(".png.meta").read_text(), re.M)
            assert match, "Missing building GUID: " + str(path)
            guid = match.group(1)
            entry = entries.get("assets/bin/Data/" + guid)
            assert entry is not None, "Building absent from APK: " + str(path)
            texture = texture_record(archive, entry)
            assert texture["name"] == path.stem, "Texture identity mismatch"
            records.append(dict(path=path.relative_to(ROOT).as_posix(), guid=guid,
                                sourceSha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                                entry=entry.filename, zipBytes=entry.compress_size,
                                serializedBytes=entry.file_size, **texture))
    report = dict(utc=datetime.now(timezone.utc).isoformat(), apk=str(apk), apkBytes=apk.stat().st_size,
                  method="Unity 6000.3 stripped serialized v22 Texture2D metadata; ZIP central-directory sizes",
                  textures=len(records), formats=dict(Counter(record["format"] for record in records)),
                  zipBuildingBytes=sum(r["zipBytes"] for r in records),
                  serializedBuildingBytes=sum(r["serializedBytes"] for r in records),
                  gpuImageBytes=sum(r["imageBytes"] for r in records),
                  actualEtc2CompressionVerified=all(r["format"] == "ETC2_RGBA8" for r in records),
                  deviceVisualAcceptance=False, records=records)
    if baseline:
        previous = json.loads(baseline.read_text(encoding="utf8"))
        identity = lambda result: {(r["path"], r["guid"], r["sourceSha256"]) for r in result["records"]}
        report["originalSourcesAndGuidsPreserved"] = identity(report) == identity(previous)
        report["deltaApkBytes"] = report["apkBytes"] - previous["apkBytes"]
        report["deltaZipBuildingBytes"] = report["zipBuildingBytes"] - previous["zipBuildingBytes"]
        report["deltaGpuImageBytes"] = report["gpuImageBytes"] - previous["gpuImageBytes"]
        assert report["originalSourcesAndGuidsPreserved"], "Source/GUID baseline differs"
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf8")
    print(json.dumps({key: value for key, value in report.items() if key != "records"}, indent=2))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("apk", type=Path, nargs="?")
    parser.add_argument("--probe", type=Path)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--baseline", type=Path)
    arguments = parser.parse_args()
    if arguments.probe:
        audit_probe(arguments.probe.resolve(strict=True))
    else:
        assert arguments.apk, "An APK or --probe report is required"
        artifact = arguments.apk.resolve(strict=True)
        audit(artifact, arguments.output or artifact.parent / "building-texture-audit.json", arguments.baseline)
