"""T07 visual vertical slice polish for KosAtmosphere_v1.blend -> ENV_KosAtmosphere_v1.fbx.

Preserves all 226 existing object names, transforms, colliders, doorway clearance (0.66m x 2.05m),
both cover niches (C1_COVER_LEFT / C1_COVER_RIGHT), all 3 SceneInteractable meshes, and all 9
LoopAtmosphere paper meshes, while upgrading PBR maps, bed/pillow sculpts, fluorescent housings,
lived-in kos details, and damp/wetness mesh decals.
"""
import json
import math
import random
from pathlib import Path
import bpy
import numpy as np
from mathutils import Vector

ROOT = Path(r"D:/GRAVKOM/No rainbow after")
BLEND_PATH = ROOT / "BlenderSources/Environment/KosAtmosphere_v1.blend"
BASELINE_BLEND_PATH = ROOT / "Temp/T07Baseline/20261107-164230/BlenderSources/KosAtmosphere_v1.blend"
EXPORT_FBX = ROOT / "Assets/_NoRainbowAfter/Art/Models/Environment/ENV_KosAtmosphere_v1.fbx"
MANIFEST_PATH = ROOT / "BlenderSources/Environment/KosAtmosphere_v1_manifest.json"
T07_MANIFEST_PATH = ROOT / "BlenderSources/Environment/T07_visual_polish_manifest.json"
TEXDIR = ROOT / "Assets/_NoRainbowAfter/Art/Textures/KosAtmosphere"
VFX_TEXDIR = ROOT / "Assets/_NoRainbowAfter/Art/Textures/VFX"

RAW_FLOOR_JPG = Path(r"C:/Users/sinholms/.gemini/antigravity/brain/bd61eee5-f568-449c-ab62-125ec4eb633e/floor_albedo_raw_1794044744689.jpg")
RAW_CLOTH_JPG = Path(r"C:/Users/sinholms/.gemini/antigravity/brain/bd61eee5-f568-449c-ab62-125ec4eb633e/cloth_albedo_raw_1794044763348.jpg")


def fbm_tileable(u, v, seed=17, octaves=(0.46, 0.25, 0.15, 0.09, 0.05), base_freq=3):
    out = np.zeros_like(u, dtype=np.float32)
    for i, amp in enumerate(octaves):
        f = float(base_freq * (2 ** i))
        x = u * f
        y = v * f
        xi = np.floor(x)
        yi = np.floor(y)
        tx = x - xi
        ty = y - yi
        tx = tx * tx * (3.0 - 2.0 * tx)
        ty = ty * ty * (3.0 - 2.0 * ty)

        def h(a, b):
            ax = np.mod(a, f)
            by = np.mod(b, f)
            return np.mod(np.sin(ax * 127.1 + by * 311.7 + (i + seed) * 43.17) * 43758.5453, 1.0)

        a0 = h(xi, yi) * (1.0 - tx) + h(xi + 1.0, yi) * tx
        b0 = h(xi, yi + 1.0) * (1.0 - tx) + h(xi + 1.0, yi + 1.0) * tx
        out += amp * (a0 * (1.0 - ty) + b0 * ty)
    return out


def save_png(folder: Path, name: str, rgba: np.ndarray, is_data: bool = False):
    folder.mkdir(parents=True, exist_ok=True)
    h, w = rgba.shape[:2]
    if rgba.ndim == 2:
        rgba = np.repeat(rgba[:, :, None], 3, axis=2)
    if rgba.shape[2] == 3:
        rgba = np.concatenate([rgba, np.ones((h, w, 1), dtype=np.float32)], axis=2)
    rgba = np.clip(rgba, 0.0, 1.0).astype(np.float32)

    img_name = f"T07TMP_{name}"
    old = bpy.data.images.get(img_name)
    if old is not None:
        bpy.data.images.remove(old)

    img = bpy.data.images.new(img_name, width=w, height=h, alpha=True, float_buffer=False)
    img.colorspace_settings.name = "Non-Color" if is_data else "sRGB"
    img.pixels.foreach_set(rgba.ravel())
    out_path = folder / f"{name}.png"
    img.filepath_raw = str(out_path)
    img.file_format = "PNG"
    img.save()
    bpy.data.images.remove(img)
    return out_path


def resize_rgb_numpy(arr: np.ndarray, target_h: int, target_w: int) -> np.ndarray:
    h0, w0 = arr.shape[:2]
    if h0 == target_h and w0 == target_w:
        return arr.astype(np.float32, copy=False)
    ys = np.clip((np.arange(target_h, dtype=np.float32) + 0.5) * (h0 / float(target_h)) - 0.5, 0.0, max(h0 - 1.0, 0.0))
    xs = np.clip((np.arange(target_w, dtype=np.float32) + 0.5) * (w0 / float(target_w)) - 0.5, 0.0, max(w0 - 1.0, 0.0))
    y0 = np.floor(ys).astype(np.int32)
    x0 = np.floor(xs).astype(np.int32)
    y1 = np.minimum(y0 + 1, h0 - 1)
    x1 = np.minimum(x0 + 1, w0 - 1)
    wy = (ys - y0)[:, None, None]
    wx = (xs - x0)[None, :, None]
    top = arr[y0[:, None], x0[None, :]] * (1.0 - wx) + arr[y0[:, None], x1[None, :]] * wx
    bot = arr[y1[:, None], x0[None, :]] * (1.0 - wx) + arr[y1[:, None], x1[None, :]] * wx
    return (top * (1.0 - wy) + bot * wy).astype(np.float32)


def load_rgb_array(path: Path, target_res: int = 1024, crop_border: int = 0) -> np.ndarray:
    img = bpy.data.images.load(str(path), check_existing=False)
    w0, h0 = img.size[0], img.size[1]
    buf = np.empty(w0 * h0 * 4, dtype=np.float32)
    img.pixels.foreach_get(buf)
    bpy.data.images.remove(img)
    arr = buf.reshape((h0, w0, 4))[:, :, :3]
    if crop_border > 0 and h0 > crop_border * 4 and w0 > crop_border * 4:
        arr = arr[crop_border:-crop_border, crop_border:-crop_border, :]
    if target_res > 0 and (arr.shape[0] != target_res or arr.shape[1] != target_res):
        arr = resize_rgb_numpy(arr, target_res, target_res)
    return arr


