// WebGL port: Application.persistentDataPath lives in an in-memory Emscripten filesystem backed by
// IndexedDB (IDBFS). Writes only become durable once FS.syncfs(false) copies them to IndexedDB, so
// the game calls WebGLFileSync_Flush() after every save/delete. Calls are debounced so that files
// written inside C# "using" blocks are closed before the sync runs.
mergeInto(LibraryManager.library, {
  WebGLFileSync_Flush: function () {
    if (typeof FS === 'undefined' || !FS.syncfs) return;
    if (Module.__aviSyncTimer) clearTimeout(Module.__aviSyncTimer);
    Module.__aviSyncTimer = setTimeout(function () {
      Module.__aviSyncTimer = null;
      FS.syncfs(false, function (err) {
        if (err) console.warn('[Aviassembly] Failed to persist save data to IndexedDB:', err);
      });
    }, 200);
  },

  WebGLFileSync_InstallUnloadHandlers: function () {
    if (Module.__aviUnloadInstalled || typeof window === 'undefined') return;
    Module.__aviUnloadInstalled = true;
    var flushNow = function () {
      if (typeof FS !== 'undefined' && FS.syncfs) FS.syncfs(false, function () {});
    };
    window.addEventListener('pagehide', flushNow);
    document.addEventListener('visibilitychange', function () {
      if (document.visibilityState === 'hidden') flushNow();
    });
  }
});
