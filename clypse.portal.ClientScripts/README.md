# Clypse Portal Client Scripts

This project contains the client-side JavaScript for the Clypse Portal Blazor application.

## Structure

- `src/` - Source JavaScript files
- `tests/` - Unit tests for JavaScript modules
- `dist/` - Build output (generated, not committed to git)

## Building

```bash
npm install
npm run build
```

The build process copies files from `src/` to `dist/`, which are then copied to the portal's `wwwroot/js/` folder during the Blazor build process.

## Testing

```bash
npm test
```

## Development

When adding new JavaScript files:
1. Add the file to `src/`
2. Add corresponding tests to `tests/`
3. The file will be automatically copied during build

## Future Enhancements

- Bundling with Webpack/Rollup/esbuild
- Minification for production
- TypeScript compilation
- Module splitting
- Source maps
