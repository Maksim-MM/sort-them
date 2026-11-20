mergeInto(LibraryManager.library, {
    SortThemRestoreFocus: function () {
        setTimeout(function () {
            try { window.focus(); } catch (e) {}
            try { if (Module && Module.canvas) Module.canvas.focus(); } catch (e) {}
        }, 0);
    }
});
