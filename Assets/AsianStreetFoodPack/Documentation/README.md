# Asian Street Food Carts & Stalls (Pack 1)

337 meshes, 128.190 triangles, 341 prefabs. Metres, Z-up authored, pivots at the foot of each prop. Exactly three materials, one per mesh, one draw call each.

## Requirements

- Unity 6000.5 or newer. Built and verified on 6000.5.9f1.
- URP or Built-in. This project ships with URP as its render pipeline; the shader graph also targets Built-in, so importing the .unitypackage into a Built-in project works too. HDRP is not supported in v1.
- `com.unity.shadergraph`: URP already depends on it. On a Built-in project, install it from Package Manager BEFORE importing, otherwise `SH_Pack01_Lit` cannot compile and every material falls back to pink.

## What is inside

- Cart: 37
- Clutter: 26
- Cook: 145
- Furniture: 19
- Ground: 27
- Sign: 14
- Stall: 12
- Street: 8
- Vendor: 9
- Wall: 40

Assembled hero carts, ready to drop in:

- `SM_Cart_BanhMi_Assembly_01`: 35 part
- `SM_Cart_Bao_Assembly_01`: 21 part
- `SM_Cart_Sorbetes_Assembly_01`: 19 part
- `SM_Cart_Kape_Assembly_01`: 42 part

Scenes:

- `LunchRushCorner`: 250 instance

## Materials

- `M_Pack01_Atlas`: every prop. Colour atlas plus a metallic-roughness atlas.
- `M_Pack01_Signage`: the six signage kits. Text sheet, no MR texture.
- `M_Pack01_Glass`: the hard-coded glass whitelist. Same graph, Surface set to Transparent through Allow Material Override, `_BaseColor.a = 0.14`.

Atlas textures use Point filtering on purpose: the palette is flat swatches, and bilinear bleeds neighbouring colours across swatch edges. Signage uses Bilinear.

## Shader properties

`SH_Pack01_Lit` is one Shader Graph targeting Built-in and URP; every property below shows up in the material Inspector.

| Property | Reference | Type | Default | What it does |
|---|---|---|---|---|
| Base Texture | `_BaseTexture` | Texture2D, white | palette atlas | Sampled on UV0, multiplied by Base Color, drives Base Color. Flagged as the main texture. |
| Base Color | `_BaseColor` | Color | 1, 1, 1, 1 | Tint over the base texture. Its alpha drives Alpha, which is where glass gets 0.14. |
| Use MR Texture | `_UseMRTexture` | Boolean | on | On, metallic and smoothness come from MR Texture; off, from the two sliders below. |
| MR Texture | `_MRTexture` | Texture2D, white | MR atlas | glTF channel layout: `g` is roughness, `b` is metallic, `r` unused. Import it as Linear, not sRGB. |
| Roughness | `_Roughness` | Slider 0-1 | 0.65 | Read only when Use MR Texture is off. Reaches Smoothness as `1 - roughness`. |
| Metallic | `_Metallic` | Slider 0-1 | 0.0 | Read only when Use MR Texture is off. |

Surface Type is exposed through Allow Material Override on both targets, so `M_Pack01_Glass` flips to Transparent on this same graph. There is no second shader to keep in sync.

## Assembling parts yourself

`Documentation/assembly_coords.csv` lists every part of every hero cart with its local position and rotation quaternion, relative to the cart body at the origin. Those are the same numbers the assembled prefabs are built from.

## Holes are real geometry

No alpha cutout anywhere. Openings in shutters, grilles and breeze blocks are modelled, so they hold up at any camera angle and cost nothing in overdraw.

## Third-party notices

Signage lettering is rasterized from Noto Sans, Noto Sans TC, Kanit and Be Vietnam Pro (SIL OFL 1.1). Font files are not included. See `Third-Party Notices.txt`.
