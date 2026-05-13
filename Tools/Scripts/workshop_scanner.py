import ctypes
import json
import re
import struct
import sys
from io import BytesIO
from pathlib import Path

# ---------------------------------------------------------------------------
# LZO decompressor
# ---------------------------------------------------------------------------

_lzo = None


def log(msg):
    print(msg, flush=True)


def _get_lzo():
    global _lzo

    if _lzo is not None:
        return _lzo

    candidates = [
        "lzo2.dll",
        "./lzo2.dll",
        str(Path(__file__).parent / "lzo2.dll"),
    ]

    for dll in candidates:
        try:
            lib = ctypes.CDLL(dll)
            lib.lzo1x_decompress_safe.restype = ctypes.c_int
            lib.lzo1x_decompress_safe.argtypes = [
                ctypes.c_char_p,
                ctypes.c_ulong,
                ctypes.c_char_p,
                ctypes.POINTER(ctypes.c_ulong),
                ctypes.c_void_p,
            ]

            _lzo = lib
            log(f"    LZO DLL загружена: {dll}")
            return _lzo
        except Exception:
            pass

    return None


def pbo_lzo_decompress(data: bytes, original_size: int) -> bytes | None:
    lzo = _get_lzo()

    if lzo is None:
        return None

    for offset in range(0, 32):
        chunk = data[offset:]

        out = ctypes.create_string_buffer(original_size + 1024)
        out_len = ctypes.c_ulong(original_size + 1024)

        ret = lzo.lzo1x_decompress_safe(
            chunk,
            len(chunk),
            out,
            ctypes.byref(out_len),
            None
        )

        if ret == 0 and out_len.value > 0:
            log(f"    LZO распаковано со смещением {offset} б")
            return out.raw[:out_len.value]

    return None


# ---------------------------------------------------------------------------
# Config parsing
# ---------------------------------------------------------------------------

CONFIG_NAMES = {"config.cpp", "config.bin"}

TARGET_SECTIONS = {
    "CfgVehicles": "Vehicle",
    "CfgWeapons": "Weapon",
    "CfgMagazines": "Magazine",
    "CfgAmmo": "Ammo",
}

CLASS_RE = re.compile(
    r"\bclass\s+([A-Za-z_][A-Za-z0-9_]*)\b"
    r"(?:\s*:\s*([A-Za-z_][A-Za-z0-9_]*))?"
    r"\s*[{;]"
)

SECTION_RE_TEMPLATE = r"class\s+{section}\s*\{{"
RAPIFIED_MAGIC = b"\x00raP"


def read_text_bytes(data: bytes) -> str:
    for enc in ("utf-8", "cp1251", "latin1"):
        try:
            return data.decode(enc, errors="ignore")
        except Exception:
            pass
    return ""


def strip_comments(text: str) -> str:
    text = re.sub(r"//[^\n]*", "", text)
    text = re.sub(r"/\*.*?\*/", "", text, flags=re.DOTALL)
    return text


def extract_brace_block(text: str, start_index: int) -> str:
    open_pos = text.find("{", start_index)

    if open_pos < 0:
        return ""

    depth = 0

    for i in range(open_pos, len(text)):
        ch = text[i]

        if ch == "{":
            depth += 1
        elif ch == "}":
            depth -= 1

            if depth == 0:
                return text[open_pos + 1:i]

    return ""


def find_class_declarations(block: str) -> list[dict]:
    result = []
    i = 0

    while i < len(block):
        m = CLASS_RE.search(block, i)

        if not m:
            break

        classname = m.group(1)
        parent = m.group(2) or ""

        brace_pos = block.find("{", m.end() - 1)

        if brace_pos < 0:
            result.append({
                "classname": classname,
                "parent": parent,
                "body": ""
            })
            i = m.end()
            continue

        body = extract_brace_block(block, m.start())

        result.append({
            "classname": classname,
            "parent": parent,
            "body": body
        })

        end_pos = brace_pos + len(body) + 2
        i = max(end_pos, m.end())

    return result


