"""Read-only fresh Unity Android APK audit; every output stays outside Unity inputs."""
import argparse, datetime, hashlib, importlib.util, io, json, pathlib, struct, subprocess, sys, tempfile, zipfile, zlib

ROOT = pathlib.Path(__file__).resolve().parents[2]
ANDROID = pathlib.Path(r'D:\Unity Games\6000.3.9f1\Editor\Data\PlaybackEngines\AndroidPlayer')
MAGIC = b'APK Sig Block 42'
CORE = {'libmain.so', 'libunity.so', 'libil2cpp.so'}

def require(condition, detail):
    if not condition: raise ValueError(detail)

def sha(path):
    with path.open('rb') as source: return hashlib.file_digest(source, 'sha256').hexdigest()

def artifact(path):
    return dict(path=str(path), bytes=path.stat().st_size, sha256=sha(path))

def read_exact(source, count):
    data = source.read(count)
    require(len(data) == count, 'Truncated ZIP structure')
    return data

def central_records(source, offset, size, count):
    source.seek(offset); records = []
    for _ in range(count):
        header = struct.unpack('<I6H3I5H2I', read_exact(source, 46))
        require(header[0] == 0x02014b50, 'Invalid central-directory entry signature')
        name = read_exact(source, header[10]); extra = read_exact(source, header[11]); comment = read_exact(source, header[12])
        require(header[13] == 0 and max(header[8], header[9], header[16]) < 0xffffffff, 'ZIP64/multidisk entries are unsupported by this bounded APK schema')
        require(b'\0' not in name, 'NUL in ZIP name')
        records.append(dict(name=name, flags=header[3], method=header[4], crc=header[7], compressed=header[8], raw=header[9], local=header[16]))
    require(source.tell() == offset + size, 'Central-directory declared size disagrees with its records')
    return records

def signing_block(source, central):
    if central < 24: return None
    source.seek(central - 24); footer = read_exact(source, 24)
    if footer[8:] != MAGIC: return None
    size = struct.unpack('<Q', footer[:8])[0]; start = central - size - 8
    require(size >= 24 and start >= 0, 'Invalid APK signing-block size')
    source.seek(start); require(struct.unpack('<Q', read_exact(source, 8))[0] == size, 'APK signing-block header/footer size mismatch')
    pair_ids = []
    while source.tell() < central - 24:
        length = struct.unpack('<Q', read_exact(source, 8))[0]
        require(length >= 4 and source.tell() + length <= central - 24, 'APK signing-block pair exceeds block bounds')
        pair_ids.append('0x%08x' % struct.unpack('<I', read_exact(source, 4))[0]); source.seek(length - 4, 1)
    require(source.tell() == central - 24, 'APK signing-block pair layout mismatch')
    return dict(offset=start, bytes=size + 8, pairIds=pair_ids)

