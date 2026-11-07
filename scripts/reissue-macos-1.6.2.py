from pathlib import Path
from zipfile import ZipFile
import hashlib
import json
import plistlib
import subprocess
import sys

root=Path(__file__).resolve().parent.parent/'artifacts/release-1.6.2-signed'
if '--verify-published' in sys.argv:
    expected=json.loads((root/'verified-hashes.json').read_text(encoding='utf-8'))
    expected['SHA256SUMS.txt']=hashlib.sha256((root/'SHA256SUMS.txt').read_bytes()).hexdigest()
    published=json.loads(subprocess.check_output(['gh','api','repos/gruetze-software/BingWallE/releases/tags/v1.6.2']))
    for name,digest in expected.items():
        asset=next(a for a in published['assets'] if a['name']==name)
        assert asset['digest']=='sha256:'+digest,f'Published checksum differs: {name}'
    assert published['body']==(root/'release-notes.md').read_text(encoding='utf-8')
    tag=json.loads(subprocess.check_output(['gh','api','repos/gruetze-software/BingWallE/git/ref/tags/v1.6.2']))
    obj=tag['object']
    if obj['type']=='tag':
        obj=json.loads(subprocess.check_output(['gh','api',f"repos/gruetze-software/BingWallE/git/tags/{obj['sha']}"]))['object']
    assert obj['sha']=='ef8222ec33768c08d6a1a5667d2e20473c89fb79'
    (root/'release-after.json').write_text(json.dumps(published,indent=2)+'\n',encoding='utf-8')
    print('All eight published asset checksums match; Windows/Linux and version tag are unchanged.')
    sys.exit(0)
before=json.loads((root/'release-before.json').read_text(encoding='utf-8-sig'))
old_lines=(root/'backup/SHA256SUMS.txt').read_text(encoding='utf-8').splitlines()
hashes={line.split(None,1)[1].strip():line.split(None,1)[0] for line in old_lines if line.strip()}
assert len(hashes)==7
new_names=[]
for arch in ('arm64','x64'):
    name=f'BingWallE-1.6.2-macos-{arch}.zip'
    with ZipFile(root/'backup'/name) as old, ZipFile(root/arch/name) as new:
        for filename in ('bingwalle.dll','bingwalle.deps.json','bingwalle.runtimeconfig.json'):
            path='BingWallE.app/Contents/MacOS/'+filename
            assert old.read(path)==new.read(path),f'Application differs: {arch}/{filename}'
        info=plistlib.loads(new.read('BingWallE.app/Contents/Info.plist'))
        assert info['CFBundleShortVersionString']=='1.6.2'
        assert info['CFBundleVersion']=='1.6.2'
        assert info['LSMinimumSystemVersion']=='14.0'
        assert 'BingWallE.app/Contents/_CodeSignature/CodeResources' in new.namelist()
    for suffix in ('zip','dmg'):
        name=f'BingWallE-1.6.2-macos-{arch}.{suffix}'
        old_hash=hashlib.file_digest((root/'backup'/name).open('rb'),'sha256').hexdigest()
        assert old_hash==hashes[name],f'Old checksum mismatch: {name}'
        hashes[name]=hashlib.file_digest((root/arch/name).open('rb'),'sha256').hexdigest()
        new_names.append(name)
unchanged=[n for n in hashes if n not in new_names]
for asset in before['assets']:
    if asset['name'] in unchanged:
        assert asset['digest']=='sha256:'+hashes[asset['name']],asset['name']
(root/'SHA256SUMS.txt').write_text(''.join(f'{digest}  {name}\n' for name,digest in hashes.items()),encoding='utf-8')
note='''## Signed macOS downloads

On 7 November 2026, the macOS ARM64 and Intel downloads were replaced with
Developer ID-signed and Apple-notarized ZIP and DMG packages. The application
assembly is identical to the original 1.6.2 release; the packages were rebuilt
with the updated signing tools from commit
[`a656d02021b056e3b4de36d109fa0088a21d0a9c`](https://github.com/gruetze-software/BingWallE/commit/a656d02021b056e3b4de36d109fa0088a21d0a9c).
Both architectures passed signature, notarization-ticket and Gatekeeper checks
in the [signed build](https://github.com/gruetze-software/BingWallE/actions/runs/42360123161).
`SHA256SUMS.txt` has been updated for the replacement files.

'''
body=before['body']
assert '## Signed macOS downloads' not in body
body=body.replace('## Linux stores',note+'## Linux stores')
(root/'release-notes.md').write_text(body,encoding='utf-8')
(root/'verified-hashes.json').write_text(json.dumps(hashes,indent=2)+'\n',encoding='utf-8')
print('Both application assemblies match the original 1.6.2 byte for byte.')
print('Verified original checksums; updated four macOS hashes and preserved three others.')
