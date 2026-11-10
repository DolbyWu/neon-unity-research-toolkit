"""Static C# checks with real Unity assemblies; does not import/run a Unity project.
Usage: python3 Tools/Compile-UnitySources.py --unity /path/to/Unity.app/Contents --cache /path/to/Library/PackageCache
An optional --sdk-runtime-dll compiles the adapter against a real SDK assembly produced by Unity.
"""
import argparse
from pathlib import Path
import subprocess


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--unity', required=True, type=Path)
    parser.add_argument('--cache', required=True, type=Path)
    parser.add_argument('--out', type=Path, default=Path('work/unity-static-compile'))
    parser.add_argument('--sdk-runtime-dll', type=Path)
    args = parser.parse_args()
    unity, cache, output = args.unity.resolve(), args.cache.resolve(), args.out.resolve()
    output.mkdir(parents=True, exist_ok=True)
    root = Path(__file__).resolve().parent.parent
    scripting = unity / 'Resources/Scripting'
    managed = scripting / 'Managed'
    api = scripting / 'UnityReferenceAssemblies/unity-4.8-api'
    # UnityEditor.dll is a compatibility facade: do not combine it with the concrete modules.
    refs = list(api.glob('*.dll')) + list((api/'Facades').glob('*.dll'))
    refs += list((managed/'UnityEngine').glob('*.dll'))
    refs += [p for p in managed.glob('UnityEditor*.dll') if p.name != 'UnityEditor.dll']
    refs += [next(p for p in cache.rglob('Newtonsoft.Json.dll') if '/AOT/' not in str(p))]
    command = [str(scripting/'NetCoreRuntime/dotnet'), str(scripting/'DotNetSdkRoslyn/csc.dll')]
    common = ['-nologo', '-target:library', '-langversion:9', '-nostdlib+',
              '-define:UNITY_EDITOR,UNITY_EDITOR_OSX,UNITY_6000_3_OR_NEWER']
    common += ['-r:'+str(p) for p in refs]
    package = root/'Packages/com.dolbywu.neon-research'
    groups = [
        ('NeonResearch.Runtime', list((package/'Runtime').glob('*.cs')), []),
        ('NeonResearch.Editor', list((package/'Editor').glob('*.cs')), [output/'NeonResearch.Runtime.dll']),
        ('NeonResearch.Editor.Tests', list((package/'Tests/Editor').glob('*.cs')),
         [output/'NeonResearch.Runtime.dll', output/'NeonResearch.Editor.dll', next(cache.rglob('nunit.framework.dll'))]),
        ('GazeFlow.ThreeTargets.Sample', list((package/'Samples~/ThreeTargets/Editor').glob('*.cs')),
         [output/'NeonResearch.Runtime.dll'])]
    if args.sdk_runtime_dll:
        groups.append(('GazeFlow.NeonXR', list((root/'Packages/com.binyanxu.gazeflow.neonxr/Runtime').glob('*.cs')),
                       [output/'NeonResearch.Runtime.dll', args.sdk_runtime_dll.resolve()]))
    for name, sources, extra in groups:
        options = common + ['-out:'+str(output/(name+'.dll'))]
        options += ['-r:'+str(p) for p in extra] + [str(p.resolve()) for p in sources]
        response = output/(name+'.rsp')
        response.write_text('\n'.join('"'+option+'"' for option in options))
        result = subprocess.run(command+['@'+str(response)], text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
        (output/(name+'.log')).write_text(result.stdout)
        print(name+': '+('PASS (static compilation only)' if result.returncode == 0 else 'FAIL'))
        if result.stdout:
            print(result.stdout)
        if result.returncode:
            raise SystemExit(result.returncode)
    if not args.sdk_runtime_dll:
        print('Optional Neon XR adapter NOT compiled: supply a real SDK assembly with --sdk-runtime-dll.')


if __name__ == '__main__':
    main()