def layout_and_crc(path):
    with path.open('rb') as source, zipfile.ZipFile(path) as archive:
        infos = archive.infolist(); size = path.stat().st_size
        source.seek(max(0, size - 65557)); tail = source.read(); end_index = tail.rfind(b'PK\x05\x06')
        require(end_index >= 0 and end_index + 22 <= len(tail), 'ZIP EOCD is absent')
        end = struct.unpack_from('<I4H2IH', tail, end_index); eocd = size - len(tail) + end_index
        require(end_index + 22 + end[7] == len(tail), 'Trailing bytes or invalid ZIP comment length')
        require(end[1] == end[2] == 0 and end[3] == end[4] == len(infos) and end[4] < 65535, 'Multidisk/ZIP64/count mismatch')
        require(end[5] < 0xffffffff and end[6] < 0xffffffff, 'ZIP64 APKs require a separate audited parser')
        central, central_size = end[6], end[5]
        require(archive.start_dir == central and central + central_size == eocd, 'Central-directory offset/EOCD disagreement')
        records = central_records(source, central, central_size, len(infos))
        require(len({i.filename for i in infos}) == len(infos), 'Duplicate APK entry names')
        block = signing_block(source, central); local_limit = block['offset'] if block else central
        intervals = []; payload_identity = {}; crc_verified = 0
        for info, record in zip(infos, records):
            name = record['name'].decode('utf-8' if record['flags'] & 0x800 else 'cp437')
            require(name == info.filename and record['local'] == info.header_offset and record['crc'] == info.CRC
                    and record['compressed'] == info.compress_size and record['raw'] == info.file_size, 'ZIP reader/central metadata disagreement: ' + info.filename)
            require(not record['flags'] & 1, 'Encrypted APK entries are unsupported')
            source.seek(record['local']); local = struct.unpack('<I5H3I2H', read_exact(source, 30))
            require(local[0] == 0x04034b50 and local[2] == record['flags'] and local[3] == record['method'], 'Local/central header mismatch: ' + name)
            require(read_exact(source, local[9]) == record['name'], 'Local/central filename mismatch: ' + name)
            read_exact(source, local[10]); data_start = source.tell(); data_end = data_start + info.compress_size
            descriptor_bytes = 0
            if info.flag_bits & 8:
                source.seek(data_end); descriptor = source.read(16)
                expected = (info.CRC, info.compress_size, info.file_size)
                if len(descriptor) >= 16 and descriptor[:4] == b'PK\x07\x08' and struct.unpack_from('<III', descriptor, 4) == expected: descriptor_bytes = 16
                elif len(descriptor) >= 12 and struct.unpack_from('<III', descriptor) == expected: descriptor_bytes = 12
                else: raise ValueError('Invalid ZIP data descriptor: ' + name)
            else:
                require((local[6], local[7], local[8]) == (info.CRC, info.compress_size, info.file_size), 'Local CRC/size mismatch: ' + name)
            require(record['local'] >= 0 and data_end + descriptor_bytes <= local_limit, 'Local entry overlaps APK signing block or central directory: ' + name)
            intervals.append((record['local'], data_end + descriptor_bytes, name))
            actual_crc = 0; actual_bytes = 0
            with archive.open(info) as payload:
                while data := payload.read(1024 * 1024): actual_crc = zlib.crc32(data, actual_crc); actual_bytes += len(data)
            require(actual_crc & 0xffffffff == info.CRC and actual_bytes == info.file_size, 'Actual CRC/raw byte mismatch: ' + name)
            crc_verified += 1; payload_identity[name] = {'crc32': '%08x' % info.CRC, 'rawBytes': info.file_size, 'compressedBytes': info.compress_size}
        gaps = []; previous_end = 0
        for start, end, name in sorted(intervals):
            require(start >= previous_end, 'Overlapping local entries: ' + name)
            if start > previous_end: gaps.append(dict(offset=previous_end, bytes=start - previous_end))
            previous_end = end
        if previous_end < local_limit: gaps.append(dict(offset=previous_end, bytes=local_limit - previous_end))
        require(previous_end <= local_limit, 'Local entry layout exceeds its allowed range')
        return dict(entries=len(infos), allEntryCrcVerified=True, crcVerifiedEntries=crc_verified,
                    compressedPayloadBytes=sum(i.compress_size for i in infos), rawPayloadBytes=sum(i.file_size for i in infos),
                    localEntryBytes=sum(end - start for start, end, _ in intervals), centralDirectoryOffset=central, centralDirectoryBytes=central_size,
                    eocdAndCommentBytes=size - eocd, signingBlock=block, signingBlockBytes=block['bytes'] if block else 0,
                    unreferencedBytesBeforeCentralDirectory=sum(g['bytes'] for g in gaps), gapCount=len(gaps), gaps=gaps, payloadIdentity=payload_identity)

