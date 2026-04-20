# Changelog

## [1.0.2] - 2026-04-20

### Fixes

- **X11:** clamp capture rect to physical framebuffer dimensions before calling `XGetImage`. Xrandr virtual resolution can exceed the actual framebuffer size, causing `XGetImage` to return null and the capture to silently fail.

## [1.0.1] - 2026-03-30

### Fixes

- Support .NET 10 target frameworks (`net10.0`, `net10.0-windows`)

## [1.0.0] - 2026-03-30

- Initial release
