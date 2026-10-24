# com.hadal.protobuf-net

Vendored [protobuf-net](https://github.com/protobuf-net/protobuf-net) **3.2.52** for Unity 6.

## Source

| Assembly | NuGet |
|----------|-------|
| protobuf-net.dll | `protobuf-net` 3.2.52 |
| protobuf-net.Core.dll | `protobuf-net.Core` 3.2.52 |
| System.Collections.Immutable.dll | `System.Collections.Immutable` 8.0.0 |
| System.Runtime.CompilerServices.Unsafe.dll | `System.Runtime.CompilerServices.Unsafe` 6.0.0 (`lib/netstandard2.0`) — required by Immutable 8.0; without it Unity refuses to load protobuf-net and every assembly that references it |

Refresh DLLs (Windows):

```bat
shared\build_and_copy.bat
```

Or re-run the NuGet extract script documented in Sprint 3 changelog.

## Unity asmdef

Reference assembly name: **`ProtobufNet`**