def elf_id(data):
    if len(data) < 64 or data[:4] != b'\x7fELF' or data[4] not in (1, 2) or data[5] != 1: return ''
    elf64 = data[4] == 2
    offset = struct.unpack_from('<Q' if elf64 else '<I', data, 32 if elf64 else 28)[0]
    header_size, count = struct.unpack_from('<HH', data, 54 if elf64 else 42)
    if header_size < (56 if elf64 else 32) or offset + header_size * count > len(data): return ''
    for i in range(count):
        header = offset + i * header_size
        if struct.unpack_from('<I', data, header)[0] != 4: continue
        note = struct.unpack_from('<Q' if elf64 else '<I', data, header + (8 if elf64 else 4))[0]
        length = struct.unpack_from('<Q' if elf64 else '<I', data, header + (32 if elf64 else 16))[0]
        end = note + length
        if end > len(data): continue
        while note + 12 <= end:
            names, description_size, kind = struct.unpack_from('<III', data, note); name = note + 12; desc = name + ((names + 3) & ~3); after = desc + ((description_size + 3) & ~3)
            if after > end: break
            if kind == 3 and names == 4 and data[name:name + 4] == b'GNU\0' and description_size: return data[desc:desc + description_size].hex()
            note = after
    return ''

def native_entry(archive, entry):
    digest = hashlib.sha256(); prefix = bytearray(); total = 0
    with archive.open(entry) as stream:
        while data := stream.read(1024 * 1024):
            digest.update(data); total += len(data)
            if len(prefix) < 128 * 1024: prefix.extend(data[:128 * 1024 - len(prefix)])
    return dict(bytes=total, sha256=digest.hexdigest(), buildId=elf_id(prefix))

def native_symbols(apk, receipt, build, apk_artifact):
    raw = receipt.read_bytes(); manifest = json.loads(raw.decode('utf-8-sig'))
    require(receipt.parent.parent == apk.parent and receipt.parent.name == 'NativeSymbols', 'Symbol receipt must belong to this exact per-build APK folder')
    require(manifest.get('schema') == 'weedtopia_native_symbols_v1' and manifest.get('status') == 'passed' and manifest.get('nativeIdentitiesMatch') is True, 'Native-symbol receipt is not passed')
    require(manifest['apkFile'] == apk.name and manifest['apkSha256'] == apk_artifact['sha256'] and manifest['apkBytes'] == apk_artifact['bytes'], 'Symbols receipt describes a different APK')
    require(pathlib.Path(manifest['symbolFile']).name == manifest['symbolFile'], 'Unsafe symbol filename')
    symbol_path = (receipt.parent / manifest['symbolFile']).resolve(strict=True); symbol_artifact = artifact(symbol_path)
    require(symbol_artifact['sha256'] == manifest['symbolSha256'] and symbol_artifact['bytes'] == manifest['symbolBytes'], 'Archived symbol ZIP hash/size mismatch')
    require(manifest['runId'] == build.get('id', build.get('runId')), 'Symbol/build run IDs disagree')
    for field, build_field in [('packageName', 'packageName'), ('version', 'version'), ('versionCode', 'versionCode'), ('architectures', 'architectures'), ('backend', 'scriptingBackend')]:
        if build_field in build: require(manifest[field] == build[build_field], 'Symbol/build identity mismatch: ' + field)
    require(manifest['symbolLevel'] == 'Full' and manifest['symbolFormat'] == 'Zip', 'Full/ZIP symbols were not effective')
    verified = []
    with zipfile.ZipFile(apk) as player, zipfile.ZipFile(symbol_path) as symbols:
        native = {entry.filename: entry for entry in player.infolist() if entry.filename.startswith('lib/') and pathlib.PurePosixPath(entry.filename).name in CORE}
        require(len(native) == len(manifest['nativeLibraries']), 'Native receipt does not cover every APK core library')
        require(len({r['apkEntry'] for r in manifest['nativeLibraries']}) == len(native), 'Duplicate/missing core identity receipt')
        by_abi = {}
        for record in manifest['nativeLibraries']:
            entry = native[record['apkEntry']]; abi = entry.filename.split('/')[1]; library = pathlib.PurePosixPath(entry.filename).name
            require(abi == record['abi'] and library == record['library'], 'Native ABI/library metadata mismatch')
            matches = [e for e in symbols.infolist() if abi in e.filename.split('/') and pathlib.PurePosixPath(e.filename).name in (library, library + '.sym', library + '.dbg')]
            require(len(matches) == 1 and matches[0].filename == record['symbolEntry'], 'Missing/ambiguous native symbol entry')
            actual = native_entry(player, entry); debug = native_entry(symbols, matches[0])
            require(actual['buildId'] and actual['buildId'] == debug['buildId'] == record['apkBuildId'] == record['symbolBuildId'] and record['matched'] is True, 'Native ELF build IDs do not agree')
            for field in ('bytes', 'sha256'):
                require(actual[field] == record['apk' + ('Bytes' if field == 'bytes' else 'Sha256')] and debug[field] == record['symbol' + ('Bytes' if field == 'bytes' else 'Sha256')], 'Native identity hash/size mismatch')
            by_abi.setdefault(abi, set()).add(library)
            verified.append(dict(abi=abi, library=library, apkEntry=entry.filename, symbolEntry=matches[0].filename, apk=actual, symbols=debug, matched=True))
        required = CORE if manifest['backend'] == 'IL2CPP' else CORE - {'libil2cpp.so'}
        require(by_abi and all(required <= libraries for libraries in by_abi.values()), 'A native ABI is missing a required core library')
    return dict(manifest=artifact(receipt), symbolZip=symbol_artifact, runId=manifest['runId'], buildGuid=manifest['buildGuid'],
                buildOptions=manifest['buildOptions'], symbolLevel=manifest['symbolLevel'], symbolFormat=manifest['symbolFormat'], allActualNativeIdentitiesMatch=True, nativeLibraries=verified)

