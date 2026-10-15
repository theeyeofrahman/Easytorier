// @ts-check
import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';

export default defineConfig({
  site: 'https://theeyeofrahman.github.io',
  base: '/Easytorier',
  integrations: [
    starlight({
      title: 'Easytorier Docs',
      description: 'Guides for building, animating, and exporting levels with Easytorier.',
      sidebar: [
        {
          label: 'Start here',
          items: [
            { label: 'Get started', slug: 'getting-started' },
            { label: 'Build a level', slug: 'building-levels' },
            { label: 'Projects', slug: 'projects' },
          ],
        },
        {
          label: 'Create',
          items: [
            { label: 'Animation', slug: 'animation' },
            { label: 'AI builder', slug: 'guides/ai-builder' },
            { label: 'Music and backgrounds', slug: 'guides/level-media' },
            { label: 'Areas and triggers', slug: 'guides/areas-and-triggers' },
            { label: 'Move Visualizer', slug: 'guides/move-visualizer' },
            { label: 'Cosmetics and achievements', slug: 'guides/extras' },
            { label: 'Themes and credits', slug: 'themes-and-credits' },
          ],
        },
        {
          label: 'Files and support',
          items: [
            { label: 'Import and export', slug: 'import-export' },
            { label: 'Texture library', slug: 'guides/texture-library' },
            { label: 'Keyboard shortcuts', slug: 'reference/keyboard-shortcuts' },
            { label: 'Troubleshooting', slug: 'troubleshooting' },
          ],
        },
        {
          label: 'For contributors',
          items: [{ label: 'Development', slug: 'development' }],
        },
      ],
    }),
  ],
});
