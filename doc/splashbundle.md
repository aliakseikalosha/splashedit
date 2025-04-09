# SPLASHPACK Binary File Format Specification

All numeric values are stored in little‐endian format. All offsets are counted from the beginning of the file.

---

## 1. File Header (32 bytes)

| Offset | Size | Type   | Description                         |
| ------ | ---- | ------ | ----------------------------------- |
| 0x00   | 2    | char   | `'SP'` – File magic                 |
| 0x02   | 2    | uint16 | Version number                      |
| 0x04   | 2    | uint16 | Number of Lua Files                 |
| 0x06   | 2    | uint16 | Number of GameObjects               |
| 0x08   | 2    | uint16 | Number of Navmeshes                 |
| 0x0A   | 2    | uint16 | Number of Texture Atlases           |
| 0x0C   | 2    | uint16 | Number of CLUTs                     |
| 0x0E   | 2    | uint16 | Player Start X (Fixed-point)        |
| 0x10   | 2    | uint16 | Player Start Y (Fixed-point)        |
| 0x12   | 2    | uint16 | Player Start Z (Fixed-point)        |
| 0x14   | 2    | uint16 | Player Rotation X (Fixed-point)     |
| 0x16   | 2    | uint16 | Player Rotation Y (Fixed-point)     |
| 0x18   | 2    | uint16 | Player Rotation Z (Fixed-point)     |
| 0x1A   | 2    | uint16 | Player Height (Fixed-point)         |
| 0x1C   | 4    | uint32 | Reserved (always 0)                 |

---

## 2. Metadata Section

### 2.1 Lua File Descriptors (8 bytes each)

| Offset (per entry) | Size | Type   | Description                       |
| ------------------ | ---- | ------ | --------------------------------- |
| 0x00               | 4    | uint32 | Lua File Data Offset              |
| 0x04               | 4    | uint32 | Lua File Size                     |

### 2.2 GameObject Descriptors (56 bytes each)

| Offset (per entry) | Size | Type     | Description                       |
| ------------------ | ---- | -------- | --------------------------------- |
| 0x00               | 4    | uint32   | Mesh Data Offset                  |
| 0x04               | 4    | int32    | X position (Fixed-point)          |
| 0x08               | 4    | int32    | Y position (Fixed-point)          |
| 0x0C               | 4    | int32    | Z position (Fixed-point)          |
| 0x10               | 36   | int32[9] | 3×3 Rotation Matrix (Fixed-point) |
| 0x34               | 2    | uint16   | Triangle count                    |
| 0x36               | 2    | int16    | Lua File Index (-1 if none)       |

### 2.3 Navmesh Descriptors (8 bytes each)

| Offset (per entry) | Size | Type   | Description                       |
| ------------------ | ---- | ------ | --------------------------------- |
| 0x00               | 4    | uint32 | Navmesh Data Offset               |
| 0x04               | 2    | uint16 | Triangle count                    |
| 0x06               | 2    | uint16 | Padding                           |

### 2.4 Texture Atlas Descriptors (12 bytes each)

| Offset (per entry) | Size | Type   | Description                      |
| ------------------ | ---- | ------ | -------------------------------- |
| 0x00               | 4    | uint32 | Pixel Data Offset                |
| 0x04               | 2    | uint16 | Atlas Width                      |
| 0x06               | 2    | uint16 | Atlas Height                     |
| 0x08               | 2    | uint16 | Atlas Position X (VRAM origin)   |
| 0x0A               | 2    | uint16 | Atlas Position Y (VRAM origin)   |

### 2.5 CLUT Descriptors (12 bytes each)

| Offset (per entry) | Size | Type   | Description                                           |
| ------------------ | ---- | ------ | ----------------------------------------------------- |
| 0x00               | 4    | uint32 | CLUT Data Offset                                      |
| 0x04               | 2    | uint16 | CLUT Packing X (in 16-pixel units)                   |
| 0x06               | 2    | uint16 | CLUT Packing Y                                        |
| 0x08               | 2    | uint16 | Palette entry count                                   |
| 0x0A               | 2    | uint16 | Padding                                               |

---

## 3. Data Section

### 3.1 Lua Data Block

Each Lua file is stored as raw bytes. The size of each Lua file is specified in the Lua File Descriptor.

---

### 3.2 Mesh Data Block (per GameObject)

Each mesh is made of triangles:

**Triangle Layout (52 bytes):**

| Field               | Size | Description                                         |
| ------------------- |------|-----------------------------------------------------|
| Vertex v0          | 6    | x, y, z (int16)                                     |
| Vertex v1          | 6    | x, y, z (int16)                                     |
| Vertex v2          | 6    | x, y, z (int16)                                     |
| Normal             | 6    | nx, ny, nz (int16)                                  |
| Color v0           | 4    | RGB + padding (uint8 × 4)                           |
| Color v1           | 4    | RGB + padding (uint8 × 4)                           |
| Color v2           | 4    | RGB + padding (uint8 × 4)                           |
| UV v0              | 2    | u, v (uint8 × 2)                                    |
| UV v1              | 2    | u, v (uint8 × 2)                                    |
| UV v2              | 2    | u, v (uint8 × 2)                                    |
| UV padding         | 2    | uint16 (always 0)                                   |
| TPage              | 2    | Texture page info                                   |
| CLUT X             | 2    | Position in VRAM (X / 16)                           |
| CLUT Y             | 2    | Position in VRAM Y                                  |
| Final padding      | 2    | uint16                                              |

---

### 3.3 Navmesh Data Block

Each triangle is 3 vertices (`int16` x/y/z), total 18 bytes per triangle.

---

### 3.4 Texture Atlas Data Block

Pixel data stored as `uint16[width * height]`.

---

### 3.5 CLUT Data Block

Pixel data stored as `uint16[length]`.

---

## Notes

- All offsets are aligned to 4-byte boundaries.
- Fixed-point values are scaled by the `GTEScaling` factor.
- Lua file indices in GameObject descriptors are `-1` if no Lua file is associated with the object.
- Navmesh triangles are stored as raw vertex data without additional attributes.