def normal_from_height(height: np.ndarray, strength: float = 8.0) -> np.ndarray:
    dx = np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)
    dy = np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)
    nx = -dx * strength
    ny = -dy * strength
    nz = np.ones_like(height, dtype=np.float32)
    norm = np.sqrt(nx * nx + ny * ny + nz * nz) + 1e-6
    rgb = np.stack([(nx / norm) * 0.5 + 0.5, (ny / norm) * 0.5 + 0.5, (nz / norm) * 0.5 + 0.5], axis=2)
    return rgb


def make_seamless_periodic(rgb: np.ndarray, margin_frac: float = 0.18) -> np.ndarray:
    h, w, c = rgb.shape
    shifted = np.roll(np.roll(rgb, h // 2, axis=0), w // 2, axis=1)
    yy = np.linspace(0.0, 1.0, h, endpoint=False, dtype=np.float32)[:, None]
    xx = np.linspace(0.0, 1.0, w, endpoint=False, dtype=np.float32)[None, :]
    dx = np.minimum(xx, 1.0 - xx)
    dy = np.minimum(yy, 1.0 - yy)
    wx = np.clip(dx / margin_frac, 0.0, 1.0)
    wy = np.clip(dy / margin_frac, 0.0, 1.0)
    w_center = (wx * wx * (3.0 - 2.0 * wx)) * (wy * wy * (3.0 - 2.0 * wy))
    w_center = w_center[:, :, None]
    return rgb * w_center + shifted * (1.0 - w_center)


def box_blur_periodic(arr: np.ndarray, radius: int = 3) -> np.ndarray:
    # True 2D separable periodic box blur (horizontal pass then vertical pass)
    horiz = np.zeros_like(arr, dtype=np.float32)
    k = 2 * radius + 1
    for shift in range(-radius, radius + 1):
        horiz += np.roll(arr, shift, axis=1)
    horiz /= float(k)
    vert = np.zeros_like(horiz, dtype=np.float32)
    for shift in range(-radius, radius + 1):
        vert += np.roll(horiz, shift, axis=0)
    return vert / float(k)


def build_pbr_textures():
    res = 1024
    y_lin = np.linspace(0.0, 1.0, res, endpoint=False, dtype=np.float32)
    x_lin = np.linspace(0.0, 1.0, res, endpoint=False, dtype=np.float32)
    u, v = np.meshgrid(x_lin, y_lin)

    # 1. Derive physically aligned Wall Normal, Mask (R=metal, A=smoothness) & AO from Wall_BaseColor.png
    wall_rgb = load_rgb_array(TEXDIR / "Wall_BaseColor.png")
    wall_lum = 0.299 * wall_rgb[:, :, 0] + 0.587 * wall_rgb[:, :, 1] + 0.114 * wall_rgb[:, :, 2]
    wall_green_damp = np.clip((wall_rgb[:, :, 1] - wall_rgb[:, :, 0]) * 3.5 + (0.48 - wall_lum) * 1.4, 0.0, 1.0)
    fine_grit = fbm_tileable(u, v, seed=11, octaves=(0.45, 0.30, 0.15, 0.10), base_freq=16) - 0.5
    peel_cavity = np.clip((wall_lum - box_blur_periodic(wall_lum, 6)) * 4.5, -0.35, 0.35)
    wall_height = wall_lum * 0.55 + peel_cavity * 0.35 + fine_grit * 0.09
    wall_normal = normal_from_height(wall_height, strength=9.5)
    wall_smooth = np.clip(0.13 + wall_green_damp * 0.24 - np.abs(peel_cavity) * 0.18 + fine_grit * 0.04, 0.05, 0.48)
    wall_ao = np.clip(0.86 + peel_cavity * 0.45 - wall_green_damp * 0.10, 0.48, 1.0)
    save_png(TEXDIR, "Wall_Normal", wall_normal, is_data=True)
    save_png(TEXDIR, "Wall_Mask", np.stack([np.zeros_like(wall_smooth), wall_ao, np.zeros_like(wall_smooth), wall_smooth], axis=2), is_data=True)
    save_png(TEXDIR, "Wall_AO", wall_ao, is_data=True)

    # 2. Derive physically aligned Wood Normal, Mask & AO from Wood_BaseColor.png
    wood_rgb = load_rgb_array(TEXDIR / "Wood_BaseColor.png")
    wood_lum = 0.299 * wood_rgb[:, :, 0] + 0.587 * wood_rgb[:, :, 1] + 0.114 * wood_rgb[:, :, 2]
    wood_fiber = fbm_tileable(u * 3.0, v * 0.6, seed=23, octaves=(0.55, 0.28, 0.17), base_freq=12) - 0.5
    scratch_mask = np.clip((wood_lum - box_blur_periodic(wood_lum, 4)) * 5.0, 0.0, 1.0)
    wood_height = wood_lum * 0.62 + wood_fiber * 0.08 - scratch_mask * 0.16
    wood_normal = normal_from_height(wood_height, strength=7.5)
    wood_smooth = np.clip(0.36 - scratch_mask * 0.22 + (0.32 - wood_lum) * 0.30 + wood_fiber * 0.06, 0.10, 0.54)
    wood_ao = np.clip(0.88 + (wood_lum - box_blur_periodic(wood_lum, 5)) * 0.65, 0.52, 1.0)
    save_png(TEXDIR, "Wood_Normal", wood_normal, is_data=True)
    save_png(TEXDIR, "Wood_Mask", np.stack([np.zeros_like(wood_smooth), wood_ao, np.zeros_like(wood_smooth), wood_smooth], axis=2), is_data=True)
    save_png(TEXDIR, "Wood_AO", wood_ao, is_data=True)

    # 3. Build Floor ceramic tile PBR set from generated scan (equalized so no bright checkerboard!)
    if RAW_FLOOR_JPG.exists():
        # Take clean single-tile interior scan patch (away from checkerboard grout seam) and tile cleanly
        full_floor = load_rgb_array(RAW_FLOOR_JPG, target_res=1024, crop_border=0)
        patch = full_floor[540:996, 36:492, :]  # uniform slate-stone ceramic quadrant in Blender bottom-up UV space
        tile_scan = make_seamless_periodic(resize_rgb_numpy(patch, res, res), margin_frac=0.22)
        scan_lum = 0.299 * tile_scan[:, :, 0] + 0.587 * tile_scan[:, :, 1] + 0.114 * tile_scan[:, :, 2]
        local_detail = scan_lum - box_blur_periodic(scan_lum, 18)
    else:
        local_detail = (fbm_tileable(u, v, seed=31, base_freq=10) - 0.5) * 0.14

    # Clean 2x2 kos ceramic tile grid with grout lines at u=0.0,0.5 and v=0.0,0.5 (aligned with tile_ix/iy!)
    gx = np.abs(np.mod(u * 2.0, 1.0) - 0.5) * 2.0
    gy = np.abs(np.mod(v * 2.0, 1.0) - 0.5) * 2.0
    edge_dist = np.minimum(1.0 - gx, 1.0 - gy)
    grout = 1.0 - np.clip((edge_dist - 0.015) / 0.016, 0.0, 1.0)
    bevel = np.clip((edge_dist - 0.013) / 0.038, 0.0, 1.0)
    tile_ix = np.floor(u * 2.0)
    tile_iy = np.floor(v * 2.0)
    tile_var = ((tile_ix * 3.0 + tile_iy * 5.0) % 4.0 - 1.5) * 0.008
    scuff = fbm_tileable(u, v, seed=47, octaves=(0.50, 0.30, 0.20), base_freq=6)
    mineral = fbm_tileable(u * 1.3, v * 1.3, seed=59, base_freq=14)

    base_tile_col = np.stack([
        0.235 + tile_var + local_detail * 0.55 - (scuff - 0.5) * 0.050,
        0.258 + tile_var + local_detail * 0.58 - (scuff - 0.5) * 0.046,
        0.252 + tile_var + local_detail * 0.52 - (scuff - 0.5) * 0.042,
    ], axis=2)
    grout_col = np.stack([
        0.102 + (mineral - 0.5) * 0.032,
        0.108 + (mineral - 0.5) * 0.032,
        0.104 + (mineral - 0.5) * 0.028,
    ], axis=2)
    floor_rgb = base_tile_col * (1.0 - grout[:, :, None]) + grout_col * grout[:, :, None]
    floor_height = bevel * 0.72 - grout * 0.28 + local_detail * 0.26 + (mineral - 0.5) * 0.04
    floor_normal = normal_from_height(floor_height, strength=8.5)
    floor_smooth = np.clip((0.46 + (scuff - 0.5) * 0.22 - np.abs(local_detail) * 0.85) * (1.0 - grout) + grout * 0.06, 0.05, 0.68)
    floor_ao = np.clip(1.0 - grout * 0.42 - np.clip(-local_detail * 2.2, 0.0, 0.18), 0.48, 1.0)
    save_png(TEXDIR, "Floor_BaseColor", floor_rgb, is_data=False)
    save_png(TEXDIR, "Floor_Normal", floor_normal, is_data=True)
    save_png(TEXDIR, "Floor_Mask", np.stack([np.zeros_like(floor_smooth), floor_ao, np.zeros_like(floor_smooth), floor_smooth], axis=2), is_data=True)
    save_png(TEXDIR, "Floor_AO", floor_ao, is_data=True)

    # 4. Build Cloth woven cotton-flannel PBR set from generated scan (crop outer border before seamless blend!)
    if RAW_CLOTH_JPG.exists():
        raw_cloth = make_seamless_periodic(load_rgb_array(RAW_CLOTH_JPG, target_res=1024, crop_border=24), margin_frac=0.22)
    else:
        weave = 0.5 + 0.06 * np.sin(u * 240.0 * math.pi) + 0.06 * np.sin(v * 240.0 * math.pi)
        raw_cloth = np.stack([weave * 0.42, weave * 0.50, weave * 0.53], axis=2)

    cloth_mottle = fbm_tileable(u, v, seed=71, base_freq=4) - 0.5
    cloth_rgb = np.clip(raw_cloth * np.array([0.66, 0.72, 0.75], dtype=np.float32)[None, None, :] + cloth_mottle[:, :, None] * 0.045, 0.08, 0.62)
    cloth_lum = 0.299 * cloth_rgb[:, :, 0] + 0.587 * cloth_rgb[:, :, 1] + 0.114 * cloth_rgb[:, :, 2]
    weave_micro = 0.025 * np.sin(u * 320.0 * math.pi) + 0.025 * np.sin(v * 320.0 * math.pi)
    cloth_height = cloth_lum * 0.85 + weave_micro
    cloth_normal = normal_from_height(cloth_height, strength=7.0)
    cloth_smooth = np.clip(0.08 + (cloth_lum - np.mean(cloth_lum)) * 0.18, 0.04, 0.16)
    cloth_ao = np.clip(0.84 + (cloth_lum - box_blur_periodic(cloth_lum, 4)) * 1.3, 0.58, 1.0)
    save_png(TEXDIR, "Cloth_BaseColor", cloth_rgb, is_data=False)
    save_png(TEXDIR, "Cloth_Normal", cloth_normal, is_data=True)
    save_png(TEXDIR, "Cloth_Mask", np.stack([np.zeros_like(cloth_smooth), cloth_ao, np.zeros_like(cloth_smooth), cloth_smooth], axis=2), is_data=True)
    save_png(TEXDIR, "Cloth_AO", cloth_ao, is_data=True)

    # 4b. Derive Metal_AO and Paper_AO from existing base maps for full PBR occlusion coverage
    metal_rgb = load_rgb_array(TEXDIR / "Metal_BaseColor.png")
    metal_lum = 0.299 * metal_rgb[:, :, 0] + 0.587 * metal_rgb[:, :, 1] + 0.114 * metal_rgb[:, :, 2]
    metal_ao = np.clip(0.86 + (metal_lum - box_blur_periodic(metal_lum, 6)) * 0.90, 0.50, 1.0)
    save_png(TEXDIR, "Metal_AO", metal_ao, is_data=True)

    paper_rgb = load_rgb_array(TEXDIR / "Paper_BaseColor.png")
    paper_lum = 0.299 * paper_rgb[:, :, 0] + 0.587 * paper_rgb[:, :, 1] + 0.114 * paper_rgb[:, :, 2]
    paper_ao = np.clip(0.92 + (paper_lum - box_blur_periodic(paper_lum, 5)) * 0.70, 0.72, 1.0)
    save_png(TEXDIR, "Paper_AO", paper_ao, is_data=True)

    # 5. Wet Puddle Decal PBR Maps (512x512 RGBA patchy organic wet-ceramic seepage, zero oval-disc look)
    dres = 512
    yd = np.linspace(-1.0, 1.0, dres, dtype=np.float32)
    xd = np.linspace(-1.0, 1.0, dres, dtype=np.float32)
    ux, uy = np.meshgrid(xd, yd)
    u01 = (ux + 1.0) * 0.5
    v01 = (uy + 1.0) * 0.5
    puddle_noise = fbm_tileable(u01, v01, seed=83, base_freq=3)
    puddle_fine = fbm_tileable(u01 * 2.0, v01 * 2.0, seed=89, base_freq=6)
    lobe1 = np.sqrt(((ux + 0.14) * 1.18) ** 2 + ((uy - 0.08) * 0.90) ** 2)
    lobe2 = np.sqrt(((ux - 0.22) * 1.30) ** 2 + ((uy + 0.18) * 1.05) ** 2)
    radius = np.minimum(lobe1, lobe2 * 0.95) + (puddle_noise - 0.5) * 0.52 + (puddle_fine - 0.5) * 0.22
    quad_guard = np.clip((0.92 - np.maximum(np.abs(ux), np.abs(uy))) / 0.22, 0.0, 1.0)
    patch_breakup = np.clip((puddle_fine * 0.65 + puddle_noise * 0.35 - 0.30) * 2.3, 0.0, 1.0)
    puddle_alpha = np.clip((0.72 - radius) / 0.28, 0.0, 1.0) * quad_guard * patch_breakup
    puddle_alpha = puddle_alpha * puddle_alpha * (3.0 - 2.0 * puddle_alpha)
    rim = np.exp(-((puddle_alpha - 0.18) / 0.08) ** 2) * (puddle_alpha > 0.02) * quad_guard
    puddle_col = np.stack([
        np.full_like(puddle_alpha, 0.024) + rim * 0.014,
        np.full_like(puddle_alpha, 0.032) + rim * 0.018,
        np.full_like(puddle_alpha, 0.038) + rim * 0.020,
        puddle_alpha * 0.28,
    ], axis=2)
    puddle_nrm = normal_from_height(puddle_alpha * 0.12 + rim * 0.05, strength=2.8)
    puddle_smooth = np.clip(0.32 + puddle_alpha * 0.52, 0.22, 0.84)
    save_png(TEXDIR, "WetPuddle_BaseColor", puddle_col, is_data=False)
    save_png(TEXDIR, "WetPuddle_Normal", puddle_nrm, is_data=True)
    save_png(TEXDIR, "WetPuddle_Mask", np.stack([np.full_like(puddle_smooth, 0.02), np.ones_like(puddle_smooth), np.zeros_like(puddle_smooth), puddle_smooth], axis=2), is_data=True)

    # 6. Damp / Peeling Wall Leak Decal PBR Maps (512x512 RGBA organic radial-capillary seepage with zero quad edge)
    streak_n = fbm_tileable(u01 * 3.5, v01 * 0.55, seed=97, base_freq=5)
    patch_n = fbm_tileable(u01, v01, seed=103, base_freq=3)
    radial_r = np.sqrt((ux * 1.06) ** 2 + ((uy + 0.14) * 1.12) ** 2)
    radial_env = np.clip((0.92 - radial_r) / 0.42, 0.0, 1.0)
    radial_env = radial_env * radial_env * (3.0 - 2.0 * radial_env)
    vertical_bias = np.clip(1.08 - v01 * 0.72 + (streak_n - 0.5) * 0.62, 0.0, 1.0)
    damp_raw = np.clip((patch_n * 0.62 + vertical_bias * 0.52 - 0.40) * 2.0, 0.0, 1.0)
    damp_alpha = damp_raw * radial_env
    salt_rim = np.exp(-((damp_alpha - 0.24) / 0.08) ** 2) * radial_env * (damp_alpha > 0.03)
    damp_col = np.stack([
        0.15 + salt_rim * 0.14 + (patch_n - 0.5) * 0.04,
        0.16 + salt_rim * 0.14 + (patch_n - 0.5) * 0.05,
        0.12 + salt_rim * 0.11 + (patch_n - 0.5) * 0.03,
        damp_alpha * 0.52,
    ], axis=2)
    damp_nrm = normal_from_height(salt_rim * 0.28 - damp_alpha * 0.20, strength=5.8)
    damp_smooth = np.clip(0.18 + damp_alpha * 0.42 - salt_rim * 0.10, 0.08, 0.64)
    save_png(TEXDIR, "DampStain_BaseColor", damp_col, is_data=False)
    save_png(TEXDIR, "DampStain_Normal", damp_nrm, is_data=True)
    save_png(TEXDIR, "DampStain_Mask", np.stack([np.zeros_like(damp_smooth), 1.0 - damp_alpha * 0.22, np.zeros_like(damp_smooth), damp_smooth], axis=2), is_data=True)

    # 7. Rain-streaked Window Condensation Decal Maps (512x512 RGBA)
    rivulets = fbm_tileable(u01 * 5.5, v01 * 0.35, seed=113, base_freq=7)
    droplets = fbm_tileable(u01 * 3.0, v01 * 2.2, seed=127, base_freq=14)
    drop_beads = np.clip((droplets - 0.66) * 4.2, 0.0, 1.0)
    streak_mask = np.clip((rivulets - 0.52) * 3.2, 0.0, 1.0)
    corner_fog = np.clip(0.32 + 0.45 * (ux * ux * 0.7 + uy * uy * 0.7) - streak_mask * 0.28, 0.06, 0.72)
    win_alpha = np.clip(corner_fog * 0.52 + drop_beads * 0.45 + streak_mask * 0.22, 0.08, 0.78)
    win_col = np.stack([
        0.16 + corner_fog * 0.16 + drop_beads * 0.18,
        0.25 + corner_fog * 0.20 + drop_beads * 0.22,
        0.33 + corner_fog * 0.24 + drop_beads * 0.26,
        win_alpha,
    ], axis=2)
    win_nrm = normal_from_height(drop_beads * 0.55 + streak_mask * 0.25, strength=8.0)
    win_smooth = np.clip(0.54 + streak_mask * 0.34 + drop_beads * 0.38 - corner_fog * 0.22, 0.35, 0.96)
    save_png(TEXDIR, "WindowRain_BaseColor", win_col, is_data=False)
    save_png(TEXDIR, "WindowRain_Normal", win_nrm, is_data=True)
    save_png(TEXDIR, "WindowRain_Mask", np.stack([np.full_like(win_smooth, 0.08), np.ones_like(win_smooth), np.zeros_like(win_smooth), win_smooth], axis=2), is_data=True)

    # 8. Soft Particle Sprites for Dust Motes & Water Drips (128x128 RGBA)
    pres = 128
    yp = np.linspace(-1.0, 1.0, pres, dtype=np.float32)
    xp = np.linspace(-1.0, 1.0, pres, dtype=np.float32)
    px, py = np.meshgrid(xp, yp)
    pr2 = px * px + py * py
    mote_a = np.exp(-pr2 * 4.8) * np.clip(1.0 - np.sqrt(pr2), 0.0, 1.0)
    save_png(VFX_TEXDIR, "T_VFX_DustMote", np.stack([np.ones_like(mote_a), np.ones_like(mote_a) * 0.97, np.ones_like(mote_a) * 0.90, mote_a], axis=2), is_data=False)

    drip_r2 = (px * 4.2) ** 2 + (py * 1.15) ** 2
    drip_a = np.exp(-drip_r2 * 2.8) * np.clip(1.0 - np.abs(py), 0.0, 1.0)
    save_png(VFX_TEXDIR, "T_VFX_WaterDrip", np.stack([np.full_like(drip_a, 0.76), np.full_like(drip_a, 0.88), np.ones_like(drip_a), drip_a], axis=2), is_data=False)


def ensure_blender_material(name: str, color=(0.5, 0.5, 0.5, 1.0), rough=0.5, metal=0.0, tex_prefix: str = ""):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.diffuse_color = color
    if hasattr(mat, "surface_render_method"):
        try:
            mat.surface_render_method = "BLENDED" if tex_prefix else "DITHERED"
        except Exception:
            pass
    nt = mat.node_tree
    nt.nodes.clear()
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Roughness"].default_value = rough
    bsdf.inputs["Metallic"].default_value = metal
    if tex_prefix:
        base_file = TEXDIR / f"{tex_prefix}_BaseColor.png"
        if base_file.exists():
            img = bpy.data.images.get(f"{tex_prefix}_BaseColor")
            if img is None:
                img = bpy.data.images.load(str(base_file), check_existing=True)
            else:
                img.filepath = str(base_file)
                img.reload()
            tex_node = nt.nodes.new("ShaderNodeTexImage")
            tex_node.image = img
            nt.links.new(tex_node.outputs["Color"], bsdf.inputs["Base Color"])
            nt.links.new(tex_node.outputs["Alpha"], bsdf.inputs["Alpha"])
    return mat


def sculpt_existing_bed_and_pillow(collection):
    # Replace uniform egg-crate sine wave on SM_PROP_WrinkledBlanket with natural slept-in diagonal folds
    blanket = collection.objects.get("SM_PROP_WrinkledBlanket")
    if blanket is not None and blanket.type == "MESH":
        mesh = blanket.data
        xs = [v.co.x for v in mesh.vertices]
        ys = [v.co.y for v in mesh.vertices]
        zs = [v.co.z for v in mesh.vertices]
        cx = -1.27
        cy = 1.45
        width = 1.08
        length = 1.70
        # Note: solidify modifier was already baked on export previously (2688 verts = top + bottom shell).
        # Identify upper vs lower shell smoothly using vertex normal / local offset while preserving thickness.
        mesh.calc_normals_split() if hasattr(mesh, "calc_normals_split") else None
        for v in mesh.vertices:
            u = np.clip((v.co.x - cx) / width + 0.5, 0.0, 1.0)
            w = np.clip((v.co.y - cy) / length + 0.5, 0.0, 1.0)
            dx = v.co.x - cx
            dy = v.co.y - cy
            # Remove old high-frequency egg-crate component and replace with organic diagonal folds
            diag1 = 0.022 * math.sin(dx * 5.8 + dy * 4.1 + 0.4) * (1.0 - 0.35 * abs(u - 0.5) * 2.0)
            diag2 = 0.014 * math.sin(dy * 7.6 - dx * 3.2 + 1.1)
            crease = 0.018 * math.exp(-((dx - 0.14 + (dy - 0.25) * 0.35) / 0.16) ** 2)
            turnback_lift = 0.028 * math.exp(-((w - 0.91) / 0.09) ** 2) * (1.0 - 0.45 * u)
            drape = 0.205 * max(0.0, (abs(u - 0.5) - 0.38) / 0.12) ** 1.32
            foot_drape = 0.055 * max(0.0, (0.09 - w) / 0.09) ** 1.4
            is_bottom = 1.0 if v.normal.z < -0.15 else 0.0
            base_z = 0.578 + diag1 + diag2 + crease + turnback_lift - drape - foot_drape - is_bottom * 0.007
            v.co.z = base_z
        for poly in mesh.polygons:
            poly.use_smooth = True
        mesh.update()

    # Add natural head indentation to SM_PROP_SoftPillow (idempotent via custom mesh flag)
    pillow = collection.objects.get("SM_PROP_SoftPillow")
    if pillow is not None and pillow.type == "MESH" and not pillow.data.get("t07_pillow_sculpted", False):
        mesh = pillow.data
        for v in mesh.vertices:
            wx = v.co.x * pillow.scale.x
            wy = v.co.y * pillow.scale.y
            wz = v.co.z * pillow.scale.z
            if wz > 0.0:
                r2 = (wx / 0.22) ** 2 + (wy / 0.14) ** 2
                indent = -0.026 * math.exp(-r2 * 1.4) + 0.010 * math.exp(-((r2 - 1.15) / 0.55) ** 2)
                v.co.z = (wz + indent) / max(pillow.scale.z, 1e-4)
        for poly in mesh.polygons:
            poly.use_smooth = True
        mesh["t07_pillow_sculpted"] = True
        mesh.update()


def add_t07_geometry_and_decals(collection, mats):
    # Clean any prior T07 additions for idempotence
    to_remove = [o for o in list(collection.objects) if o.name.startswith("SM_T07_") or o.name.startswith("SM_DEC_")]
    for obj in to_remove:
        mesh_data = obj.data
        bpy.data.objects.remove(obj, do_unlink=True)
        if mesh_data is not None and mesh_data.users == 0:
            bpy.data.meshes.remove(mesh_data)

    def link_only(obj, mat_key, collision="none"):
        for c in list(obj.users_collection):
            c.objects.unlink(obj)
        collection.objects.link(obj)
        obj.data.materials.clear()
        obj.data.materials.append(mats[mat_key])
        obj["nra_collision"] = collision
        return obj

    def make_box(name, pos, size, mat_key, bevel=0.004, rot=(0.0, 0.0, 0.0)):
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=pos, rotation=rot)
        obj = bpy.context.object
        obj.name = name
        obj.scale = size
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        if bevel > 0.0:
            mod = obj.modifiers.new("Bevel", "BEVEL")
            mod.width = bevel
            mod.segments = 2
            bpy.ops.object.modifier_apply(modifier=mod.name)
        uv = obj.data.uv_layers.active or obj.data.uv_layers.new(name="UVMap")
        for poly in obj.data.polygons:
            poly.use_smooth = bevel > 0.0
            axes = (0, 1) if abs(poly.normal.z) > 0.5 else (1, 2) if abs(poly.normal.x) > 0.5 else (0, 2)
            for li in poly.loop_indices:
                co = obj.data.vertices[obj.data.loops[li].vertex_index].co
                uv.data[li].uv = (co[axes[0]] * 0.9, co[axes[1]] * 0.9)
        return link_only(obj, mat_key, "none")

    def make_cylinder(name, pos, radius, length, mat_key, rot=(0.0, 0.0, 0.0), vertices=16):
        bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=length, location=pos, rotation=rot)
        obj = bpy.context.object
        obj.name = name
        for poly in obj.data.polygons:
            poly.use_smooth = True
        return link_only(obj, mat_key, "none")

    def make_decal_quad(name, center, normal_axis, width, height, mat_key):
        # Builds an oriented single-sided quad flush with floor/wall/ceiling with 0..1 UVs
        hw = width * 0.5
        hh = height * 0.5
        cx, cy, cz = center
        if normal_axis == "+Z":  # Floor decal
            verts = [(cx - hw, cy - hh, cz), (cx + hw, cy - hh, cz), (cx + hw, cy + hh, cz), (cx - hw, cy + hh, cz)]
            faces = [(0, 1, 2, 3)]
        elif normal_axis == "-Z":  # Ceiling decal
            verts = [(cx - hw, cy + hh, cz), (cx + hw, cy + hh, cz), (cx + hw, cy - hh, cz), (cx - hw, cy - hh, cz)]
            faces = [(0, 1, 2, 3)]
        elif normal_axis == "+X":  # Left wall facing right (+X into room/hall)
            verts = [(cx, cy - hw, cz - hh), (cx, cy + hw, cz - hh), (cx, cy + hw, cz + hh), (cx, cy - hw, cz + hh)]
            faces = [(0, 1, 2, 3)]
        elif normal_axis == "-X":  # Right wall facing left (-X into room/hall)
            verts = [(cx, cy + hw, cz - hh), (cx, cy - hw, cz - hh), (cx, cy - hw, cz + hh), (cx, cy + hw, cz + hh)]
            faces = [(0, 1, 2, 3)]
        elif normal_axis == "-Y":  # Door/end wall facing back (-Y)
            verts = [(cx - hw, cy, cz - hh), (cx + hw, cy, cz - hh), (cx + hw, cy, cz + hh), (cx - hw, cy, cz + hh)]
            faces = [(0, 1, 2, 3)]
        else:  # "+Y"
            verts = [(cx + hw, cy, cz - hh), (cx - hw, cy, cz - hh), (cx - hw, cy, cz + hh), (cx + hw, cy, cz + hh)]
            faces = [(0, 1, 2, 3)]

        mesh = bpy.data.meshes.new(name + "_mesh")
        mesh.from_pydata(verts, [], faces)
        uv = mesh.uv_layers.new(name="UVMap")
        uv_coords = [(0.0, 0.0), (1.0, 0.0), (1.0, 1.0), (0.0, 1.0)]
        for li in range(4):
            uv.data[li].uv = uv_coords[li]
        mesh.update()
        obj = bpy.data.objects.new(name, mesh)
        return link_only(obj, mat_key, "none")

    # A. Upgrade Fluorescent Batten Fixtures (Dorm + 3 Corridor fixtures, z >= 2.53 m, no colliders)
    # Dorm fixture is along X at (0, 2.6, 2.60), width 0.75 x 0.18
    for idx, x_off in enumerate((-0.042, 0.042)):
        make_cylinder(f"SM_T07_RoomTube_{idx}", (0.0, 2.60 + x_off, 2.548), 0.013, 0.68, "LampWarm", rot=(0.0, math.pi * 0.5, 0.0), vertices=14)
    for side_idx, sx in enumerate((-0.355, 0.355)):
        make_box(f"SM_T07_RoomLampCap_{side_idx}", (sx, 2.60, 2.565), (0.028, 0.15, 0.045), "Metal", bevel=0.003)
    make_cylinder("SM_T07_RoomStarter", (0.26, 2.685, 2.575), 0.009, 0.032, "Paper", vertices=12)

    # Corridor fixtures at y = 6.3, 10.8, 17.1 (width 0.62 along X, 0.16 along Y)
    for f_idx, fy in enumerate((6.3, 10.8, 17.1)):
        lamp_key = "LampCold" if fy > 15.0 else "LampWarm"
        for t_idx, dy in enumerate((-0.036, 0.036)):
            make_cylinder(f"SM_T07_HallTube_{f_idx}_{t_idx}", (0.0, fy + dy, 2.562), 0.012, 0.54, lamp_key, rot=(0.0, math.pi * 0.5, 0.0), vertices=12)
        for c_idx, sx in enumerate((-0.285, 0.285)):
            make_box(f"SM_T07_HallLampCap_{f_idx}_{c_idx}", (sx, fy, 2.58), (0.024, 0.135, 0.04), "Metal", bevel=0.003)
        make_box(f"SM_T07_JunctionBox_{f_idx}", (-0.48, fy, 2.64), (0.09, 0.09, 0.045), "Metal", bevel=0.004)

    # B. Desk Hero Details + Kos Architectural Trim
    # Study mug on rear right of desk (away from INT_BOOK at 0.91,3.82 and INT_OVERTIME at 1.45,3.92)
    make_cylinder("SM_T07_StudyMug", (1.72, 3.55, 0.848), 0.038, 0.092, "Paper", vertices=18)
    make_cylinder("SM_T07_StudyMugInner", (1.72, 3.55, 0.892), 0.032, 0.006, "Ink", vertices=16)

    # Wall switch & conduit beside dorm doorway (well outside 0.66m clear door gap)
    make_box("SM_T07_WallSwitchPlate", (0.78, 4.985, 1.24), (0.075, 0.014, 0.085), "Paper", bevel=0.003)
    make_box("SM_T07_WallSwitchRocker", (0.78, 4.976, 1.24), (0.026, 0.010, 0.042), "Metal", bevel=0.002, rot=(0.12, 0.0, 0.0))
    make_cylinder("SM_T07_SwitchConduit", (0.78, 4.988, 1.96), 0.008, 1.38, "Metal", vertices=10)

    # Pinned semester calendar / class timetable sheet on right wall above desk (fictional, non-interactive)
    make_box("SM_T07_WallCalendar", (1.992, 3.56, 1.50), (0.005, 0.28, 0.36), "Paper", bevel=0.001)
    make_box("SM_T07_WallCalendarHeader", (1.988, 3.56, 1.64), (0.004, 0.26, 0.055), "Cloth", bevel=0.001)

    # Modest 19L water gallon jug in front-left corner of room (x=-1.68, y=0.38 -> clear of spawn & walk path)
    make_cylinder("SM_T07_WaterJugBody", (-1.68, 0.38, 0.19), 0.135, 0.36, "Glass", vertices=20)
    make_cylinder("SM_T07_WaterJugNeck", (-1.68, 0.38, 0.41), 0.042, 0.09, "Glass", vertices=16)
    make_cylinder("SM_T07_WaterJugCap", (-1.68, 0.38, 0.46), 0.046, 0.025, "Cloth", vertices=16)

    # Ceiling cornice / plaster cove trim in dorm room for grounded architectural framing
    for sx in (-1.975, 1.975):
        make_box(f"SM_T07_RoomCorniceX_{1 if sx > 0 else 0}", (sx, 2.50, 2.665), (0.045, 4.96, 0.045), "Wall", bevel=0.004)
    for sy_i, sy in enumerate((0.025, 4.975)):
        make_box(f"SM_T07_RoomCorniceY_{sy_i}", (0.0, sy, 2.665), (3.96, 0.045, 0.045), "Wall", bevel=0.004)

    # C. Mesh Decals: Window Rain Streaks, Damp Plaster Seepage, and Wet Corridor Puddles
    # 1) Rain-streaked interior window condensation decal just inside SM_PROP_WindowGlass (x = -2.088)
    make_decal_quad("SM_DEC_WindowRainPane", (-2.086, 3.40, 1.575), "+X", 1.38, 1.06, "WindowRain")

    # 2) Damp plaster seepage & peeling stains in dorm + corridor
    make_decal_quad("SM_DEC_WindowBelowDamp", (-1.995, 3.40, 0.53), "+X", 1.48, 0.88, "DampStain")
    make_decal_quad("SM_DEC_BedWallDamp", (-1.995, 1.55, 0.42), "+X", 1.55, 0.72, "DampStain")
    make_decal_quad("SM_DEC_DeskWallDamp", (1.995, 2.85, 0.44), "-X", 1.75, 0.76, "DampStain")
    make_decal_quad("SM_DEC_DoorWallDamp", (1.22, 4.985, 0.46), "-Y", 1.15, 0.80, "DampStain")
    make_decal_quad("SM_DEC_RoomCeilingLeak", (-1.18, 3.32, 2.685), "-Z", 1.15, 1.25, "DampStain")

    # Corridor damp wall & ceiling pipe leak stains
    make_decal_quad("SM_DEC_HallWallDamp_01", (-0.895, 6.85, 0.48), "+X", 1.65, 0.84, "DampStain")
    make_decal_quad("SM_DEC_HallWallDamp_02", (0.895, 9.40, 0.46), "-X", 1.85, 0.82, "DampStain")
    make_decal_quad("SM_DEC_HallWallDamp_03", (-0.895, 11.45, 0.50), "+X", 1.90, 0.88, "DampStain")
    make_decal_quad("SM_DEC_HallWallDamp_04", (0.895, 15.65, 0.48), "-X", 1.80, 0.84, "DampStain")
    make_decal_quad("SM_DEC_HallEndDamp", (0.0, 18.985, 0.62), "-Y", 1.72, 1.18, "DampStain")
    make_decal_quad("SM_DEC_HallCeilingLeak_01", (-0.52, 8.05, 2.685), "-Z", 0.88, 1.15, "DampStain")
    make_decal_quad("SM_DEC_HallCeilingLeak_02", (-0.48, 14.60, 2.685), "-Z", 0.92, 1.25, "DampStain")

    # 3) Wet Floor Puddles (near window + corridor leak pools + silhouette floor sheen)
    make_decal_quad("SM_DEC_WindowFloorPuddle", (-1.52, 3.38, 0.0035), "+Z", 0.92, 1.48, "WetPuddle")
    make_decal_quad("SM_DEC_HallPuddle_Near", (-0.12, 8.02, 0.0035), "+Z", 1.48, 2.05, "WetPuddle")
    make_decal_quad("SM_DEC_HallPuddle_Mid", (0.10, 11.35, 0.0038), "+Z", 1.42, 1.95, "WetPuddle")
    make_decal_quad("SM_DEC_HallPuddle_Leak2", (-0.08, 14.58, 0.0035), "+Z", 1.56, 2.25, "WetPuddle")
    make_decal_quad("SM_DEC_HallPuddle_FarRim", (0.00, 16.95, 0.0040), "+Z", 1.66, 2.40, "WetPuddle")


