import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import { defineConfig, globalIgnores } from 'eslint/config'

export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{js,jsx}'],
    extends: [
      js.configs.recommended,
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      globals: globals.browser,
      parserOptions: { ecmaFeatures: { jsx: true } },
    },
    rules: {
      // Bu kural, effect içinden veri çekmeyi (fetch -> setState) baştan yasaklıyor ve
      // çözüm olarak bir data-fetching katmanı (React Query, Next.js) öneriyor.
      // DataScrub'da bileşen başına tek bir REST çağrısı var; bunun için ekstra bir
      // bağımlılık eklemek fazla. setState'ler zaten await sonrasında çalışıyor,
      // yani kuralın uyardığı senkron cascading render durumu oluşmuyor.
      // Not: bu kural satır içi eslint-disable ile susturulamıyor, o yüzden burada.
      'react-hooks/set-state-in-effect': 'off',
    },
  },
])
