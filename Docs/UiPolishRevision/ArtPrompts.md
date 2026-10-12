# Weedtopia title art

Generated with the built-in ImageGen tool on 2026-10-03. The two selected PNGs were copied unchanged into the project's production Art/Title directory. The logo retains its generated alpha channel. Unity composes the logo and functional Play button over the background, preserving their proportions at each aspect ratio.

## Title background prompt

Use case: stylized-concept.
Asset type: production title-screen background illustration for the mobile Unity city game Weedtopia. Brand palette deep petrol teal, jade green, warm cream and sunlight gold.
Create an inviting, beautifully finished, hand-painted 2.5D game key art scene: a flourishing small riverside town with cozy restored timber-and-brick coffeeshops, lush cannabis-growing greenhouses, neat planters, a warm town square, soft green hills and a turquoise river, golden morning sunlight. This must feel like a complete game title illustration, with rich depth and an authored scenic composition rather than an in-game empty level screenshot.
Wide landscape composition, roughly 16:9. Keep the important town silhouette centered. Frame the foreground with tasteful lush botanical foliage at both sides. Keep the upper middle area calm sky and misty hills for the Weedtopia logo to be overlaid by Unity, and keep the lower middle pathway/square relatively quiet for one Play button. The town and warm light give a welcoming sense of community and rebuilding. Elegant painterly forms with clear silhouettes and polished mobile-game quality, coherent scale, no visual clutter.
Text: no text, no words or signs with lettering, no logo, no UI, no button, no frame, no watermark. Unity will render the exact word Weedtopia and a single functional Play button separately so they remain sharp and readable at all aspect ratios. No ruined buildings, no railroad grid, no characters, no fantasy castle, no jelly candy, no smoking imagery. Opaque full-bleed image.

## Wordmark prompt

Use case: logo-brand. Asset type: transparent production title logo for the mobile city-building game Weedtopia, placed on a sunny painterly riverside town background.
Create a polished, highly readable game title wordmark with exactly the text "Weedtopia" (W e e d t o p i a). One line, pleasantly chunky rounded hand-lettered letters, gentle arch, warm ivory faces with a very subtle sunlight gold bevel, rich dark petrol/teal outline and soft dark green drop shadow. A modest jade cannabis leaf botanical flourish behind the center/top gives the town's theme without obscuring any letters. Sophisticated cozy city-game quality. Wide horizontal logo, all letters equally legible, no subtitle, no other words, no border box, no background, no UI, no watermark. Real transparent background, tightly composed with a little transparent margin. Do not include any button or scenery.

## Deterministic UI surfaces

The five new rounded UI surfaces are original drawing primitives produced by Tools/Implementation/draw_ui_polish_skin.py. No existing raster art was modified. Panel/field borders are sliced at 24 pixels, button borders at 36 pixels; circular shortcuts use the existing RoundNavigationGraphic vector component.
