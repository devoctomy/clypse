// Helpers for the text editor of a vault's files, called from TextEditorInteropService and the editor panel through
// JS interop: the formatting buttons need the text area's selection, and the shortcuts need a key handler.
window.clypseEditor = (function () {
    const shortcutHandlers = new Map();

    return {
        getSelection: function (id) {
            const element = document.getElementById(id);
            return element ? { text: element.value, start: element.selectionStart, end: element.selectionEnd } : null;
        },

        // Replaces a range of text. execCommand keeps the browser's undo history (Ctrl+Z works) and raises the input
        // event Blazor's binding listens for; setRangeText is the fallback where execCommand is unavailable.
        applyEdit: function (id, edit) {
            const element = document.getElementById(id);
            if (!element) {
                return;
            }

            element.focus();
            element.setSelectionRange(edit.start, edit.end);
            if (edit.start !== edit.end || edit.replacement.length > 0) {
                let applied = false;
                try {
                    applied = edit.replacement.length > 0
                        ? document.execCommand('insertText', false, edit.replacement)
                        : document.execCommand('delete', false);
                } catch (e) {
                    applied = false;
                }

                if (!applied || element.value.substring(edit.start, edit.start + edit.replacement.length) !== edit.replacement) {
                    element.setRangeText(edit.replacement, edit.start, edit.end, 'end');
                    element.dispatchEvent(new Event('input', { bubbles: true }));
                }
            }

            element.setSelectionRange(edit.selectionStart, edit.selectionEnd);
        },

        // Ctrl/Cmd + S, B and I call the panel's OnShortcut method. Returns whether the shortcuts are attached: false
        // while the text area is not on the page yet, so the caller tries again once it is.
        attachShortcuts: function (id, dotNetRef) {
            const element = document.getElementById(id);
            if (!element) {
                return false;
            }

            if (shortcutHandlers.has(id)) {
                return true;
            }

            // Save works anywhere on the page, formatting only in the editor.
            const handler = function (e) {
                if (!(e.ctrlKey || e.metaKey) || e.altKey || e.shiftKey) {
                    return;
                }

                const key = e.key.toLowerCase();
                if (key === 's' || (e.target === element && (key === 'b' || key === 'i'))) {
                    e.preventDefault();
                    dotNetRef.invokeMethodAsync('OnShortcut', key);
                }
            };

            document.addEventListener('keydown', handler);
            shortcutHandlers.set(id, handler);
            return true;
        },

        detachShortcuts: function (id) {
            const handler = shortcutHandlers.get(id);
            if (handler) {
                document.removeEventListener('keydown', handler);
                shortcutHandlers.delete(id);
            }
        }
    };
})();
