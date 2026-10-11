# Ассеты Luma

Логотип: `Assets/luma-logo.png`, копия для Android-ресурсов:
`Resources/drawable/luma_logo.png`. Создан встроенным инструментом image_gen,
с настоящим прозрачным фоном. В приложение включён готовый PNG.

Промпт:

> Use case: logo-brand. Create a premium mobile match-3 puzzle game app logo symbol for a new game called Luma. A single bold polished faceted crystal in a distinctive simple four-point luminous star / diamond silhouette, amethyst lavender upper facets blending into deep violet lower facets with a small icy cyan glint along one edge. Elegant jewel with crisp geometric facets, soft restrained inner luminescence, glossy highlights, a strong clean readable silhouette that works at 48 pixels. Sophisticated playful art direction, slightly dimensional, facing straight on, symmetrical, generous thick proportions. Symbol fills 85% of the square canvas, centered. Transparent background with real alpha. NO lettering, NO words, NO extra separate gems, NO framing tile, NO circular backdrop, NO decorative particles, NO watermark. Final production app icon emblem; the surrounding background must be entirely transparent.

Кристаллы на поле рисуются кодом через Android Canvas: шесть различных форм,
градиенты граней и обозначения спецфишек. Сторонние спрайты не используются.

Семь звуковых эффектов синтезированы для проекта из синусоидальных тонов.
Записанных чужих музыкальных фрагментов нет.

Шрифт Montserrat: SIL Open Font License 1.1, полный текст в `Assets/OFL.txt`.