def sdk_verify(apk, output, android):
    java = android / 'OpenJDK/bin/java.exe'; candidates = [p for p in (android / 'SDK/build-tools').iterdir() if (p / 'lib/apksigner.jar').is_file()]
    require(java.is_file() and candidates, 'Existing Unity Java/apksigner installation is unavailable')
    tools = max(candidates, key=lambda p: tuple(int(n) for n in p.name.split('.') if n.isdigit()))
    commands = {'signature': [str(java), '-jar', str(tools / 'lib/apksigner.jar'), 'verify', '--verbose', '--print-certs', str(apk)],
                'alignment': [str(tools / 'zipalign.exe'), '-c', '-v', '4', str(apk)],
                'manifest': [str(tools / 'aapt.exe'), 'dump', 'badging', str(apk)]}
    results = {}
    for name, command in commands.items():
        run = subprocess.run(command, capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=180)
        log = output / ('sdk-' + name + '.txt'); log.write_text(run.stdout + run.stderr, encoding='utf-8')
        results[name] = dict(exitCode=run.returncode, log=artifact(log))
        require(run.returncode == 0, 'SDK ' + name + ' verification failed; inspect ' + str(log))
        if name == 'signature': results[name]['v2Verified'] = 'Verified using v2 scheme (APK Signature Scheme v2): true' in run.stdout
        if name == 'manifest': results[name]['debuggable'] = 'application-debuggable' in run.stdout
    return dict(buildTools=tools.name, java=artifact(java), apksigner=artifact(tools / 'lib/apksigner.jar'), results=results)

