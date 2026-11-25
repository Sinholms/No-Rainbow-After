"""Bake tileable rain runoff and a smoothness channel for vehicle enamel and painted steel."""
from pathlib import Path
import numpy as np
from PIL import Image

size = 1024
rng = np.random.default_rng(4107)
u = np.arange(size) / size


def noise(columns, rows):
    grid = rng.random((rows, columns))
    def samples(count):
        coordinate = u*count
        index = coordinate.astype(int)
        f = coordinate-index
        return index, (index+1)%count, f*f*(3-2*f)
    x, xn, sx = samples(columns)
    y, yn, sy = samples(rows)
    horizontal = grid[:,x]*(1-sx) + grid[:,xn]*sx
    return horizontal[y,:]*(1-sy[:,None]) + horizontal[yn,:]*sy[:,None]


film = noise(6,4)*.46 + noise(23,14)*.32 + noise(91,32)*.22
runoff = np.maximum(0, noise(182,5)-.53)*noise(9,8)
chips = np.clip((noise(132,107)*.44 + noise(26,23)*.37 + rng.random((size,size))*.19-.755)*12,0,1)
rub = .72 + film*.25 - runoff*.55 + (rng.random((size,size))-.5)*.042
rgba = np.stack([rub*(1-.29*chips), rub*(1-.46*chips), rub*(1-.55*chips),
                 .48+.49*film-.45*chips-.4*runoff],axis=2)
out=Path(__file__).resolve().parents[2]/"Assets/_NoRainbowAfter/Art/Models/Environment/ChapterTwo"
Image.fromarray((rgba.clip(0,1)*255).astype("uint8")).save(out/"WeatheredEnamel.png")
