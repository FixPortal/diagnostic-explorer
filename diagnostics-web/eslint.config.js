import tseslint from 'typescript-eslint'
import sonarjs from 'eslint-plugin-sonarjs'

// Minimal flat config: TypeScript parser + the SonarJS recommended set only.
// We deliberately do NOT pull js/tseslint "recommended" rules here — this repo has
// never been linted, and the goal is the advisory cognitive-complexity gate, not a
// new baseline of errors. (Angular template linting via angular-eslint is a separate
// follow-up.) Every Sonar rule runs as a warning; `eslint .` stays exit 0.
export default [
  { ignores: ['dist', 'coverage', '.angular', 'node_modules', '**/*.spec.ts'] },
  {
    files: ['src/**/*.ts'],
    languageOptions: { parser: tseslint.parser },
    plugins: { sonarjs },
    rules: {
      ...Object.fromEntries(
        Object.keys(sonarjs.configs.recommended.rules ?? {}).map(name => [name, 'warn']),
      ),
      // Stylistic policy / false-positive noise, not quality signals.
      'sonarjs/file-header': 'off',
      'sonarjs/arrow-function-convention': 'off',
      'sonarjs/declarations-in-global-scope': 'off',
      'sonarjs/cyclomatic-complexity': 'off',
    },
  },
]