def settings_restore(build):
    diagnostic = build.get('buildType') in ('ReleaseDiagnostic', 'Development')
    required = ['SymbolLevel', 'SymbolFormat', 'ManagedStacktrace', 'ErrorStacktrace', 'ExceptionStacktrace']
    if diagnostic: required = ['TextureCompressionOverride', 'MaximumTextureSizeOverride'] + required
    additional = ['BuildAppBundle', 'ExportAsGoogleAndroidProject', 'SplitApplicationBinary', 'BuildApkPerCpuArchitecture',
                  'UseCustomKeystore', 'JvmHeapMb', 'TextureSubtarget', 'ApplicationErrorStacktrace', 'ApplicationExceptionStacktrace']
    if diagnostic: required += additional
    expected_effective = {'TextureCompressionOverride': 'NoOverride', 'MaximumTextureSizeOverride': 0,
                          'SymbolLevel': 'Full', 'SymbolFormat': 'Zip', 'ManagedStacktrace': 'MethodFileLineNumber',
                          'ErrorStacktrace': 'Full', 'ExceptionStacktrace': 'Full'}
    verified = {}
    for field in required:
        previous, restored, effective = ('previous' + field, 'restored' + field, 'effective' + field)
        require(previous in build and restored in build, 'Missing required before/restored setting: ' + field)
        require(type(build[previous]) is type(build[restored]) and build[previous] == build[restored], 'Build settings were not restored: ' + field)
        if field in expected_effective and (diagnostic or field not in ('SymbolLevel', 'SymbolFormat')):
            require(effective in build and build[effective] == expected_effective[field], 'Effective diagnostic setting was not proven: ' + field)
        verified[field] = {'previous': build[previous], 'restored': build[restored]}
        if effective in build: verified[field]['effective'] = build[effective]
    unrecorded = [] if diagnostic else ['Application.ErrorStacktrace', 'Application.ExceptionStacktrace']
    return dict(scope='temporary_overrides_in_AndroidTestBuild.Build' if diagnostic else 'recorded_ProductionReleaseBuild_overrides',
                fields=verified, allRecordedRequiredSettingsRestored=True, additionalFinallyRestoresWithoutResultFields=unrecorded,
                allBuildSettingsRestorationVerified=diagnostic)

def audit(args, output):
    apk = args.apk.resolve(strict=True); before = (apk.stat().st_size, apk.stat().st_mtime_ns); apk_info = artifact(apk)
    build_path = (args.build_result or apk.parent / 'build-result.json').resolve(strict=True); build = json.loads(build_path.read_text(encoding='utf-8-sig'))
    require(build['status'] in ('succeeded', 'built_pending_release_acceptance') and pathlib.Path(build.get('apkPath', build.get('apk', ''))).resolve() == apk, 'Successful build receipt does not describe this APK')
    if 'bytes' in build: require(build['bytes'] == apk_info['bytes'] and build.get('errors') == 0, 'Build result byte/error count disagrees')
    report = dict(apk=apk_info, buildResult=artifact(build_path), buildType=build.get('buildType', 'ProductionRelease'), settingsRestoration=settings_restore(build))
    report['layout'] = layout_and_crc(apk)
    require(report['layout']['unreferencedBytesBeforeCentralDirectory'] <= args.max_orphan_bytes, 'APK contains unreferenced package holes')
    report['sdk'] = sdk_verify(apk, output, args.android_player)
    if build.get('buildType') == 'ReleaseDiagnostic': require(not report['sdk']['results']['manifest']['debuggable'], 'ReleaseDiagnostic APK is unexpectedly debuggable')
    receipt = args.symbol_manifest or pathlib.Path(build.get('nativeSymbolReceipt', '') or apk.parent / 'NativeSymbols/symbols.json')
    report['nativeSymbols'] = native_symbols(apk, receipt.resolve(strict=True), build, apk_info)
    if build.get('buildType') == 'ReleaseDiagnostic': require(report['nativeSymbols']['buildOptions'] == 'None', 'ReleaseDiagnostic used Development options')
    if args.textures:
        tool = ROOT / 'Tools/Implementation/audit_android_building_textures.py'; spec = importlib.util.spec_from_file_location('weedtopia_texture_audit', tool)
        module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
        texture_output = output / 'building-textures.json'; module.audit(apk, texture_output, args.texture_baseline)
        textures = json.loads(texture_output.read_text(encoding='utf-8'))
        require(textures['textures'] == 251 and textures['actualEtc2CompressionVerified'] is True, 'Actual 251 ETC2 texture proof failed')
        report['buildingTextures'] = dict(report=artifact(texture_output), tool=artifact(tool), textures=textures['textures'], formats=textures['formats'], gpuImageBytes=textures['gpuImageBytes'], originalSourcesAndGuidsPreserved=textures.get('originalSourcesAndGuidsPreserved'))
    require(before == (apk.stat().st_size, apk.stat().st_mtime_ns), 'APK changed while being audited')
    report['apkUnmodifiedByAudit'] = True
    return report

