import typescript from '@rollup/plugin-typescript';
import terser from '@rollup/plugin-terser';

export default {
  input: 'lib/simple-webauthn.ts',
  output: [
    {
      file: 'dist/simple-webauthn.js',
      format: 'iife',
      name: 'SimpleWebAuthn',
      sourcemap: true
    },
    {
      file: 'dist/simple-webauthn.min.js',
      format: 'iife',
      name: 'SimpleWebAuthn',
      plugins: [terser()],
      sourcemap: true
    }
  ],
  plugins: [
    typescript({
      tsconfig: './tsconfig.json'
    })
  ]
};