def read_int_property(body: str, prop: str):
    m = re.search(rf"\b{re.escape(prop)}\s*=\s*(-?\d+)\s*;", body)

    if not m:
        return None

    try:
        return int(m.group(1))
    except Exception:
        return None


def read_string_property(body: str, prop: str) -> str:
    m = re.search(rf'\b{re.escape(prop)}\s*=\s*"([^"]*)"\s*;', body)

    if not m:
        return ""

    return m.group(1)


def resolve_display_name(classname: str, index: dict) -> str:
    visited = set()
    current = classname.lower()

    for _ in range(16):
        if current in visited:
            break

        visited.add(current)

        node = index.get(current)

        if not node:
            break

        display_name = node.get("display_name", "")

        if display_name:
            return display_name

        parent = node.get("parent", "").lower()

        if not parent or parent == current:
            break

        current = parent

    return classname


def parse_config_text(text: str, source_mod: str, source_file: str) -> dict:
    result = {}
    text = strip_comments(text)

    for section, category in TARGET_SECTIONS.items():
        pattern = SECTION_RE_TEMPLATE.format(section=re.escape(section))
        m = re.search(pattern, text)

        if not m:
            continue

        section_block = extract_brace_block(text, m.start())

        if not section_block:
            continue

        classes = find_class_declarations(section_block)

        index = {}

        for cls in classes:
            key = cls["classname"].lower()
            index[key] = {
                "display_name": read_string_property(cls["body"], "displayName"),
                "parent": cls["parent"],
            }

        added = 0

        for cls in classes:
            classname = cls["classname"]
            parent = cls["parent"]
            body = cls["body"]

            scope = read_int_property(body, "scope")

            if scope != 2:
                continue

            display_name = resolve_display_name(classname, index) or classname

            result[classname.lower()] = {
                "ClassName": classname,
                "DisplayName": display_name,
                "Category": category,
                "SourceMod": source_mod,
                "SourceFile": source_file,
                "Parent": parent,
                "Scope": scope,
            }

            added += 1

        if added:
            log(f"    [{section}] scope=2 добавлено: {added}")

    return result


# ---------------------------------------------------------------------------
# Fallback string scanner for hard config.bin
# ---------------------------------------------------------------------------

def parse_config_bin_fallback_strings(data: bytes, source_mod: str, source_file: str) -> dict:
    result = {}

    raw = re.findall(rb"[A-Za-z_][A-Za-z0-9_]{3,80}", data)
    strings = [s.decode("latin1", errors="ignore") for s in raw]

    good_prefixes = (
        "ALV_",
        "ACO_",
        "Alevaric_",
        "Ammo_",
        "Mag_",
        "DZ_",
    )

    bad_words = (
        "Cfg",
        "config",
        "scope",
        "model",
        "displayName",
        "hiddenSelections",
        "texture",
        "material",
        "sound",
        "DamageSystem",
        "GlobalHealth",
        "Health",
        "class",
        "parent",
        "proxy",
        "skeleton",
    )

    lower_file = source_file.lower()
    category = "Workshop"

    if any(x in lower_file for x in ["pants", "jacket", "tops", "vest", "gloves", "shoes", "belts", "scarves", "armband"]):
        category = "Clothing"
    elif any(x in lower_file for x in ["headgear", "masks", "glasses"]):
        category = "Clothing"
    elif "backpacks" in lower_file:
        category = "Backpack"

    for s in strings:
        lower = s.lower()

        if any(b.lower() in lower for b in bad_words):
            continue

        if not any(s.startswith(p) for p in good_prefixes):
            continue

        result[lower] = {
            "ClassName": s,
            "DisplayName": s,
            "Category": category,
            "SourceMod": source_mod,
            "SourceFile": source_file,
            "Parent": "",
            "Scope": -1,
        }

    log(f"    fallback strings: принято={len(result)}")
    return result


# ---------------------------------------------------------------------------
# Rapified config.bin parser
# ---------------------------------------------------------------------------