def self_test(output):
    checks = []
    with tempfile.TemporaryDirectory(prefix='synthetic-zip-', dir=output) as folder:
        folder = pathlib.Path(folder); ordinary = folder / 'ordinary.zip'
        with zipfile.ZipFile(ordinary, 'w') as archive: archive.writestr('first.bin', b'first'); archive.writestr('second.bin', b'second')
        result = layout_and_crc(ordinary); require(result['unreferencedBytesBeforeCentralDirectory'] == 0 and result['crcVerifiedEntries'] == 2, 'Ordinary fixture failed'); checks.append('central/local/CRC exact')
        prefix = folder / 'prefix.zip'
        with prefix.open('wb') as source:
            source.write(b'ORPHAN00')
            with zipfile.ZipFile(source, 'w') as archive: archive.writestr('test.bin', b'value')
        require(layout_and_crc(prefix)['unreferencedBytesBeforeCentralDirectory'] == 8, 'Orphan detection failed'); checks.append('prefix orphan bytes detected')
        data = bytearray(ordinary.read_bytes()); central = result['centralDirectoryOffset']; block = struct.pack('<Q', 24) + struct.pack('<Q', 24) + MAGIC
        signed = folder / 'structural-signing-block.zip'; data[central:central] = block; eocd = data.rfind(b'PK\x05\x06'); struct.pack_into('<I', data, eocd + 16, central + len(block)); signed.write_bytes(data)
        measured = layout_and_crc(signed); require(measured['signingBlockBytes'] == 32 and measured['unreferencedBytesBeforeCentralDirectory'] == 0, 'Signing block accounting failed'); checks.append('synthetic structural block excluded (no cryptographic claim)')
        corrupt = folder / 'corrupt.zip'; data = bytearray(ordinary.read_bytes()); data[30 + len('first.bin')] ^= 1; corrupt.write_bytes(data)
        try: layout_and_crc(corrupt)
        except (ValueError, zipfile.BadZipFile): checks.append('corrupt payload CRC rejected')
        else: raise ValueError('CRC corruption fixture was accepted')
        class NonSeekable(io.BytesIO):
            def seek(self, *args): raise io.UnsupportedOperation('Nonseekable fixture')
        stream = NonSeekable()
        with zipfile.ZipFile(stream, 'w', zipfile.ZIP_DEFLATED) as archive: archive.writestr('descriptor.bin', b'repeated' * 100)
        descriptor = folder / 'descriptor.zip'; descriptor.write_bytes(stream.getvalue())
        require(layout_and_crc(descriptor)['unreferencedBytesBeforeCentralDirectory'] == 0, 'Descriptor fixture failed'); checks.append('data descriptor bounds/CRC matched')
        for elf64 in (False, True):
            data = bytearray(256); data[:6] = b'\x7fELF' + bytes([2 if elf64 else 1, 1]); header_size = 56 if elf64 else 32
            struct.pack_into('<Q' if elf64 else '<I', data, 32 if elf64 else 28, 64)
            struct.pack_into('<HH', data, 54 if elf64 else 42, header_size, 2)
            for i, note in enumerate((184, 216)):
                header = 64 + i * header_size; struct.pack_into('<I', data, header, 4)
                struct.pack_into('<Q' if elf64 else '<I', data, header + (8 if elf64 else 4), note)
                struct.pack_into('<Q' if elf64 else '<I', data, header + (32 if elf64 else 16), 20)
                struct.pack_into('<III', data, note, 4, 4, 1 if i == 0 else 3); data[note + 12:note + 16] = b'GNU\0'; data[note + 16:note + 20] = b'\x12\xab\x00\xef'
            require(elf_id(data) == '12ab00ef', 'Multiple-note ELF reader failed')
            struct.pack_into('<I', data, 220, 0xffffffff); require(not elf_id(data), 'Malformed ELF note was accepted')
        checks.append('ELF32/64 multiple PT_NOTE IDs and malformed lengths')
        apk = folder / 'WeedtopiaInc-Android-ReleaseDiagnostic.apk'; symbol_folder = folder / 'NativeSymbols'; symbol_folder.mkdir()
        symbol_zip = symbol_folder / 'WeedtopiaInc-Android-ReleaseDiagnostic-0.2.0-v7-IL2CPP.symbols.zip'
        identities = []
        with zipfile.ZipFile(apk, 'w') as player, zipfile.ZipFile(symbol_zip, 'w') as symbols:
            for abi, elf64 in [('armeabi-v7a', False), ('arm64-v8a', True)]:
                for library in sorted(CORE):
                    data = bytearray(256); data[:6] = b'\x7fELF' + bytes([2 if elf64 else 1, 1])
                    struct.pack_into('<Q' if elf64 else '<I', data, 32 if elf64 else 28, 64)
                    struct.pack_into('<HH', data, 54 if elf64 else 42, 56 if elf64 else 32, 1)
                    struct.pack_into('<I', data, 64, 4)
                    struct.pack_into('<Q' if elf64 else '<I', data, 64 + (8 if elf64 else 4), 184)
                    struct.pack_into('<Q' if elf64 else '<I', data, 64 + (32 if elf64 else 16), 36)
                    struct.pack_into('<III', data, 184, 4, 20, 3); data[196:200] = b'GNU\0'
                    data[200:220] = hashlib.sha256((abi + '/' + library).encode()).digest()[:20]
                    apk_entry = 'lib/' + abi + '/' + library
                    symbol_entry = abi + '/' + library + ('.dbg' if library == 'libil2cpp.so' else '.sym')
                    debug = bytes(data) + b'synthetic-debug-content'; player.writestr(apk_entry, data); symbols.writestr(symbol_entry, debug)
                    identities.append(dict(abi=abi, library=library, apkEntry=apk_entry, symbolEntry=symbol_entry,
                                           apkBytes=len(data), symbolBytes=len(debug), apkSha256=hashlib.sha256(data).hexdigest(),
                                           symbolSha256=hashlib.sha256(debug).hexdigest(), apkBuildId=elf_id(data), symbolBuildId=elf_id(debug), matched=True))
        build = dict(id='synthetic-full-symbols', buildType='ReleaseDiagnostic', packageName='synthetic.fixture', version='0.2.0',
                     versionCode=7, architectures='ARMv7, ARM64', scriptingBackend='IL2CPP')
        manifest = dict(schema='weedtopia_native_symbols_v1', status='passed', nativeIdentitiesMatch=True,
                        runId=build['id'], buildGuid='synthetic-guid', packageName=build['packageName'], version=build['version'],
                        versionCode=7, architectures=build['architectures'], backend='IL2CPP', buildOptions='None', symbolLevel='Full',
                        symbolFormat='Zip', apkFile=apk.name, apkSha256=sha(apk), apkBytes=apk.stat().st_size,
                        symbolFile=symbol_zip.name, symbolSha256=sha(symbol_zip), symbolBytes=symbol_zip.stat().st_size, nativeLibraries=identities)
        receipt = symbol_folder / 'symbols.json'; receipt.write_text(json.dumps(manifest), encoding='utf-8')
        native_result = native_symbols(apk, receipt, build, artifact(apk))
        require(native_result['allActualNativeIdentitiesMatch'] and len(native_result['nativeLibraries']) == 6, 'Positive Full symbols ZIP fixture failed')
        checks.append('positive Full ZIP Unity filename: public .so.sym and IL2CPP .so.dbg; ELF32/64, both ABIs, six actual hashes/IDs')
        manifest['nativeLibraries'][0]['symbolBuildId'] = 'different'; receipt.write_text(json.dumps(manifest), encoding='utf-8')
        try: native_symbols(apk, receipt, build, artifact(apk))
        except ValueError: checks.append('Full symbol receipt with wrong ELF ID rejected')
        else: raise ValueError('Wrong Full-symbol ELF identity was accepted')
        effective = {'TextureCompressionOverride': 'NoOverride', 'MaximumTextureSizeOverride': 0, 'SymbolLevel': 'Full',
                     'SymbolFormat': 'Zip', 'ManagedStacktrace': 'MethodFileLineNumber', 'ErrorStacktrace': 'Full', 'ExceptionStacktrace': 'Full'}
        for field, value in effective.items():
            build['previous' + field] = value; build['restored' + field] = value; build['effective' + field] = value
        for field, value in {'BuildAppBundle': False, 'ExportAsGoogleAndroidProject': False, 'SplitApplicationBinary': False,
                             'BuildApkPerCpuArchitecture': False, 'UseCustomKeystore': True, 'JvmHeapMb': 4096,
                             'TextureSubtarget': 'ASTC', 'ApplicationErrorStacktrace': 'ScriptOnly', 'ApplicationExceptionStacktrace': 'None'}.items():
            build['previous' + field] = value; build['restored' + field] = value
        require(len(settings_restore(build)['fields']) == 16 and settings_restore(build)['allBuildSettingsRestorationVerified'], 'Required restoration fields fixture failed')
        build['restoredUseCustomKeystore'] = False
        try: settings_restore(build)
        except ValueError: checks.append('changed keystore restoration rejected')
        else: raise ValueError('Incorrect keystore restoration was accepted')
        build['restoredUseCustomKeystore'] = True
        del build['previousSymbolLevel']
        try: settings_restore(build)
        except ValueError: checks.append('all 16 diagnostic restoration pairs plus seven effective overrides verified; missing required evidence rejected')
        else: raise ValueError('Missing restoration evidence was accepted')
    return dict(status='passed', gate='synthetic_parser_self_test_only', checks=checks, actualApkVerified=False, nativeBuildExecuted=False)

