import ntpath
import posixpath
import sys
import os.path
import json
import shutil
import subprocess
import hashlib
import tarfile
import zipfile
import time


def find_basedir():
    base = os.path.abspath(os.path.dirname(__file__))
    tries = 10

    while not os.path.isfile(os.path.join(base, 'DEPS.py')):
        base = os.path.dirname(base)
        if not os.path.isdir(base):
            return False

        tries -= 1
        if tries < 1:
            return False

    return base


def safe_rmtree(path):
    tries = 30
    while tries > 0:
        tries -= 1

        try:
            shutil.rmtree(path)
            break
        except Exception:
            if tries <= 0:
                raise

            time.sleep(0.3)


def main(update_hashes=False):
    base = find_basedir()
    scope = {}

    if not base:
        print('ERROR: DEPS.py not found!')
        sys.exit(1)

    deps_path = os.path.join(base, 'DEPS.py')
    with open(deps_path, 'r', encoding='utf-8') as stream:
        exec(stream.read(), scope)

    deps = scope.get('deps', {})
    cache = {}
    cache_path = os.path.join(base, 'DEPS.cache')
    dl_path = os.path.join(base, '.deps_dl')

    if os.path.isfile(cache_path):
        with open(cache_path, 'r', encoding='utf-8') as stream:
            cache = json.load(stream)

    old = set(cache.keys())
    new = set(deps.keys())

    missing = new - old
    obsolete = old - new
    outdated = set()

    for key, meta in deps.items():
        if not os.path.isdir(os.path.join(base, meta['dest'])):
            missing.add(key)
        elif 'hash' in meta and key in old and cache[key].get('hash', (None, None))[1] != meta['hash'][1] or update_hashes:
            outdated.add(key)

    if os.path.isdir(dl_path):
        print('Removing left overs...')
        safe_rmtree(dl_path)

    os.mkdir(dl_path)
    rep_map = {}

    try:
        if missing | outdated:
            print('Fetching missing or outdated dependencies...')
            count = len(missing | outdated)
            for i, key in enumerate(missing | outdated):
                print('[%3d/%3d]: %s' % (i + 1, count, key))

                meta = deps[key]
                dlname = os.path.join(dl_path, os.path.basename(meta['url']).split('.', 1)[1])
                link = meta['url'].split('#')[0]
                dest = os.path.join(base, meta['dest'])

                subprocess.check_call(['curl', '-Lo', dlname, link])

                if 'hash' in meta:
                    print('Hashing...')
                    h = hashlib.new(meta['hash'][0])

                    with open(dlname, 'rb') as stream:
                        while data := stream.read(16 * 1024):
                            h.update(data)

                    if update_hashes:
                        rep_map[meta['hash'][1]] = meta['hash'][1] = h.hexdigest()

                    elif h.hexdigest() != meta['hash'][1]:
                        print('ERROR: %s failed the hash check.' % key)
                        break

                if os.path.isdir(dest):
                    print('Removing old files...')
                    safe_rmtree(dest)

                print('Extracting...')
                if meta['url'].endswith('.zip'):
                    with zipfile.ZipFile(dlname) as archive:
                        top_dir = os.path.commonprefix([x.filename for x in archive.filelist])
                        for member in archive.filelist:
                            if member.is_dir():
                                continue
                            local_path = archive_file_output_path(member.filename, dest, top_dir)
                            os.makedirs(os.path.dirname(local_path), exist_ok=True)
                            with open(local_path, 'wb+') as local, archive.open(member) as z:
                                local.write(z.read())
                elif meta['url'].endswith(('.tar.gz', '.tar.xz', '.tgz', '.txz', '.tar')):
                    with tarfile.open(dlname) as archive:
                        top_dir = os.path.commonprefix([x.name for x in archive.getmembers()])
                        for member in archive.getmembers():
                            if member.isdir():
                                continue
                            local_path = archive_file_output_path(member.name, dest, top_dir)
                            os.makedirs(os.path.dirname(local_path), exist_ok=True)
                            with open(local_path, 'wb+') as local, archive.extractfile(member.name) as z:
                                local.write(z.read())
                else:
                    print('ERROR: %s has an unknown archive type!' % meta['url'])
                    continue
                cache[key] = meta

        if obsolete:
            print('Removing old dependencies...')
            count = len(obsolete)
            for i, key in enumerate(obsolete):
                print('[%3d/%3d]: %s' % (i + 1, count, key))

                dest = os.path.join(base, meta['dest'])

                if os.path.isdir(dest):
                    safe_rmtree(dest)

                del cache[key]

        if not missing | outdated | obsolete:
            print('Nothing to do.')

    finally:
        print('Saving dependency cache...')
        with open(cache_path, 'w', newline='\n') as stream:
            json.dump(cache, stream)

        if rep_map:
            print('Updating hashes...')
            print(rep_map)

            with open(deps_path, 'r', encoding='utf-8') as stream:
                data = stream.read()

            for old, new in rep_map.items():
                data = data.replace(old, new)

            with open(deps_path, 'w', newline='\n') as stream:
                stream.write(data)

        print('Cleaning up...')
        safe_rmtree(dl_path)


def archive_file_output_path(path: str, dest_path: str, archive_top_dir: str) -> str:
    # normpath only convert '/' to '\' on windows.
    local_path = os.path.join(dest_path, os.path.relpath(path.replace(ntpath.sep, posixpath.sep), archive_top_dir))
    return os.path.normpath(local_path)


if __name__ == '__main__':
    if '-h' in sys.argv or '--help' in sys.argv:
        print('Usage: python fetch_deps.py [--update-hashes|-u]')
        sys.exit(0)

    main(update_hashes=('--update-hashes' in sys.argv or '-u' in sys.argv))