def run():
    build_pbr_textures()

    scene = bpy.data.scenes["NRA_KosAtmosphere_v1"]
    for win in bpy.context.window_manager.windows:
        win.scene = scene
    collection = bpy.data.collections["NRA_KosAtmosphere_Geometry"]

    for img_key in ("Floor_BaseColor", "Floor_Normal", "Floor_Mask", "Cloth_BaseColor", "Cloth_Normal", "Cloth_Mask", "Wall_Normal", "Wall_Mask", "Wood_Normal", "Wood_Mask"):
        existing_img = bpy.data.images.get(img_key)
        target_f = TEXDIR / f"{img_key}.png"
        if existing_img is not None and target_f.exists():
            existing_img.filepath = str(target_f)
            existing_img.reload()

    mats = {
        "Wall": bpy.data.materials["MAT_H_Wall"],
        "Floor": bpy.data.materials["MAT_H_Floor"],
        "Wood": bpy.data.materials["MAT_H_Wood"],
        "Cloth": bpy.data.materials["MAT_H_Cloth"],
        "Metal": bpy.data.materials["MAT_H_Metal"],
        "Paper": bpy.data.materials["MAT_H_Paper"],
        "Ink": bpy.data.materials["MAT_H_Ink"],
        "Glass": bpy.data.materials["MAT_H_Glass"],
        "LampWarm": bpy.data.materials["MAT_H_LampWarm"],
        "LampCold": bpy.data.materials["MAT_H_LampCold"],
        "WetPuddle": ensure_blender_material("MAT_H_WetPuddle", (0.05, 0.07, 0.08, 0.72), rough=0.07, metal=0.04, tex_prefix="WetPuddle"),
        "DampStain": ensure_blender_material("MAT_H_DampStain", (0.14, 0.16, 0.12, 0.78), rough=0.45, metal=0.0, tex_prefix="DampStain"),
        "WindowRain": ensure_blender_material("MAT_H_WindowRain", (0.20, 0.30, 0.38, 0.55), rough=0.10, metal=0.08, tex_prefix="WindowRain"),
    }

    sculpt_existing_bed_and_pillow(collection)
    add_t07_geometry_and_decals(collection, mats)
    bpy.context.view_layer.update()

    # Verify critical invariants before exporting
    must_exist = [
        "SM_PROP_StackedTextbook", "SM_PROP_WorkSlip", "SM_PROP_HallClosedDoor",
        "SM_PROP_OpenDormDoor", "SM_PROP_DoorJamb", "SM_PROP_DoorJamb.001", "SM_PROP_DoorLintel",
        "SM_PROP_OpenNotes", "SM_PROP_NotesHeading", "SM_PROP_WorkSlipHeading",
        "SM_ENV_NicheBack", "SM_ENV_NicheBack.001", "SM_PROP_UtilityCabinet", "SM_PROP_UtilityCabinet.001",
    ]
    for req in must_exist:
        if collection.objects.get(req) is None:
            raise RuntimeError(f"Missing required baseline object: {req}")

    # Export FBX with exact same axis & scale contract as KosAtmosphere_v1 / T04
    for obj in scene.objects:
        obj.select_set(False)
    for obj in collection.objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = collection.objects["SM_ENV_RoomFloor"]

    bpy.ops.export_scene.fbx(
        filepath=str(EXPORT_FBX),
        use_selection=True,
        object_types={"MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
        use_custom_props=True,
    )

    graph = bpy.context.evaluated_depsgraph_get()
    parts = []
    triangles = 0
    t07_added = []
    for obj in collection.objects:
        if obj.type != "MESH":
            continue
        mat_name = obj.data.materials[0].name if obj.data.materials else "MAT_H_Wall"
        col_mode = obj.get("nra_collision", "none")
        parts.append({"name": obj.name, "material": mat_name, "collision": col_mode})
        ev = obj.evaluated_get(graph)
        m = ev.to_mesh()
        m.calc_loop_triangles()
        tri_c = len(m.loop_triangles)
        triangles += tri_c
        ev.to_mesh_clear()
        if obj.name.startswith("SM_T07_") or obj.name.startswith("SM_DEC_"):
            t07_added.append({"name": obj.name, "material": mat_name, "triangles": tri_c})

    manifest = json.loads(MANIFEST_PATH.read_text(encoding="utf-8"))
    existing_mat_names = {m["name"] for m in manifest["materials"]}
    extra_mats = [
        {"name": "MAT_H_WetPuddle", "texturePrefix": "WetPuddle", "baseColor": [1, 1, 1, 1], "metallic": 0.04, "smoothness": 0.93, "emission": [0, 0, 0]},
        {"name": "MAT_H_DampStain", "texturePrefix": "DampStain", "baseColor": [1, 1, 1, 1], "metallic": 0.0, "smoothness": 0.58, "emission": [0, 0, 0]},
        {"name": "MAT_H_WindowRain", "texturePrefix": "WindowRain", "baseColor": [1, 1, 1, 1], "metallic": 0.08, "smoothness": 0.92, "emission": [0.02, 0.05, 0.08]},
    ]
    for em in extra_mats:
        if em["name"] not in existing_mat_names:
            manifest["materials"].append(em)
    manifest["parts"] = parts
    manifest["meshCount"] = len(parts)
    manifest["triangles"] = triangles
    manifest["revision"] = "T07: PBR wetness/damp decals, natural blanket/pillow sculpt, detailed fluorescent tubes/junctions, kos set dressing"
    MANIFEST_PATH.write_text(json.dumps(manifest, indent=2), encoding="utf-8")

    t07_report = {
        "revision": "T07",
        "meshCount": len(parts),
        "triangles": triangles,
        "addedMeshCount": len(t07_added),
        "addedMeshes": t07_added,
        "doorwayClearGapMetres": 0.66,
        "doorwayLintelMetres": 2.05,
        "roomMetres": [4.0, 5.0, 2.7],
        "corridorMetres": [1.8, 14.0, 2.7],
    }
    T07_MANIFEST_PATH.write_text(json.dumps(t07_report, indent=2), encoding="utf-8")

    # Frame hero dorm view in Blender 3D Viewport and keep work visible
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type == "VIEW_3D":
                space = area.spaces.active
                eye = Vector((0.35, 0.85, 1.65))
                target = Vector((0.05, 3.65, 1.05))
                space.region_3d.view_rotation = (target - eye).to_track_quat("-Z", "Y")
                space.region_3d.view_distance = (target - eye).length
                space.region_3d.view_location = target
                space.region_3d.view_perspective = "PERSP"
                space.clip_start = 0.02
                try:
                    space.shading.type = "MATERIAL"
                except Exception:
                    pass
                area.tag_redraw()

    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH), copy=False, compress=True)
    return t07_report


if __name__ == "__main__":
    print(json.dumps(run()))