class RapParser:
    def __init__(self, data: bytes):
        self.buf = BytesIO(data)

    def read_byte(self) -> int:
        b = self.buf.read(1)

        if not b:
            raise EOFError("Unexpected end of data")

        return b[0]

    def read_uint32(self) -> int:
        return struct.unpack("<I", self.buf.read(4))[0]

    def read_int32(self) -> int:
        return struct.unpack("<i", self.buf.read(4))[0]

    def read_float(self) -> float:
        return struct.unpack("<f", self.buf.read(4))[0]

    def read_cstring(self) -> str:
        result = bytearray()

        while True:
            b = self.buf.read(1)

            if not b or b == b"\x00":
                break

            result.extend(b)

        return result.decode("latin1", errors="ignore")

    def read_compressed_int(self) -> int:
        result = 0
        shift = 0

        while True:
            b = self.read_byte()
            result |= (b & 0x7F) << shift

            if not (b & 0x80):
                break

            shift += 7

        return result

    def skip_value(self, vtype: int):
        if vtype == 0:
            self.read_cstring()
        elif vtype == 1:
            self.buf.read(4)
        elif vtype == 2:
            self.buf.read(4)
        elif vtype == 3:
            self.skip_array()
        elif vtype == 6:
            self.buf.read(8)

    def skip_array(self):
        count = self.read_compressed_int()

        for _ in range(count):
            vtype = self.read_byte()
            self.skip_value(vtype)

    def read_class_body(self, offset: int) -> dict:
        saved = self.buf.tell()
        self.buf.seek(offset)

        parent = self.read_cstring()
        entry_count = self.read_compressed_int()

        scope = None
        display_name = ""
        children = []

        for _ in range(entry_count):
            try:
                etype = self.read_byte()
            except EOFError:
                break

            if etype == 0:
                child_name = self.read_cstring()
                child_offset = self.read_uint32()
                children.append((child_name, child_offset))
            elif etype == 1:
                vtype = self.read_byte()
                prop_name = self.read_cstring()
                prop_lower = prop_name.lower()

                if vtype == 0:
                    val = self.read_cstring()

                    if prop_lower == "displayname":
                        display_name = val
                elif vtype == 1:
                    val = self.read_float()

                    if prop_lower == "scope":
                        scope = int(round(val))
                elif vtype == 2:
                    val = self.read_int32()

                    if prop_lower == "scope":
                        scope = val
                else:
                    self.skip_value(vtype)
            elif etype == 2:
                self.read_cstring()
                self.skip_array()
            elif etype == 3:
                self.read_cstring()
            elif etype == 4:
                self.read_cstring()
            elif etype == 5:
                self.buf.read(4)
                self.read_cstring()
                self.skip_array()
            else:
                break

        self.buf.seek(saved)

        return {
            "parent": parent,
            "scope": scope,
            "display_name": display_name,
            "children": children,
        }

    def parse_top_level(self) -> list[tuple[str, int]]:
        magic = self.buf.read(4)

        if magic != RAPIFIED_MAGIC:
            return []

        self.buf.read(4)
        self.buf.read(4)
        self.buf.read(4)

        self.read_cstring()

        entry_count = self.read_compressed_int()

        top = []

        for _ in range(entry_count):
            try:
                etype = self.read_byte()
            except EOFError:
                break

            if etype == 0:
                name = self.read_cstring()
                offset = self.read_uint32()
                top.append((name, offset))
            elif etype == 1:
                vtype = self.read_byte()
                self.read_cstring()
                self.skip_value(vtype)
            elif etype == 2:
                self.read_cstring()
                self.skip_array()
            elif etype == 3:
                self.read_cstring()
            elif etype == 4:
                self.read_cstring()
            elif etype == 5:
                self.buf.read(4)
                self.read_cstring()
                self.skip_array()
            else:
                break

        return top


def build_class_index(parser: RapParser, children: list[tuple[str, int]]) -> dict:
    index = {}
    stack = list(children)

    while stack:
        name, offset = stack.pop()

        if not name:
            continue

        try:
            body = parser.read_class_body(offset)
        except Exception:
            continue

        index[name.lower()] = {
            "classname": name,
            "parent": body["parent"],
            "scope": body["scope"],
            "display_name": body["display_name"],
        }

        for child_name, child_offset in body["children"]:
            stack.append((child_name, child_offset))

    return index