def main():
    parser = argparse.ArgumentParser(description=__doc__); parser.add_argument('apk', nargs='?', type=pathlib.Path)
    parser.add_argument('--output', type=pathlib.Path, required=True); parser.add_argument('--build-result', type=pathlib.Path); parser.add_argument('--symbol-manifest', type=pathlib.Path)
    parser.add_argument('--android-player', type=pathlib.Path, default=ANDROID); parser.add_argument('--max-orphan-bytes', type=int, default=0)
    parser.add_argument('--textures', action='store_true'); parser.add_argument('--texture-baseline', type=pathlib.Path); parser.add_argument('--self-test', action='store_true'); args = parser.parse_args()
    output = args.output.resolve()
    require(not any((ROOT / name).resolve() == output or (ROOT / name).resolve() in output.parents for name in ('Assets', 'Packages', 'ProjectSettings', 'Library', 'Temp')), 'Audit outputs must stay outside Unity inputs/cache')
    output.mkdir(parents=True, exist_ok=True); report = dict(schema='weedtopia_release_apk_audit_v1', utc=datetime.datetime.now(datetime.timezone.utc).isoformat(), status='failed', tool=artifact(pathlib.Path(__file__).resolve()), physicalDeviceAcceptance=False, humanAcceptance=False, releaseAccepted=False)
    try:
        if args.self_test: report.update(self_test(output))
        else:
            require(args.apk is not None, 'An actual completed APK path is required'); report.update(audit(args, output)); report['status'] = 'passed'; report['gate'] = 'actual_artifact_verification_only'
    except Exception as error: report['failure'] = dict(type=type(error).__name__, detail=str(error))
    (output / 'apk-audit.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({key: report[key] for key in ('status', 'gate', 'failure', 'physicalDeviceAcceptance', 'releaseAccepted') if key in report}, indent=2))
    return 0 if report['status'] == 'passed' else 1

if __name__ == '__main__': sys.exit(main())