def parse_config_bin(data: bytes, source_mod: str, source_file: str) -> dict:
    offset = data.find(RAPIFIED_MAGIC, 0, 16)

    if offset < 0:
        log("    config.bin: не rapified формат, fallback")
        return parse_config_bin_fallback_strings(data, source_mod, source_file)

    if offset > 0:
        data = data[offset:]

    result = {}

    try:
        parser = RapParser(data)
        top_classes = parser.parse_top_level()
    except Exception as ex:
        log(f"    config.bin: ошибка парсинга — {ex}")
        return parse_config_bin_fallback_strings(data, source_mod, source_file)

    for section, category in TARGET_SECTIONS.items():
        section_entry = next((t for t in top_classes if t[0] == section), None)

        if not section_entry:
            continue

        _, section_offset = section_entry

        try:
            section_body = parser.read_class_body(section_offset)
        except Exception as ex:
            log(f"    config.bin: ошибка чтения {section} — {ex}")
            continue

        index = build_class_index(parser, section_body["children"])

        added = 0

        for key, node in index.items():
            if node["scope"] != 2:
                continue

            classname = node["classname"]
            display_name = resolve_display_name(classname, index) or classname

            result[key] = {
                "ClassName": classname,
                "DisplayName": display_name,
                "Category": category,
                "SourceMod": source_mod,
                "SourceFile": source_file,
                "Parent": node["parent"],
                "Scope": 2,
            }

            added += 1

        if added:
            log(f"    [{section}] scope=2 добавлено: {added}")

        if not result:
            log("    config.bin: parser не смог достать scope=2, fallback отключён")

    log(f"    config.bin: принято={len(result)}")
    return result


# ---------------------------------------------------------------------------
# PBO reader
# ---------------------------------------------------------------------------

def read_cstring_f(f):
    data = bytearray()

    while True:
        b = f.read(1)

        if not b:
            return None

        if b == b"\x00":
            break

        data.extend(b)

    return data.decode("latin1", errors="ignore")


def skip_extension_header(f):
    while True:
        key = read_cstring_f(f)

        if key is None or key == "":
            return

        read_cstring_f(f)


def normalize_packed_data(data: bytes, packing: int, original_size: int, entry_name: str) -> bytes | None:
    actual_data = data

    if packing == 0x43707273 and original_size > 0:
        if data.startswith(b"_\x00raP"):
            actual_data = data[1:]
        elif data.startswith(b"\x00raP"):
            actual_data = data
        elif b"class " in data[:128]:
            idx = data.find(b"class ")
            actual_data = data[idx:]
        else:
            decompressed = pbo_lzo_decompress(data, original_size)

            if decompressed is not None:
                actual_data = decompressed
            else:
                log(f"    Ошибка декомпрессии: {entry_name}")
                return None

    return actual_data


def read_pbo_configs(pbo_path: Path):
    configs_cpp = []
    configs_bin = []

    try:
        with pbo_path.open("rb") as f:
            entries = []

            while True:
                name = read_cstring_f(f)

                if name is None:
                    return []

                header = f.read(20)

                if len(header) < 20:
                    return []

                packing, original_size, reserved, timestamp, data_size = struct.unpack("<IIIII", header)

                if name == "" and packing == 0 and original_size == 0 and data_size == 0:
                    break

                if name == "" and packing != 0:
                    skip_extension_header(f)
                    continue

                if name == "":
                    continue

                entries.append({
                    "name": name.replace("\\", "/"),
                    "size": data_size,
                    "packing": packing,
                    "original_size": original_size,
                })

            for e in entries:
                data = f.read(e["size"])

                entry_name = e["name"].replace("\\", "/")
                entry_lower = entry_name.lower().replace("\\", "/")
                base = entry_lower.split("/")[-1]

                if base not in ("config.cpp", "config.bin"):
                    continue

                skip_dirs = (
                    "gui/",
                    "proxy/",
                    "ui/",
                    "sound/",
                    "sounds/",
                    "animations/",
                    "anim/",
                    "textures/",
                    "models/",
                )

                if any(entry_lower.startswith(d) for d in skip_dirs):
                    continue

                actual_data = normalize_packed_data(
                    data,
                    e["packing"],
                    e["original_size"],
                    entry_name
                )

                if actual_data is None:
                    continue

                log(f"    Конфиг в PBO: {entry_name} ({len(actual_data)} б)")

                if base == "config.cpp":
                    configs_cpp.append((entry_name, actual_data))
                elif base == "config.bin":
                    configs_bin.append((entry_name, actual_data))

    except Exception as ex:
        log(f"  PBO ошибка {pbo_path.name}: {ex}")

    # Если в той же папке есть config.cpp, он главнее config.bin.
    seen_dirs = set()
    result = []
    seen_paths = set()

    for e in configs_cpp:
        directory = e[0].rsplit("/", 1)[0] if "/" in e[0] else ""
        seen_dirs.add(directory)

        if e[0] not in seen_paths:
            seen_paths.add(e[0])
            result.append(e)

    for e in configs_bin:
        directory = e[0].rsplit("/", 1)[0] if "/" in e[0] else ""

        if directory not in seen_dirs and e[0] not in seen_paths:
            seen_paths.add(e[0])
            result.append(e)

    return result


def should_skip_pbo(pbo_name: str) -> bool:
    lower = pbo_name.lower()

    skip_parts = [
        "_client",
        "client",
        "_scripts",
        "scripts",
        "_gui",
        "gui",
        "_ui",
        "_sound",
        "sound",
        "sounds",
    ]

    return any(x in lower for x in skip_parts)


def scan_loose_root_config(mod_path: Path, mod_name: str) -> dict:
    result = {}

    config_cpp = mod_path / "config.cpp"
    config_bin = mod_path / "config.bin"

    if config_cpp.exists():
        try:
            data = config_cpp.read_bytes()
            log("  Loose root config.cpp")
            result.update(parse_config_text(read_text_bytes(data), mod_name, str(config_cpp)))
        except Exception:
            pass
    elif config_bin.exists():
        try:
            data = config_bin.read_bytes()
            log("  Loose root config.bin")
            result.update(parse_config_bin(data, mod_name, str(config_bin)))
        except Exception:
            pass

    return result


def scan_mod(mod: dict) -> dict:
    mod_name = mod["name"]
    mod_path = Path(mod["path"])
    result = {}

    log(f"\nМод: {mod_name}")

    if not mod_path.exists():
        log("  Пропуск — папка не найдена")
        return result

    result.update(scan_loose_root_config(mod_path, mod_name))

    pbos = list(mod_path.rglob("*.pbo"))

    if pbos:
        log(f"  PBO: {len(pbos)} файлов")

    for pbo in pbos:
        if should_skip_pbo(pbo.name):
            log(f"  Пропуск PBO: {pbo.name}")
            continue

        for fname, data in read_pbo_configs(pbo):
            base = fname.lower().split("/")[-1]

            if base == "config.cpp":
                found = parse_config_text(
                    read_text_bytes(data),
                    mod_name,
                    f"{pbo.name}/{fname}"
                )
            elif base == "config.bin":
                found = parse_config_bin(
                    data,
                    mod_name,
                    f"{pbo.name}/{fname}"
                )
            else:
                found = {}

            result.update(found)

    log(f"  Итого classnames: {len(result)}")
    return result


def main():
    if len(sys.argv) < 3:
        print("Usage: workshop_scanner.py selected_mods.json output.json")
        return 2

    input_json = Path(sys.argv[1])
    output_json = Path(sys.argv[2])

    mods = json.loads(input_json.read_text(encoding="utf-8"))
    log(f"Модов для сканирования: {len(mods)}")

    all_items = {}

    for mod in mods:
        found = scan_mod(mod)
        all_items.update(found)

    items = sorted(all_items.values(), key=lambda x: x["ClassName"].lower())

    output_json.parent.mkdir(parents=True, exist_ok=True)
    output_json.write_text(
        json.dumps(items, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )

    log(f"\nВсего classnames: {len(items)}")
    log(f"Сохранено: {output_json}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())